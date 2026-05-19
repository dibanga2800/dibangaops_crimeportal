#!/usr/bin/env bash
# Smoke-test public www edge: TLS SAN, SPA root, API health, optional Azure route binding.
set -euo pipefail

HOST="${1:-www.dibangops.com}"
BASE_URL="https://${HOST}"
MAX_ATTEMPTS="${EDGE_VERIFY_ATTEMPTS:-12}"
SLEEP_SECONDS="${EDGE_VERIFY_SLEEP_SECONDS:-15}"

echo "verify-production-edge: ${BASE_URL} (attempts=${MAX_ATTEMPTS}, sleep=${SLEEP_SECONDS}s)"

verify_tls() {
	echo | openssl s_client -connect "${HOST}:443" -servername "${HOST}" 2>/dev/null \
		| openssl x509 -noout -subject 2>/dev/null
}

verify_http() {
	local path="$1"
	local expected_substr="$2"
	local body
	body="$(curl -fsS --max-time 30 "${BASE_URL}${path}")"
	if [ -n "${expected_substr}" ] && ! grep -q "${expected_substr}" <<<"${body}"; then
		echo "Unexpected body for ${path}: ${body}"
		return 1
	fi
	echo "OK ${path}"
}

attempt=1
while [ "${attempt}" -le "${MAX_ATTEMPTS}" ]; do
	echo "--- attempt ${attempt}/${MAX_ATTEMPTS} ---"
	subject="$(verify_tls || true)"
	if grep -q "CN=${HOST}" <<<"${subject}" 2>/dev/null || grep -q "CN = ${HOST}" <<<"${subject}" 2>/dev/null; then
		echo "TLS subject: ${subject}"
		if verify_http "/" "" && verify_http "/api/health" '"status":"healthy"'; then
			echo "verify-production-edge: all checks passed."
			exit 0
		fi
	else
		echo "TLS not ready (got: ${subject:-<none>}); expected CN=${HOST}"
	fi
	if [ "${attempt}" -lt "${MAX_ATTEMPTS}" ]; then
		sleep "${SLEEP_SECONDS}"
	fi
	attempt=$((attempt + 1))
done

echo "::error::Production edge verification failed for ${HOST} after ${MAX_ATTEMPTS} attempts."
exit 1
