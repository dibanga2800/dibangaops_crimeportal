# Documentation Directory

This folder contains documentation for the AIP Frontend application.

## Quick Links

### Production Deployment

- **[Production Checklist](PRODUCTION_CHECKLIST.md)** — Pre-deployment checklist
- **[Build Instructions](BUILD_INSTRUCTIONS.md)** — Build commands and environment configuration

### Optimization

- **[Bundle Optimization](BUNDLE_OPTIMIZATION_SUMMARY.md)** — Route lazy loading improvements
- **[Image Optimization](IMAGE_OPTIMIZATION_GUIDE.md)** — Image compression and best practices

### Archive

- **[archive/](archive/)** — Historical change logs (reference only)

## Quick Start

### For Production Deployment

1. Follow [Production Checklist](PRODUCTION_CHECKLIST.md)
2. Build using [Build Instructions](BUILD_INSTRUCTIONS.md)

### For Development

See the main [README.md](../README.md) in the project root. Run the .NET API and `npm run dev` in `AIP_UI`; all data is loaded from the backend.

## Documentation Structure

```
AIP_UI/
├── README.md
└── docs/
    ├── README.md
    ├── BUILD_INSTRUCTIONS.md
    ├── PRODUCTION_CHECKLIST.md
    ├── BUNDLE_OPTIMIZATION_SUMMARY.md
    ├── IMAGE_OPTIMIZATION_GUIDE.md
    └── archive/
```

## Common Tasks

### Deploy to Production

1. Create `.env.production` (see [Build Instructions](BUILD_INSTRUCTIONS.md))
2. Run `npm run build:production`
3. Deploy the `dist/` folder

### Optimize Bundle

See [Bundle Optimization](BUNDLE_OPTIMIZATION_SUMMARY.md).
