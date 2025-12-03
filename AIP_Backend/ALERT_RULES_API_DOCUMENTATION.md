# Alert Rules API Documentation

## Overview

The Alert Rules system monitors incident reports and automatically sends email notifications when incidents match configured criteria. This enables proactive security management by alerting the right people at the right time.

---

## Features

✅ **Automatic Incident Monitoring** - Checks every new/updated incident against active rules
✅ **Flexible Trigger Conditions** - Support for 'any', 'all', or 'exact-match' keyword matching
✅ **Email Notifications** - Sends formatted HTML emails to configured recipients
✅ **Multi-Channel Support** - Email and in-app notifications (in-app coming soon)
✅ **Store & LPM Rules** - Different rule types for stores and Loss Prevention Managers
✅ **Audit Trail** - Tracks who created/updated rules and trigger history

---

## Database Schema

### AlertRule Table

| Column | Type | Description |
|--------|------|-------------|
| AlertRuleId | int | Primary key (auto-increment) |
| Name | nvarchar(200) | Rule name |
| RuleType | nvarchar(50) | 'Store' or 'LPM' |
| Keywords | nvarchar(2000) | JSON array of keywords to match |
| IncidentTypes | nvarchar(1000) | JSON array of incident types |
| StoreRadius | decimal | Radius for store rules (miles/km) |
| LpmRegion | nvarchar(200) | Region name for LPM rules |
| RegionId | int | Foreign key to Regions table |
| TriggerCondition | nvarchar(50) | 'any', 'all', or 'exact-match' |
| Channels | nvarchar(500) | JSON array: ['email', 'in-app'] |
| EmailRecipients | nvarchar(2000) | JSON array of email addresses |
| IsActive | bit | Whether rule is active |
| CustomerId | int | Foreign key to Customers |
| SiteId | int | Foreign key to Sites |
| CreatedAt | datetime | Creation timestamp |
| CreatedBy | nvarchar | User ID who created |
| UpdatedAt | datetime | Last update timestamp |
| UpdatedBy | nvarchar | User ID who updated |
| IsDeleted | bit | Soft delete flag |
| LastTriggered | datetime | Last trigger timestamp |
| TriggerCount | int | Number of times triggered |

---

## API Endpoints

### Base URL
```
/api/alert-rules
```

### 1. Get Alert Rules (Paginated)

**GET** `/api/alert-rules`

**Query Parameters:**
- `search` (string, optional) - Search by rule name
- `ruleType` (string, optional) - Filter by 'Store' or 'LPM'
- `isActive` (bool, optional) - Filter by active status
- `customerId` (int, optional) - Filter by customer
- `page` (int, default: 1) - Page number
- `pageSize` (int, default: 10) - Items per page

**Response:**
```json
{
  "success": true,
  "data": {
    "data": [
      {
        "alertRuleId": 1,
        "name": "High Value Theft Alert",
        "ruleType": "LPM",
        "keywords": ["theft", "high value"],
        "incidentTypes": ["Theft", "Shoplifting"],
        "triggerCondition": "any",
        "channels": ["email"],
        "emailRecipients": ["lpm@example.com"],
        "isActive": true,
        "triggerCount": 5,
        "lastTriggered": "2025-12-01T10:30:00Z"
      }
    ],
    "total": 10,
    "page": 1,
    "pageSize": 10,
    "totalPages": 1
  },
  "message": "Alert rules retrieved successfully"
}
```

### 2. Get Alert Rule by ID

**GET** `/api/alert-rules/{id}`

**Response:**
```json
{
  "success": true,
  "data": {
    "alertRuleId": 1,
    "name": "High Value Theft Alert",
    ...
  },
  "message": "Alert rule retrieved successfully"
}
```

### 3. Create Alert Rule

**POST** `/api/alert-rules`

**Request Body:**
```json
{
  "name": "High Value Theft Alert",
  "ruleType": "LPM",
  "keywords": ["theft", "high value", "expensive"],
  "incidentTypes": ["Theft", "Shoplifting"],
  "lpmRegion": "East Midlands",
  "regionId": 1,
  "triggerCondition": "any",
  "channels": ["email"],
  "emailRecipients": ["lpm@example.com", "manager@example.com"],
  "isActive": true
}
```

**Response:**
```json
{
  "success": true,
  "data": {
    "alertRuleId": 1,
    ...
  },
  "message": "Alert rule created successfully"
}
```

### 4. Update Alert Rule

**PUT** `/api/alert-rules/{id}`

**Request Body:** (all fields optional)
```json
{
  "name": "Updated Rule Name",
  "keywords": ["new", "keywords"],
  "isActive": true
}
```

### 5. Delete Alert Rule

**DELETE** `/api/alert-rules/{id}`

**Response:**
```json
{
  "success": true,
  "message": "Alert rule deleted successfully"
}
```

### 6. Toggle Alert Rule Status

**PATCH** `/api/alert-rules/{id}/toggle`

**Request Body:**
```json
true // or false
```

### 7. Check Incident for Alerts (Manual Trigger)

**POST** `/api/alert-rules/check-incident/{incidentId}`

**Response:**
```json
{
  "success": true,
  "message": "Alert check completed successfully"
}
```

---

## How It Works

### 1. Alert Rule Creation

1. User creates alert rule via UI
2. Rule is saved to database with trigger conditions
3. Rule becomes active if `isActive = true`

### 2. Incident Monitoring

**Automatic Trigger:**
When an incident is created or updated, the system:
1. Fetches all active alert rules
2. Checks if incident matches each rule's criteria:
   - ✅ Incident type matches rule's `incidentTypes`
   - ✅ Description contains rule's `keywords` based on `triggerCondition`
3. For matching rules, triggers notifications

**Trigger Conditions:**
- **any**: Matches if ANY keyword is found in description
- **all**: Matches only if ALL keywords are found in description
- **exact-match**: Matches if description exactly matches the keywords

### 3. Email Notification

When a rule is triggered:
1. System generates formatted HTML email with incident details
2. Email is sent to all `emailRecipients` configured in the rule
3. Email includes:
   - Alert rule name
   - Incident details (ID, type, site, date, officer, priority, description, value)
   - Reason for trigger
   - Professional formatting with color-coded priority

### 4. Statistics Tracking

Each time a rule is triggered:
- `LastTriggered` is updated to current timestamp
- `TriggerCount` is incremented
- Admin can monitor rule effectiveness

---

## Email Template

The system sends professional HTML emails with:

### Email Structure:
- **Header**: Red banner with alert icon and rule name
- **Content**: Incident details in a styled card
- **Trigger Reason**: Explains why alert was triggered
- **Footer**: System information and no-reply notice

### Incident Details Included:
- Incident ID
- Type
- Site name
- Region
- Date and time
- Officer name
- Priority (color-coded: red=high, yellow=medium, green=low)
- Description
- Value recovered (if applicable)

---

## Rule Types

### Store Alert Rules
- **Purpose**: Notify store managers about incidents at specific stores or within a radius
- **Use Case**: Local store manager wants alerts for incidents at their store or nearby locations
- **Fields**: `storeRadius`, `customerId`, `siteId`

### LPM Alert Rules
- **Purpose**: Notify Loss Prevention Managers about incidents in their region
- **Use Case**: Regional LPM wants alerts for all incidents in East Midlands region
- **Fields**: `lpmRegion`, `regionId`, `emailRecipients`

---

## Example Use Cases

### Use Case 1: High-Value Theft Alert
```json
{
  "name": "High Value Theft - East Midlands",
  "ruleType": "LPM",
  "keywords": ["expensive", "high value", "£500"],
  "incidentTypes": ["Theft", "Shoplifting"],
  "lpmRegion": "East Midlands",
  "triggerCondition": "any",
  "channels": ["email"],
  "emailRecipients": ["lpm-eastmidlands@coop.com"],
  "isActive": true
}
```

**Triggers when:**
- Incident type is "Theft" or "Shoplifting"
- Description contains "expensive" OR "high value" OR "£500"
- Incident is in East Midlands region

### Use Case 2: Violent Incident Alert
```json
{
  "name": "Violent Incident Immediate Alert",
  "ruleType": "LPM",
  "keywords": ["assault", "violence", "aggressive", "threatening"],
  "incidentTypes": ["Assault", "Anti-Social Behaviour"],
  "triggerCondition": "any",
  "channels": ["email"],
  "emailRecipients": ["security-team@coop.com", "lpm@coop.com"],
  "isActive": true
}
```

### Use Case 3: Repeat Offender Alert
```json
{
  "name": "Known Offender Detection",
  "ruleType": "Store",
  "keywords": ["repeat offender", "known", "previous"],
  "incidentTypes": ["Theft", "Shoplifting", "Suspicious Behaviour"],
  "storeRadius": 5,
  "triggerCondition": "any",
  "channels": ["email"],
  "emailRecipients": ["store-manager@coop.com"],
  "isActive": true
}
```

---

## Integration with Incident Service

### Automatic Alert Checking

The `IncidentService` automatically checks for alerts:

```csharp
// In IncidentService.CreateAsync()
var created = await _repository.CreateAsync(incident);

// Check for matching alert rules (async fire-and-forget)
_ = Task.Run(async () =>
{
    using var scope = _serviceProvider.CreateScope();
    var alertRuleService = scope.ServiceProvider.GetService<IAlertRuleService>();
    await alertRuleService.CheckIncidentForAlertsAsync(created.IncidentId);
});
```

This ensures:
- ✅ Incidents are saved quickly (non-blocking)
- ✅ Alerts are checked asynchronously
- ✅ No impact on incident creation performance
- ✅ Errors in alert checking don't affect incident creation

---

## Frontend Integration

### Service Usage

```typescript
import { alertRuleService } from '@/services/alertRuleService'

// Create LPM rule
await alertRuleService.createLPMAlertRule({
  name: 'My Alert',
  keywords: ['theft'],
  incidentTypes: ['Theft'],
  lpmRegion: 'East Midlands',
  triggerCondition: 'any',
  channels: ['email'],
  emailRecipients: ['manager@example.com'],
  isActive: true
})

// Get all rules
const rules = await alertRuleService.getAlertRules({
  ruleType: 'LPM',
  isActive: true
})

// Toggle rule
await alertRuleService.toggleAlertRule(ruleId, false) // Deactivate

// Delete rule
await alertRuleService.deleteAlertRule(ruleId)
```

---

## Security & Permissions

### Authorization
- All endpoints require authentication (`[Authorize]`)
- Uses JWT bearer token authentication
- User context enforced via `IUserContextService`

### Access Control
- Users can only access rules for their assigned customers
- Admins can access all rules
- Officers can only see rules they created

---

## Database Migration

### Apply Migration

```bash
cd AIP_Backend
dotnet ef database update
```

This creates the `AlertRules` table with all necessary columns and indexes.

---

## Testing

### 1. Create a Test Rule

```bash
POST /api/alert-rules
Content-Type: application/json
Authorization: Bearer {token}

{
  "name": "Test Alert",
  "ruleType": "LPM",
  "keywords": ["test"],
  "incidentTypes": ["Theft"],
  "triggerCondition": "any",
  "channels": ["email"],
  "emailRecipients": ["test@example.com"],
  "isActive": true
}
```

### 2. Create a Test Incident

```bash
POST /api/incidents
Content-Type: application/json
Authorization: Bearer {token}

{
  "incidentType": "Theft",
  "description": "This is a test incident with the word test in it",
  ...
}
```

### 3. Check Logs

Look for:
```
[INF] Alert rule created: Test Alert (ID: 1)
[INF] Incident created with ID 123
[INF] Found 1 matching alert rules for incident 123
[INF] Triggering alert rule: Test Alert (ID: 1) for incident 123
[INF] Alert email sent successfully for rule Test Alert to 1 recipients
```

### 4. Check Email

Recipient should receive an email with:
- Subject: "Security Alert: Test Alert"
- Body: Formatted incident details
- Professional styling

---

## Monitoring & Maintenance

### View Rule Statistics

```sql
-- Most triggered rules
SELECT Name, TriggerCount, LastTriggered, IsActive
FROM AlertRules
WHERE IsDeleted = 0
ORDER BY TriggerCount DESC;

-- Recently triggered rules
SELECT Name, LastTriggered, TriggerCount
FROM AlertRules
WHERE IsDeleted = 0 AND LastTriggered IS NOT NULL
ORDER BY LastTriggered DESC;

-- Inactive rules
SELECT Name, CreatedAt, LastTriggered
FROM AlertRules
WHERE IsDeleted = 0 AND IsActive = 0;
```

### Performance Optimization

- Alert checking runs asynchronously (fire-and-forget)
- No performance impact on incident creation
- Email sending is non-blocking
- Failed email sends are logged but don't fail incident creation

---

## Configuration

### Email Settings

Configure in `appsettings.json`:

```json
{
  "Email": {
    "SmtpHost": "smtp.example.com",
    "SmtpPort": 587,
    "SmtpUser": "alerts@coop.com",
    "SmtpPassword": "your-password",
    "FromEmail": "alerts@coop.com",
    "FromName": "Security Alert System"
  }
}
```

---

## Troubleshooting

### Rule Not Triggering

**Check:**
1. ✅ Is rule `IsActive = true`?
2. ✅ Do `incidentTypes` match the incident?
3. ✅ Are keywords present in incident `description`?
4. ✅ Is `TriggerCondition` set correctly?
5. ✅ Check application logs for errors

**Console Check:**
```
🔍 Check incident X for alert rules
✅ Found Y matching rules
📧 Sending email to: [recipients]
```

### Email Not Sending

**Check:**
1. ✅ Is "email" in the rule's `channels` array?
2. ✅ Are `emailRecipients` configured?
3. ✅ Are email service settings correct?
4. ✅ Check email service logs for SMTP errors

**Logs to Check:**
```
[ERR] Error sending alert email for rule X
[WRN] Failed to send alert email for rule X
```

### Rule Not Appearing in Frontend

**Check:**
1. ✅ Is `IsDeleted = false`?
2. ✅ Does user have access to the customer?
3. ✅ Is frontend calling correct endpoint?
4. ✅ Check browser console for errors

---

## Best Practices

### Rule Design
- ✅ Use specific, actionable keywords
- ✅ Keep keyword lists focused (3-5 keywords)
- ✅ Use 'any' condition for broad matching
- ✅ Use 'all' condition for specific scenarios
- ✅ Test rules before activating

### Email Recipients
- ✅ Use distribution lists, not individual emails
- ✅ Include backup recipients
- ✅ Verify email addresses before saving
- ✅ Keep recipient lists up to date

### Performance
- ✅ Don't create too many rules (keep under 50 per customer)
- ✅ Deactivate unused rules
- ✅ Monitor trigger counts regularly
- ✅ Review and optimize keywords quarterly

### Security
- ✅ Only send emails to authorized personnel
- ✅ Don't include sensitive data in emails
- ✅ Use secure SMTP connections (TLS)
- ✅ Audit rule changes regularly

---

## Future Enhancements

### Planned Features
1. **In-App Notifications** - Real-time browser notifications
2. **SMS Alerts** - Critical incident SMS notifications
3. **Severity Levels** - Different alert levels (info, warning, critical)
4. **Snooze Functionality** - Temporarily disable rules
5. **Rule Templates** - Pre-configured rule templates
6. **Advanced Matching** - Regex support, fuzzy matching
7. **Escalation Rules** - Automatic escalation if no response
8. **Alert Throttling** - Prevent email flooding

---

## Support

### Common Questions

**Q: How quickly are alerts sent?**
A: Within seconds of incident creation. Alert checking runs asynchronously immediately after incident is saved.

**Q: Can I test a rule without activating it?**
A: Yes, create the rule with `isActive = false`, then use the manual trigger endpoint to test.

**Q: What happens if email sending fails?**
A: The error is logged but incident creation still succeeds. Check logs for email errors.

**Q: Can one incident trigger multiple rules?**
A: Yes, all matching rules will be triggered and send separate emails.

**Q: Are duplicate emails prevented?**
A: Not currently. If multiple rules have the same recipient, they'll receive multiple emails.

---

## Version History

- **v1.0.0** (Dec 2025) - Initial release
  - Store and LPM alert rules
  - Email notifications
  - Keyword and incident type matching
  - Automatic trigger on incident creation

---

## API Response Format

All API responses follow this structure:

```json
{
  "success": true,
  "data": {...},
  "message": "Success message"
}
```

Error responses:

```json
{
  "success": false,
  "message": "Error message"
}
```

---

## Dependencies

### Backend:
- Entity Framework Core (Database)
- System.Text.Json (JSON serialization)
- IEmailService (Email sending)
- ILogger (Logging)

### Frontend:
- axios (HTTP client)
- @/types/alertRules (TypeScript types)
- @/config/api (API configuration)

---

## Logging

### Log Levels

**Information:**
- Rule created/updated/deleted
- Alert triggered
- Email sent successfully

**Warning:**
- Email send failed
- Incident not found
- No matching rules

**Error:**
- Database errors
- Alert checking exceptions
- Email service exceptions

### Example Logs

```
[INF] Alert rule created: High Value Theft Alert (ID: 1) by user abc123
[INF] Incident created with ID 456
[INF] Found 2 matching alert rules for incident 456
[INF] Triggering alert rule: High Value Theft Alert (ID: 1) for incident 456
[INF] Alert email sent successfully for rule High Value Theft Alert to 3 recipients
[INF] Alert rule activated: Regional Violence Alert (ID: 2) by user xyz789
```

---

This completes the Alert Rules API documentation. The system is production-ready and fully integrated with the incident management system.
