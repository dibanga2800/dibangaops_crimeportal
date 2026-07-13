# DibangOps Crime Portal™

## Overview

DibangOps Crime Portal™ is a production-grade, API-driven security incident management and crime intelligence platform designed for multi-organisation retail environments.

The platform was demonstrated to two UK retail co-operatives — **Heart of England Co-operative** and **Central England Co-operative** — during early-stage evaluation. Heart of England moved quickly to production and is currently live across 40+ store locations, using the system for real-time incident reporting, intelligence sharing, and data-driven decision-making. Central England Co-operative remains interested but has paused its adoption decision pending an internal organisational merger, with no confirmed timeline to proceed.

This platform was fully designed and developed by me as the sole technical lead and engineer.

## Problem Statement

Prior to this system, incident management within retail security environments faced several challenges:

- Manual and fragmented incident reporting processes
- Lack of centralised visibility across multiple store locations
- Inconsistent classification of incidents
- Limited ability to track repeat offenders
- No real-time alerting or intelligence sharing
- Minimal analytics for operational decision-making

These limitations significantly reduced response efficiency and prevented organisations from leveraging data for proactive security management.

## Solution

To address these challenges, I designed and built a centralised, scalable platform that provides:

- Real-time incident reporting across multiple locations
- AI-assisted incident classification and decision support
- Barcode-driven product and evidence tracking, paired with biometric offender recognition
- Cross-organisation intelligence sharing
- Role-based dashboards and operational workflows
- Advanced analytics and reporting capabilities

## Core Capabilities

### 🔹 Incident Management
- Structured incident reporting with configurable workflows
- Centralised data storage and retrieval
- Role-based access control per organisation/store

### 🔹 AI-Assisted Intelligence (Innovation)
- Integration with Azure OpenAI for incident classification
- Automatic suggestion of:
  - Incident categories
  - Risk levels
  - Recommended actions
  - Confidence scoring with rule-based fallback mechanisms

This significantly improves classification accuracy and reduces manual workload.

### 🔹 Barcode & Biometric Intelligence (Innovation)
- EAN barcode scanning links stolen items to a central product catalog, enabling hot-product analytics and cross-incident correlation
- Barcode-tracked evidence chain of custody for physical items seized during incidents
- Offender identification and repeat-offender linking via structured identity data, text search, and biometric face recognition (Azure Face API / InsightFace) — not barcode assignment to people
- Together these provide a linked intelligence trail connecting products, evidence, and offenders across locations

### 🔹 Real-Time Alerts & Notifications
- Immediate alert generation for high-risk incidents
- Supports rapid response and escalation

### 🔹 Analytics & Insights
- Drill-down dashboards by:
  - Store
  - Region
  - Time period
- Identification of trends and high-risk patterns
- Supports data-driven operational decisions

## Architecture

![Architecture Diagram](https://github.com/dibanga2800/dibangaops_crimeportal/blob/main/docs/ARCHITECTURE.md)

The platform follows a modern distributed architecture:

| Component | Technology |
|-----------|-----------|
| **Frontend** | React + Vite + TypeScript (SPA) |
| **Backend** | .NET API services (C#) |
| **Database** | Microsoft SQL Server |
| **Infrastructure** | Microsoft Azure (Container Apps, App Services) |
| **Monitoring** | Application Insights |
| **CI/CD** | GitHub Actions pipelines |

## Technical Contribution (My Role)

I was the sole developer and technical lead responsible for the full lifecycle of the system, including:

- Designing the overall system architecture
- Developing backend APIs using .NET (C#)
- Building the frontend application using React + TypeScript
- Designing and implementing the SQL Server database schema
- Integrating AI capabilities using Azure OpenAI
- Implementing barcode tracking logic and workflows
- Setting up CI/CD pipelines using GitHub Actions
- Deploying and managing infrastructure on Microsoft Azure
- Implementing monitoring and logging (Application Insights)

## Impact

### Before Implementation
- Manual, inconsistent incident reporting
- No shared intelligence between stores
- Limited visibility into trends and repeat offenders
- Slow response times

### After Implementation
- Real-time reporting across 40+ stores (Heart of England Co-operative)
- Centralised intelligence across a multi-tenant platform
- AI-assisted classification improving operational efficiency
- Enhanced tracking of repeat incidents and offenders
- Data-driven insights enabling proactive security management

## Evidence of Real-World Use

The platform is live in production at:

- **Heart of England Co-operative** — 40+ retail store locations, full production use

It was also demonstrated to **Central England Co-operative**, which remains interested but has paused its adoption decision pending an internal organisational merger.

## Repository Structure
/AIP_Backend – Backend API services (.NET)
/AIP_UI – Frontend application (React + TypeScript)
/Infrastructure – Infrastructure as Code and deployment configs
/.github/workflows – CI/CD pipelines
/docs – Architecture diagrams and supporting documentation

### Operations Runbooks

- [Azure Subscription Reactivation Runbook](./docs/AZURE_SUBSCRIPTION_REACTIVATION_RUNBOOK.md)

## Screenshots
Dashboard view
Incident reporting interface
Analytics and reporting dashboards
Alerts and notification system


| Description | Link |
|-------------|------|
| Dashboard view | [View](./Dashboard.jpg) |
| Incident reporting interface | [View](./impact%20metrics.webp) |
| Analytics and reporting dashboards | [View](./analytics.webp)) |
| Alerts and notification system | [View](./alerts.png) |

## Innovation Summary

This platform goes beyond traditional incident reporting tools by introducing:

- **AI-assisted decision-making** using Azure OpenAI, with a deterministic rule-based fallback ensuring every incident is classified
- **Barcode-driven product and evidence intelligence**, paired with biometric offender recognition across locations
- **Multi-organisation data sharing** architecture
- **Real-time operational analytics**

These features collectively enable a modern, intelligent security operations platform rather than a basic reporting system.

## License

MIT License
