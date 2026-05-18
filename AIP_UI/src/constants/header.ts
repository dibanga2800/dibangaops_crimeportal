// Default avatar used when no user session is available
export const DEFAULT_AVATAR = '/A1logo1.png'

// Common button styles
export const BUTTON_STYLES = {
  primary: "bg-primary text-primary-foreground hover:bg-primary/90",
  secondary: "bg-secondary text-secondary-foreground hover:bg-secondary/80",
  ghost: "hover:bg-accent hover:text-accent-foreground",
  link: "text-primary underline-offset-4 hover:underline"
} as const;

// Common class names
export const COMMON_CLASSES = {
  mobileHeader:
    "flex h-18 items-center justify-between px-5 bg-header-bg text-header-text lg:hidden border-b border-header-border dark:bg-gray-900 dark:text-white dark:border-gray-800",
  desktopHeader:
    "hidden lg:flex h-20 items-center px-4 bg-header-bg text-header-text border-b border-header-border dark:bg-gray-900 dark:text-white dark:border-gray-800",
  sheetContent: "w-[340px] sm:w-[400px] p-0 flex flex-col h-full bg-blue-950 text-white border-r border-blue-900",
  searchInput:
    "w-full pl-9 h-10 rounded-full bg-white text-gray-900 placeholder:text-gray-500 border border-gray-200 focus-visible:ring-2 focus-visible:ring-accent-teal/60 focus-visible:ring-offset-0 focus-visible:border-accent-teal dark:bg-[#1A1A1A] dark:text-gray-100 dark:placeholder:text-gray-400 dark:border-gray-700"
} as const;

// Navigation related constants
export const NOTIFICATION_COUNT = 3;
export const LOGO_SIZES = {
  mobile: "h-28",
  sheet: "h-32",
  ipad: "h-34",
  desktop: "h-14"
} as const;

/** Consistent touch targets and icon sizes for the mobile app bar */
export const MOBILE_HEADER = {
  bar: "h-14 min-h-14 px-3 gap-2",
  iconButton: "h-9 w-9 shrink-0 p-0",
  icon: "h-5 w-5",
  logo: "h-9 w-auto max-w-[7.5rem] sm:max-w-[9rem] object-contain object-left",
  actions: "gap-1.5 sm:gap-2",
} as const;
