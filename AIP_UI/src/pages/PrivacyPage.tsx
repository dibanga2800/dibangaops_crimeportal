
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'

const PrivacyPage = () => {
	return (
		<div className="min-h-screen flex items-center justify-center bg-slate-50 px-4 py-10">
			<Card className="w-full max-w-2xl shadow-md border border-slate-200 bg-white/95">
				<CardHeader>
					<CardTitle className="text-lg sm:text-xl">Privacy Statement</CardTitle>
				</CardHeader>
				<CardContent className="space-y-3 text-sm text-slate-700">
					<p>
						DibangOps Crime Portal is built with a security-first mindset. All customer data is handled according to
						modern SaaS best practices, including strict access controls and separation between production
						and development environments.
					</p>
					<p>
						This platform is a live production service and can be configured to meet your organisation&apos;s
						data protection, retention, and compliance requirements.
					</p>
				</CardContent>
			</Card>
		</div>
	)
}

export default PrivacyPage
