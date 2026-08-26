import { PublicPageShell } from '@/components/layout/PublicPageShell'

const SECTIONS = [
	{
		id: 'acceptance',
		title: '1. Acceptance of terms',
		body: 'By accessing or using the DibangOps Crime Portal platform, you agree to be bound by these Terms of Use. If you do not agree, you may not use the platform.',
	},
	{
		id: 'production',
		title: '2. Live production service',
		body: 'DibangOps Crime Portal is a live, production-grade security intelligence platform provided by David Ibanga. Users are expected to operate responsibly and in compliance with all applicable laws.',
	},
	{
		id: 'ip',
		title: '3. Intellectual property',
		body: 'All content, screenshots, recordings, case studies, software, and associated materials produced within DibangOps Crime Portal remain the exclusive intellectual property of David Ibanga. Users may not reproduce, distribute, or use such materials for commercial purposes without explicit permission, except for internal technical documentation or portfolio use with proper attribution.',
	},
	{
		id: 'authorised',
		title: '4. Authorised use only',
		body: 'Access is limited to authorised users. Unauthorised access, misuse, or tampering with the platform is strictly prohibited and may result in legal action.',
	},
	{
		id: 'warranty',
		title: '5. No warranty',
		body: 'DibangOps Crime Portal is provided "as is" and without warranties of any kind, whether express or implied, including but not limited to performance, uptime, accuracy, or fitness for a particular purpose.',
	},
	{
		id: 'liability',
		title: '6. Limitation of liability',
		body: 'David Ibanga and DibangOps Crime Portal are not liable for any direct, indirect, incidental, or consequential damages arising from the use, inability to use, or reliance on the platform. Users assume all risk associated with using the service.',
	},
	{
		id: 'changes',
		title: '7. Changes to terms',
		body: 'David Ibanga reserves the right to modify or update these Terms of Use at any time. Users are responsible for reviewing the Terms regularly. Continued use of the platform constitutes acceptance of any changes.',
	},
	{
		id: 'law',
		title: '8. Governing law',
		body: 'These Terms are governed by the laws of the United Kingdom. Any disputes arising under these Terms will be resolved in the appropriate courts of the United Kingdom.',
	},
] as const

const TermsPage = () => {
	return (
		<PublicPageShell
			title="Terms of use"
			description="Conditions that apply to authorised use of the DibangOps Crime Portal."
		>
			{SECTIONS.map(({ id, title, body }) => (
				<section key={id} aria-labelledby={id}>
					<h2 id={id} className="mb-1.5 text-sm font-semibold text-slate-900">
						{title}
					</h2>
					<p>{body}</p>
				</section>
			))}
		</PublicPageShell>
	)
}

export default TermsPage
