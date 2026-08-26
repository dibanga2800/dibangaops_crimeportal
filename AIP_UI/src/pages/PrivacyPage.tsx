import { PublicPageShell } from '@/components/layout/PublicPageShell'

const PrivacyPage = () => {
	return (
		<PublicPageShell
			title="Privacy statement"
			description="How customer data is handled on the DibangOps Crime Portal."
		>
			<p>
				DibangOps Crime Portal is built with a security-first mindset. All customer data is handled
				according to modern SaaS best practices, including strict access controls and separation
				between production and development environments.
			</p>
			<p>
				This platform is a live production service and can be configured to meet your
				organisation&apos;s data protection, retention, and compliance requirements.
			</p>
		</PublicPageShell>
	)
}

export default PrivacyPage
