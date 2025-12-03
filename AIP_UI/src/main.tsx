import React from 'react'
import ReactDOM from 'react-dom/client'
import App from './App'
import './index.css'
import { Provider } from 'react-redux'
import { store } from './store/store'

// Initialize MSW in development mode
async function enableMocking() {
	if (import.meta.env.DEV) {
		const { worker } = await import('./mocks/browser')
		return worker.start({
			onUnhandledRequest: 'bypass', // Don't warn on unhandled requests
		})
	}
}

// Start MSW and then render the app
enableMocking().then(() => {
	ReactDOM.createRoot(document.getElementById('root')!).render(
		<React.StrictMode>
			<Provider store={store}>
				<App />
			</Provider>
		</React.StrictMode>
	)
})
