#!/usr/bin/env bash
# Validates production tfvars guardrails before Terraform apply.
# When crimeportal-rg already exists, unified Front Door + www custom domain must stay enabled.
set -euo pipefail

TFVARS_PATH="${1:-Infrastructure/terraform.prod.tfvars}"
RESOURCE_GROUP="${2:-crimeportal-rg}"

if [ ! -f "${TFVARS_PATH}" ]; then
	echo "::error::Missing tfvars file: ${TFVARS_PATH}"
	exit 1
fi

hcl_bool() {
	local key="$1"
	local line
	line="$(grep -E "^[[:space:]]*${key}[[:space:]]*=" "${TFVARS_PATH}" | tail -n 1 || true)"
	if [ -z "${line}" ]; then
		echo ""
		return
	fi
	case "${line}" in
		*'= true' | *'=true' | *'= "true"' | *"= 'true'") echo "true" ;;
		*'= false' | *'=false' | *'= "false"' | *"= 'false'") echo "false" ;;
		*) echo "" ;;
	esac
}

hcl_string() {
	local key="$1"
	local line
	line="$(grep -E "^[[:space:]]*${key}[[:space:]]*=" "${TFVARS_PATH}" | tail -n 1 || true)"
	if [ -z "${line}" ]; then
		echo ""
		return
	fi
	echo "${line}" | sed -E 's/^[^=]*=[[:space:]]*"?([^"#]+)"?.*/\1/' | tr -d " \t\r" | sed "s/^'//;s/'$//"
}

# Reads the default value of an HCL string variable from a variables file.
# Tolerates both single-line `default = "..."` and heredoc-style descriptions
# before the default by walking the variable block until the `default = "..."` line.
hcl_string_default() {
	local key="$1"
	local var_file="${2:-Infrastructure/variables.tf}"
	if [ ! -f "${var_file}" ]; then
		echo ""
		return
	fi
	awk -v k="${key}" '
		$0 ~ "^variable[[:space:]]+\""k"\"[[:space:]]*\\{" { in_block=1; next }
		in_block && /^[[:space:]]*default[[:space:]]*=/ {
			if (match($0, /"[^"]*"/)) {
				print substr($0, RSTART+1, RLENGTH-2)
			}
			exit
		}
		in_block && /^\}/ { in_block=0 }
	' "${var_file}"
}

# Resolves an HCL string variable by checking the tfvars file first then falling
# back to its variables.tf default. Empty result means the value is unset and has
# no default.
hcl_string_resolved() {
	local key="$1"
	local val
	val="$(hcl_string "${key}")"
	if [ -n "${val}" ]; then
		echo "${val}"
		return
	fi
	hcl_string_default "${key}"
}

# Converts a dotted-quad IPv4 to its 32-bit integer form. Echos the integer or
# empty when the input is malformed.
ipv4_to_int() {
	local ip="$1"
	local a b c d
	IFS='.' read -r a b c d <<<"${ip}"
	case "${a}${b}${c}${d}" in
		'' | *[!0-9]*)
			echo ""
			return
			;;
	esac
	if [ "${a}" -gt 255 ] || [ "${b}" -gt 255 ] || [ "${c}" -gt 255 ] || [ "${d}" -gt 255 ]; then
		echo ""
		return
	fi
	echo $(((a << 24) | (b << 16) | (c << 8) | d))
}

# Echos "<start> <end>" as 32-bit integers for the given CIDR (e.g. 10.40.0.0/23).
cidr_to_range() {
	local cidr="$1"
	local ip prefix ip_int size
	ip="${cidr%/*}"
	prefix="${cidr#*/}"
	if [ -z "${ip}" ] || [ -z "${prefix}" ] || [ "${prefix}" = "${cidr}" ]; then
		echo ""
		return
	fi
	if [ "${prefix}" -lt 0 ] 2>/dev/null || [ "${prefix}" -gt 32 ] 2>/dev/null; then
		echo ""
		return
	fi
	ip_int="$(ipv4_to_int "${ip}")"
	if [ -z "${ip_int}" ]; then
		echo ""
		return
	fi
	size=$((1 << (32 - prefix)))
	echo "${ip_int} $((ip_int + size - 1))"
}

RG_EXISTS="false"
if command -v az >/dev/null 2>&1; then
	if [ "$(az group exists --name "${RESOURCE_GROUP}" -o tsv 2>/dev/null || echo false)" = "true" ]; then
		RG_EXISTS="true"
	fi
fi

UNIFIED="$(hcl_bool enable_unified_front_door)"
CUSTOM_DOMAIN="$(hcl_bool enable_unified_front_door_custom_domain)"
HOSTNAME="$(hcl_string unified_front_door_hostname)"
WWW_HOST="$(hcl_string frontend_www_custom_domain)"

echo "validate-terraform-prod-tfvars: resource_group_exists=${RG_EXISTS} unified_fd=${UNIFIED:-<unset>} custom_domain=${CUSTOM_DOMAIN:-<unset>}"

if [ "${RG_EXISTS}" != "true" ]; then
	echo "Bootstrap mode: skipping strict production edge checks (resource group not found)."
	exit 0
fi

fail() {
	echo "::error::$1"
	exit 1
}

if [ "${UNIFIED}" != "true" ]; then
	fail "Production resource group exists but enable_unified_front_door is not true. Disabling unified Front Door breaks www TLS and same-origin auth. See docs/production-edge-runbook.md"
fi

if [ "${CUSTOM_DOMAIN}" != "true" ]; then
	fail "Production resource group exists but enable_unified_front_door_custom_domain is not true. This causes NET::ERR_CERT_COMMON_NAME_INVALID on www after DNS cutover. See docs/production-edge-runbook.md"
fi

if [ -z "${HOSTNAME}" ] && [ -z "${WWW_HOST}" ]; then
	fail "Set unified_front_door_hostname or frontend_www_custom_domain in terraform.prod.tfvars (e.g. www.dibangops.com)."
fi

SQL_PE="$(hcl_bool enable_sql_private_endpoint)"
SQL_PUBLIC="$(hcl_bool sql_public_network_access_enabled)"
INGRESS_RESTRICT="$(hcl_bool backend_ingress_ip_restrictions_enabled)"

if [ "${SQL_PE}" = "true" ] && [ "${SQL_PUBLIC}" = "true" ]; then
	fail "enable_sql_private_endpoint = true requires sql_public_network_access_enabled = false."
fi

if [ "${INGRESS_RESTRICT}" = "true" ]; then
	if ! grep -qE '^[[:space:]]*backend_ingress_allowed_cidrs[[:space:]]*=[[:space:]]*\[' "${TFVARS_PATH}"; then
		fail "backend_ingress_ip_restrictions_enabled = true requires backend_ingress_allowed_cidrs with at least one CIDR. Run scripts/extract-front-door-backend-cidrs.ps1."
	fi
fi

if [ "${SQL_PE}" != "true" ]; then
	fail "enable_sql_private_endpoint must be true for production (SQL private endpoint required). See docs/phase1-perimeter-deploy-checklist.md"
fi

if [ "${INGRESS_RESTRICT}" != "true" ]; then
	echo "::warning::backend_ingress_ip_restrictions_enabled is not true — direct Container App FQDN bypass remains open."
fi

# Phase 1 only creates these subnets when enable_sql_private_endpoint = true,
# but the defaults are part of the module either way, so we always validate.
PE_SUBNET="$(hcl_string_resolved private_endpoint_subnet_prefix)"
CA_SUBNET="$(hcl_string_resolved container_apps_subnet_prefix)"

if [ -z "${PE_SUBNET}" ] || [ -z "${CA_SUBNET}" ]; then
	fail "Could not resolve private_endpoint_subnet_prefix or container_apps_subnet_prefix from tfvars or variables.tf."
fi

PE_RANGE="$(cidr_to_range "${PE_SUBNET}")"
CA_RANGE="$(cidr_to_range "${CA_SUBNET}")"

if [ -z "${PE_RANGE}" ]; then
	fail "private_endpoint_subnet_prefix is not a valid IPv4 CIDR: ${PE_SUBNET}"
fi
if [ -z "${CA_RANGE}" ]; then
	fail "container_apps_subnet_prefix is not a valid IPv4 CIDR: ${CA_SUBNET}"
fi

PE_START="${PE_RANGE% *}"
PE_END="${PE_RANGE#* }"
CA_START="${CA_RANGE% *}"
CA_END="${CA_RANGE#* }"

if [ "${PE_START}" -le "${CA_END}" ] && [ "${CA_START}" -le "${PE_END}" ]; then
	fail "private_endpoint_subnet_prefix (${PE_SUBNET}) overlaps with container_apps_subnet_prefix (${CA_SUBNET}). Azure Container Apps needs at least a /23, so the private endpoint subnet must sit outside that range (e.g. 10.40.2.0/24 when Container Apps consumes 10.40.0.0/23)."
fi

# Container Apps requires at least /23.
CA_PREFIX="${CA_SUBNET#*/}"
if [ "${CA_PREFIX}" -gt 23 ] 2>/dev/null; then
	fail "container_apps_subnet_prefix (${CA_SUBNET}) is smaller than /23. Azure Container Apps requires a /23 or larger delegated subnet."
fi

echo "Production edge tfvars validation passed."
