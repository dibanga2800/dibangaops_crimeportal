import { useEffect, useMemo, useState } from 'react'
import { DayPicker, type DateRange } from 'react-day-picker'
import { enGB } from 'date-fns/locale'
import { format, isValid, parseISO, startOfDay, startOfMonth, startOfYear, subDays, subMonths } from 'date-fns'
import { CalendarRange, ChevronDown, ChevronLeft, ChevronRight } from 'lucide-react'
import { Popover, PopoverContent, PopoverTrigger } from '@/components/ui/popover'
import { Button } from '@/components/ui/button'
import { cn } from '@/lib/utils'

export interface DateRangeFieldProps {
	/** yyyy-MM-dd, or '' for no start date. */
	from: string
	/** yyyy-MM-dd, or '' for no end date. */
	to: string
	onFromChange: (value: string) => void
	onToChange: (value: string) => void
	className?: string
	fromLabel?: string
	toLabel?: string
	min?: string
	max?: string
	/** Text on the button when no dates are chosen. */
	placeholder?: string
}

const toDate = (value?: string) => {
	if (!value) return undefined
	const d = parseISO(value)
	return isValid(d) ? d : undefined
}
const toValue = (d?: Date) => (d ? format(d, 'yyyy-MM-dd') : '')
const label = (d: Date, withYear = true) => format(d, withYear ? 'd MMM yyyy' : 'd MMM', { locale: enGB })

const PRESETS: Array<{ label: string; range: () => DateRange }> = [
	{ label: 'Today', range: () => ({ from: startOfDay(new Date()), to: startOfDay(new Date()) }) },
	{ label: 'Last 7 days', range: () => ({ from: subDays(startOfDay(new Date()), 6), to: startOfDay(new Date()) }) },
	{ label: 'Last 30 days', range: () => ({ from: subDays(startOfDay(new Date()), 29), to: startOfDay(new Date()) }) },
	{ label: 'This month', range: () => ({ from: startOfMonth(new Date()), to: startOfDay(new Date()) }) },
	{ label: 'Last 3 months', range: () => ({ from: subMonths(startOfDay(new Date()), 3), to: startOfDay(new Date()) }) },
	{ label: 'Year to date', range: () => ({ from: startOfYear(new Date()), to: startOfDay(new Date()) }) },
	{ label: 'Last 12 months', range: () => ({ from: subMonths(startOfDay(new Date()), 12), to: startOfDay(new Date()) }) },
]

const sameDay = (a?: Date, b?: Date) => (!a && !b) || (!!a && !!b && toValue(a) === toValue(b))

/**
 * One button that opens a two-month calendar with common presets. Dates always
 * read day-first (21 Oct 2026) whatever the browser's language, and nothing is
 * applied until the user presses Apply.
 */
export function DateRangePicker({
	from,
	to,
	onFromChange,
	onToChange,
	className,
	min,
	max,
	placeholder = 'Any date',
}: DateRangeFieldProps) {
	const [open, setOpen] = useState(false)
	const applied = useMemo<DateRange | undefined>(() => {
		const f = toDate(from)
		const t = toDate(to)
		return f || t ? { from: f, to: t } : undefined
	}, [from, to])
	const [draft, setDraft] = useState<DateRange | undefined>(applied)
	const [twoMonths, setTwoMonths] = useState(() => (typeof window !== 'undefined' ? window.innerWidth >= 640 : true))

	useEffect(() => {
		if (open) setDraft(applied)
	}, [open, applied])

	useEffect(() => {
		const onResize = () => setTwoMonths(window.innerWidth >= 640)
		window.addEventListener('resize', onResize)
		return () => window.removeEventListener('resize', onResize)
	}, [])

	const text = (() => {
		if (!applied?.from && !applied?.to) return placeholder
		if (applied.from && applied.to) {
			if (sameDay(applied.from, applied.to)) return label(applied.from)
			const sameYear = applied.from.getFullYear() === applied.to.getFullYear()
			return `${label(applied.from, !sameYear)} – ${label(applied.to)}`
		}
		if (applied.from) return `From ${label(applied.from)}`
		return `Until ${label(applied.to as Date)}`
	})()

	const apply = (range: DateRange | undefined) => {
		onFromChange(toValue(range?.from))
		onToChange(toValue(range?.to ?? range?.from))
		setOpen(false)
	}

	const activePreset = PRESETS.find((p) => {
		const r = p.range()
		return sameDay(r.from, draft?.from) && sameDay(r.to, draft?.to)
	})?.label

	return (
		<Popover open={open} onOpenChange={setOpen}>
			<PopoverTrigger asChild>
				<button
					type="button"
					aria-label={`Date range: ${text}`}
					className={cn(
						'!flex !h-9 w-full !items-center gap-2 !rounded-md !border !border-input bg-card !px-2.5 !py-0 text-left text-sm font-normal text-foreground transition-colors hover:!border-foreground/30 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring/30 data-[state=open]:!border-ring sm:w-auto',
						className,
					)}
				>
					<CalendarRange className="h-4 w-4 shrink-0 text-muted-foreground" aria-hidden />
					<span className={cn('figures min-w-0 flex-1 truncate', !applied && 'text-muted-foreground')}>{text}</span>
					<ChevronDown className="h-4 w-4 shrink-0 text-muted-foreground" aria-hidden />
				</button>
			</PopoverTrigger>
			<PopoverContent align="start" className="w-auto max-w-[calc(100vw-1.5rem)] p-0">
				<div className="flex flex-col sm:flex-row">
					<ul className="flex gap-1 overflow-x-auto border-b border-border p-2 sm:w-40 sm:flex-col sm:overflow-visible sm:border-b-0 sm:border-r" aria-label="Quick ranges">
						{PRESETS.map((preset) => (
							<li key={preset.label}>
								<button
									type="button"
									onClick={() => setDraft(preset.range())}
									className={cn(
										'!w-full whitespace-nowrap !rounded-md !border-0 !px-2.5 !py-1.5 text-left text-[13px] transition-colors',
										activePreset === preset.label ? 'bg-accent font-semibold text-accent-foreground' : 'text-foreground hover:bg-muted',
									)}
								>
									{preset.label}
								</button>
							</li>
						))}
					</ul>
					<div className="p-3">
						<DayPicker
							mode="range"
							locale={enGB}
							weekStartsOn={1}
							numberOfMonths={twoMonths ? 2 : 1}
							defaultMonth={draft?.from ? (twoMonths ? subMonths(draft.to ?? draft.from, 1) : draft.from) : undefined}
							selected={draft}
							onSelect={setDraft}
							disabled={[...(min ? [{ before: toDate(min) as Date }] : []), ...(max ? [{ after: toDate(max) as Date }] : [])]}
							showOutsideDays
							components={{
								Chevron: ({ orientation }) =>
									orientation === 'left' ? <ChevronLeft className="h-4 w-4" aria-hidden /> : <ChevronRight className="h-4 w-4" aria-hidden />,
							}}
							classNames={{
								root: 'relative',
								months: 'flex flex-col gap-6 sm:flex-row',
								month: 'space-y-2',
								month_caption: 'flex h-8 items-center justify-center',
								caption_label: 'text-sm font-semibold text-foreground',
								nav: 'absolute inset-x-0 top-0 z-10 flex h-8 items-center justify-between',
								button_previous: '!inline-flex !h-8 !w-8 !items-center !justify-center !rounded-md !border-0 !p-0 text-muted-foreground hover:bg-muted hover:text-foreground disabled:opacity-40',
								button_next: '!inline-flex !h-8 !w-8 !items-center !justify-center !rounded-md !border-0 !p-0 text-muted-foreground hover:bg-muted hover:text-foreground disabled:opacity-40',
								month_grid: 'border-collapse',
								weekdays: '',
								weekday: 'h-8 w-9 text-center text-[11px] font-medium text-muted-foreground',
								week: '',
								day: 'p-0 text-center text-sm',
								day_button:
									'figures !inline-flex !h-9 !w-9 !items-center !justify-center !rounded-md !border-0 !p-0 font-normal text-foreground hover:bg-muted focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring/40',
								today: '[&>button]:font-semibold [&>button]:underline [&>button]:decoration-signal [&>button]:decoration-2 [&>button]:underline-offset-4',
								outside: 'opacity-40',
								disabled: 'opacity-30 [&>button]:cursor-not-allowed',
								range_middle: 'bg-accent [&>button]:!rounded-none [&>button]:text-accent-foreground',
								range_start: 'rounded-l-md bg-accent [&>button]:!bg-primary [&>button]:!text-primary-foreground',
								range_end: 'rounded-r-md bg-accent [&>button]:!bg-primary [&>button]:!text-primary-foreground',
								selected: '',
								hidden: 'invisible',
							}}
						/>
					</div>
				</div>
				<div className="flex items-center justify-between gap-3 border-t border-border px-3 py-2.5">
					<p className="figures text-xs text-muted-foreground">
						{draft?.from ? (draft.to && !sameDay(draft.from, draft.to) ? `${label(draft.from)} – ${label(draft.to)}` : label(draft.from)) : 'Pick a start date'}
					</p>
					<div className="flex gap-2">
						<Button type="button" variant="ghost" size="sm" onClick={() => apply(undefined)}>
							Clear
						</Button>
						<Button type="button" size="sm" onClick={() => apply(draft)} disabled={!draft?.from}>
							Apply
						</Button>
					</div>
				</div>
			</PopoverContent>
		</Popover>
	)
}
