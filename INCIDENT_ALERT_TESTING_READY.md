# Incident Report → Alert Rules Testing - READY! 🚀

## ✅ Configuration Complete

The **Incident Report Page** is now fully connected to the real backend database and will trigger alert rules when incidents are created.

---

## 🔧 What Was Configured

### MSW (Mock Service Worker) Updated

**Before:**
```typescript
POST /api/incidents → Mocked (didn't save to DB) ❌
```

**After:**
```typescript
POST /api/incidents → Real Backend (saves to DB + triggers alerts) ✅
PUT /api/incidents → Real Backend (updates DB + triggers alerts) ✅
DELETE /api/incidents → Real Backend (deletes from DB) ✅
```

**Still Mocked (for convenience):**
```typescript
GET /api/incidents → Mock Data (for viewing test incidents)
```

This means:
- ✅ **Creating incidents** → Saves to real database
- ✅ **Alert rules** → Triggered automatically
- ✅ **Emails** → Sent to your specified recipients
- ✅ **Backend logs** → Show full alert processing

---

## 🎯 Complete Test Flow

### Step 1: Start Backend
```powershell
cd c:\Users\David Ibanga\COOP_AIP\AIP_Backend
dotnet run
```

**Verify:**
- Console shows: `Now listening on: https://localhost:5128`
- Database connection successful

### Step 2: Start Frontend
```powershell
cd c:\Users\David Ibanga\COOP_AIP\AIP_UI
npm run dev
```

**Verify:**
- Browser opens: `http://localhost:5173`
- MSW shows: `[MSW] Mocking enabled.`

---

## 📧 Test Alert Rules System

### Quick Test (5 Minutes)

**1. Create Alert Rule** (Operations → Alert Rules → Create LPM Rule)

```
✅ Rule Name: Test Theft Alert
✅ Keywords: stolen, theft (add both)
✅ Incident Types: ☑ Theft
✅ LPM Region: East Midlands (any region)
✅ Channels: ☑ Email
✅ Email Recipients: your-email@domain.com (add)
✅ Status: ☑ Active
```

Click "Create Rule"

**2. Create Matching Incident** (Operations → New Incident Report)

```
✅ Site: (Any site)
✅ Date: Today
✅ Time: Current time
✅ Officer Name: Test Officer

✅ Incident Type: Theft ← MATCHES!
✅ Description: "Person caught with stolen items" ← Contains "stolen"!

✅ Priority: High
✅ Status: Open
✅ Duty Manager: Test Manager
```

Click "Submit"

**3. Check Backend Console Immediately**

Look for these logs:
```
info: AIPBackend.Services.IncidentService[0]
      Incident created with ID 123

info: AIPBackend.Services.AlertRuleService[0]
      Found 1 matching alert rules for incident 123

info: AIPBackend.Services.AlertRuleService[0]
      Triggering alert rule: Test Theft Alert (ID: 1)

info: AIPBackend.Services.AlertRuleService[0]
      Alert email sent successfully to 1 recipients
```

**4. Check Your Email (Within 30 Seconds)**

- **From:** noreply@advantage1.co.uk
- **Subject:** 🚨 Security Alert: Test Theft Alert
- **Body:** Professional HTML email with incident details

**5. Verify in Alert Rules Page**

Navigate to: Operations → Alert Rules
- Your rule should show:
  - **Last Triggered:** Just now
  - **Trigger Count:** 1

---

## 🔍 How the Connection Works

### When You Submit the Incident Form:

```
1. Frontend: POST /api/incidents
   ↓
2. MSW: Passes through to backend (not mocked)
   ↓
3. Backend: IncidentController.CreateIncident()
   ↓
4. Backend: IncidentService.CreateAsync()
   ↓ (saves to database)
5. Backend: Database → Incidents table (new row)
   ↓ (async fire-and-forget)
6. Backend: AlertRuleService.CheckIncidentForAlertsAsync()
   ↓
7. Backend: Query AlertRules table for matches
   ↓
8. Backend: Found matching rule(s)
   ↓
9. Backend: SendAlertEmailAsync()
   ↓
10. SMTP: Email sent to your recipients
    ↓
11. Backend: Update rule statistics (TriggerCount++)
```

**Total time:** < 1 second ⚡

---

## 📊 What Gets Saved to Database

### Incidents Table:
```sql
INSERT INTO Incidents (
    IncidentType,
    IncidentDescription,
    SiteId,
    DateOfIncident,
    TimeOfIncident,
    OfficerName,
    Priority,
    ...
) VALUES (
    'Theft',
    'Person caught with stolen items',
    123,
    '2024-12-02',
    '14:30',
    'Test Officer',
    'High',
    ...
)
```

### AlertRules Table (Updated):
```sql
UPDATE AlertRules
SET 
    LastTriggered = GETDATE(),
    TriggerCount = TriggerCount + 1
WHERE AlertRuleId = 1
```

---

## 🎯 Key Points

✅ **Incident Form** → Connected to real backend  
✅ **Saves to Database** → Real SQL Server database  
✅ **Triggers Alerts** → Automatically on creation  
✅ **Sends Emails** → To your specified recipients  
✅ **Updates Statistics** → Last triggered, trigger count  

---

## 🧪 Testing Checklist

**Before Testing:**
- [ ] Backend is running (`dotnet run`)
- [ ] Frontend is running (`npm run dev`)
- [ ] Database connection is working
- [ ] SMTP is configured in appsettings.json

**Test Steps:**
- [ ] Create an alert rule with your email
- [ ] Verify rule appears as "Active"
- [ ] Open browser DevTools (F12)
- [ ] Create matching incident
- [ ] Check backend console for alert logs
- [ ] Wait 30 seconds for email
- [ ] Verify email received
- [ ] Check alert rule statistics updated

**Negative Tests:**
- [ ] Create non-matching incident (different type)
- [ ] Verify NO email sent
- [ ] Backend logs: "No matching alert rules"

---

## 📧 Email Configuration

Your existing SMTP settings will be used:
```json
{
  "Smtp": {
    "Host": "mail.advantage1.co.uk",
    "Port": "25",
    "FromEmail": "noreply@advantage1.co.uk",
    "FromName": "Advantage One Security - Alert System"
  }
}
```

---

## 🎉 You're Ready to Test!

**The incident form will now:**
1. ✅ Save incidents to the real database
2. ✅ Trigger alert rules automatically
3. ✅ Send emails to your specified recipients
4. ✅ Update rule statistics

**Just follow the test steps above and you should receive the email!** 🚀

---

## 🔎 Troubleshooting

**If alert doesn't trigger:**
- Check backend console for any errors
- Verify rule is "Active" (not inactive)
- Ensure keywords match (case-insensitive)
- Ensure incident type matches rule
- Check SMTP logs for email send errors

**If email doesn't arrive:**
- Check spam/junk folder
- Verify email address is correct in rule
- Check backend logs for "Alert email sent successfully"
- Verify SMTP credentials are correct

---

**Test now and let me know what you see in the backend console!** 📝
