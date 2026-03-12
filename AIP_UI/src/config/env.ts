import { z } from 'zod'

/**
 * Environment variable schema validation
 * Ensures all required environment variables are present and valid
 */
const envSchema = z.object({
	// API Configuration
	VITE_API_BASE_URL: z.string().url().default('http://localhost:5128/api'),
	
	// Application Environment
	VITE_APP_ENV: z.enum(['development', 'staging', 'production']).default('development'),
	
	// Feature Flags
	VITE_ENABLE_ANALYTICS: z.string().transform(val => val === 'true').default('false'),
	VITE_ENABLE_ERROR_TRACKING: z.string().transform(val => val === 'true').default('false'),
	
	// Application Settings
	VITE_APP_NAME: z.string().default('Central Co-op Interactive Portal'),
	VITE_APP_VERSION: z.string().default('1.0.0'),
	
	// Optional: External Services
	VITE_SENTRY_DSN: z.string().optional(),
	VITE_GOOGLE_ANALYTICS_ID: z.string().optional(),
	VITE_MIXPANEL_TOKEN: z.string().optional(),
	
	// Optional: Authentication
	VITE_AUTH_DOMAIN: z.string().optional(),
	VITE_AUTH_CLIENT_ID: z.string().optional(),
	
	// Optional: Storage
	VITE_STORAGE_BUCKET: z.string().optional(),
	
	// Build Configuration
	VITE_PUBLIC_URL: z.string().default('/'),
})

type Env = z.infer<typeof envSchema>

/**
 * Validates and parses environment variables
 * @throws {Error} If required environment variables are missing or invalid
 */
function validateEnv(): Env {
	try {
		const env = {
			VITE_API_BASE_URL: import.meta.env.VITE_API_BASE_URL,
			VITE_APP_ENV: import.meta.env.VITE_APP_ENV,
			VITE_ENABLE_ANALYTICS: import.meta.env.VITE_ENABLE_ANALYTICS,
			VITE_ENABLE_ERROR_TRACKING: import.meta.env.VITE_ENABLE_ERROR_TRACKING,
			VITE_APP_NAME: import.meta.env.VITE_APP_NAME,
			VITE_APP_VERSION: import.meta.env.VITE_APP_VERSION,
			VITE_SENTRY_DSN: import.meta.env.VITE_SENTRY_DSN,
			VITE_GOOGLE_ANALYTICS_ID: import.meta.env.VITE_GOOGLE_ANALYTICS_ID,
			VITE_MIXPANEL_TOKEN: import.meta.env.VITE_MIXPANEL_TOKEN,
			VITE_AUTH_DOMAIN: import.meta.env.VITE_AUTH_DOMAIN,
			VITE_AUTH_CLIENT_ID: import.meta.env.VITE_AUTH_CLIENT_ID,
			VITE_STORAGE_BUCKET: import.meta.env.VITE_STORAGE_BUCKET,
			VITE_PUBLIC_URL: import.meta.env.VITE_PUBLIC_URL,
		}

		return envSchema.parse(env)
	} catch (error) {
		if (error instanceof z.ZodError) {
			const missingVars = error.errors.map(err => err.path.join('.')).join(', ')
			throw new Error(
				`❌ Environment variable validation failed!\n\n` +
				`Missing or invalid variables: ${missingVars}\n\n` +
				`Please check your .env file and ensure all required variables are set.\n` +
				`See .env.example for reference.`
			)
		}
		throw error
	}
}

// Validate environment variables on module load
export const env = validateEnv()

// Helper to check if running in production
export const isProduction = env.VITE_APP_ENV === 'production'
export const isDevelopment = env.VITE_APP_ENV === 'development'
export const isStaging = env.VITE_APP_ENV === 'staging'

// Export commonly used values
export const API_BASE_URL = env.VITE_API_BASE_URL
export const APP_NAME = env.VITE_APP_NAME
export const APP_VERSION = env.VITE_APP_VERSION

// Log environment info in development
if (isDevelopment) {
	console.log('🚀 Environment Configuration:', {
		environment: env.VITE_APP_ENV,
		apiBaseUrl: API_BASE_URL,
		appName: APP_NAME,
		version: APP_VERSION,
		features: {
			analytics: env.VITE_ENABLE_ANALYTICS,
			errorTracking: env.VITE_ENABLE_ERROR_TRACKING,
		}
	})
}
