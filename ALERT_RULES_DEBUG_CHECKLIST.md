# Alert Rules Not Triggering - Debug Checklist 🔍

## 📋 Step-by-Step Debug

### 1️⃣ Check Backend Console Logs

**When you submit an incident, check the backend console (where `dotnet run` is) for:**

**Look for these specific lines:**

```
✅ Should see:
info: AIPBackend.Services.IncidentService[0]
      Incident created with ID [number] by user [userId]
```

**If you see this, the incident was created. Now check:**

```
✅ Should see ONE of these:
info: AIPBackend.Services.AlertRuleService[0]
      Found 1 matching alert rules for incident [number]

OR

debug: AIPBackend.Services.AlertRuleService[0]
      No matching alert rules for incident [number]

OR

warn: AIPBackend.Services.AlertRuleService[0]
      Incident not found for alert check: ID [number]

OR

error: AIPBackend.Services.AlertRuleService[0]
      Error checking incident [number] for alerts
```

**❌ If you see NOTHING after "Incident created":**
- The alert checking code is not running at all
- Service might not be registered

---

### 2️⃣ Run These SQL Queries

Open **SQL Server Management Studio** or **Azure Data Studio** and run:

```sql
-- Check if alert rules exist and are active
SELECT 
    AlertRuleId,
    Name,
    IsActive,
    IsDeleted,
    Keywords,
    IncidentTypes,
    LpmRegion,
    Channels,
    EmailRecipients
FROM AlertRules
WHERE IsActive = 1 AND IsDeleted = 0;
```

**Expected Result:**
- At least 1 row showing your test rule
- `IsActive = 1` (true)
- `IsDeleted = 0` (false)
- `EmailRecipients` should contain your email in JSON format: `["your@email.com"]`
- `Keywords` should contain: `["stolen","theft"]`
- `IncidentTypes` should contain: `["Theft"]`

**❌ If NO rows returned:**
- Alert rule wasn't created
- Go back to Alert Rules page and create it again

---

```sql
-- Check the most recent incident
SELECT TOP 1
    IncidentId,
    IncidentType,
    IncidentDescription,
    SiteId,
    DateOfIncident,
    CreatedAt
FROM Incidents
ORDER BY CreatedAt DESC;
```

**Check:**
- Does `IncidentType` match your alert rule's `IncidentTypes`?
- Does `IncidentDescription` contain any of your keywords?

---

### 3️⃣ Check Service Registration

Check if `IAlertRuleService` is registered in `Program.cs`:

```csharp
// Should be around line 125
builder.Services.AddScoped<IAlertRuleService, AlertRuleService>();
```

**If this line is missing, the alert service won't work!**

---

### 4️⃣ Common Issues

#### Issue A: Rule Not Matching

**Check Alert Rule:**
```
Incident Type: "Theft"
Rule's Incident Types: ["Theft"] ← Must match EXACTLY
```

**Check Keywords (Case-Insensitive):**
```
Incident Description: "Person caught with stolen items"
Rule Keywords: ["stolen", "theft"]
Match: "stolen" found ✅
```

**Check Channels:**
```
Rule Channels: ["email"] ← Must include "email"
```

**Check Email Recipients:**
```
Rule Email Recipients: ["your@email.com"] ← Must not be empty
```

#### Issue B: Service Not Running

**Backend console should show at startup:**
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: https://localhost:5128
```

**If backend is not running:**
```powershell
cd c:\Users\David Ibanga\COOP_AIP\AIP_Backend
dotnet run
```

#### Issue C: Database Migration Not Applied

**Check if AlertRules table exists:**
```sql
SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'AlertRules';
```

**If no rows returned:**
```powershell
cd c:\Users\David Ibanga\COOP_AIP\AIP_Backend
dotnet ef database update
```

---

### 5️⃣ Enable Debug Logging

**Update `appsettings.Development.json`:**

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "AIPBackend.Services.AlertRuleService": "Debug",
      "AIPBackend.Services.IncidentService": "Debug",
      "AIPBackend.Services.EmailService": "Debug"
    }
  }
}
```

**Restart backend** and try creating an incident again.

---

### 6️⃣ Manual Test Query

**Run this to manually check if rule would match:**

```sql
DECLARE @IncidentType NVARCHAR(100) = 'Theft'
DECLARE @Description NVARCHAR(MAX) = 'Person caught with stolen items'

SELECT 
    AlertRuleId,
    Name,
    Keywords,
    IncidentTypes,
    TriggerCondition,
    EmailRecipients
FROM AlertRules
WHERE IsActive = 1 
    AND IsDeleted = 0
    AND (
        -- Check if incident type matches
        IncidentTypes LIKE '%' + @IncidentType + '%'
    )
    AND (
        -- Check if any keyword matches (simplified check)
        @Description LIKE '%stolen%' OR @Description LIKE '%theft%'
    );
```

**Expected Result:**
- Should return your alert rule
- If NO rows, the rule won't trigger

---

## 🔍 What to Share with Me

Please provide:

1. **Backend Console Output** after creating incident:
   ```
   [Copy everything from "Incident created" to end]
   ```

2. **SQL Query Result:**
   ```sql
   SELECT * FROM AlertRules WHERE IsActive = 1 AND IsDeleted = 0;
   ```
   
3. **Recent Incident:**
   ```sql
   SELECT TOP 1 * FROM Incidents ORDER BY CreatedAt DESC;
   ```

4. **Any Errors:**
   - Red text in backend console
   - Exception messages
   - SMTP errors

---

## 🚨 Quick Fixes

### Fix 1: Restart Backend
```powershell
# Stop backend (Ctrl+C)
cd c:\Users\David Ibanga\COOP_AIP\AIP_Backend
dotnet run
```

### Fix 2: Verify Alert Rule Created
```
Navigate to: Operations → Alert Rules
Should see: Your rule with "Active" badge
```

### Fix 3: Create Simple Test Rule
```
Name: Simple Test
Keywords: test (just one word)
Types: ☑ Theft
Region: Any region
Email: your-email@domain.com
Status: ☑ Active
```

Then create incident with:
```
Type: Theft
Description: "This is a test incident"
```

---

**Please share the backend console logs so I can see exactly what's happening!** 📝
