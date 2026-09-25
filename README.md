# DibangOps Crime Portal™

[![CodeQL](https://github.com/dibanga2800/dibangaops_crimeportal/actions/workflows/codeql.yml/badge.svg)](https://github.com/dibanga2800/dibangaops_crimeportal/actions/workflows/codeql.yml)
[![Deploy Backend](https://github.com/dibanga2800/dibangaops_crimeportal/actions/workflows/deploy-backend.yml/badge.svg)](https://github.com/dibanga2800/dibangaops_crimeportal/actions/workflows/deploy-backend.yml)
[![Deploy Frontend](https://github.com/dibanga2800/dibangaops_crimeportal/actions/workflows/deploy-frontend.yml/badge.svg)](https://github.com/dibanga2800/dibangaops_crimeportal/actions/workflows/deploy-frontend.yml)

## Overview

DibangOps Crime Portal™ is a multi-tenant security incident management and crime intelligence platform for retail loss prevention teams. It is live at [www.dibangops.com](https://www.dibangops.com).

It is in production at **Heart of England Co-operative** across all **40 of its retail stores**. 32 of those stores had logged incidents as of August 2026.

I designed and built the platform and am its principal architect and technical owner: architecture, backend, frontend, database, infrastructure, deployment and client onboarding.

## Origin and rollout

Heart of England's previous incident reporting system was unreliable. Advantage 1 Ltd, the co-operative's security services provider and my employer, committed to deliver a replacement, and I was given that brief as a Software Engineer. I conceived the architecture and built the platform myself, including capabilities that were not in the brief, such as AI-assisted classification and biometric offender recognition.

| Period | Milestone |
|--------|-----------|
| December 2025 to April 2026 | Platform designed, built and deployed to Azure |
| February 2026 | Requirements discussions with Heart of England's Loss Prevention Manager |
| April 2026 | Early build tested by the client, then a pilot in trial stores; first live incident logged on 20 April 2026 |
| May 2026 | Rollout to 38 stores across three operational areas |
| June 2026 | Two newly opened stores added, bringing the platform to all 40 stores |

## Problem

Before the platform, incident management across the Heart of England store estate had these gaps:

- Incident reporting was manual and fragmented, with no consistent audit trail
- Stores had no shared view of incidents, so cross-store patterns went unseen
- Incident classification depended on individual judgement
- Repeat offenders were hard to track across locations
- High-risk incidents were escalated ad hoc, by phone or email
- Management reporting was collated by hand

## Measured impact

| Measure | Before | After |
|---------|--------|-------|
| Time to report an incident | 20 to 30 minutes | 5 to 10 minutes |
| Stores on the platform | 0 | All 40 stores live; 32 had logged incidents (August 2026) |
| Incidents recorded | Paper and spreadsheets | 977 incidents logged since go-live on 20 April 2026 (August 2026) |
| Users | None | 48 active user accounts across store and management roles |
| Management admin | Manual collation | Several hours per week saved |
| Cross-store visibility | None | Live dashboards by store, region and period |

Before and after figures come from a signed operational impact letter from the client's Loss Prevention Manager (August 2026). Usage figures come from the production system. [`scripts/impact-metrics.sql`](./scripts/impact-metrics.sql) gives the queries used to refresh them.

## Core capabilities

### Incident management
- Structured incident reporting with configurable workflows
- Role-based access at four levels: role, organisation, page and record
- Database-driven page permissions, so access changes need no redeploy

### AI-assisted classification
- Every new incident is classified inline by Azure OpenAI, returning a category, a confidence score (0 to 1), a risk level and recommended actions
- If Azure OpenAI is disabled, times out or errors, a deterministic rule-based classifier takes over, so **every incident is still classified**
- Each incident records which classifier produced its result (`ClassificationVersion`), so AI and fallback usage can be audited

### Barcode and biometric intelligence
- EAN barcode scanning links stolen items to a central product catalogue, enabling hot-product analytics and cross-incident correlation
- Barcode-tracked chain of custody for physical evidence (register, transfer, audit trail)
- Offender identification and repeat-offender linking through structured identity data, text search and face recognition (Azure Face API, or a self-hosted InsightFace service on internal-only ingress)
- Face indexing runs in the background after an incident is saved, so reporting stays fast

### Alerts and escalation
- Configurable alert rules (keywords, incident types, region, trigger condition) are evaluated automatically against every new incident
- Alerts go through an in-app lifecycle (acknowledge, escalate, resolve) and trigger email notifications to Loss Prevention Managers
- Alert checks run in the background after the incident is saved

### Analytics
- Drill-down dashboards by store, region and time period
- Crime trends, hot products, recovery rates, offender activity, crime linking and daily per-store risk scores

## Architecture

| Component | Technology |
|-----------|-----------|
| Frontend | React 19, Vite, TypeScript (SPA) on Azure Static Web Apps |
| Backend | ASP.NET Core (.NET 10) API on Azure Container Apps |
| Face recognition | Python InsightFace service (internal-only Container App) and Azure Face API |
| Database | Azure SQL with Entity Framework Core (150+ migrations) |
| Storage | Azure Blob Storage for incident images and evidence |
| Edge | Azure Front Door, serving the SPA and `/api/*` from one origin |
| Secrets | Azure Key Vault (RBAC); no credentials in source control |
| Infrastructure | Terraform, including blue/green production environments |
| CI/CD | GitHub Actions with Azure OIDC login, CodeQL and Dependabot |
| Monitoring | Log Analytics and Container Apps diagnostics, post-deploy `/api/health` and TLS smoke tests; Application Insights can be enabled via Terraform |

Diagrams (system context, containers, AI request sequence, deployment and tenancy) and the main design decisions and trade-offs are in **[ARCHITECTURE.md](./ARCHITECTURE.md)**.

## Quality and security

- 119 backend tests (xUnit, `WebApplicationFactory` integration tests) run in the backend deploy pipeline
- Frontend tests use Vitest and Testing Library
- CodeQL scans every push and pull request; Dependabot keeps dependencies current
- Authentication uses HttpOnly cookies, CSRF validation, login protection and two-factor authentication for management roles
- Tenant isolation is enforced in the API through claim-based query filters on a shared database

## My role

I am the technical lead and have been the only developer on the project. I was responsible for:

- System architecture and the multi-tenant data model
- Backend APIs in .NET (C#) and the SQL Server schema
- The React and TypeScript frontend
- Azure OpenAI classification with a rule-based fallback, and the face recognition pipeline
- Barcode product and evidence workflows
- Terraform infrastructure, CI/CD pipelines and production deployment on Azure
- Client requirements, pilot, rollout and user onboarding for the 40 Heart of England stores

**Tooling:** I use AI-assisted development tools (including Cursor) in my day-to-day workflow. The product and architecture decisions, security model, infrastructure design, code review and client delivery are my own.

## Repository history

The first commit (December 2025) imported the application as it stood then, which is why it is a single large commit. In March 2026 this repository was merged with a working repository (`crime-portal-ai`). All development since then is visible commit by commit.

## Repository structure

| Path | Contents |
|------|----------|
| `/AIP_Backend` | ASP.NET Core API, EF Core migrations, tests, InsightFace service |
| `/AIP_UI` | React and TypeScript frontend |
| `/Infrastructure` | Terraform for Azure (Front Door, Container Apps, SQL, Key Vault, networking) |
| `/.github/workflows` | CI/CD, infrastructure plan and CodeQL pipelines |
| `/scripts` | Deployment, verification and reporting scripts |
| `ARCHITECTURE.md` | Architecture diagrams and design decisions |

## Screenshots

Staff names are blurred in the screenshots.

| Screen | Link |
|--------|------|
| Management dashboard | [View](./Dashboard.jpg) |
| Incident reports (581 incidents across 31 stores, July 2026) | [View](./impact%20metrics.webp) |
| Crime Analytics and AI Hub | [View](./analytics.jpg) |
| Alert rule configuration | [View](./alerts.png) |

## Licence

MIT Licence
