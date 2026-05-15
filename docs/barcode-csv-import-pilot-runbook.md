# Barcode CSV import — pilot rollout

This checklist supports the pilot called for in the barcode admin import plan. Complete it in a staging or production-like environment before broad rollout.

## Preconditions

- Backend is deployed with `POST /api/ProductImport/barcode-csv` available and `[Authorize(Roles = "administrator")]` enforced.
- Default page access has been initialized (or synced) so `barcode-catalog-import` and `product-catalog` pages exist and **administrator** has access.
- Large catalogs: split files to **≤10,000 data rows** and **≤5 MB** per upload.

## Pilot file

1. Prepare a small CSV (for example 5–20 rows) with exact headers: `barcode`, `Department`, `VMECode`, `ProductName`, `RetailPrice` (case-insensitive).
2. Confirm prices are stored when `RetailPrice` cells have values; empty `RetailPrice`, `Department`, or `VMECode` cells on **update** preserve existing values.
3. `CostPrice` is ignored if present.
4. Include one intentionally invalid row (for example empty `barcode`) and confirm the UI preview and API summary report it without blocking valid rows.
5. Duplicate barcodes in one file: **last row wins** (not an error).

## Execution

1. Sign in as an **administrator**.
2. Open **Administration → Barcode catalog import**.
3. Upload the pilot CSV, review preview counts (valid, invalid, merged duplicates), then run **Run import**.
4. Confirm the outcome banner:
   - **Full success:** no invalid rows; created + updated = valid unique barcodes.
   - **Partial success:** some row issues; valid rows still saved.
5. Download row errors CSV if any issues are listed.

## Verification

1. Open **Administration → Product catalog**, search sample barcodes, confirm name, VME/description, and retail price.
2. Use product lookup / scan (incident report EAN flow) for a few imported barcodes.
3. Re-import the same file and confirm idempotent updates (no duplicate key failures).
4. Update a single price in the catalog UI and confirm it persists after refresh.
5. Review application logs for the structured import line (actor, file name, counts).

## Sign-off

- Record pilot owner, date, environment, file name, row counts, and any row-level issues from the API `rowErrors` payload (or downloaded CSV).
- After sign-off, enable wider operational use and monitor first large imports for duration and error rates.
