#!/usr/bin/env bash
# Copy images + evidence containers from legacy storage to green storage.
# Requires azcopy v10+ and SAS or Azure AD auth configured.
set -euo pipefail

LEGACY_STORAGE="${LEGACY_STORAGE:?Set LEGACY_STORAGE (account name, no .blob...)}"
GREEN_STORAGE="${GREEN_STORAGE:?Set GREEN_STORAGE}"

CONTAINERS=(images evidence)

for c in "${CONTAINERS[@]}"; do
	echo "Copying container: ${c}"
	azcopy copy \
		"https://${LEGACY_STORAGE}.blob.core.windows.net/${c}" \
		"https://${GREEN_STORAGE}.blob.core.windows.net/${c}" \
		--recursive
done

echo "AzCopy complete. Verify blob counts in Portal before cutover."
