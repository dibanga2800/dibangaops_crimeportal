**DibangaOps Crime Portal™**
**Overview**

DibangaOps Crime Portal™ is a production-grade, API-driven security incident management and crime intelligence platform designed for multi-organisation retail environments.

The system is currently used to support operations across organisations including Heart of England Co-operative and Central England Co-operative, enabling real-time incident reporting, intelligence sharing, and data-driven decision-making across multiple store locations.

This platform was fully designed and developed by me as the sole technical lead and engineer.

##Problem Statement

Prior to this system, incident management within retail security environments faced several challenges:

Manual and fragmented incident reporting processes
Lack of centralised visibility across multiple store locations
Inconsistent classification of incidents
Limited ability to track repeat offenders
No real-time alerting or intelligence sharing
Minimal analytics for operational decision-making

These limitations significantly reduced response efficiency and prevented organisations from leveraging data for proactive security management.

Solution

To address these challenges, I designed and built a centralised, scalable platform that provides:

Real-time incident reporting across multiple locations
AI-assisted incident classification and decision support
Barcode-based tracking for offender and incident linkage
Cross-organisation intelligence sharing
Role-based dashboards and operational workflows
Advanced analytics and reporting capabilities
Core Capabilities
🔹 Incident Management
Structured incident reporting with configurable workflows
Centralised data storage and retrieval
Role-based access control per organisation/store
🔹 AI-Assisted Intelligence (Innovation)
Integration with Azure OpenAI for incident classification
Automatic suggestion of:
Incident categories
Risk levels
Recommended actions
Confidence scoring with rule-based fallback mechanisms

This significantly improves classification accuracy and reduces manual workload.

🔹 Barcode-Based Tracking System (Innovation)
Unique barcode tagging of incidents and offenders
Enables tracking of repeat incidents across locations
Provides a linked intelligence trail across multiple stores
🔹 Real-Time Alerts & Notifications
Immediate alert generation for high-risk incidents
Supports rapid response and escalation
🔹 Analytics & Insights
Drill-down dashboards by:
Store
Region
Time period
Identification of trends and high-risk patterns
Supports data-driven operational decisions
Architecture

(Insert architecture diagram in /docs/architecture.png)

The platform follows a modern distributed architecture:

Frontend: React + Vite + TypeScript (SPA)
Backend: .NET API services (C#)
Database: Microsoft SQL Server
Infrastructure: Microsoft Azure (Container Apps, App Services)
Monitoring: Application Insights
CI/CD: GitHub Actions pipelines
Technical Contribution (My Role)

I was the sole developer and technical lead responsible for the full lifecycle of the system, including:

Designing the overall system architecture
Developing backend APIs using .NET (C#)
Building the frontend application using React + TypeScript
Designing and implementing the SQL Server database schema
Integrating AI capabilities using Azure OpenAI
Implementing barcode tracking logic and workflows
Setting up CI/CD pipelines using GitHub Actions
Deploying and managing infrastructure on Microsoft Azure
Implementing monitoring and logging (Application Insights)
Impact
Before Implementation
Manual, inconsistent incident reporting
No shared intelligence between stores
Limited visibility into trends and repeat offenders
Slow response times
After Implementation
Real-time reporting across 38+ stores
Centralised intelligence across multiple organisations
AI-assisted classification improving operational efficiency
Enhanced tracking of repeat incidents and offenders
Data-driven insights enabling proactive security management
Evidence of Real-World Use

The platform has been deployed and used within:

Heart of England Co-operative
Central England Co-operative

Supporting security operations across multiple retail locations.

Repository Structure
/AIP_Backend – Backend API services (.NET)
/AIP_UI – Frontend application (React + TypeScript)
/Infrastructure – Infrastructure as Code and deployment configs
/.github/workflows – CI/CD pipelines
/docs – Architecture diagrams and supporting documentation
Screenshots

(Add real screenshots in /docs folder and link here)

Dashboard view
Incident reporting interface
Analytics and reporting dashboards
Alerts and notification system
Innovation Summary

This platform goes beyond traditional incident reporting tools by introducing:

AI-assisted decision-making using Azure OpenAI
Barcode-based intelligence tracking across locations
Multi-organisation data sharing architecture
Real-time operational analytics

These features collectively enable a modern, intelligent security operations platform rather than a basic reporting system.

License

MIT License
