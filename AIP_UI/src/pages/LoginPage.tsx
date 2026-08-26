import { useState, useEffect, useRef } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import {
	Eye, EyeOff, Lock, Mail, ShieldCheck, AlertCircle,
	BarChart3, Bell, FileWarning,
} from 'lucide-react'
import { usePageAccess } from '@/contexts/PageAccessContext'
import { useAuth } from '@/contexts/AuthContext'
import { api, AUTH_REQUEST_TIMEOUT_MS } from '@/config/api'

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
	const [twoFactorNotice, setTwoFactorNotice] = useState<string | null>(null)
	const [isVerifying2FA, setIsVerifying2FA] = useState(false)
	const [isCooldown, setIsCooldown] = useState(false)
	const formRef = useRef<HTMLFormElement>(null)
	const navigate = useNavigate()
	const { setCurrentRole } = usePageAccess()
	const { login, completeSessionFromPayload, clearError, error: authError, isLoading: authLoading } = useAuth()

	useEffect(() => {
		clearError()
		setError(null)
		 
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
				if (result.twoFactorEmailSent === false) {
					setTwoFactorNotice(
						result.twoFactorDeliveryMessage ??
							'We could not send the verification email. Contact your administrator or try again later.',
					)
				} else {
					setTwoFactorNotice(
						result.email
							? `A verification code was sent to ${result.email}.`
							: null,
					)
				}
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
			const response = await api.post(
				'/Auth/2fa/complete',
				{
					email: twoFactorEmail,
					code: twoFactorCode.trim(),
				},
				{ timeout: AUTH_REQUEST_TIMEOUT_MS },
			)

			const apiResponse = response.data
			const isSuccess = apiResponse?.Success ?? apiResponse?.success ?? false
			const data = apiResponse?.Data ?? apiResponse?.data

			if (!isSuccess || !data) {
				const message = apiResponse?.Message ?? apiResponse?.message ?? 'Invalid verification code'
				throw new Error(message)
			}

			const loggedInUser = await completeSessionFromPayload(data as Record<string, unknown>)

			setCurrentRole(loggedInUser.role).catch(err => {
				console.warn('⚠️ [LoginPage] Error setting role after 2FA:', err)
			})

			navigate('/dashboard', { replace: true })
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

	const fieldClass =
		'block w-full h-11 rounded-md border border-slate-300 bg-white pl-10 pr-3 text-sm text-slate-900 placeholder:text-slate-400 transition-colors focus:border-brand-600 focus:ring-2 focus:ring-brand-600/15 focus:outline-none hover:border-slate-400 disabled:bg-slate-50 disabled:opacity-60'

	return (
		<>
			<style>{`
				@keyframes login-shake {
					0%, 100% { transform: translateX(0); }
					20%      { transform: translateX(-4px); }
					40%      { transform: translateX(4px); }
					60%      { transform: translateX(-2px); }
					80%      { transform: translateX(2px); }
				}
				.login-shake { animation: login-shake 0.4s ease-in-out; }
			`}</style>

			<div className="min-h-screen flex flex-col lg:flex-row bg-white text-slate-900">

				{/* Left panel: institutional briefing */}
				<div className="relative hidden lg:flex lg:w-[52%] xl:w-[54%] flex-col justify-between overflow-hidden bg-slate-950">
					<img
						src="/Login-bg.png"
						alt=""
						aria-hidden="true"
						className="absolute inset-0 h-full w-full object-cover opacity-80"
					/>
					<div className="absolute inset-0 bg-gradient-to-t from-slate-950/85 via-slate-950/40 to-slate-950/25" />

					<div className="relative z-10 px-10 pt-10 xl:px-14 xl:pt-12">
						<div className="flex items-center gap-3">
							<div className="flex h-10 w-10 items-center justify-center rounded-md bg-white/15 border border-white/20">
								<ShieldCheck className="h-5 w-5 text-brand-300" aria-hidden="true" />
							</div>
							<div className="leading-tight">
								<span className="block text-base font-semibold text-white tracking-tight">
									DibangOps Crime Portal
								</span>
								<span className="block text-xs text-white/80 mt-0.5">
									Retail crime intelligence
								</span>
							</div>
						</div>
					</div>

					<div className="relative z-10 px-10 pb-10 xl:px-14 xl:pb-12 [text-shadow:0_1px_10px_rgba(0,0,0,0.45)]">
						<h1 className="font-bold text-white leading-tight tracking-tight">
							<span className="block whitespace-nowrap text-2xl xl:text-3xl">
								Heart of England Co-operative
							</span>
							<span className="mt-2 block text-3xl xl:text-4xl text-brand-300">
								Crime Intelligence Portal
							</span>
						</h1>
						<p className="mt-5 max-w-lg text-lg leading-relaxed text-white">
							Incident reporting, repeat offender intelligence, and store risk analytics in a single
							controlled workspace.
						</p>

						<ul className="mt-8 space-y-3">
							{FEATURES.map(({ icon: Icon, label }) => (
								<li key={label} className="flex items-center gap-3 text-base font-medium text-white">
									<Icon className="h-5 w-5 shrink-0 text-brand-300" aria-hidden="true" />
									<span>{label}</span>
								</li>
							))}
						</ul>

						<dl className="mt-10 grid grid-cols-3 gap-6 border-t border-white/25 pt-6">
							<div>
								<dt className="text-sm font-medium text-white/80">
									Uptime SLA
								</dt>
								<dd className="mt-1 text-2xl font-bold text-white">98.9%</dd>
							</div>
							<div>
								<dt className="text-sm font-medium text-white/80">
									Monitoring
								</dt>
								<dd className="mt-1 text-2xl font-bold text-white">24/7</dd>
							</div>
							<div>
								<dt className="text-sm font-medium text-white/80">
									Encryption
								</dt>
								<dd className="mt-1 text-2xl font-bold text-white">256-bit</dd>
							</div>
						</dl>
					</div>
				</div>

				{/* Mobile header */}
				<div className="flex lg:hidden items-center gap-2.5 border-b border-slate-200 px-5 py-4">
					<div className="flex h-8 w-8 items-center justify-center rounded-md bg-slate-900">
						<ShieldCheck className="h-4 w-4 text-brand-400" aria-hidden="true" />
					</div>
					<div>
						<p className="text-sm font-semibold text-slate-900">DibangOps Crime Portal</p>
						<p className="text-[11px] text-slate-500">Heart of England Co-operative</p>
					</div>
				</div>

				{/* Right panel: sign-in */}
				<div className="flex flex-1 flex-col bg-slate-50">
					<div className="flex flex-1 flex-col justify-center px-6 py-10 sm:px-10 lg:px-16 xl:px-20">
						<div className="mx-auto w-full max-w-[380px] border border-slate-300 bg-white px-6 py-8 sm:px-8 sm:py-9">
							<div className="mb-8">
								<img
									src="/HOEnbg.png"
									alt="Heart of England Co-operative"
									className="h-11 sm:h-12 w-auto"
								/>
								<h2 className="mt-6 text-[1.35rem] font-semibold tracking-tight text-slate-900">
									Sign in
								</h2>
								<p className="mt-1.5 text-sm leading-relaxed text-slate-600">
									Authorised access to the crime intelligence workspace.
								</p>
							</div>

							{effectiveError && (
								<div
									className={`mb-5 flex items-start gap-2.5 border border-red-200 bg-red-50 p-3 ${shakeError ? 'login-shake' : ''}`}
									role="alert"
								>
									<AlertCircle className="mt-0.5 h-4 w-4 shrink-0 text-red-600" aria-hidden="true" />
									<div className="text-sm leading-snug">
										<p className="font-semibold text-red-800">
											{effectiveError.type === 'credentials'
												? 'Sign-in failed'
												: effectiveError.type === 'validation'
													? 'Check your details'
													: 'Something went wrong'}
										</p>
										<p className="mt-0.5 text-red-700">{effectiveError.message}</p>
									</div>
								</div>
							)}

							<form
								ref={formRef}
								onSubmit={twoFactorEmail ? handleVerifyTwoFactor : handleSubmit}
								className="space-y-4"
							>
								{!twoFactorEmail && (
									<div className="space-y-1.5">
										<label htmlFor="username" className="block text-[13px] font-medium text-slate-800">
											Email address
										</label>
										<div className="relative">
											<Mail className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-400" aria-hidden="true" />
											<input
												id="username"
												name="username"
												autoComplete="username"
												value={username}
												onChange={e => setUsername(e.target.value)}
												className={fieldClass}
												placeholder="you@company.com"
												disabled={authLoading || isVerifying2FA}
											/>
										</div>
									</div>
								)}

								{twoFactorEmail && twoFactorNotice && (
									<p
										className={`text-sm leading-snug border px-3 py-2.5 ${
											twoFactorNotice.includes('could not')
												? 'border-amber-200 bg-amber-50 text-amber-900'
												: 'border-slate-200 bg-slate-50 text-slate-700'
										}`}
									>
										{twoFactorNotice}
									</p>
								)}

								<div className="space-y-1.5">
									<label
										htmlFor={twoFactorEmail ? 'twofactor' : 'password'}
										className="block text-[13px] font-medium text-slate-800"
									>
										{twoFactorEmail ? 'Verification code' : 'Password'}
									</label>
									<div className="relative">
										{twoFactorEmail ? (
											<>
												<ShieldCheck className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-400" aria-hidden="true" />
												<input
													id="twofactor"
													name="twofactor"
													type="text"
													inputMode="numeric"
													autoComplete="one-time-code"
													value={twoFactorCode}
													onChange={e => setTwoFactorCode(e.target.value)}
													className={fieldClass}
													placeholder="6-digit code"
													disabled={isVerifying2FA}
												/>
											</>
										) : (
											<>
												<Lock className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-400" aria-hidden="true" />
												<input
													id="password"
													name="password"
													type={showPassword ? 'text' : 'password'}
													autoComplete="current-password"
													value={password}
													onChange={e => setPassword(e.target.value)}
													className={`${fieldClass} pr-10`}
													placeholder="Enter your password"
													disabled={authLoading}
												/>
												<button
													type="button"
													onClick={() => setShowPassword(!showPassword)}
													className="absolute right-2.5 top-1/2 -translate-y-1/2 rounded p-1 text-slate-400 hover:text-slate-700 focus:outline-none focus-visible:ring-2 focus-visible:ring-brand-500 touch-manipulation"
													disabled={authLoading}
													tabIndex={-1}
													aria-label={showPassword ? 'Hide password' : 'Show password'}
												>
													{showPassword ? (
														<EyeOff className="h-4 w-4" />
													) : (
														<Eye className="h-4 w-4" />
													)}
												</button>
											</>
										)}
									</div>
								</div>

								<button
									type="submit"
									disabled={authLoading || isVerifying2FA || isCooldown}
									className="flex h-11 w-full items-center justify-center gap-2 rounded-md bg-slate-900 text-sm font-medium text-white transition-colors hover:bg-slate-800 focus:outline-none focus-visible:ring-2 focus-visible:ring-brand-500 focus-visible:ring-offset-2 disabled:pointer-events-none disabled:opacity-60"
								>
									{(authLoading || isVerifying2FA) ? (
										<>
											<span className="h-4 w-4 animate-spin rounded-full border-2 border-white/30 border-t-white" />
											<span>{twoFactorEmail ? 'Verifying...' : 'Signing in...'}</span>
										</>
									) : (
										<span>{twoFactorEmail ? 'Verify code' : 'Sign in'}</span>
									)}
								</button>
							</form>

							<nav
								className="mt-8 flex items-center gap-4 border-t border-slate-200 pt-5 text-[12px] text-slate-500"
								aria-label="Legal"
							>
								<Link to="/about" className="hover:text-slate-800 underline-offset-4 hover:underline">
									About
								</Link>
								<Link to="/privacy" className="hover:text-slate-800 underline-offset-4 hover:underline">
									Privacy
								</Link>
								<Link to="/terms" className="hover:text-slate-800 underline-offset-4 hover:underline">
									Terms
								</Link>
							</nav>
						</div>
					</div>

					<div className="border-t border-slate-200 bg-white px-6 py-4 text-[12px] text-slate-500 sm:px-10">
						<p>
							&copy; {new Date().getFullYear()} DibangOps. Developed by David Ibanga.
							{' '}Proprietary and confidential.
						</p>
					</div>
				</div>
			</div>
		</>
	)
} 