import { PublicPageShell } from '@/components/layout/PublicPageShell'

const CAPABILITIES = [
	{
		title: 'Secure, scalable architecture',
		body: 'Built on a .NET Core backend with Azure cloud services, designed for reliability across multiple live sites.',
	},
	{
		title: 'Computer vision and barcode-based identification',
		body: 'Integrated directly into the incident workflow, rather than bolted on as a separate tool.',
	},
	{
		title: 'AI-assisted incident classification',
		body: 'Automatically categorises incoming incidents, with fallback logic to keep the system reliable under unusual or high-load conditions.',
	},
	{
		title: 'Automated deployment and access control',
		body: 'CI/CD pipelines and role-based access control keep the platform secure and maintainable as it scales to new sites.',
	},
] as const

const AboutPage = () => {
	return (
		<PublicPageShell
			title="About"
			description="A production crime and incident management platform for the Heart of England Co-operative, deployed across multiple retail locations."
		>
			<section className="space-y-2" aria-labelledby="about-what-it-does">
				<h2 id="about-what-it-does" className="text-sm font-semibold text-slate-900">
					What it does
				</h2>
				<p>
					The platform gives operational teams a single, secure system to log, track, and manage
					incidents in real time. Structured, searchable data feeds trend analysis, repeat-incident
					identification, and risk reporting.
				</p>
			</section>

			<section className="space-y-3" aria-labelledby="about-how-it-works">
				<h2 id="about-how-it-works" className="text-sm font-semibold text-slate-900">
					How it works
				</h2>
				<ul className="divide-y divide-slate-200 border border-slate-200">
					{CAPABILITIES.map(({ title, body }) => (
						<li key={title} className="px-4 py-3.5">
							<p className="font-medium text-slate-900">{title}</p>
							<p className="mt-1 text-slate-600">{body}</p>
						</li>
					))}
				</ul>
			</section>

			<section className="space-y-2" aria-labelledby="about-why-it-matters">
				<h2 id="about-why-it-matters" className="text-sm font-semibold text-slate-900">
					Why it matters
				</h2>
				<p>
					Retail security teams need incident data structured so it surfaces patterns, supports
					faster decisions, and reduces risk over time. DibangOps Crime Portal turns day-to-day
					incident logging into operational intelligence.
				</p>
			</section>

			<p className="border-t border-slate-200 pt-5 text-slate-600">
				Designed, built, and led end-to-end by David Ibanga.
			</p>
		</PublicPageShell>
	)
}

export default AboutPage
