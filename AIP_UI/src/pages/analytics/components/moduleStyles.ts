/** Shared class names for the analytics hub modules. */

export const RISK_CHIP: Record<string, string> = {
	low: 'bg-recovered-soft text-recovered',
	medium: 'bg-warning-soft text-warning',
	high: 'bg-loss-soft text-loss',
	critical: 'bg-loss text-white',
}
export const SUBTABS_LIST = 'mb-4 inline-flex h-auto w-full items-center justify-start gap-0 rounded-md border border-border bg-muted/70 p-0.5 sm:w-auto'
export const SUBTABS_TRIGGER =
	'flex-1 whitespace-nowrap rounded-[5px] border-0 px-3 py-1 text-[13px] font-medium text-muted-foreground shadow-none data-[state=active]:bg-card data-[state=active]:text-foreground data-[state=active]:shadow-[0_1px_2px_hsl(var(--foreground)/0.08)] sm:flex-none'
