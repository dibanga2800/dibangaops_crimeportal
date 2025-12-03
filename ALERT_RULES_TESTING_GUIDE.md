# Alert Rules System - Testing Guide 🧪

## 🎯 Complete End-to-End Test

Follow these steps to test that alert emails are sent when incidents match your configured rules.

---

## 📋 Prerequisites

### 1. Start Backend
```powershell
cd c:\Users\David Ibanga\COOP_AIP\AIP_Backend
dotnet run
```

**✅ Verify Backend is Running:**
- Console shows: `Now listening on: https://localhost:5128`
- No errors in console

### 2. Start Frontend
```powershell
cd c:\Users\David Ibanga\COOP_AIP\AIP_UI
npm run dev
```

**✅ Verify Frontend is Running:**
- Browser opens to: `http://localhost:5173`
- MSW mocking enabled message appears

### 3. Check Email Configuration
Verify in `AIP_Backend/appsettings.json` or `appsettings.Development.json`:

```json
{
  "Smtp": {
    "Host": "mail.advantage1.co.uk",
    "Port": "25",
    "EnableSsl": "false",
    "Username": "noreply@advantage1.co.uk",
    "Password": "Hippo.swim1!",
    "FromEmail": "noreply@advantage1.co.uk",
    "FromName": "Advantage One Security - Alert System"
  }
}
```

---

## 🧪 Test Scenario 1: Simple Theft Alert

### Step 1: Create Alert Rule

1. **Navigate to:** Operations → Alert Rules
2. **Click:** "Create LPM Rule" (or Store Rule)
3. **Fill in the form:**

```
Rule Name: Test Theft Alert
Keywords: 
  - stolen (click Add)
  - theft (click Add)
  
Incident Types:
  ☑ Theft
  
LPM Region: East Midlands (or any region you have)

Trigger Condition: Any keyword matches

Channels:
  ☑ Email
  
Email Recipients:
  - your-email@domain.com (click Add)
  
Status:
  ☑ Active (enable this rule)
```

4. **Click:** "Create Rule"
5. **Verify:** Rule appears in the list with "Active" badge

### Step 2: Open Browser Console

**Important:** Open DevTools (F12) to monitor:
- Console tab for logs
- Network tab for API calls

### Step 3: Create Matching Incident

1. **Navigate to:** Operations → New Incident Report
2. **Fill in the form:**

```
Site: (Select any site in East Midlands region)
Date: Today's date
Time: Current time

Incident Type: Theft ← MATCHES your rule!

Description: "Suspected shoplifter caught stealing meat products"
             ← Contains keyword "stealing" (similar to "stolen")

OR use: "Person caught with stolen items worth £50"
        ← Contains exact keyword "stolen"

Officer Name: Your name
Priority: High
Status: Open
```

3. **Click:** "Submit Incident"

### Step 4: Check Backend Console

**Immediately after submitting, look for these logs:**

```
info: AIPBackend.Services.IncidentService[0]
      Incident created successfully: INC-000123

info: AIPBackend.Services.AlertRuleService[0]
      Found 1 matching alert rules for incident 123

info: AIPBackend.Services.AlertRuleService[0]
      Triggering alert rule: Test Theft Alert (ID: 1) for incident 123

info: AIPBackend.Services.AlertRuleService[0]
      Alert email sent successfully for rule Test Theft Alert to 1 recipients
```

### Step 5: Check Your Email

**Expected:**
- Email should arrive within **5-30 seconds**
- From: `noreply@advantage1.co.uk`
- Subject: `🚨 Security Alert: Test Theft Alert`

**Email Content Should Include:**
```
🚨 Security Alert Triggered
Alert Rule: Test Theft Alert

⚠️ Immediate Attention Required: An incident has been reported...

┌─────────────────────────────────┐
│ Incident Details                │
├─────────────────────────────────┤
│ Incident ID    │ INC-000123     │
│ Type           │ Theft          │
│ Site           │ [Site Name]    │
│ Region         │ East Midlands  │
│ Date           │ 02/12/2024     │
│ Priority       │ HIGH           │
│ Description    │ [Your desc]    │
└─────────────────────────────────┘

Alert Rule Information:
- Rule Name: Test Theft Alert
- Matching Keywords: stolen, theft
- Trigger Condition: any
```

---

## 🧪 Test Scenario 2: No Match (Negative Test)

### Create Non-Matching Incident

1. **Navigate to:** Operations → New Incident Report
2. **Fill in the form:**

```
Incident Type: Criminal Damage ← DIFFERENT from rule (Theft)
Description: "Graffiti on store wall"  ← NO matching keywords
```

3. **Submit**

**Expected Result:**
- ✅ Incident created successfully
- ❌ NO email sent
- Backend logs: `"No matching alert rules for incident"`

---

## 🧪 Test Scenario 3: Multiple Rules Matching

### Create Multiple Rules

**Rule 1:**
```
Name: High Priority Alert
Keywords: urgent, emergency
Incident Types: Theft, Assault
Email: manager@domain.com
```

**Rule 2:**
```
Name: Violence Alert  
Keywords: assault, aggressive, violence
Incident Types: Assault
Email: security@domain.com
```

### Create Matching Incident

```
Type: Assault
Description: "Aggressive customer attacked staff member - urgent response needed"
```

**Expected Result:**
- ✅ Both rules should match
- ✅ Two emails sent (one to each recipient)
- Backend logs show both rules triggered

---

## 🔍 Troubleshooting

### Issue 1: No Email Received

**Check:**
1. Backend console for email send logs
2. SMTP credentials are correct in appsettings.json
3. Email address is valid
4. Check spam/junk folder

**Backend Logs to Look For:**
```
✅ SUCCESS:
info: AIPBackend.Services.AlertRuleService[0]
      Alert email sent successfully...

❌ FAILURE:
error: AIPBackend.Services.AlertRuleService[0]
      Failed to send alert email...
      SMTP error: [details]
```

### Issue 2: Rule Not Triggering

**Check:**
1. Rule is marked as "Active"
2. Keywords match incident description (case-insensitive)
3. Incident type matches rule's incident types
4. Region matches (if region-specific rule)

**Add Debug Logging:**

Check backend console for:
```
debug: AIPBackend.Services.AlertRuleService[0]
      No matching alert rules for incident [ID]
```

### Issue 3: Backend Error

**Common Issues:**
- AlertRules table doesn't exist → Run migration
- IAlertRuleService not registered → Check Program.cs
- Email service not configured → Check appsettings.json

---

## 📊 Test Results Matrix

| Test Case | Incident Type | Keywords | Region | Expected | Result |
|-----------|---------------|----------|--------|----------|--------|
| Match All | Theft | "stolen" | East Midlands | ✅ Email Sent | [ ] |
| Match Partial | Theft | "violence" | East Midlands | ❌ No Email | [ ] |
| Wrong Type | Assault | "stolen" | East Midlands | ❌ No Email | [ ] |
| Wrong Region | Theft | "stolen" | Yorkshire | ❌ No Email | [ ] |
| Inactive Rule | Theft | "stolen" | East Midlands | ❌ No Email | [ ] |

---

## 🎯 Expected Behavior

### What Should Happen:

1. **Incident Created:**
   ```
   POST /api/incidents
   Response: 201 Created
   ```

2. **Alert Check (Async):**
   ```
   Background: AlertRuleService.CheckIncidentForAlertsAsync(incidentId)
   ```

3. **Find Matching Rules:**
   ```
   Query: Active rules with matching type + keywords
   Result: 1 matching rule found
   ```

4. **Send Email:**
   ```
   SMTP: Send to recipients
   Result: Email sent successfully
   ```

5. **Update Statistics:**
   ```
   Rule.LastTriggered = Now
   Rule.TriggerCount++
   ```

### Backend Log Flow:

```
[INFO] Incident created: INC-000123
[INFO] Found 1 matching alert rules for incident 123
[INFO] Triggering alert rule: Test Theft Alert
[INFO] Sending alert email to 1 recipients
[INFO] Alert email sent successfully
```

---

## 🔧 Quick Test Commands

### Check Alert Rules in Database:
```sql
SELECT 
    AlertRuleId,
    Name,
    RuleType,
    IsActive,
    Keywords,
    IncidentTypes,
    LpmRegion,
    EmailRecipients,
    TriggerCount,
    LastTriggered
FROM AlertRules
WHERE IsDeleted = 0
ORDER BY CreatedAt DESC;
```

### Check Recent Incidents:
```sql
SELECT TOP 5
    IncidentId,
    IncidentType,
    IncidentDescription,
    SiteId,
    DateOfIncident,
    CreatedAt
FROM Incidents
ORDER BY CreatedAt DESC;
```

### Check if Alert was Triggered:
```sql
SELECT 
    Name,
    TriggerCount,
    LastTriggered,
    DATEDIFF(SECOND, LastTriggered, GETDATE()) AS SecondsAgo
FROM AlertRules
WHERE TriggerCount > 0
ORDER BY LastTriggered DESC;
```

---

## ✅ Success Indicators

You'll know the system is working if you see:

1. **✅ Frontend Console:**
   ```
   ✅ Keyword added: theft | Total keywords: 1
   ✅ LPM Alert Rule validated successfully: {...}
   [API] POST /api/alertrules → 201 Created
   [API] POST /api/incidents → 201 Created
   ```

2. **✅ Backend Console:**
   ```
   [INFO] Alert rule created: Test Theft Alert
   [INFO] Incident created successfully: INC-000123
   [INFO] Found 1 matching alert rules
   [INFO] Alert email sent successfully
   ```

3. **✅ Email Inbox:**
   - Email received from `noreply@advantage1.co.uk`
   - Professional HTML formatting
   - Contains incident details
   - Shows matched keywords

4. **✅ Alert Rules Page:**
   - Navigate back to Operations → Alert Rules
   - Your rule should show:
     - Last Triggered: Just now
     - Trigger Count: 1

---

## 🎉 Happy Path Test Script

**Copy and follow this exact sequence:**

1. ✅ Start backend and frontend
2. ✅ Create alert rule:
   - Name: "Test Theft Alert"
   - Keywords: stolen, theft
   - Type: Theft
   - Region: East Midlands
   - Email: your-email@domain.com
   - Active: Yes
3. ✅ Submit and verify rule created
4. ✅ Open browser console (F12)
5. ✅ Navigate to New Incident Report
6. ✅ Fill form with matching data:
   - Type: Theft
   - Description: "Caught person with stolen items"
7. ✅ Submit incident
8. ✅ Check backend console for alert logs
9. ✅ Check email inbox (within 30 seconds)
10. ✅ Return to Alert Rules page
11. ✅ Verify trigger count = 1

---

**Ready to test! Let me know what happens after you submit the incident.** 🚀
