# Operations - API Integration Quick Reference

## ✅ Status: All Connected to Real API

### Pages Under Operations Section

| Page | Path | API Status | Endpoint Used |
|------|------|------------|---------------|
| **Incident Graph** | `/operations/incident-graph` | ✅ Connected | `GET /incidents` |
| **Crime Intelligence** | `/operations/crime-intelligence` | ✅ Connected | `GET /incidents` |

---

## API Endpoints

### GET /incidents

**Used by:** All operations pages

**Query Parameters:**
```typescript
{
  page?: number           // Pagination
  pageSize?: number       // Items per page
  customerId?: string     // Filter by customer
  fromDate?: string       // Start date (YYYY-MM-DD)
  toDate?: string         // End date (YYYY-MM-DD)
  siteId?: string         // Filter by site
  regionId?: string       // Filter by region
  incidentType?: string   // Filter by type
  status?: string         // Filter by status
  search?: string         // Search query
}
```

**Response Format:**
```typescript
{
  success: boolean
  data: Incident[]
  pagination?: {
    currentPage: number
    totalPages: number
    pageSize: number
    totalCount: number
  }
}
```

---

## Implementation Details

### Crime Intelligence (`CustomerCrimeIntelligence.tsx`)

**How it works:**
1. Fetches incidents using `incidentsApi.getIncidents()`
2. Processes incidents client-side with `processIncidentsToInsights()`
3. Generates crime intelligence metrics:
   - Hero metrics (total incidents, value impact, etc.)
   - Top incident types (pie chart)
   - Hot stores (bar chart with pagination)
   - Regional exposure (bar chart with pagination)
   - Time-of-day activity (bar chart)
   - Most stolen products (table)
   - Hot products (featured card)
   - Analyst notes (auto-generated insights)

**Fallback:**
- If API fails, falls back to `MOCK_INCIDENTS`
- Shows toast: "Using offline data"
- Enables offline development

**Code Location:**
```typescript
// Line ~483 in CustomerCrimeIntelligence.tsx
const fetchInsights = useCallback(async () => {
  const response = await incidentsApi.getIncidents({
    page: 1,
    pageSize: 1000,
    customerId: customerId.toString(),
    fromDate: startDate ? format(startDate, 'yyyy-MM-dd') : undefined,
    toDate: endDate ? format(endDate, 'yyyy-MM-dd') : undefined,
    siteId: selectedSiteId !== 'all' ? selectedSiteId : undefined,
    regionId: selectedRegionId !== 'all' ? selectedRegionId : undefined
  })
  // ... process insights
}, [/* dependencies */])
```

---

### Incident Graph (`IncidentGraph.tsx`)

**How it works:**
1. Uses `incidentGraphService.fetchGraphData()` 
2. Service calls `GET /incidents` with filters
3. Aggregates data client-side by location
4. Displays bar charts with pagination

**Features:**
- Graph types: Value, Quantity, Count
- Officer type filtering: All, Uniform, Detective
- Region filtering
- Date range filtering
- Pagination (20 stores per page)
- Incident types pie chart

---

## Testing with Real API

### When Backend is Running:

**Expected Behavior:**
- Pages load incidents from real API
- Filters work correctly
- Charts and tables populate with real data
- Pagination works correctly

**Test Commands:**
```bash
# Check if backend is running
curl http://localhost:5000/api/incidents?page=1&pageSize=10

# Check with filters
curl "http://localhost:5000/api/incidents?customerId=1&fromDate=2024-01-01&toDate=2024-12-31"
```

---

### When Backend is NOT Running:

**Expected Behavior (Crime Intelligence only):**
- Shows toast: "Using offline data"
- Falls back to `MOCK_INCIDENTS`
- All features work with mock data
- No errors or crashes

---

## Developer Notes

### To Remove Fallback (Production):

If you want to remove the mock data fallback:

```typescript
// In CustomerCrimeIntelligence.tsx, remove the try-catch fallback:

} catch (error) {
  console.error('❌ [CrimeIntelligence] Failed to fetch insights:', error)
  const message = error instanceof Error ? error.message : 'Unable to load insights'
  setPageError(message)
  
  // Remove this entire fallback block:
  // console.warn('⚠️ [CrimeIntelligence] Falling back to mock data')
  // try { ... } catch { ... }
  
  // Just show error:
  toast({
    variant: 'destructive',
    title: 'Unable to load data',
    description: message
  })
}
```

---

### To Add Caching:

For better performance, consider adding caching:

```typescript
import { useQuery } from '@tanstack/react-query'

const { data, isLoading, error } = useQuery({
  queryKey: ['incidents', customerId, startDate, endDate, siteId, regionId],
  queryFn: () => incidentsApi.getIncidents({ /* params */ }),
  staleTime: 5 * 60 * 1000, // 5 minutes
  cacheTime: 10 * 60 * 1000, // 10 minutes
})
```

---

## Common Issues & Solutions

### Issue: No data showing
**Check:**
1. Is backend running? `curl http://localhost:5000/api/incidents`
2. Is authentication working? Check browser console for 401 errors
3. Is customerId correct? Check the API request in Network tab

### Issue: Filters not working
**Check:**
1. Are parameters being sent to API? Check Network tab
2. Does backend support the filter? Check backend logs
3. Is data format correct? Date should be YYYY-MM-DD

### Issue: Charts not rendering
**Check:**
1. Is data in correct format? Check console logs
2. Are there any incidents returned? Empty data = empty charts
3. Check for JavaScript errors in console

---

## Contact

For issues or questions, check:
- Console logs (comprehensive logging added)
- Network tab (see actual API requests/responses)
- `API_CONNECTION_SUMMARY.md` (detailed changes)

**Last Updated:** December 3, 2025
