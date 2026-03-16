## Crime Portal

Crime Portal is a modern, API‑driven **security management and crime intelligence platform** for Heart of England Co‑operative and Central England Co‑operative retail partners.  
It centralises incident reporting, analytics, operational workflows, and customer‑specific dashboards into a single, role‑aware web application.

The frontend is a **React + Vite + TypeScript** SPA, backed by a **.NET + MSSQL** API.

---

## Core capabilities

- **AI‑assisted incident & operations**
  - Rich incident reporting with configurable workflows.
  - **AI‑assisted incident classification** (Azure OpenAI + rule‑based fallback) with suggested category, risk level, confidence, and actions.
  - Operations dashboard for incident volumes, value recovered, and priority cases, including **AI insight badges** on incident tables.
  - Customer‑specific incident views (per society / company).

- **AI analytics hub (mandatory for managers)**
  - Central analytics hub with:
    - Store heatmaps and high‑risk locations.
    - Hot products and loss trends.
    - **AI‑driven store risk scores** (per store/day) exposed via the `AiAnalytics` API and surfaced in the UI.
    - **Pattern summaries** (hot locations, trend lines, category breakdowns) from the AI analytics service layer.
  - Always available to administrator and manager roles; controlled via the Page Access settings.

- **Role‑based navigation & page access**
  - Roles: **administrator**, **manager**, **security‑officer**, **store**.
  - Central `Settings → Configure User Role Access` matrix:
    - Admins can grant/revoke pages per role (except admin, which is always full access).
    - Manager access to **Data Analytics Hub** is enforced and cannot be disabled.
  - Sidebar and route protection both respect these settings.

- **Authentication, sessions, and security**
  - JWT **access tokens** with **refresh tokens** for seamless re‑authentication.
  - Front‑end **idle timeout** (e.g. 10 minutes) with automatic logout to protect unattended sessions.
  - Backend **brute‑force protection**:
    - Per‑user lockout and per‑IP throttling on login / 2FA / refresh endpoints.
    - Friendly UX messaging on the frontend.
  - Protected routes and central Axios interceptors with refresh‑on‑401 logic.

- **Alerts & notifications**
  - Alert rules and alert instances for operations teams.
  - Notification bell in the header:
    - Shows live alert counts (polling + event‑driven updates).
    - Routes to the dashboard instead of any removed calendar view.

-- **Customer & administration**
  - User, employee, and company setup modules for administrators.
  - Customer‑specific dashboards and crime intelligence views.
  - Role‑aware page access configuration so AI views (e.g. Analytics Hub, risk cards) are reserved for appropriate roles.

- **Facial recognition & offender intelligence**
  - **InsightFace** (open-source) powers face detection and recognition: guided capture (red/green oval), auto-capture when face is in frame, and search-by-image against previously stored offender faces. No cloud approval required; runs as a local Python sidecar.
  - Optional **Azure Face API** can be used instead by setting `InsightFace:Enabled = false` and configuring `AzureFace` in appsettings.
  - Offender recognition API (`OffenderRecognitionController`): `detect-only`, `search-by-image`, `index-and-match`, and `reindex` for backfilling verification evidence.
  - Incident form **Offender Details**: **Search by details** (name, DOB, marks) and **Search by image** (camera capture or verification image) with repeat-offender match results.


---

## Tech stack

### Frontend

- **React** + **TypeScript**
- **Vite** (dev server & bundler)
- **TailwindCSS** (utility‑first styling)
- **Radix UI** + **shadcn/ui** (accessible primitives & components)
- **TanStack Query** (server state & caching)
- **Zod** + **React Hook Form** (form validation where required)
- **Recharts** (charts for dashboards and analytics)

### Backend

- **ASP.NET Core** Web API
- **Entity Framework Core** (MSSQL)
- **MSSQL Server**
- Built‑in Identity + custom login protection service
- **AI services layer**:
  - `IIncidentClassifier` with `AzureOpenAiIncidentClassifier` (Azure OpenAI + rule‑based classifier).
  - `AiAnalyticsController` for AI pattern summaries and store risk scores.
  - **Facial recognition**: `IOffenderRecognitionService` with **InsightFace** (default) or Azure Face API. InsightFace runs as a Python REST service (`AIP_Backend/InsightFaceService`) exposing `/detect` and `/embed`; the .NET backend stores embeddings in SQL and performs similarity search.

---

## Getting started

### Prerequisites

- Node.js **18+**
- .NET SDK (matching backend project version)
- MSSQL Server (local or remote)
- **Python 3.8+** (for InsightFace facial recognition service when `InsightFace:Enabled` is true)
- `npm` (or `pnpm`/`yarn` if you prefer)

### Clone

```bash
git clone https://github.com/dibanga2800/crime-portal-ai.git
cd COOP_AIP
```

### Install frontend

```bash
cd AIP_UI
npm install
```

### Install backend

```bash
cd AIP_Backend
dotnet restore
```

---

## Configuration

### Frontend (`AIP_UI`)

1. Copy environment file:

```bash
cd AIP_UI
cp .env.example .env
```

2. Key variables:

- `VITE_API_BASE_URL` – backend API base URL, e.g. `http://localhost:5128/api`
- `VITE_APP_ENV` – `development` | `staging` | `production`

### Backend (`AIP_Backend`)

1. Configure MSSQL connection string in `appsettings.Development.json` / `appsettings.json`.
2. **Facial recognition (InsightFace)** – when `InsightFace:Enabled` is true (default in Development):
   - `InsightFace:BaseUrl` – URL of the InsightFace Python service (e.g. `http://localhost:8000`).
   - See [InsightFace service setup](#insightface-facial-recognition-service) below to run the Python sidecar.
3. Apply migrations:

```bash
cd AIP_Backend
dotnet ef database update
```

---

## Running the application

### Development

Frontend:

```bash
cd AIP_UI
npm run dev
```

Runs at `http://localhost:5173` by default.

Backend:

```bash
cd AIP_Backend
dotnet run
```

Runs at `http://localhost:5128` (or configured port).

**InsightFace (facial recognition):** If using InsightFace, start the Python service first so the backend can call it:

```bash
cd AIP_Backend/InsightFaceService
python -m venv .venv
.venv\Scripts\activate    # Windows
pip install -r requirements.txt
uvicorn main:app --host 0.0.0.0 --port 8000
```

Leave it running; the .NET backend will use `http://localhost:8000` when `InsightFace:Enabled` is true. See [AIP_Backend/InsightFaceService/README.md](AIP_Backend/InsightFaceService/README.md) for details.

### Production build (frontend)

```bash
cd AIP_UI
npm run build
npm run preview   # optional: serve built assets locally
```

Build output is written to `AIP_UI/dist`.

---

## Hosting the backend (with InsightFace)

The backend has two runtime parts when facial recognition is enabled:

| Component | Role |
|-----------|------|
| **.NET API** (`AIP_Backend`) | Main API (IIS, Kestrel, Azure App Service, etc.). Calls InsightFace over HTTP. |
| **InsightFace service** (Python) | Face detection and embedding. Must be reachable at the URL set in `InsightFace:BaseUrl`. |

### Option 1: Same server (recommended for single-box deployment)

Run both on one machine so the .NET backend can call InsightFace on localhost. **If you publish the .NET backend and host on IIS**, see **[AIP_Backend/deployment/PUBLISH_TO_SERVER_GUIDE.md](AIP_Backend/deployment/PUBLISH_TO_SERVER_GUIDE.md#insightface-on-the-same-iis-server)** for step-by-step instructions to run InsightFace on the same Windows server and configure production.

1. **.NET backend** – Host as usual (IIS, Kestrel, or reverse proxy). Example with Kestrel:
   ```bash
   cd AIP_Backend
   dotnet publish -c Release -o ./publish
   dotnet ./publish/AIP_Backend.dll --urls "http://0.0.0.0:5128"
   ```

2. **InsightFace service** – Run as a separate process (same server). Use a process manager (e.g. Windows Service, NSSM, or systemd on Linux) so it restarts on failure:
   ```bash
   cd AIP_Backend/InsightFaceService
   python -m venv .venv
   .venv\Scripts\activate   # Windows
   pip install -r requirements.txt
   uvicorn main:app --host 0.0.0.0 --port 8000
   ```

3. **Config** – In production config (e.g. `appsettings.Production.json` or environment), set:
   - `InsightFace:Enabled`: `true`
   - `InsightFace:BaseUrl`: `http://127.0.0.1:8000` (or `http://localhost:8000` if both run on the same host)

The API and InsightFace do not need to be exposed to the internet; only the .NET API needs to be public. Keep the InsightFace port (8000) bound to localhost only if possible.

### Option 2: Separate hosts

- Deploy the .NET API on one server (e.g. Azure App Service, VM, container).
- Deploy the InsightFace app on another server or container (e.g. another VM, Azure Container Apps, or Kubernetes pod).
- Set `InsightFace:BaseUrl` to the InsightFace service URL (e.g. `https://insightface.yourdomain.com` or `http://insightface-internal:8000`). Ensure the .NET host can reach that URL (network/firewall and TLS if over the internet).

### Option 3: Docker Compose (both services)

A `docker-compose.yml` at the repo root runs the .NET API and InsightFace in separate containers. The backend is configured to use `http://insightface:8000`. See [Deploy with Docker Compose](#deploy-with-docker-compose) below.

### Option 4: Disable InsightFace

To host only the .NET backend and not run Python:

- Set `InsightFace:Enabled` to `false` in production config.
- Configure the **Azure Face** section if you use Azure for face recognition instead. Face search will then use Azure; no InsightFace process is required.

### Production checklist

- [ ] InsightFace process is running and reachable at `InsightFace:BaseUrl` (or InsightFace is disabled).
- [ ] `InsightFace:BaseUrl` uses `http://127.0.0.1:8000` when both run on the same machine, or the correct URL when InsightFace is on another host.
- [ ] Firewall allows the .NET host to call the InsightFace URL; port 8000 does not need to be public if InsightFace is on the same server.
- [ ] If you use a reverse proxy (e.g. nginx) in front of both, ensure the backend is configured with the internal URL to InsightFace (e.g. `http://127.0.0.1:8000`), not the public URL.

---

## Deploy with Docker Compose

You can run the .NET API and the InsightFace service together using Docker Compose. The backend is configured to call InsightFace at `http://insightface:8000`.

**Prerequisites:** Docker and Docker Compose installed.

1. **Database:** The backend needs a MSSQL database. Set `ConnectionStrings__DefaultDbConnection` for the `backend` service (e.g. in a `.env` file or in `docker-compose.yml` under `backend.environment`). For a database on the host machine, use `host.docker.internal` as the server (e.g. `Server=host.docker.internal,1433;...`).

2. **Build and run:**
   ```bash
   docker compose up -d --build
   ```

3. The API is exposed on port **5128**; InsightFace is on **8000** (optional to expose; the backend reaches it internally). Override ports in `docker-compose.yml` if needed.

Files: `docker-compose.yml` (repo root), `AIP_Backend/Dockerfile`, `AIP_Backend/InsightFaceService/Dockerfile`.

---

## System architecture

At a high level the system is a **two-tier web application** with an opinionated AI services layer inside the backend:

```text
Browser (React + Vite SPA)
    │
    │  HTTPS / JSON (JWT-authenticated)
    ▼
ASP.NET Core API (AIP_Backend)
    ├─ Controllers
    │    ├─ Auth, Users, Incidents, Analytics
    │    └─ AI endpoints (classification, analytics, offender recognition, evidence)
    ├─ Services
    │    ├─ Domain services (incidents, alerts, users, products, reports)
    │    ├─ AI services
    │    │    ├─ IIncidentClassifier → AzureOpenAiIncidentClassifier + RuleBasedIncidentClassifier
    │    │    ├─ IIncidentAnalyticsService / IRiskScoringService / IIncidentPatternService
    │    │    └─ IOffenderRecognitionService (computer-vision offender matching)
    │    └─ Infrastructure (email, login protection, seeding, context)
+    ├─ Data
    │    ├─ ApplicationDbContext (EF Core)
    │    ├─ Migrations
    │    └─ Repositories for complex queries
    └─ MSSQL Database
         ├─ Core tables (Incidents, Products, Users, Sites, Regions, etc.)
         ├─ AI tables (StoreRiskScore, FaceEmbedding, AlertInstances, EvidenceItems)
         └─ Lookup tables and audit fields
```

- **Frontend (`AIP_UI`)**
  - React + TypeScript SPA built with Vite.
  - Uses Axios + a typed API/config layer for all calls into `AIP_Backend`.
  - Auth handled via `AuthContext` + `sessionStore` with JWTs and refresh tokens.
  - Role- and page-based access enforced by `PageAccessContext`, `ProtectedRoute`, and server-side policies.
  - AI outputs are surfaced via:
    - AI badges (incident classification + risk) on dashboards/incident tables.
    - AI Risk Engine and analytics widgets in the Data Analytics Hub.
    - Visual offender search and repeat-offender intelligence inside the Incident form.

- **Backend (`AIP_Backend`)**
  - ASP.NET Core Web API with EF Core + MSSQL.
  - Clean separation between:
    - Controllers (HTTP boundary).
    - Services (business logic and AI orchestration).
    - Data layer (DbContext, repositories, migrations).
  - AI service layer:
    - `AzureOpenAiClient` and `AzureOpenAiIncidentClassifier` for incident classification (category, risk, confidence, actions).
    - `IncidentAnalyticsService` / `RiskScoringService` / `IncidentPatternService` for pattern detection and store risk scores.
    - **InsightFace** (Python REST service) for face detection and embedding; `InsightFaceOffenderRecognitionService` stores embeddings in SQL and performs cosine-similarity search. Optional Azure Face API can be used instead via config.
  - All configuration (DB, email, AI providers, IIS/hosting) is driven via `appsettings.*.json` and environment variables.

---

## Project structure (frontend)

```text
AIP_UI/
├── src/
│   ├── components/      # Reusable React components (UI, layout, features)
│   ├── pages/           # Route-level pages (Dashboard, Settings, etc.)
│   ├── api/             # Page access + other API helpers
│   ├── services/        # Feature-specific service layers
│   ├── config/          # Axios, navigation, environment config
│   ├── contexts/        # Auth, page access, customer selection, etc.
│   ├── hooks/           # Custom hooks (e.g. alert counts, session timeout)
│   ├── state/           # Client-side stores (e.g. sessionStore)
│   ├── types/           # Shared TypeScript types
│   └── utils/           # Utilities and helpers
├── public/              # Static assets
└── dist/                # Production build output
```

## Project structure (backend)

```text
AIP_Backend/
├── Controllers/         # API endpoints
├── Models/              # EF Core entities
├── Services/            # Business logic, AI services, InsightFace client
├── Data/                # DbContext & migrations
├── Repositories/        # Data access abstractions
├── Models/DTOs/         # API DTOs
└── InsightFaceService/  # Python REST API for face detection & embedding (InsightFace)
    ├── main.py          # FastAPI app: POST /detect, POST /embed
    ├── requirements.txt
    └── README.md
```

---

## InsightFace facial recognition service

Facial recognition is provided by **[InsightFace](https://github.com/deepinsight/insightface)** (open-source, state-of-the-art face detection and recognition). It runs as a separate Python service that the .NET backend calls over HTTP.

- **Endpoints:** `POST /detect` (face in image + bounding box), `POST /embed` (512-d normalized embedding for matching).
- **Flow:** When an incident is saved with verification evidence, the backend sends the image to InsightFace, gets an embedding, and stores it in `FaceEmbeddings` with `ModelId = "insightface-v1"`. When a user searches by image (camera or upload), the backend gets an embedding from InsightFace, loads stored embeddings from the DB, computes cosine similarity, and returns the best-matching offenders.
- **Config:** In `appsettings.Development.json` (or your environment), set `InsightFace:Enabled = true` and `InsightFace:BaseUrl` to the service URL (e.g. `http://localhost:8000`). To use Azure Face API instead, set `InsightFace:Enabled = false` and configure the `AzureFace` section.
- **Run:** From `AIP_Backend/InsightFaceService`, create a venv, `pip install -r requirements.txt`, then `uvicorn main:app --host 0.0.0.0 --port 8000`. Full steps are in [AIP_Backend/InsightFaceService/README.md](AIP_Backend/InsightFaceService/README.md).
- **License:** InsightFace code is MIT. For the recognition models (e.g. buffalo_l), see [InsightFace licensing](https://github.com/deepinsight/insightface#license); commercial use may require contacting the authors.

---

## NPM scripts (frontend)

- `npm run dev` – start Vite dev server.
- `npm run build` – production build.
- `npm run preview` – serve built app locally.
- `npm run lint` – run ESLint.

> The old `mock-api` / MSW setup has been removed; development now uses the real API.

---

## Security & session behaviour

- **JWT + refresh tokens** with automatic refresh in the Axios interceptor.
- **Idle session timeout** enforced on the frontend (configurable; currently 10 minutes) with automatic logout and cleanup of stored tokens.
- **Brute‑force protection**:
  - Login, 2FA, and refresh endpoints are guarded by a dedicated login protection service and integration with ASP.NET Identity lockout.
  - IIS/reverse proxy rate limiting is recommended and documented.
- All sensitive routes are wrapped in `ProtectedRoute` and page‑access checks from `PageAccessContext`.

---

## Documentation

- Build & deployment guides and additional docs live under [`docs/`](docs/).
- Backend deployment / IIS notes are in `AIP_Backend/deployment` and related markdown files.

---

## License & contact

- License: see [`LICENSE`](LICENSE).
- For questions or support:
  - Open an issue in this repository.
  - Or contact: `dibanga2800@gmail.com`.

---

This is an actively developed production application; the README reflects the current **Crime Portal** architecture and feature set (role‑based access, AI‑assisted classification, AI analytics hub, computer‑vision offender search, hardened authentication, and real API integration).

## Getting Started

### Prerequisites
- Node.js 18 or later
- .NET SDK (for backend)
- MSSQL Server (for backend)
- npm or yarn

## 📚 Documentation

**For Production Deployment:**
- 🚀 **[Production Ready Summary](PRODUCTION_READY_SUMMARY.md)** - Start here!
- 📋 [Production Checklist](docs/PRODUCTION_CHECKLIST.md)
- 🔧 [Build Instructions](docs/BUILD_INSTRUCTIONS.md)
- 🔀 [Hybrid Setup Guide](docs/HYBRID_SETUP_GUIDE.md) - Real backend + mock analytics

**All documentation:** See [`docs/`](docs/) folder

### Installation

1. **Clone the repository:**
```bash
git clone https://github.com/dibanga2800/crime-portal-ai.git
```

2. **Install Frontend Dependencies:**
```bash
cd AIP_UI
npm install
```

3. **Install Backend Dependencies:**
```bash
cd AIP_Backend
dotnet restore
```

### Configuration

#### Frontend Configuration

1. **Environment Variables:**
   - Copy `.env.example` to `.env`
   - Update environment variables as needed:

```bash
cp .env.example .env
```

Key environment variables:
- `VITE_API_BASE_URL` - Backend API URL (default: http://localhost:5128/api)
- `VITE_APP_ENV` - Environment (development, staging, production)

#### Backend Configuration

1. **Database Setup:**
   - Configure MSSQL connection string in `appsettings.json`
   - Run Entity Framework migrations:
   ```bash
   dotnet ef database update
   ```

2. **CORS Configuration:**
   - CORS is pre-configured for cross-origin requests
   - Update CORS policy in backend if needed for your domain

### Running the Application

#### Development Mode

1. **Start the Frontend:**
```bash
cd AIP_UI
npm run dev
```

The frontend will run on `http://localhost:5173` (or next available port)

2. **Start the InsightFace service** (when using facial recognition; optional):
```bash
cd AIP_Backend/InsightFaceService
python -m venv .venv && .venv\Scripts\activate
pip install -r requirements.txt
uvicorn main:app --host 0.0.0.0 --port 8000
```

3. **Start the Backend:**
```bash
cd AIP_Backend
dotnet run
```

The backend API will run on `http://localhost:5128` (or configured port). With `InsightFace:Enabled = true`, it will use the InsightFace service at `http://localhost:8000` for face detection and search.

#### Production Build

1. **Build the Frontend:**
```bash
cd AIP_UI
npm run build
```

This will:
- Type-check with TypeScript
- Build optimized production bundle
- Output to `dist` directory
- Strip console.logs and debug statements
- Generate source maps (if configured)
- Create bundle analysis report

2. **Preview Production Build:**
```bash
npm run preview
```

## Project Structure

### Frontend Structure
```
AIP_UI/
├── src/
│   ├── components/      # Reusable React components
│   ├── pages/          # Main application pages
│   ├── services/       # API service layers
│   ├── types/          # TypeScript type definitions
│   ├── config/         # Configuration files
│   ├── hooks/          # Custom React hooks
│   ├── utils/          # Utility functions
│   ├── store/          # Redux store and slices
│   ├── contexts/       # React contexts
├── public/             # Static assets
└── dist/              # Production build output
```

### Backend Structure
```
AIP_Backend/
├── Controllers/        # API endpoints
├── Models/            # Entity Framework models
├── Services/          # Business logic, AI services, InsightFace client
├── Data/              # Database context and migrations
├── Models/DTOs/       # Data transfer objects
└── InsightFaceService/  # Python face detection & embedding (InsightFace)
    ├── main.py        # FastAPI: /detect, /embed
    └── requirements.txt
```

## Available Scripts

- `npm run dev` - Start development server
- `npm run build` - Build for production
- `npm run preview` - Preview production build
- `npm run lint` - Run ESLint
- `npm run mock-api` - Start JSON Server (legacy)

## Deployment

### Vercel Deployment
The project is configured for Vercel deployment with `vercel.json`.

### Netlify Deployment
Alternative deployment configured with `netlify.toml`.

### Manual Deployment
1. Build the project: `npm run build`
2. Deploy the `dist` folder to your hosting provider
3. Ensure proper routing configuration for SPA

## Environment-Specific Configurations

### Development
- Full console logging
- React DevTools
- Hot module replacement

### Staging
- Real API endpoints
- Limited logging
- Performance monitoring

### Production
- Minified and optimized bundles
- Console logs stripped (except errors/warnings)
- Performance monitoring
- Error tracking (if configured)

## Security Features

- JWT-based authentication
- Protected routes
- CORS configuration
- Input validation with Zod
- SQL injection prevention (Entity Framework)
- XSS protection

## Performance Optimizations

- Code splitting
- Lazy loading of routes
- Image optimization
- Bundle size optimization
- Efficient re-rendering with React.memo
- Virtualized lists for large datasets
- Debounced search inputs

## Browser Support

- Chrome (latest 2 versions)
- Firefox (latest 2 versions)
- Safari (latest 2 versions)
- Edge (latest 2 versions)

## Contributing

We welcome contributions to improve AIP. Please:
1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Contact

For questions or support:
- Open an issue on GitHub
- Email: dibanga2800@gmail.com

## Acknowledgments

- Built with modern React and TypeScript
- UI components from Radix UI and Shadcn
- Backend powered by .NET and Entity Framework
- Database management with MSSQL
- Facial recognition by [InsightFace](https://github.com/deepinsight/insightface) (open-source face analysis)

---

**Note:** This is an active development project. Some features may be in progress or subject to change.
