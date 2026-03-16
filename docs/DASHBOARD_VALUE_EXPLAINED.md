# Dashboard Value Metrics Explained

This document clarifies how the two value-related metrics on the dashboard are derived and why they can differ.

## The Two Values

| Location | Label | Example | Source |
|----------|-------|---------|--------|
| **Top card (orange)** | Value Recovered | £758 | Frontend: sum of `totalValueRecovered` from loaded incidents |
| **AI Risk Indicators** | Total value impact (under "Value at Risk") | £718 | Backend: sum from Analytics API |

Both represent the same concept: **total value recovered / estimated loss** from incidents.  
They are derived as: `TotalValueRecovered` (or sum of `StolenItems.TotalAmount` when not set) per incident.

---

## Why They Differ

The values come from **different APIs with different filters**:

### Value Recovered (top card – £758)

- **API**: `GET /api/Incident` (incidents list)
- **Params**: `page=1, pageSize=500` – **no date range** sent
- **Scope**: First 500 incidents (by date descending), scoped by the current user’s customer/sites
- **Frontend filter**: Selected region + optional date range (if "Apply Dates" is used)
- **Effect**: If no date filter is applied, this is the sum over the **first 500 incidents** in the selected region (effectively all time or recent, depending on data)

### Total value impact (AI Risk Indicators – £718)

- **API**: `GET /api/Analytics/summary` (analytics)
- **Params**: **No filters passed** from the dashboard – backend uses defaults
- **Default range**: **Last 90 days**
- **Scope**: Same user context (customer/sites) as incidents
- **Effect**: Sum over **all incidents** in the last 90 days (no 500 limit)

---

## Typical Causes of Difference

1. **Different date ranges**  
   Top card may include older incidents (all time / first 500). AI indicator is last 90 days only.

2. **Pagination cap**  
   Top card sums up to 500 incidents. Analytics sums all matching incidents, which can be more or fewer.

3. **Region / filter alignment**  
   The analytics API is not given the dashboard’s selected region or date range. It uses its own defaults.

---

## Which Is “Correct”?

- Both use the same **value logic** (TotalValueRecovered or StolenItems total).
- Neither is wrong – they answer slightly different questions:
  - **Value Recovered (top card)** – “What’s the total value from the incidents I’m currently viewing?” (region + optional date)
  - **Total value impact** – “What’s the total value in the last 90 days for my scope?” (used for risk scoring)

### Fix applied (March 2025)

The dashboard now passes the same filters to the analytics API:
- **Date range**: When a date range is applied, both use it. When not applied, analytics uses the last 90 days.
- **Region**: The selected region is passed to analytics; when "All Regions" is selected, no region filter is applied.
- **Customer**: The effective customer ID (for managers) is passed.

This aligns the two values so they represent the same dataset.

---

## Currency

Both should display in **Pounds Sterling (£)**. The previous `$` on AI Risk Indicators was due to backend culture formatting; that is corrected to `en-GB` (£).
