# DibangOps Crime Portal™ System Architecture

## 1. System Context

```
┌─────────────────────────────────────────────────────────────────┐
│                     DibangOps Crime Portal™ Platform                         │
│                                                                 │
│  ┌──────────────┐         ┌──────────────────┐                  │
│  │  React/Vite  │◄───────►│  .NET 8 Web API  │                  │
│  │   Frontend   │  HTTPS  │    (Backend)      │                  │
│  │  (Vercel)    │  JSON   │    (IIS/Kestrel)  │                  │
│  └──────────────┘         └────────┬─────────┘                  │
│                                    │                            │
│                           ┌────────▼─────────┐                  │
│                           │   SQL Server DB   │                  │
│                           │  (EF Core / MSSQL)│                  │
│                           └──────────────────┘                  │
│                                                                 │
│  External Services:                                             │
│  ┌───────────┐ ┌───────────┐ ┌──────────────┐                  │
│  │ SMTP/Graph │ │ Azure Blob│ │ AI Service   │                  │
│  │  (Email)   │ │ (Files)   │ │ (Extension)  │                  │
│  └───────────┘ └───────────┘ └──────────────┘                  │
└─────────────────────────────────────────────────────────────────┘
```

## 2. Frontend Architecture (React / Vite / TypeScript)

### Tech Stack
- **Framework**: React 18 + Vite
- **Language**: TypeScript (strict)
- **Styling**: Tailwind CSS + Shadcn/Radix UI
- **Routing**: React Router v6
- **State**: React Context (Auth, PageAccess, CustomerSelection) + Redux (users)
- **HTTP**: Axios (global interceptors) + React Query
- **Validation**: Zod
- **Charts**: Recharts
- **Hosting**: Vercel

### Domain Modules

| Module | Components | Services |
|--------|-----------|----------|
| **Auth** | LoginPage, ProtectedRoute | AuthContext, sessionStore, auth.ts |
| **Incidents** | IncidentForm, IncidentReportPage | incidentsApi, incidentService |
| **Dashboards** | Admin, Officer, Customer | analyticsService, dashboardService |
| **Analytics** | DataAnalyticsHub | analyticsService (mock/real) |
| **Alerts** | LPMAlertRuleForm, StoreAlertRuleForm | Direct fetch to API |
| **Evidence** | BarcodeTestGenerator | productService |
| **Administration** | UserSetup, EmployeeRegistration, Settings | userService, usersSlice |
| **Customer** | CustomerSetup, CustomerDetail | customerService, regionService |

### Auth Flow
1. User submits credentials → `POST /Auth/login`
2. JWT access token + user stored in `sessionStore` (localStorage)
3. Axios interceptor attaches `Authorization: Bearer {token}` to every request
4. `ProtectedRoute` checks user existence and role match
5. `PageAccessContext` enforces page-level access per role
6. 401 from `/Auth/me` or `/Auth/refresh` → session cleared, redirect to `/login`

### Role Hierarchy

| Role | Display Name | Dashboard | Access Level |
|------|-------------|-----------|-------------|
| `administrator` | Admin | AdminDashboard | Full platform access — user/employee/customer setup, settings, page access, lookup tables, product import, all operations |
| `manager` | Manager | AdminDashboard | Operations management — analytics, alert rules, incident review, employee management, crime intelligence |
| `store` | Store User | StoreDashboard | Store-level operations — incident reporting, incident graph, basic operational pages |

### Authorization Policies

| Policy | Required Roles | Used For |
|--------|---------------|----------|
| `AdminOnly` | `administrator` | System settings, page access config, lookup tables, product import, customer CRUD |
| `ManagerAndAbove` | `administrator`, `manager` | User/employee management, customer assignments |
| `AllRoles` | `administrator`, `manager`, `store` | General authenticated access |

### Customer-Linked vs Platform Users

Users can be either **platform users** (manage multiple customers via `AssignedCustomerIds`) or **customer-linked users** (directly bound to one customer via `CustomerId`). This is determined by data, not role name — any role can be either type.


## 3. Backend Architecture (.NET 8 / EF Core)

### Tech Stack
- **Framework**: ASP.NET Core 8 Web API
- **ORM**: Entity Framework Core (SQL Server)
- **Identity**: ASP.NET Core Identity (JWT Bearer)
- **Email**: SMTP or Microsoft Graph
- **Storage**: Azure Blob Storage
- **Hosting**: IIS / Kestrel

### Layer Architecture
```
Controllers (API surface)
    │
    ▼
Services (Business logic, validation, orchestration)
    │
    ▼
Repositories (Data access, EF Core queries)
    │
    ▼
DbContext → SQL Server
```

### Core Domain Entities

| Entity | Key Fields | Relationships |
|--------|-----------|---------------|
| **Customer** | CustomerId, CompanyName, CompanyNumber | → Regions, Sites, Incidents |
| **Region** | RegionID, fkCustomerID, RegionName | → Customer, Sites |
| **Site** | SiteID, fkCustomerID, fkRegionID, LocationName | → Customer, Region |
| **Incident** | IncidentId, CustomerId, SiteId, IncidentType, Status | → Customer, StolenItems |
| **StolenItem** | StolenItemId, IncidentId, Barcode, ProductName | → Incident |
| **AlertRule** | AlertRuleId, RuleType, Keywords, TriggerCondition | → Customer, Site, Region |
| **ApplicationUser** | Identity user + Role (admin/manager/store), CustomerId, PageAccessRole | → CustomerAssignments |

### Organisation Hierarchy

> **Note:** The backend data model uses `Customer` internally. The UI displays this as "Company" to users.

```
Company (Organisation) [DB: Customer]
  └── Region (geographical area)
       └── Site (physical location)
            └── Incidents (events at this site)
                 └── StolenItems (evidence items with barcodes)
```

### Incident Lifecycle
1. **Create**: Officer fills IncidentForm → `POST /api/incidents` → validation → persist → alert check
2. **Alert Check**: `AlertRuleService.CheckIncidentForAlertsAsync` evaluates active rules against incident
3. **Update**: `PUT /api/incidents/{id}` → update fields → re-check alerts
4. **Query**: Paginated, filtered by customer/site/region/date/type/status
5. **Analytics**: Dashboard aggregates incidents by time, type, value, officer

### Alert Rule Engine (Current)
- **Rule Types**: Store (site-level) and LPM (region-level)
- **Matching**: Incident type match + keyword search in description/details
- **Trigger Conditions**: any | all | exact-match
- **Channels**: in-app (trigger count) | email (HTML notification)
- **Lifecycle**: Create → Active/Inactive → Triggered → Count incremented

### Security Model
- JWT with role-based claims (role, customerId, assignedCustomerIds)
- Authorization policies: AdminOnly, ManagerAndAbove, AllRoles
- `UserContextService` resolves current user's customer scope via `CustomerId` or `AssignedCustomerIds`
- Incident queries scoped by customer for non-admin users
- Soft delete pattern across entities

## 4. Data Flow Diagram

```
User (Browser)
    │
    ├── Login ──────────► AuthController ──► JwtService ──► JWT Token
    │
    ├── Create Incident ► IncidentController ──► IncidentService
    │                                               │
    │                                               ├──► IncidentRepository (persist)
    │                                               └──► AlertRuleService (check rules)
    │                                                        │
    │                                                        └──► EmailService (notify)
    │
    ├── View Dashboard ──► Dashboard loads ──► incidentsApi + analyticsService
    │
    └── Manage Alerts ──► AlertRuleController ──► AlertRuleService ──► AlertRuleRepository
```

## 5. Deployment Architecture

| Environment | Frontend | Backend | Database |
|------------|----------|---------|----------|
| **Development** | localhost:5173 | localhost:5128 | Local SQL Server |
| **Production** | Vercel (coop-aip-ui.vercel.app) | IIS (dedicated server) | Production SQL Server |

### CORS Policy
- Development: localhost origins
- Production: Vercel domains + configurable `FrontendUrl`

## 6. Current Capability Gaps (Pre-Enhancement)

| Capability | Current State | Gap |
|-----------|--------------|-----|
| AI Classification | None | No AI service, no classification interface |
| Evidence Chain | StolenItem has Barcode field | No custody chain, no evidence timeline |
| Alert Escalation | Basic trigger + email | No escalation paths, no acknowledgment flow |
| Multi-Org Deployment | CustomerId on entities | No formal tenancy abstraction, no org provisioning |
| Analytics Intelligence | Mock data + basic aggregation | No AI-driven insights, no predictive analytics |
| Secure Data Models | Basic role-based access | No row-level security abstraction, limited field-level filtering |

---

*DibangOps Crime Portal™ — AI-Driven Enterprise Security Intelligence Platform*
*Founder & Lead Architect: David Ibanga*
