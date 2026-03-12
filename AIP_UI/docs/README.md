# Documentation Directory

This folder contains all documentation for the AIP Frontend application.

## 📚 Quick Links

### Production Deployment
- **[Production Ready Summary](../PRODUCTION_READY_SUMMARY.md)** - Start here! Quick overview of what's ready
- **[Production Checklist](PRODUCTION_CHECKLIST.md)** - Complete pre-deployment checklist
- **[Build Instructions](BUILD_INSTRUCTIONS.md)** - How to build for different environments

### Setup Guides
- **[Hybrid Setup Guide](HYBRID_SETUP_GUIDE.md)** - Real backend + Mock Analytics (Recommended)
- **[Demo Build Guide](DEMO_BUILD_GUIDE.md)** - Full demo with mock data

### Optimization
- **[Bundle Optimization](BUNDLE_OPTIMIZATION_SUMMARY.md)** - Route lazy loading improvements
- **[Image Optimization](IMAGE_OPTIMIZATION_GUIDE.md)** - Image compression and best practices

### Archive
- **[archive/](archive/)** - Old fix logs and change summaries (historical reference)

## 🚀 Quick Start

### For Production Deployment

1. Read: [Production Ready Summary](../PRODUCTION_READY_SUMMARY.md)
2. Follow: [Production Checklist](PRODUCTION_CHECKLIST.md)
3. Build: See [Build Instructions](BUILD_INSTRUCTIONS.md)

### For Development

See the main [README.md](../README.md) in the root directory.

## 📖 Documentation Structure

```
AIP_UI/
├── README.md                           # Main project README
├── PRODUCTION_READY_SUMMARY.md         # Quick production overview
│
└── docs/
    ├── README.md                       # This file
    ├── BUILD_INSTRUCTIONS.md           # Build commands and configuration
    ├── PRODUCTION_CHECKLIST.md         # Deployment checklist
    ├── HYBRID_SETUP_GUIDE.md           # Real backend + mock analytics
    ├── DEMO_BUILD_GUIDE.md             # Demo app setup
    ├── BUNDLE_OPTIMIZATION_SUMMARY.md  # Performance optimization
    ├── IMAGE_OPTIMIZATION_GUIDE.md     # Image optimization
    │
    └── archive/                        # Historical docs
        ├── HOLIDAY_PAGES_REMOVAL.md
        ├── API_CONNECTION_SUMMARY.md
        ├── OPERATIONS_API_*.md
        ├── INCIDENT_GRAPH_API_FIX.md
        ├── VALUE_IMPACT_*.md
        ├── CRIME_INTELLIGENCE_FIX.md
        └── DASHBOARD_IMPROVEMENTS_SUMMARY.md
```

## 🎯 Common Tasks

### Deploy to Production
1. Create `.env.production` (see [Build Instructions](BUILD_INSTRUCTIONS.md))
2. Run `npm run build:production`
3. Deploy `dist/` folder

### Build with Real Backend
See [Hybrid Setup Guide](HYBRID_SETUP_GUIDE.md) - your recommended setup!

### Optimize Bundle
Already done! See [Bundle Optimization](BUNDLE_OPTIMIZATION_SUMMARY.md)

### Deploy as Demo
See [Demo Build Guide](DEMO_BUILD_GUIDE.md)

## 🔄 Updates

- **Dec 2025**: Organized documentation structure
- **Dec 2025**: Added production ready summary
- **Dec 2025**: Bundle optimization implemented
- **Dec 2025**: Hybrid setup guide created

---

**Need help?** Start with [Production Ready Summary](../PRODUCTION_READY_SUMMARY.md)
