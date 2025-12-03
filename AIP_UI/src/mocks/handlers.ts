// Mock Service Worker Handlers
// Only mock what's necessary for development - all incidents use real backend

export const handlers = [
	// ============================================================================
	// MSW Configuration - ALL INCIDENTS USE REAL BACKEND
	// ============================================================================
	// 
	// ALL PASS-THROUGH (Real Backend):
	//   - GET /api/incidents → Fetches real incidents from database
	//   - GET /api/incidents/:id → Fetches real incident details
	//   - POST /api/incidents → Creates in database, TRIGGERS ALERT RULES ✅
	//   - PUT /api/incidents/:id → Updates in database, TRIGGERS ALERT RULES ✅
	//   - DELETE /api/incidents/:id → Deletes from database
	//   - /api/site → Fetches real sites
	//   - /api/region → Fetches real regions
	//   - /api/alertrules → All alert rule operations
	// 
	// NO MOCKING: All incident operations use real backend and database
	// ============================================================================
	
	// All handlers removed - everything passes through to real backend
]
