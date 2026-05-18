import { Moon, Sun } from "lucide-react"

import { Button } from "@/components/ui/button"
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu"
import { useTheme } from "@/components/theme-provider"
import { cn } from "@/lib/utils"

type ThemeToggleSize = "default" | "compact"

interface ThemeToggleProps {
	size?: ThemeToggleSize
	className?: string
}

const themeToggleSizes: Record<
	ThemeToggleSize,
	{ button: string; icon: string; strokeWidth: number }
> = {
	default: {
		button: "h-10 w-10",
		icon: "h-5 w-5",
		strokeWidth: 2,
	},
	compact: {
		button: "h-8 w-8 min-h-8 min-w-8 p-0",
		icon: "h-5 w-5",
		strokeWidth: 2.5,
	},
}

export function ThemeToggle({ size = "default", className }: ThemeToggleProps) {
  const { setTheme, resolvedTheme } = useTheme()
	const dimensions = themeToggleSizes[size]

  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <Button
          variant="ghost"
          className={cn(
						"relative flex shrink-0 items-center justify-center shadow-none transition-colors focus-visible:ring-2 focus-visible:ring-neutral-400 focus-visible:ring-offset-2 focus-visible:ring-offset-white dark:focus-visible:ring-gray-500 dark:focus-visible:ring-offset-gray-900",
						size === "compact"
							? "rounded-md border border-gray-200/90 bg-white hover:bg-neutral-50 dark:border-gray-600 dark:bg-gray-800 dark:hover:bg-gray-700"
							: "rounded-md border border-gray-200 bg-white text-neutral-900 hover:bg-neutral-50 dark:border-gray-600 dark:bg-gray-800 dark:text-white dark:hover:bg-gray-700",
						dimensions.button,
						className,
					)}
        >
          <Sun
            strokeWidth={dimensions.strokeWidth}
            className={cn(
							dimensions.icon,
							"text-neutral-900 rotate-0 scale-100 transition-all dark:-rotate-90 dark:scale-0",
						)}
          />
          <Moon
            strokeWidth={dimensions.strokeWidth}
            className={cn(
							"absolute",
							dimensions.icon,
							"rotate-90 scale-0 text-neutral-900 transition-all dark:rotate-0 dark:scale-100 dark:text-white",
						)}
          />
          <span className="sr-only">Toggle theme</span>
        </Button>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end">
        <DropdownMenuItem onClick={() => setTheme("light")}>
          <Sun className="mr-2 h-4 w-4" />
          Light
        </DropdownMenuItem>
        <DropdownMenuItem onClick={() => setTheme("dark")}>
          <Moon className="mr-2 h-4 w-4" />
          Dark
        </DropdownMenuItem>
        <DropdownMenuItem onClick={() => setTheme("system")}>
          <span className="mr-2 inline-flex h-4 w-4 items-center justify-center text-xs">
            {resolvedTheme === "dark" ? "D" : "L"}
          </span>
          System
        </DropdownMenuItem>
      </DropdownMenuContent>
    </DropdownMenu>
  )
}
