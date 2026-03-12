# DibangOps Multi-Organisation Tenancy Strategy

## Tenancy Model: Shared Database with Per-Row Organisation Scoping

DibangOps uses a **single shared database** with per-row `CustomerId` scoping. Every core entity carries a `CustomerId` foreign key that ties the record to a specific organisation. This approach balances operational simplicity with strong data isolation.

### Why Shared Database?

| Factor | Shared DB (chosen) | DB-per-org |
|--------|-------------------|------------|
| Deployment complexity | Low | High |
| Migration management | Single migration path | N migrations |
| Cross-org analytics | Native queries | Complex federation |
| Data isolation | Row-level | Physical |
| Cost per organisation | Marginal | Fixed per DB |

For the current scale and deployment model (single IIS server + SQL Server), shared DB is the optimal choice.

## Organisation Hierarchy

```
Organisation (Customer)
├── Regions (geographical)
│   └── Sites (physical locations)
│       ├── Incidents
│       │   ├── StolenItems
│       │   ├── EvidenceItems
│       │   │   └── CustodyEvents
│       │   └── AlertInstances
│       └── DailyActivityReports
├── AlertRules
├── Users (via UserCustomerAssignment)
└── CustomerPageAccess (UI access control)
```

## Row-Level Scoping

### Entities with Direct CustomerId

| Entity | CustomerId Column | Enforcement |
|--------|------------------|-------------|
| Incident | `CustomerId` (required) | Repository + UserContextService |
| AlertRule | `CustomerId` (nullable — global rules allowed) | Service layer |
| Site | `fkCustomerID` (required) | Repository |
| Region | `fkCustomerID` (required) | Repository |
| DailyActivityReport | `CustomerId` (required) | Service layer |
| DailyOccurrenceBook | `CustomerId` (required) | Service layer |
| CustomerPageAccess | `CustomerId` (required) | Service layer |

### Entities Scoped via Parent

| Entity | Scoped Through | Path |
|--------|---------------|------|
| StolenItem | Incident.CustomerId | Incident → StolenItems |
| EvidenceItem | Incident.CustomerId | Incident → EvidenceItems |
| EvidenceCustodyEvent | EvidenceItem.IncidentId | EvidenceItem → CustodyEvents |
| AlertInstance | AlertRule.CustomerId | AlertRule → AlertInstances |

### User-Organisation Binding

- **Customer users** (`customersitemanager`, `customerhomanager`): bound to `CustomerId` on their user record
- **Officer users** (`advantageoneofficer`, `advantageonehoofficer`): bound via `UserCustomerAssignment` table (many-to-many)
- **Administrators**: access all organisations

## Access Control Enforcement

### UserContextService
The `UserContextService` resolves the authenticated user's context from JWT claims:

- `CustomerId` — direct customer association
- `AssignedCustomerIds` — comma-separated list from `UserCustomerAssignment`
- `Role` — determines access tier

### Enforcement Points

1. **EnsureCanAccessCustomer(customerId)**: verifies the user has the right to read/write data for a given customer
2. **EnsureCanAccessRecord(customerId, createdByUserId)**: additionally verifies officer-level ownership
3. **Repository queries**: filtered by `customerId` parameter at the data layer
4. **Controller-level auth**: `[Authorize]` with role-based policies

### Missing / Recommended Enhancements

- [ ] Global query filter on `ApplicationDbContext` for `CustomerId` scoping (defense-in-depth)
- [ ] Tenant middleware that resolves `OrganisationId` early in the pipeline
- [ ] Separate audit log table for security-relevant events (login, role change, evidence access)

## New Organisation Onboarding

### Steps to Provision a New Organisation

1. **Create Customer record** via Admin UI or API: `POST /api/customer`
2. **Create Regions** for the customer: `POST /api/region`
3. **Create Sites** within regions: `POST /api/site`
4. **Assign Users** to the customer via `UserCustomerAssignment`
5. **Configure CustomerPageAccess** for UI visibility
6. **Set up AlertRules** specific to the customer's needs
7. **Verify** by logging in as a user assigned to the new customer

### Database Migrations

- All migrations are applied to the shared database
- New organisations inherit the current schema automatically
- Seed data (lookup tables, page access defaults) is applied on startup

## Environment Strategy

| Environment | Purpose | Database |
|------------|---------|----------|
| Development | Local dev + testing | Local SQL Server |
| Staging | Pre-production validation | Staging SQL Server |
| Production | Live service | Production SQL Server |

### Configuration & Secrets

- **appsettings.json** — shared defaults
- **appsettings.{Environment}.json** — environment-specific overrides
- **Environment variables** — secrets (JWT key, DB connection, email credentials)
- Per-organisation settings: stored in `LookupTable` with `Category = "OrgSettings:{CustomerId}"`

## Future Enhancements

1. **Tenant-aware caching**: cache per `CustomerId` to avoid cross-org data leaks
2. **Organisation-level feature flags**: enable/disable features per customer
3. **Custom branding per organisation**: logo, colour scheme stored in customer config
4. **Data export per organisation**: GDPR-compliant data extraction for a single customer
5. **Organisation-scoped API keys**: for system-to-system integrations

---

*DibangOps™ — AI-Driven Enterprise Security Intelligence Platform*
*Founder & Lead Architect: David Ibanga*
