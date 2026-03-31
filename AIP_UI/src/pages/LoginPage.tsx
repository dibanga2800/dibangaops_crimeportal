import { useState, useEffect, useRef } from 'react'
import { useNavigate } from 'react-router-dom'
import { Input } from '@/components/ui/input'
import { Button } from '@/components/ui/button'
import {
	Eye, EyeOff, Lock, Mail, ShieldCheck, AlertCircle,
	BarChart3, Bell, FileWarning, ArrowRight,
} from 'lucide-react'
import { usePageAccess } from '@/contexts/PageAccessContext'
import { useAuth } from '@/contexts/AuthContext'
import { api } from '@/config/api'
import { sessionStore } from '@/state/sessionStore'

interface LoginError {
	type: 'credentials' | 'network' | 'server' | 'validation'
	message: string
}

const FEATURES = [
	{ icon: FileWarning, label: 'AI-Assisted Incident Reporting' },
	{ icon: BarChart3, label: 'Store Risk & Trend Analytics' },
	{ icon: Bell, label: 'Smart Alerts & Escalation' },
	{ icon: ShieldCheck, label: 'Repeat Offender Intelligence' },
] as const

export default function LoginPage() {
	const [username, setUsername] = useState('')
	const [password, setPassword] = useState('')
	const [showPassword, setShowPassword] = useState(false)
	const [error, setError] = useState<LoginError | null>(null)
	const [shakeError, setShakeError] = useState(false)
	const [twoFactorEmail, setTwoFactorEmail] = useState<string | null>(null)
	const [twoFactorCode, setTwoFactorCode] = useState('')
	const [isVerifying2FA, setIsVerifying2FA] = useState(false)
	const [isCooldown, setIsCooldown] = useState(false)
	const formRef = useRef<HTMLFormElement>(null)
	const navigate = useNavigate()
	const { setCurrentRole } = usePageAccess()
	const { login, clearError, error: authError, isLoading: authLoading } = useAuth()

	useEffect(() => {
		clearError()
		setError(null)
		// eslint-disable-next-line react-hooks/exhaustive-deps
	}, [])

	const triggerShake = () => {
		setShakeError(true)
		setTimeout(() => setShakeError(false), 500)
	}

	const startCooldown = () => {
		setIsCooldown(true)
		setTimeout(() => setIsCooldown(false), 1000)
	}

	const validateForm = (): boolean => {
		if (!username.trim()) {
			clearError()
			setError({ type: 'validation', message: 'Please enter your email address' })
			triggerShake()
			return false
		}
		if (!password.trim()) {
			clearError()
			setError({ type: 'validation', message: 'Please enter your password' })
			triggerShake()
			return false
		}
		return true
	}

	const handleSubmit = async (e: React.FormEvent) => {
		e.preventDefault()
		if (authLoading) return

		setError(null)
		if (!validateForm()) return

		try {
			const result = await login(username, password) as any

			// If backend indicates 2FA is required, show code entry step
			if (result && result.requiresTwoFactor) {
				setTwoFactorEmail(result.email)
				setTwoFactorCode('')
				return
			}

			const loggedInUser = result

			setCurrentRole(loggedInUser.role).catch(err => {
				console.warn('⚠️ [LoginPage] Error setting role:', err)
			})

			navigate('/dashboard', { replace: true })
		} catch (err) {
			setError({
				type: 'credentials',
				message: err instanceof Error ? err.message : 'An error occurred during login',
			})
			triggerShake()
			startCooldown()
		}
	}

	const handleVerifyTwoFactor = async (e: React.FormEvent) => {
		e.preventDefault()
		if (!twoFactorEmail || !twoFactorCode.trim()) {
			clearError()
			setError({ type: 'validation', message: 'Please enter the verification code sent to your email' })
			triggerShake()
			return
		}

		setIsVerifying2FA(true)
		try {
			const response = await api.post('/Auth/2fa/complete', {
				email: twoFactorEmail,
				code: twoFactorCode.trim(),
			})

			const apiResponse = response.data
			const isSuccess = apiResponse?.Success ?? apiResponse?.success ?? false
			const data = apiResponse?.Data ?? apiResponse?.data

			if (!isSuccess || !data) {
				const message = apiResponse?.Message ?? apiResponse?.message ?? 'Invalid verification code'
				throw new Error(message)
			}

			const loginData = data as any
			const accessToken = loginData?.AccessToken ?? loginData?.accessToken
			const refreshToken = loginData?.RefreshToken ?? loginData?.refreshToken
			const expiresAt = loginData?.ExpiresAt ?? loginData?.expiresAt
			const user = loginData?.User ?? loginData?.user

			if (!accessToken || !user) {
				throw new Error('Invalid response from server: missing token or user data')
			}

			// Persist auth state so a full reload lands in an authenticated session
			sessionStore.setToken(accessToken)
			sessionStore.setRefreshToken(refreshToken ?? null)
			sessionStore.setTokenExpiresAt(expiresAt ?? null)
			sessionStore.setUser(user)

			// Reload app so AuthProvider picks up the new token/user cleanly
			window.location.href = '/'
		} catch (err) {
			setError({
				type: 'credentials',
				message: err instanceof Error ? err.message : 'Invalid or expired verification code',
			})
			triggerShake()
			startCooldown()
		} finally {
			setIsVerifying2FA(false)
		}
	}

	const effectiveError =
		error ??
		(authError ? { type: 'credentials' as const, message: authError } : null)

	return (
		<>
			<style>{`
				@keyframes login-fade-up {
					from { opacity: 0; transform: translateY(16px); }
					to   { opacity: 1; transform: translateY(0); }
				}
				@keyframes login-fade-in {
					from { opacity: 0; }
					to   { opacity: 1; }
				}
				@keyframes login-shake {
					0%, 100% { transform: translateX(0); }
					15%      { transform: translateX(-6px); }
					30%      { transform: translateX(5px); }
					45%      { transform: translateX(-4px); }
					60%      { transform: translateX(3px); }
					75%      { transform: translateX(-2px); }
				}
				@keyframes login-gradient-shift {
					0%, 100% { background-position: 0% 50%; }
					50%      { background-position: 100% 50%; }
				}
				.login-animate-up    { animation: login-fade-up 0.6s ease-out both; }
				.login-animate-up-1  { animation: login-fade-up 0.6s ease-out 0.1s both; }
				.login-animate-up-2  { animation: login-fade-up 0.6s ease-out 0.2s both; }
				.login-animate-up-3  { animation: login-fade-up 0.6s ease-out 0.35s both; }
				.login-animate-fade  { animation: login-fade-in 1s ease-out 0.3s both; }
				.login-shake         { animation: login-shake 0.45s ease-in-out; }
				.login-gradient-bar  {
					background: linear-gradient(90deg, #94C949, #7AB32E, #5F8C23, #7AB32E, #94C949);
					background-size: 200% 100%;
					animation: login-gradient-shift 4s ease infinite;
				}
			`}</style>

			<div className="min-h-screen flex flex-col lg:flex-row bg-background text-foreground">

				{/* ─── LEFT PANEL: hero image + branding (desktop) ─── */}
				<div className="relative hidden lg:flex lg:w-[55%] xl:w-[58%] 2xl:w-[60%] flex-col justify-between overflow-hidden">
					<img
						src="/Login-bg.png"
						alt=""
						aria-hidden="true"
						className="absolute inset-0 h-full w-full object-cover scale-[1.02] login-animate-fade"
					/>
					{/* Multi-layer gradient overlay for depth */}
					<div className="absolute inset-0 bg-gradient-to-br from-slate-950/85 via-slate-900/60 to-slate-950/75" />
					<div className="absolute inset-0 bg-gradient-to-t from-slate-950/50 via-transparent to-transparent" />

					{/* Subtle top-left glow */}
					<div className="absolute -top-32 -left-32 h-96 w-96 rounded-full bg-brand-500/10 blur-3xl" />

					{/* Top: platform branding (neutral, non-owner specific) */}
					<div className="relative z-10 p-10 xl:p-14 login-animate-up">
						<div className="flex items-center gap-3.5">
							<div className="flex h-11 w-11 items-center justify-center rounded-xl bg-white/10 backdrop-blur-sm border border-white/10 shadow-lg">
								<ShieldCheck className="h-6 w-6 text-brand-400" />
							</div>
							<div className="leading-tight">
								<span className="block text-[17px] font-bold text-white tracking-tight">
									DibangOps Crime Portal<span className="align-top text-[11px] ml-0.5">™</span>
								</span>
								<span className="block text-[10px] font-semibold uppercase tracking-[0.25em] text-white/40 mt-0.5">
									AI-Powered Retail Crime Intelligence Platform
								</span>
							</div>
						</div>
					</div>

					{/* Bottom: headline + features */}
					<div className="relative z-10 p-10 xl:p-14">
						<div className="login-animate-up-1">
							<p className="text-xs font-semibold uppercase tracking-[0.25em] text-brand-400/80 mb-4">
								AI-Powered Retail Crime Intelligence
							</p>
							<h1 className="text-[2.5rem] xl:text-5xl font-extrabold text-white leading-[1.1] tracking-tight">
								Heart of England Coop
								<br />
								<span className="bg-gradient-to-r from-brand-300 via-brand-400 to-brand-500 bg-clip-text text-transparent">
									Crime Intelligence Portal
								</span>
							</h1>
							<p className="mt-5 max-w-lg text-[15px] leading-relaxed text-white/50 font-light">
								AI-assisted incident reporting, repeat offender intelligence, and store risk analytics —
								purpose-built for modern loss prevention and security teams.
							</p>
						</div>

						{/* Feature pills */}
						<div className="mt-10 flex flex-wrap items-center gap-3 login-animate-up-2">
							{FEATURES.map(({ icon: Icon, label }) => (
								<span
									key={label}
									className="inline-flex items-center gap-2 rounded-full border border-white/10 bg-white/5 backdrop-blur-sm px-4 py-2 text-xs font-medium text-white/70 transition-colors hover:bg-white/10 hover:text-white/90"
								>
									<Icon className="h-3.5 w-3.5 text-brand-400" />
									{label}
								</span>
							))}
						</div>

						{/* Trust bar */}
						<div className="mt-10 pt-8 border-t border-white/10 login-animate-up-3">
							<div className="flex items-center gap-8">
								<div>
									<span className="block text-2xl font-bold text-white">97.9%</span>
									<span className="block text-[11px] text-white/40 mt-0.5">Uptime SLA</span>
								</div>
								<div className="h-8 w-px bg-white/10" />
								<div>
									<span className="block text-2xl font-bold text-white">24/7</span>
									<span className="block text-[11px] text-white/40 mt-0.5">Monitoring</span>
								</div>
								<div className="h-8 w-px bg-white/10" />
								<div>
									<span className="block text-2xl font-bold text-white">256-bit</span>
									<span className="block text-[11px] text-white/40 mt-0.5">Encryption</span>
								</div>
							</div>
						</div>
					</div>
				</div>

				{/* ─── MOBILE HERO (below lg) ─── */}
				<div className="relative flex lg:hidden h-52 sm:h-64 w-full items-end overflow-hidden">
					<img
						src="/Login-bg.png"
						alt=""
						aria-hidden="true"
						className="absolute inset-0 h-full w-full object-cover object-center"
					/>
					<div className="absolute inset-0 bg-gradient-to-t from-background via-slate-900/60 to-slate-900/40" />
						<div className="relative z-10 w-full px-6 pb-6">
							<div className="flex items-center gap-2.5 mb-3">
								<div className="flex h-9 w-9 items-center justify-center rounded-lg bg-white/15 backdrop-blur-sm border border-white/20">
									<ShieldCheck className="h-5 w-5 text-brand-400" />
								</div>
								<span className="text-sm font-bold text-white tracking-tight">
								DibangOps Crime Portal<span className="align-top text-[10px] ml-0.5">™</span>
								</span>
							</div>
						<h1 className="text-2xl sm:text-[1.7rem] font-extrabold text-white leading-snug tracking-tight">
							Heart of England Coop{' '}
							<span className="text-brand-400">Crime Intelligence</span>
						</h1>
						<p className="mt-1.5 text-xs text-white/50">
							AI-powered retail crime intelligence platform
						</p>
					</div>
				</div>

				{/* ─── RIGHT PANEL: login form ─── */}
				<div className="flex flex-1 flex-col bg-background lg:bg-muted/40">
					{/* Animated brand bar at the top of form panel */}
					<div className="login-gradient-bar h-1 w-full lg:hidden" />

					<div className="flex flex-1 flex-col justify-center px-6 py-8 sm:px-10 lg:px-16 xl:px-24">
						<div className="mx-auto w-full max-w-[400px]">

							{/* Card wrapper (desktop gets elevated card, mobile is flat) */}
							<div className="lg:bg-card lg:text-card-foreground lg:rounded-2xl lg:shadow-xl lg:shadow-black/10 dark:lg:shadow-black/40 lg:border lg:border-border lg:px-10 lg:py-12">

								{/* Logo + heading */}
								<div className="mb-9 login-animate-up">
									<div className="flex items-center justify-center mb-5">
										<img
											src="/HOEnbg.png"
											alt="Heart of England Co-operative"
											className="h-10 sm:h-11 w-auto drop-shadow-sm"
										/>
									</div>
									<h2 className="text-[1.4rem] font-extrabold tracking-tight text-foreground text-center">
										Sign in to your crime intelligence workspace
									</h2>
									<p className="mt-2 text-[13.5px] text-muted-foreground leading-relaxed text-center">
										Enter your credentials to access AI-assisted incident reporting and security analytics.
									</p>
								</div>

								{/* Error banner */}
								{effectiveError && (
									<div
										className={`mb-6 flex items-start gap-3 rounded-xl border border-red-200/70 bg-red-50/80 dark:border-red-900/70 dark:bg-red-950/40 p-4 login-animate-up ${shakeError ? 'login-shake' : ''}`}
									>
										<div className="flex h-8 w-8 shrink-0 items-center justify-center rounded-lg bg-red-100 dark:bg-red-900/60">
											<AlertCircle className="h-4 w-4 text-red-500 dark:text-red-300" />
										</div>
										<div className="text-sm leading-snug pt-0.5">
											<p className="font-semibold text-red-800 dark:text-red-200">
												{effectiveError.type === 'credentials'
													? 'Authentication failed'
													: effectiveError.type === 'validation'
														? 'Check your input'
														: 'Something went wrong'}
											</p>
											<p className="mt-0.5 text-red-600/80 dark:text-red-300/90">{effectiveError.message}</p>
										</div>
									</div>
								)}

								<form
									ref={formRef}
									onSubmit={twoFactorEmail ? handleVerifyTwoFactor : handleSubmit}
									className="space-y-5 login-animate-up-1"
								>
									{/* Email */}
									{!twoFactorEmail && (
										<div className="space-y-1.5">
											<label
												htmlFor="username"
												className="block text-[13px] font-semibold text-foreground/90"
											>
												Email address
											</label>
											<div className="group relative">
												<Mail className="pointer-events-none absolute left-3.5 top-1/2 -translate-y-1/2 h-[17px] w-[17px] text-muted-foreground transition-colors group-focus-within:text-brand-500" />
												<input
													id="username"
													name="username"
													autoComplete="username"
													value={username}
													onChange={e => setUsername(e.target.value)}
													className="block w-full h-12 rounded-xl border border-border bg-muted/40 pl-11 pr-4 text-sm text-foreground placeholder:text-muted-foreground transition-all duration-200 focus:bg-background focus:border-brand-400 focus:ring-2 focus:ring-brand-400/20 focus:outline-none hover:border-brand-300/60 disabled:opacity-50"
													placeholder="you@company.com"
													disabled={authLoading || isVerifying2FA}
												/>
											</div>
										</div>
									)}

									{/* Password or 2FA code */}
									<div className="space-y-1.5">
										<label
											htmlFor={twoFactorEmail ? 'twofactor' : 'password'}
											className="block text-[13px] font-semibold text-foreground/90"
										>
											{twoFactorEmail ? 'Verification code' : 'Password'}
										</label>
										<div className="group relative">
											{twoFactorEmail ? (
												<>
													<ShieldCheck className="pointer-events-none absolute left-3.5 top-1/2 -translate-y-1/2 h-[17px] w-[17px] text-muted-foreground transition-colors group-focus-within:text-brand-500" />
													<input
														id="twofactor"
														name="twofactor"
														type="text"
														value={twoFactorCode}
														onChange={e => setTwoFactorCode(e.target.value)}
														className="block w-full h-12 rounded-xl border border-border bg-muted/40 pl-11 pr-4 text-sm text-foreground placeholder:text-muted-foreground transition-all duration-200 focus:bg-background focus:border-brand-400 focus:ring-2 focus:ring-brand-400/20 focus:outline-none hover:border-brand-300/60 disabled:opacity-50"
														placeholder="Enter the 6-digit code sent to your email"
														disabled={isVerifying2FA}
													/>
												</>
											) : (
												<>
													<Lock className="pointer-events-none absolute left-3.5 top-1/2 -translate-y-1/2 h-[17px] w-[17px] text-muted-foreground transition-colors group-focus-within:text-brand-500" />
													<input
														id="password"
														name="password"
														type={showPassword ? 'text' : 'password'}
														autoComplete="current-password"
														value={password}
														onChange={e => setPassword(e.target.value)}
														className="block w-full h-12 rounded-xl border border-border bg-muted/40 pl-11 pr-11 text-sm text-foreground placeholder:text-muted-foreground transition-all duration-200 focus:bg-background focus:border-brand-400 focus:ring-2 focus:ring-brand-400/20 focus:outline-none hover:border-brand-300/60 disabled:opacity-50"
														placeholder="Enter your password"
														disabled={authLoading}
													/>
													<button
														type="button"
														onClick={() => setShowPassword(!showPassword)}
														className="absolute right-3.5 top-1/2 -translate-y-1/2 rounded-md p-0.5 text-muted-foreground transition-colors hover:text-foreground focus:text-foreground focus:outline-none touch-manipulation"
														disabled={authLoading}
														tabIndex={-1}
														aria-label={showPassword ? 'Hide password' : 'Show password'}
													>
														{showPassword ? (
															<EyeOff className="h-[17px] w-[17px]" />
														) : (
															<Eye className="h-[17px] w-[17px]" />
														)}
													</button>
												</>
											)}
										</div>
									</div>

									{/* Submit */}
									<div className="pt-1">
										<button
											type="submit"
											disabled={authLoading || isVerifying2FA || isCooldown}
											className="group relative flex w-full items-center justify-center gap-2 h-12 rounded-xl bg-brand-600 font-semibold text-sm text-white shadow-lg shadow-brand-600/25 transition-all duration-200 hover:bg-brand-700 hover:shadow-brand-700/30 hover:-translate-y-px active:translate-y-0 active:shadow-brand-800/20 focus:outline-none focus:ring-2 focus:ring-brand-500 focus:ring-offset-2 disabled:opacity-60 disabled:pointer-events-none"
										>
											{(authLoading || isVerifying2FA) ? (
												<>
													<span className="h-4.5 w-4.5 animate-spin rounded-full border-2 border-white/30 border-t-white" />
													<span>{twoFactorEmail ? 'Verifying code...' : 'Signing in...'}</span>
												</>
											) : (
												<>
													<span>{twoFactorEmail ? 'Verify code' : 'Sign in'}</span>
													<ArrowRight className="h-4 w-4 transition-transform duration-200 group-hover:translate-x-0.5" />
												</>
											)}
										</button>
									</div>
								</form>

								{/* Divider + footer */}
								<div className="mt-10 space-y-4 login-animate-up-2">
									<div className="relative">
											<div className="absolute inset-0 flex items-center">
												<div className="w-full border-t border-border" />
										</div>
										<div className="relative flex justify-center">
											<span className="bg-background lg:bg-card px-3 text-[11px] text-muted-foreground">
												SECURED PLATFORM
											</span>
										</div>
									</div>
									<div className="mt-5 flex items-center justify-center gap-2">
										<div className="flex h-6 w-6 items-center justify-center rounded-md bg-muted">
											<ShieldCheck className="h-3.5 w-3.5 text-muted-foreground" />
										</div>
										<span className="text-xs text-muted-foreground">
											AI-powered retail crime intelligence for modern loss prevention teams
										</span>
									</div>

									{/* Legal links under card (SaaS-style) */}
									<div className="flex items-center justify-center gap-4 text-[11px] text-muted-foreground">
										<a href="/about" className="hover:text-foreground underline-offset-4 hover:underline">
											About
										</a>
										<span className="h-3 w-px bg-border" aria-hidden="true" />
										<a href="/privacy" className="hover:text-foreground underline-offset-4 hover:underline">
											Privacy
										</a>
										<span className="h-3 w-px bg-border" aria-hidden="true" />
										<a href="/terms" className="hover:text-foreground underline-offset-4 hover:underline">
											Terms
										</a>
									</div>
								</div>

							</div>
						</div>
					</div>

					{/* Bottom branding & copyright */}
					<div className="pb-5 text-center space-y-0.5">
						<p className="text-[11px] font-semibold text-muted-foreground">
							DibangOps Crime Portal<span className="align-top text-[9px] ml-0.5">™</span>
						</p>
						<p className="text-[11px] text-muted-foreground">
							&copy; {new Date().getFullYear()} DibangaOps – Developed by David Ibanga
						</p>
						<p className="text-[11px] text-muted-foreground">
							Proprietary &amp; Confidential.
						</p>
					</div>
				</div>
			</div>
		</>
	)
} 