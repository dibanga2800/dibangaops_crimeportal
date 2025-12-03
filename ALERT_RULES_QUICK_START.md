# Alert Rules System - Quick Start Guide

## 🚀 Getting Started

### Step 1: Apply Database Migration

```bash
cd AIP_Backend
dotnet ef database update
```

This creates the `AlertRules` table in your database.

### Step 2: Start the Backend

```bash
cd AIP_Backend
dotnet run
```

Backend will be available at: `http://localhost:5128`

### Step 3: Start the Frontend

```bash
cd AIP_UI
npm run dev
```

Frontend will be available at: `http://localhost:5173`

---

## 📧 Configure Email (Required for Alerts)

Update `AIP_Backend/appsettings.json`:

```json
{
  "Email": {
    "SmtpHost": "smtp.gmail.com",
    "SmtpPort": 587,
    "SmtpUser": "your-email@gmail.com",
    "SmtpPassword": "your-app-password",
    "FromEmail": "alerts@yourcompany.com",
    "FromName": "Security Alert System",
    "EnableSsl": true
  }
}
```

**Note:** For Gmail, use an [App Password](https://support.google.com/accounts/answer/185833?hl=en), not your regular password.

---

## 🎯 Create Your First Alert Rule

### Via Frontend (Recommended)

1. Login to the application
2. Navigate to **Operations** → **Alert Rules**
3. Click the **LPM Alert Rules** tab
4. Click **Create LPM Rule** button
5. Fill in the form:
   - **Rule Name**: "Test Theft Alert"
   - **Keywords**: Type "theft" and press Enter
   - **Incident Types**: Select "Theft"
   - **LPM Region**: Select your region
   - **Trigger Condition**: Select "Match ANY keyword"
   - **Channels**: Check "Email"
   - **Email Recipients**: Enter your email and press Enter
6. Click **Create Rule**

### Via API (For Testing)

```bash
curl -X POST http://localhost:5128/api/alert-rules \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -d '{
    "name": "Test Theft Alert",
    "ruleType": "LPM",
    "keywords": ["theft"],
    "incidentTypes": ["Theft"],
    "lpmRegion": "East Midlands",
    "regionId": 1,
    "triggerCondition": "any",
    "channels": ["email"],
    "emailRecipients": ["your-email@example.com"],
    "isActive": true
  }'
```

---

## 🧪 Test the Alert System

### 1. Create a Test Incident

1. Navigate to **Operations** → **Incident Reports**
2. Click **New Incident**
3. Fill in the form:
   - Select a customer
   - Select a site in East Midlands region
   - Incident Type: **Theft**
   - Description: "A theft incident was reported at the store" (contains "theft")
   - Fill in other required fields
4. Click **Submit**

### 2. Check for Email

Within seconds, you should receive an email at the configured recipient address with:
- Subject: "Security Alert: Test Theft Alert"
- Professional HTML formatted email
- Full incident details
- Reason for alert trigger

### 3. Verify in Backend Logs

Look for these logs:
```
[INF] Incident created with ID 123
[INF] Found 1 matching alert rules for incident 123
[INF] Triggering alert rule: Test Theft Alert (ID: 1) for incident 123
[INF] Alert email sent successfully for rule Test Theft Alert to 1 recipients
```

---

## 📊 View Alert Statistics

### In Frontend
1. Go to **Alert Rules** page
2. View the table showing all rules
3. Check the **Triggered** column for trigger count
4. See when each rule was **Last Triggered**

### In Database
```sql
SELECT 
    Name,
    IsActive,
    TriggerCount,
    LastTriggered,
    EmailRecipients
FROM AlertRules
WHERE IsDeleted = 0
ORDER BY TriggerCount DESC;
```

---

## 🎨 UI Improvements Made

### Alert Rules Page
- ✅ Subtle blue gradient background
- ✅ Loading indicator while fetching rules
- ✅ Rule count badges on tabs
- ✅ API integration complete
- ✅ Professional error handling

### Incident Pages
- ✅ Incident Report Page - Blue gradient background
- ✅ Incident List Page - Blue gradient background + enhanced filters
- ✅ Incident Form - Blue gradient background

### Dashboard
- ✅ Admin Dashboard - Region filtering + working time period filters
- ✅ Incidents loaded from API
- ✅ Stats update when region/period changes

### Analytics Hub
- ✅ Crime Trend Explorer - Interactive day/hour filtering
- ✅ Store details populate instantly
- ✅ Subtle blue gradient background
- ✅ Fixed dependency issues

---

## 🔍 Common Issues & Solutions

### Issue: No Email Received

**Check:**
1. Is email service configured in appsettings.json?
2. Are SMTP credentials correct?
3. Is rule isActive = true?
4. Does incident match rule criteria?
5. Check backend logs for email errors

**Solution:**
```bash
# Check logs
tail -f AIP_Backend/logs/application.log | grep -i email
```

### Issue: Rule Not Triggering

**Check:**
1. Is rule active?
2. Do keywords match (case-insensitive)?
3. Does incident type match?
4. Check trigger condition logic

**Solution:**
Use the manual trigger endpoint:
```bash
curl -X POST http://localhost:5128/api/alert-rules/check-incident/123 \
  -H "Authorization: Bearer YOUR_TOKEN"
```

### Issue: Frontend Shows 0 Rules

**Check:**
1. Backend running?
2. API endpoint correct?
3. Authentication working?
4. Check browser console for errors

**Solution:**
```javascript
// Check console for:
🔄 Loading regions and sites for analytics...
✅ Loaded store rules: X
✅ Loaded LPM rules: Y
```

---

## 📚 API Endpoints Summary

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/alert-rules` | Get paginated rules |
| GET | `/api/alert-rules/{id}` | Get specific rule |
| POST | `/api/alert-rules` | Create new rule |
| PUT | `/api/alert-rules/{id}` | Update rule |
| DELETE | `/api/alert-rules/{id}` | Delete rule (soft) |
| PATCH | `/api/alert-rules/{id}/toggle` | Toggle active status |
| POST | `/api/alert-rules/check-incident/{id}` | Manual alert check |

---

## 🎓 Best Practices

### Creating Effective Rules

**DO:**
- ✅ Use specific keywords (3-5 keywords per rule)
- ✅ Test rules with historical incidents first
- ✅ Keep recipient lists up to date
- ✅ Monitor trigger counts regularly
- ✅ Deactivate unused rules

**DON'T:**
- ❌ Use too many keywords (causes too many matches)
- ❌ Use very generic keywords like "the", "and"
- ❌ Create duplicate rules
- ❌ Send alerts to personal emails (use distribution lists)
- ❌ Forget to test before activating

### Keyword Selection Tips

**Good Keywords:**
- "high value", "£500", "aggressive", "weapon", "repeat offender"

**Bad Keywords:**
- "the", "and", "incident", "report" (too common)

### Email Recipients

**Best:**
- Use distribution lists: `security-team@coop.com`
- Include backup: `[primary@coop.com, backup@coop.com]`
- Regional lists: `east-midlands-lpm@coop.com`

**Avoid:**
- Personal emails that might change
- Too many recipients (causes alert fatigue)
- Unmonitored inboxes

---

## 🔧 Customization

### Modify Email Template

Edit `Services/AlertRuleService.cs`, method `SendAlertEmailAsync`:

```csharp
var subject = $"Security Alert: {rule.Name}";
var body = $@"
    <html>
    <!-- Customize your HTML email template here -->
    </html>";
```

### Add New Trigger Conditions

Edit `Repositories/AlertRuleRepository.cs`, method `GetActiveRulesForIncidentAsync`:

```csharp
bool shouldTrigger = rule.TriggerCondition.ToLower() switch
{
    "any" => matchedKeywords.Count > 0,
    "all" => matchedKeywords.Count == ruleKeywords.Count,
    "exact-match" => descriptionLower == string.Join(" ", ruleKeywords).ToLower(),
    "your-new-condition" => /* your logic */,
    _ => matchedKeywords.Count > 0
};
```

---

## 📞 Support

### Logs Location
- Backend: `AIP_Backend/logs/`
- Frontend: Browser Developer Console

### Debug Mode
Add to `appsettings.Development.json`:
```json
{
  "Logging": {
    "LogLevel": {
      "AIPBackend.Services.AlertRuleService": "Debug",
      "AIPBackend.Services.EmailService": "Debug"
    }
  }
}
```

---

## ✅ Success Criteria

You'll know it's working when:
1. ✅ Alert Rules page loads without errors
2. ✅ Can create/edit/delete rules via UI
3. ✅ Rule count badges show correct numbers
4. ✅ Creating matching incident sends email
5. ✅ Email arrives at configured recipients
6. ✅ Trigger count increments after match
7. ✅ Backend logs show successful operations

---

## 🎉 You're All Set!

The Alert Rules system is now fully operational. Create your first rule and start receiving automated security alerts!

For detailed API documentation, see: `ALERT_RULES_API_DOCUMENTATION.md`
