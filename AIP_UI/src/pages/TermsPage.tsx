
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'

const TermsPage = () => {
	return (
		<div className="min-h-screen flex items-center justify-center bg-slate-50 px-4 py-10">
			<Card className="w-full max-w-3xl shadow-md border border-slate-200 bg-white/95">
				<CardHeader>
					<CardTitle className="text-lg sm:text-xl">DibangOps Crime Portal Terms of Use</CardTitle>
				</CardHeader>
				<CardContent className="space-y-4 text-sm text-slate-700">
					<section>
						<h2 className="font-semibold text-slate-900 mb-1">1. Acceptance of Terms</h2>
						<p>
							By accessing or using the DibangOps Crime Portal platform, you agree to be bound by these Terms of Use.
							If you do not agree, you may not use the platform.
						</p>
					</section>

					<section>
						<h2 className="font-semibold text-slate-900 mb-1">2. Live Production Service</h2>
						<p>
							DibangOps Crime Portal is a live, production-grade security intelligence platform provided by David
							Ibanga. Users are expected to operate responsibly and in compliance with all applicable laws.
						</p>
					</section>

					<section>
						<h2 className="font-semibold text-slate-900 mb-1">3. Intellectual Property</h2>
						<p>
							All content, screenshots, recordings, case studies, software, and associated materials
							produced within DibangOps Crime Portal remain the exclusive intellectual property of David Ibanga. Users
							may not reproduce, distribute, or use such materials for commercial purposes without explicit
							permission, except for internal technical documentation or portfolio use with proper
							attribution.
						</p>
					</section>

					<section>
						<h2 className="font-semibold text-slate-900 mb-1">4. Authorized Use Only</h2>
						<p>
							Access is limited to authorized users. Unauthorized access, misuse, or tampering with the
							platform is strictly prohibited and may result in legal action.
						</p>
					</section>

					<section>
						<h2 className="font-semibold text-slate-900 mb-1">5. No Warranty</h2>
						<p>
							DibangOps Crime Portal is provided &quot;as is&quot; and without warranties of any kind, whether express
							or implied, including but not limited to performance, uptime, accuracy, or fitness for a
							particular purpose.
						</p>
					</section>

					<section>
						<h2 className="font-semibold text-slate-900 mb-1">6. Limitation of Liability</h2>
						<p>
							David Ibanga and DibangOps Crime Portal are not liable for any direct, indirect, incidental, or
							consequential damages arising from the use, inability to use, or reliance on the platform.
							Users assume all risk associated with using the service.
						</p>
					</section>

					<section>
						<h2 className="font-semibold text-slate-900 mb-1">7. Changes to Terms</h2>
						<p>
							David Ibanga reserves the right to modify or update these Terms of Use at any time. Users are
							responsible for reviewing the Terms regularly. Continued use of the platform constitutes
							acceptance of any changes.
						</p>
					</section>

					<section>
						<h2 className="font-semibold text-slate-900 mb-1">8. Governing Law</h2>
						<p>
							These Terms are governed by the laws of the United Kingdom. Any disputes arising under these
							Terms will be resolved in the appropriate courts of the United Kingdom.
						</p>
					</section>
				</CardContent>
			</Card>
		</div>
	)
}

export default TermsPage
