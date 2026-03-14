import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Shield } from 'lucide-react'

const AboutPage = () => {
	return (
		<div className="min-h-screen flex items-center justify-center bg-slate-50 px-4 py-10">
			<Card className="w-full max-w-2xl shadow-md border border-slate-200 bg-white/95">
				<CardHeader>
					<CardTitle className="flex items-center gap-2 text-lg sm:text-xl">
						<Shield className="h-5 w-5 text-brand-500" />
						<span>DibangOps Crime Portal<span className="align-top text-[11px] ml-0.5">™</span></span>
					</CardTitle>
				</CardHeader>
				<CardContent className="space-y-4 text-sm text-slate-700">
					<p className="font-medium">
						<span className="text-slate-900">Founder &amp; Lead Architect:</span> David Ibanga
					</p>
					<p>
						DibangOps Crime Portal<span className="align-top text-[10px] ml-0.5">™</span> was architected and engineered to modernise
						enterprise incident management through intelligent automation, real-time analytics, and
						AI-assisted operational workflows.
					</p>
				</CardContent>
			</Card>
		</div>
	)
}

export default AboutPage

