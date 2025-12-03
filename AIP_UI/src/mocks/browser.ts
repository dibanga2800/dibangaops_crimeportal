import { setupWorker } from 'msw/browser'
import { handlers } from './handlers'

// This configures a Service Worker with the given request handlers.
// Note: /api/site endpoint is NOT mocked - it will pass through to the backend API
export const worker = setupWorker(...handlers)
