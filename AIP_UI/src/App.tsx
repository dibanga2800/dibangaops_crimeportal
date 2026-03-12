import React from 'react'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { ReactQueryDevtools } from '@tanstack/react-query-devtools'
import { Provider } from 'react-redux'
import { store } from './store/store'
import { ThemeProvider } from './components/theme-provider'
import { Toaster } from './components/ui/toaster'
import { ToastContainer } from 'react-toastify'
import 'react-toastify/dist/ReactToastify.css'
import { AuthProvider } from './contexts/AuthContext'
import { CustomerSelectionProvider } from './contexts/CustomerSelectionContext'
import { RouterProvider } from 'react-router-dom'
import router from './routes'
import { AppErrorBoundary } from './components/error-boundary/AppErrorBoundary'

// Create a client with production-ready defaults
const queryClient = new QueryClient({
	defaultOptions: {
		queries: {
			retry: import.meta.env.PROD ? 3 : 1,
			refetchOnWindowFocus: import.meta.env.PROD,
			staleTime: 1000 * 60 * 5, // 5 minutes
		},
		mutations: {
			retry: import.meta.env.PROD ? 2 : 0,
		},
	},
})

const App = () => {
	return (
		<Provider store={store}>
			<AppErrorBoundary>
				<QueryClientProvider client={queryClient}>
					<ThemeProvider defaultTheme="system" storageKey="aip-theme">
						<AuthProvider>
							<CustomerSelectionProvider>
								<RouterProvider router={router} />
							</CustomerSelectionProvider>
						</AuthProvider>
						<Toaster />
						<ToastContainer />
					</ThemeProvider>
					{import.meta.env.DEV && <ReactQueryDevtools initialIsOpen={false} />}
				</QueryClientProvider>
			</AppErrorBoundary>
		</Provider>
	)
}

export default App
