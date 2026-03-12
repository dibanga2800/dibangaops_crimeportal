import React from 'react'
import ReactDOM from 'react-dom/client'
import App from './App'
import './index.css'
import { Provider } from 'react-redux'
import { store } from './store/store'
import { initWebVitals } from './utils/vitals'
import { logger } from './utils/logger'

// Initialize performance monitoring
if (import.meta.env.PROD || import.meta.env.VITE_ENABLE_ANALYTICS === 'true') {
	initWebVitals((metric) => {
		// In production, you would send this to your analytics service
		// Example: analytics.send('web-vitals', metric)
		logger.info('Web Vital', {
			name: metric.name,
			value: metric.value,
			rating: metric.rating,
		})
	})
}

// Render the app
// Note: Using static mock data from src/data/ - no MSW service worker needed
ReactDOM.createRoot(document.getElementById('root')!).render(
	<React.StrictMode>
		<Provider store={store}>
			<App />
		</Provider>
	</React.StrictMode>
)
