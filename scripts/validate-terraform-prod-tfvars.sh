#!/usr/bin/env bash
# Validates production tfvars guardrails before Terraform apply.
# When the target resource group already exists, unified Front Door +
# www custom domain must stay enabled (production edge invariants).
#
# Phase 1 perimeter internal-consistency checks (SQL PE / public access
# / NAT Gateway / ingress allow-list / subnet CIDR overlap / /23 minimum)
# always run regardless of the target environment so they catch
# misconfiguration on bootstrap, prod, and scratch alike.
#
# Usage:
#   validate-terraform-prod-tfvars.sh [tfvars] [resource_group] [strict_prod_edge]
# Args:
#   tfvars            Path to tfvars file (default Infrastructure/terraform.prod.tfvars)
#   resource_group    Target resource group (default crimeportal-rg)
#   strict_prod_edge  When "false", skip the production edge invariants
#                     (unified Front Door, custom domain, hostname) and the
#                     Phase 1 rollback warnings. Use for non-production
#                     environments (e.g. phase1-scratch). Default "true".
set -euo pipefail

TFVARS_PATH="${1:-Infrastructure/terraform.prod.tfvars}"
RESOURCE_GROUP="${2:-crimeportal-rg}"
STRICT_PROD_EDGE="${3:-true}"

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

fail() {
	echo "::error::$1"
	exit 1
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

SQL_PE="$(hcl_bool enable_sql_private_endpoint)"
SQL_PUBLIC="$(hcl_bool sql_public_network_access_enabled)"
INGRESS_RESTRICT="$(hcl_bool backend_ingress_ip_restrictions_enabled)"
ENABLE_NAT="$(hcl_bool enable_nat_gateway_egress)"
# enable_nat_gateway_egress defaults to true in variables.tf; treat unset as enabled.
if [ -z "${ENABLE_NAT}" ]; then
	ENABLE_NAT="true"
fi

echo "validate-terraform-prod-tfvars: resource_group=${RESOURCE_GROUP} exists=${RG_EXISTS} strict_prod_edge=${STRICT_PROD_EDGE} unified_fd=${UNIFIED:-<unset>} custom_domain=${CUSTOM_DOMAIN:-<unset>} sql_pe=${SQL_PE:-<unset>} nat=${ENABLE_NAT:-<unset>}"

# ---------------------------------------------------------------------------
# Phase 1 perimeter internal-consistency checks.
# These ALWAYS run, regardless of RG_EXISTS or STRICT_PROD_EDGE, because they
# detect tfvars configurations that would either fail apply or leave production
# in an unsafe state (e.g. PE on without public-access off).
# ---------------------------------------------------------------------------

if [ "${SQL_PE}" = "true" ] && [ "${SQL_PUBLIC}" = "true" ]; then
	fail "enable_sql_private_endpoint = true requires sql_public_network_access_enabled = false."
fi

if [ "${SQL_PE}" = "true" ] && [ "${ENABLE_NAT}" = "false" ]; then
	fail "enable_sql_private_endpoint = true requires enable_nat_gateway_egress = true. VNet-integrated Container Apps cannot reliably pull from ACR without a NAT Gateway."
fi

if [ "${INGRESS_RESTRICT}" = "true" ]; then
	if ! grep -qE '^[[:space:]]*backend_ingress_allowed_cidrs[[:space:]]*=[[:space:]]*\[' "${TFVARS_PATH}"; then
		fail "backend_ingress_ip_restrictions_enabled = true requires backend_ingress_allowed_cidrs with at least one CIDR. Run scripts/extract-front-door-backend-cidrs.ps1."
	fi
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

SQL_SKU="$(hcl_string_resolved sql_sku_name)"
SQL_MAX_GB="$(hcl_string "${sql_max_size_gb}")"
if [ -z "${SQL_MAX_GB}" ]; then
	SQL_MAX_GB="$(hcl_string_default sql_max_size_gb)"
fi
if [ "${SQL_SKU}" = "Basic" ] && [ -n "${SQL_MAX_GB}" ] && [ "${SQL_MAX_GB}" -gt 2 ] 2>/dev/null; then
	fail "sql_sku_name = Basic requires sql_max_size_gb <= 2."
fi

# ---------------------------------------------------------------------------
# Production edge invariants.
# Only enforced when the target RG already exists AND we are in strict prod
# edge mode. Skipped for bootstrap (RG not yet created) and for non-prod
# environments (e.g. phase1-scratch) where unified Front Door and the
# dibangops.com custom domain do not apply.
# ---------------------------------------------------------------------------

if [ "${RG_EXISTS}" != "true" ]; then
	echo "Bootstrap mode: skipping production edge invariants (resource group '${RESOURCE_GROUP}' not found). Phase 1 perimeter checks above still ran."
	echo "Tfvars validation passed."
	exit 0
fi

if [ "${STRICT_PROD_EDGE}" != "true" ]; then
	echo "Non-strict mode (strict_prod_edge=${STRICT_PROD_EDGE}): skipping production edge invariants. Phase 1 perimeter checks above still ran."
	echo "Tfvars validation passed."
	exit 0
fi

if [ "${UNIFIED}" != "true" ]; then
	fail "Production resource group exists but enable_unified_front_door is not true. Disabling unified Front Door breaks www TLS and same-origin auth. See docs/production-edge-runbook.md"
fi

if [ "${CUSTOM_DOMAIN}" != "true" ]; then
	fail "Production resource group exists but enable_unified_front_door_custom_domain is not true. This causes NET::ERR_CERT_COMMON_NAME_INVALID on www after DNS cutover. See docs/production-edge-runbook.md"
fi

if [ -z "${HOSTNAME}" ] && [ -z "${WWW_HOST}" ]; then
	fail "Set unified_front_door_hostname or frontend_www_custom_domain in terraform.prod.tfvars (e.g. www.dibangops.com)."
fi

# Phase 1 perimeter is the desired posture in production, but rollback to the
# public-SQL + allow-list configuration is a legitimate operational decision
# (see docs/phase1-perimeter-deploy-checklist.md → Rollback). Emit warnings
# instead of failing so an informed rollback can land. The accompanying SQL
# public-access / Azure-services firewall toggles still enforce internal
# consistency above (Phase 1 perimeter section).
if [ "${SQL_PE}" != "true" ]; then
	echo "::warning::enable_sql_private_endpoint is not true — production SQL private endpoint is disabled. Confirm this is an intentional rollback, not a regression. See docs/phase1-perimeter-deploy-checklist.md"
fi

if [ "${INGRESS_RESTRICT}" != "true" ]; then
	echo "::warning::backend_ingress_ip_restrictions_enabled is not true — direct Container App FQDN bypass remains open."
fi

echo "Production edge tfvars validation passed."
