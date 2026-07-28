import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Shield } from 'lucide-react'

const AboutPage = () => {
	return (
		<div className="min-h-screen flex items-center justify-center bg-slate-50 px-4 py-10">
			<Card className="w-full max-w-3xl shadow-md border border-slate-200 bg-white/95">
				<CardHeader>
					<CardTitle className="flex items-center gap-2 text-lg sm:text-xl">
						<Shield className="h-5 w-5 text-brand-500" aria-hidden="true" />
						<span>
							About DibangOps Crime Portal
							<span className="align-top text-[11px] ml-0.5">™</span>
						</span>
					</CardTitle>
				</CardHeader>
				<CardContent className="space-y-6 text-sm text-slate-700 leading-relaxed">
					<p>
						DibangOps Crime Portal<span className="align-top text-[10px] ml-0.5">™</span> is a
						production crime and incident management platform built for the Heart of England
						Co-operative, currently deployed across multiple retail locations.
					</p>

					<section className="space-y-2" aria-labelledby="about-what-it-does">
						<h2 id="about-what-it-does" className="text-base font-semibold text-slate-900">
							What it does
						</h2>
						<p>
							The platform gives operational teams a single, secure system to log, track, and manage
							incidents in real time — replacing fragmented manual reporting with structured,
							searchable data that feeds directly into trend analysis, repeat-incident identification,
							and risk reporting.
						</p>
					</section>

					<section className="space-y-3" aria-labelledby="about-how-it-works">
						<h2 id="about-how-it-works" className="text-base font-semibold text-slate-900">
							How it works
						</h2>
						<ul className="list-disc space-y-2 pl-5">
							<li>
								<span className="font-medium text-slate-900">Secure, scalable architecture</span>
								{' '}— built on a .NET Core backend with Azure cloud services, designed for reliability
								across multiple live sites
							</li>
							<li>
								<span className="font-medium text-slate-900">
									Computer vision &amp; barcode-based identification
								</span>
								{' '}— integrated directly into the incident workflow, rather than bolted on as a
								separate tool
							</li>
							<li>
								<span className="font-medium text-slate-900">AI-assisted incident classification</span>
								{' '}— automatically categorises incoming incidents, with fallback logic to ensure the
								system stays reliable even under unusual or high-load conditions
							</li>
							<li>
								<span className="font-medium text-slate-900">
									Automated deployment &amp; access control
								</span>
								{' '}— CI/CD pipelines and role-based access control keep the platform secure and easy
								to maintain as it scales to new sites
							</li>
						</ul>
					</section>

					<section className="space-y-2" aria-labelledby="about-why-it-matters">
						<h2 id="about-why-it-matters" className="text-base font-semibold text-slate-900">
							Why it matters
						</h2>
						<p>
							Retail security teams need more than a place to store incident reports — they need the
							data structured in a way that surfaces patterns, supports faster decisions, and reduces
							risk over time. DibangOps Crime Portal
							<span className="align-top text-[10px] ml-0.5">™</span> was designed from the ground up
							to turn day-to-day incident logging into operational intelligence.
						</p>
					</section>

					<p className="border-t border-slate-200 pt-4 font-medium text-slate-900">
						Designed, built, and led end-to-end by David Ibanga.
					</p>
				</CardContent>
			</Card>
		</div>
	)
}

export default AboutPage
