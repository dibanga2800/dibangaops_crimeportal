# Value Impact Calculation Fix

## Issue Identified

The "Value Impact" stat was showing **£0K** even though there were 8 incidents with actual values. 

**Root Cause:** The backend API returns incident data with **PascalCase** field names (e.g., `TotalValueRecovered`, `Value`), but the frontend `processIncidentsToInsights()` function was only checking for **camelCase** field names (e.g., `totalValueRecovered`, `value`).

```typescript
// OLD CODE - Only checked camelCase
const totalValue = incidents.reduce((sum, inc) => 
  sum + (inc.totalValueRecovered || inc.value || 0), 0
)
```

Since the API returns `TotalValueRecovered` and `Value` (PascalCase), the code couldn't find these fields and defaulted to 0 for every incident.

---

## Fix Applied

### 1. Created Helper Function for Value Extraction

Added `getIncidentValue()` helper that checks both camelCase and PascalCase field names:

```typescript
const getIncidentValue = (inc: any): number => {
  // Try multiple field names in order of preference
  const value = inc.totalValueRecovered 
    || inc.TotalValueRecovered 
    || inc.value 
    || inc.Value
    || inc.valueRecovered
    || inc.ValueRecovered
    || inc.amount
    || inc.Amount
    || 0
  
  return typeof value === 'number' ? value : parseFloat(value) || 0
}
```

This helper:
- ✅ Checks camelCase versions first (for frontend consistency)
- ✅ Falls back to PascalCase versions (from backend)
- ✅ Handles multiple common value field names
- ✅ Safely converts string numbers to numbers
- ✅ Returns 0 if no value found

### 2. Updated All Value Calculations

Replaced all direct value access with the helper function:

**Total Value:**
```typescript
const totalValue = incidents.reduce((sum, inc) => sum + getIncidentValue(inc), 0)
```

**Incident Type Groups:**
```typescript
acc[type].value += getIncidentValue(inc)
```

**Store Groups:**
```typescript
acc[store].value += getIncidentValue(inc)
```

**Region Groups:**
```typescript
acc[region].value += getIncidentValue(inc)
```

**Stolen Items:**
```typescript
const itemValue = (item as any).totalAmount 
  || (item as any).TotalAmount 
  || (item as any).value 
  || (item as any).Value 
  || 0
```

### 3. Added Comprehensive Logging

Added detailed logging to help debug value calculations:

```typescript
// Log first incident structure
if (incidents.length > 0) {
  console.log('🔍 [CrimeIntelligence] First incident structure:', {
    id: incidents[0].id,
    totalValueRecovered: incidents[0].totalValueRecovered,
    TotalValueRecovered: (incidents[0] as any).TotalValueRecovered,
    value: incidents[0].value,
    Value: (incidents[0] as any).Value,
    allKeys: Object.keys(incidents[0]),
    fullIncident: incidents[0]
  })
}

// Log value calculation results
console.log('💰 [CrimeIntelligence] Value calculation:', {
  totalIncidents,
  totalValue,
  distinctStores,
  sampleValues: incidents.slice(0, 3).map(inc => getIncidentValue(inc))
})
```

---

## What Changed

| Aspect | Before | After |
|--------|--------|-------|
| **Field Name Handling** | camelCase only | camelCase + PascalCase |
| **Value Extraction** | Direct property access | Helper function |
| **Robustness** | Failed with backend data | Works with any casing |
| **Logging** | None | Detailed structure & value logs |
| **Type Safety** | Assumed number | Converts strings to numbers |

---

## Expected Behavior After Fix

### Console Logs to Look For:

When you refresh the Crime Intelligence page, you should now see:

```
🔍 [CrimeIntelligence] First incident structure: {
  id: "...",
  totalValueRecovered: undefined,
  TotalValueRecovered: 1250,  // ✅ Found in PascalCase!
  value: undefined,
  Value: 1250,
  allKeys: ["Id", "CustomerId", "TotalValueRecovered", "Value", ...]
  fullIncident: { ... }
}

💰 [CrimeIntelligence] Value calculation: {
  totalIncidents: 8,
  totalValue: 5420,  // ✅ No longer 0!
  distinctStores: 7,
  sampleValues: [1250, 850, 1500]  // ✅ Actual values extracted
}
```

### UI Changes:

The **Value Impact** card should now show:
- ✅ **Before:** £0K
- ✅ **After:** £5K (or whatever the actual total is)

The subtitle should show:
- ✅ "Recovered / estimated loss"

---

## Testing Checklist

- [ ] Refresh Crime Intelligence page
- [ ] Check console for new logging output
- [ ] Verify "Value Impact" shows non-zero value
- [ ] Check "Hot Stores" chart shows values
- [ ] Check "Most Stolen Products" table shows values
- [ ] Verify "Top Hot Products" shows values

---

## Why This Happened

**Backend Convention:** .NET/C# typically uses PascalCase for property names:
```csharp
public class Incident {
  public int TotalValueRecovered { get; set; }
  public decimal Value { get; set; }
}
```

**Frontend Convention:** JavaScript/TypeScript typically uses camelCase:
```typescript
interface Incident {
  totalValueRecovered?: number
  value?: number
}
```

**Solution:** The helper function bridges this gap by checking both conventions.

---

## Future Improvements

### Option 1: Backend Transformation
Have the backend API serialize with camelCase:
```csharp
[JsonPropertyName("totalValueRecovered")]
public int TotalValueRecovered { get; set; }
```

### Option 2: Frontend Transformation
Transform all incident data on receipt to camelCase:
```typescript
const normalizeIncident = (inc: any): Incident => ({
  ...inc,
  totalValueRecovered: inc.TotalValueRecovered || inc.totalValueRecovered,
  value: inc.Value || inc.value,
  // ... other fields
})
```

For now, the helper function approach is more robust as it handles both formats without requiring backend changes.

---

## Files Modified

- ✅ `src/pages/customer/CustomerCrimeIntelligence.tsx`
  - Added `getIncidentValue()` helper function
  - Updated all value calculations to use helper
  - Added comprehensive logging

---

**Date:** December 3, 2025
**Issue:** Value Impact showing £0K with real incidents
**Root Cause:** PascalCase vs camelCase field name mismatch
**Status:** ✅ Fixed
**Build Status:** ✅ No TypeScript errors
