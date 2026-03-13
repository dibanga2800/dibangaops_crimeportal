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

- **Computer vision offender intelligence**
  - Offender recognition API (`OffenderRecognitionController`) with an `index-and-match` endpoint for image‑based search.
  - Incident form **Offender Details** section supports:
    - **Search by details** (name, DOB, marks) for repeat‑offender detection.
    - **Search by image** – passes an image URL or captured verification image to the offender recognition service and reuses the repeat‑offender matches UI.
  - Designed to plug into a real CV provider (Azure AI Vision, custom service, etc.) without changing the UI.


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
  - `OffenderRecognitionController` for offender image indexing and matching.

---

## Getting started

### Prerequisites

- Node.js **18+**
- .NET SDK (matching backend project version)
- MSSQL Server (local or remote)
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
2. Apply migrations:

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

### Production build (frontend)

```bash
cd AIP_UI
npm run build
npm run preview   # optional: serve built assets locally
```

Build output is written to `AIP_UI/dist`.

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
├── Services/            # Business logic & login protection
├── Data/                # DbContext & migrations
├── Repositories/        # Data access abstractions
└── Models/DTOs/         # API DTOs
```

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

2. **Start the Backend (when ready):**
```bash
cd AIP_Backend
dotnet run
```

The backend API will run on `http://localhost:5128` (or configured port)

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
├── Services/          # Business logic
├── Data/              # Database context and migrations
└── DTOs/              # Data transfer objects
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

---

**Note:** This is an active development project. Some features may be in progress or subject to change.
