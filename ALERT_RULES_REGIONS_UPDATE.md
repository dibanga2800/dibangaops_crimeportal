# Alert Rules - Regions Update

## ✅ Changes Made

Updated the **LPM Alert Rule Form** to dynamically load regions from the API for **Central England COOP** (customerId = 1).

---

## 📋 What Changed

### File Updated
- `AIP_UI/src/components/operations/LPMAlertRuleForm.tsx`

### Changes

#### 1. **Added Dynamic Region Loading**

**Before:**
```typescript
// Hardcoded regions
const lpmRegions = [
  'North',
  'South',
  'East',
  'West',
  'Central',
  'Scotland',
  'Wales',
]
```

**After:**
```typescript
// Fetch regions from API
interface Region {
  regionId: number
  regionName: string
  customerId: number
}

const [regions, setRegions] = useState<Region[]>([])
const [loadingRegions, setLoadingRegions] = useState(true)

useEffect(() => {
  const fetchRegions = async () => {
    const response = await fetch(`${BASE_API_URL}/regions`)
    const data = await response.json()
    // Filter for Central England COOP (customerId = 1)
    const coopRegions = data.filter((region: Region) => region.customerId === 1)
    setRegions(coopRegions)
  }
  fetchRegions()
}, [])
```

#### 2. **Updated Region Dropdown**

**Features Added:**
- ✅ Fetches all regions for Central England COOP on form load
- ✅ Shows loading spinner while fetching
- ✅ Displays region names from database
- ✅ Disables dropdown during loading
- ✅ Shows message if no regions available
- ✅ Updates helper text to show "Central England COOP"

**UI Changes:**
```tsx
<Select value={lpmRegion} onValueChange={setLpmRegion} disabled={loadingRegions}>
  <SelectTrigger>
    {loadingRegions ? (
      <div className="flex items-center gap-2">
        <Loader2 className="h-4 w-4 animate-spin" />
        <span>Loading regions...</span>
      </div>
    ) : (
      <SelectValue placeholder="Select region" />
    )}
  </SelectTrigger>
  <SelectContent>
    {regions.map(region => (
      <SelectItem key={region.regionId} value={region.regionName}>
        {region.regionName}
      </SelectItem>
    ))}
  </SelectContent>
</Select>
<p className="text-sm text-muted-foreground">
  Target region for LPM alerts (Central England COOP)
</p>
```

---

## 🔧 How It Works

### API Call Flow

```
1. Form Component Mounts
   ↓
2. useEffect Triggers
   ↓
3. Fetch GET /api/regions
   ↓
4. Filter for customerId === 1
   ↓
5. Populate Dropdown with Central England COOP regions
```

### Example API Response

```json
[
  {
    "regionId": 1,
    "regionName": "East Midlands",
    "customerId": 1
  },
  {
    "regionId": 2,
    "regionName": "West Midlands",
    "customerId": 1
  },
  {
    "regionId": 3,
    "regionName": "Yorkshire",
    "customerId": 1
  }
]
```

### Filtered Result (customerId = 1 only)

The dropdown will show only regions belonging to **Central England COOP**:
- East Midlands
- West Midlands  
- Yorkshire
- ... (all other Central England COOP regions)

---

## 📱 User Experience

### Loading State
```
┌────────────────────────────────────┐
│ LPM Region *                       │
│ ┌────────────────────────────────┐ │
│ │ ⏳ Loading regions...          │ │
│ └────────────────────────────────┘ │
│ Target region for LPM alerts       │
│ (Central England COOP)             │
└────────────────────────────────────┘
```

### Loaded State
```
┌────────────────────────────────────┐
│ LPM Region *                       │
│ ┌────────────────────────────────┐ │
│ │ Select region              ▼   │ │
│ └────────────────────────────────┘ │
│ Target region for LPM alerts       │
│ (Central England COOP)             │
└────────────────────────────────────┘

When clicked:
├── East Midlands
├── West Midlands
├── Yorkshire
└── ... (all Central England COOP regions)
```

---

## ✅ Benefits

1. **Dynamic Data** - Always up-to-date with database regions
2. **Filtered Results** - Shows only Central England COOP regions (customerId = 1)
3. **Better UX** - Loading indicator during API call
4. **Consistent** - Uses same API as other parts of the application
5. **Maintainable** - No hardcoded region names to update

---

## 🧪 Testing Checklist

- [ ] Form loads and fetches regions on mount
- [ ] Loading spinner shows during API call
- [ ] Dropdown populates with Central England COOP regions only
- [ ] Selecting a region works correctly
- [ ] Form submission includes correct region name
- [ ] No console errors during region loading
- [ ] Works with existing alert rules that have regions set

---

**Updated:** December 2, 2024  
**Customer:** Central England COOP (customerId = 1)  
**Regions API Endpoint:** `/api/regions`
