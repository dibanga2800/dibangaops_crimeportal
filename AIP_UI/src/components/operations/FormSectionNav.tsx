import { useEffect, useState } from 'react'
import { cn } from '@/lib/utils'

export interface FormSection {
	id: string
	label: string
	/** Short hint shown under the label in the wide rail. */
	hint?: string
}

function useActiveSection(sectionIds: string[]) {
	const [active, setActive] = useState(sectionIds[0])
	const key = sectionIds.join('|')
	useEffect(() => {
		const ids = key.split('|')
		const elements = ids.map((id) => document.getElementById(id)).filter(Boolean) as HTMLElement[]
		if (!elements.length) return
		const visible = new Map<string, number>()
		const observer = new IntersectionObserver(
			(entries) => {
				entries.forEach((entry) => visible.set(entry.target.id, entry.isIntersecting ? entry.intersectionRatio : 0))
				// The first section (in document order) that is meaningfully on screen wins.
				const first = ids.find((id) => (visible.get(id) ?? 0) > 0.15)
				if (first) setActive(first)
			},
			{ threshold: [0, 0.15, 0.4, 0.75], rootMargin: '-10% 0px -45% 0px' },
		)
		elements.forEach((el) => observer.observe(el))
		return () => observer.disconnect()
	}, [key])
	return [active, setActive] as const
}

const scrollParent = (el: HTMLElement): HTMLElement | null => {
	let node = el.parentElement
	while (node) {
		const { overflowY } = getComputedStyle(node)
		if ((overflowY === 'auto' || overflowY === 'scroll') && node.scrollHeight > node.clientHeight) return node
		node = node.parentElement
	}
	return null
}

/** Scroll only the nearest scrolling container (not the dialog or page around it). */
const jump = (id: string) => {
	const el = document.getElementById(id)
	if (!el) return
	const reduce = window.matchMedia?.('(prefers-reduced-motion: reduce)').matches
	const container = scrollParent(el)
	const marginTop = parseFloat(getComputedStyle(el).scrollMarginTop) || 0
	if (container) {
		const top = el.getBoundingClientRect().top - container.getBoundingClientRect().top + container.scrollTop - marginTop
		container.scrollTo({ top, behavior: reduce ? 'auto' : 'smooth' })
	} else {
		window.scrollTo({ top: el.getBoundingClientRect().top + window.scrollY - marginTop, behavior: reduce ? 'auto' : 'smooth' })
	}
	const focusable = el.querySelector<HTMLElement>('input, textarea, button[role="combobox"], select')
	window.setTimeout(() => focusable?.focus({ preventScroll: true }), reduce ? 0 : 450)
}


/**
 * Navigation for long forms. Use variant="rail" beside the form on wide screens
 * and variant="strip" at the top of the form for narrower screens (a horizontal
 * step strip pinned to the top of the scroll area). Both follow the section in view and jump to a section on click.
 */
export function FormSectionNav({
	sections,
	variant,
	className,
}: {
	sections: FormSection[]
	variant: 'rail' | 'strip'
	className?: string
}) {
	const [active, setActive] = useActiveSection(sections.map((s) => s.id))

	if (variant === 'rail') {
		return (
			<nav aria-label="Form sections" className={cn('sticky top-6 hidden w-52 shrink-0 self-start xl:block', className)}>
				<ol className="relative space-y-0.5 border-l border-border">
					{sections.map((section, index) => {
						const isActive = section.id === active
						return (
							<li key={section.id}>
								<button
									type="button"
									onClick={() => {
										setActive(section.id)
										jump(section.id)
									}}
									aria-current={isActive ? 'step' : undefined}
									className={cn(
										'!relative !-ml-px !flex w-full !items-start !justify-start gap-3 !rounded-none !border-0 !border-l-2 !px-3 !py-2 text-left transition-colors',
										isActive ? '!border-primary text-foreground' : '!border-transparent text-muted-foreground hover:text-foreground',
									)}
								>
									<span
										className={cn(
											'figures mt-px flex h-5 w-5 shrink-0 items-center justify-center rounded-full text-[11px] font-semibold',
											isActive ? 'bg-primary text-primary-foreground' : 'bg-muted text-muted-foreground',
										)}
									>
										{index + 1}
									</span>
									<span className="min-w-0">
										<span className={cn('block text-sm', isActive ? 'font-semibold' : 'font-medium')}>{section.label}</span>
										{section.hint ? <span className="block text-xs font-normal text-muted-foreground">{section.hint}</span> : null}
									</span>
								</button>
							</li>
						)
					})}
				</ol>
			</nav>
		)
	}

	return (
			<nav
				aria-label="Form sections"
				className={cn('sticky top-0 z-20 -mx-4 mb-2 overflow-x-auto border-b border-border bg-background/95 px-4 py-2 backdrop-blur sm:-mx-6 sm:px-6 xl:hidden', className)}
			>
				<ol className="flex min-w-max items-center gap-1">
					{sections.map((section, index) => {
						const isActive = section.id === active
						return (
							<li key={section.id}>
								<button
									type="button"
									onClick={() => {
										setActive(section.id)
										jump(section.id)
									}}
									aria-current={isActive ? 'step' : undefined}
									className={cn(
										'!flex !items-center gap-1.5 !rounded-full !border-0 !px-3 !py-1.5 !text-[13px] transition-colors',
										isActive ? 'bg-foreground font-semibold text-background' : 'font-medium text-muted-foreground hover:bg-muted hover:text-foreground',
									)}
								>
									<span className="figures text-[11px] opacity-70">{index + 1}</span>
									{section.label}
								</button>
							</li>
						)
					})}
				</ol>
			</nav>
	)
}
