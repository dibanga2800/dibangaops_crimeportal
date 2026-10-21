import React, { useState, useEffect } from 'react'
import { Outlet, useLocation } from 'react-router-dom'
import { motion, AnimatePresence, useReducedMotion } from 'framer-motion'
import { X } from 'lucide-react'
import { Footer } from '../Footer'
import { SidebarNavigation } from '@/components/dashboard/SidebarNavigation'
import { Header } from '../Header'
import { ProductMark } from './ProductMark'
import { SidebarUserCard } from './SidebarUserCard'
import { MobileTabBar } from './MobileTabBar'
import { useAppChrome } from '@/lib/app-chrome'

interface LayoutProps {
  children?: React.ReactNode
}

/**
 * Main application shell: ink sidebar, light top bar, paper content area.
 */
export const Layout = ({ children }: LayoutProps) => {
  useAppChrome()
  const [isMobileOpen, setIsMobileOpen] = useState(false)
  const location = useLocation()
  const prefersReducedMotion = useReducedMotion()

  // Close mobile menu on route change
  useEffect(() => {
    setIsMobileOpen(false)
  }, [location.pathname])

  // Close the mobile drawer with Escape
  useEffect(() => {
    if (!isMobileOpen) return
    const onKey = (e: KeyboardEvent) => {
      if (e.key === 'Escape') setIsMobileOpen(false)
    }
    window.addEventListener('keydown', onKey)
    return () => window.removeEventListener('keydown', onKey)
  }, [isMobileOpen])

  const sidebarBody = (
    <>
      <div className="flex h-16 shrink-0 items-center justify-between gap-2 border-b border-sidebar-border px-5">
        <ProductMark />
        {isMobileOpen && (
          <button
            type="button"
            onClick={() => setIsMobileOpen(false)}
            className="!p-1.5 text-sidebar-foreground hover:bg-sidebar-accent hover:text-white lg:hidden"
            aria-label="Close navigation"
          >
            <X className="h-5 w-5" />
          </button>
        )}
      </div>

      <nav aria-label="Main" className="flex-1 overflow-y-auto overscroll-contain [scrollbar-color:hsl(var(--sidebar-accent))_transparent]">
        <SidebarNavigation
          onNavigate={() => setIsMobileOpen(false)}
          isMobileOpen={isMobileOpen}
          onMobileClose={() => setIsMobileOpen(false)}
        />
      </nav>

      <SidebarUserCard />
    </>
  )

  return (
    <div className="min-h-screen bg-background">
      <a
        href="#main-content"
        className="sr-only focus:not-sr-only focus:fixed focus:left-3 focus:top-3 focus:z-[60] focus:rounded-md focus:bg-card focus:px-3 focus:py-2 focus:text-sm focus:shadow-float"
      >
        Skip to content
      </a>

      {/* Desktop sidebar */}
      <aside className="fixed inset-y-0 left-0 z-30 sidebar-gloss hidden w-64 flex-col text-sidebar-foreground lg:flex">
        {sidebarBody}
      </aside>

      {/* Mobile sidebar */}
      <AnimatePresence>
        {isMobileOpen && (
          <>
            <motion.div
              key="scrim"
              className="fixed inset-0 z-40 bg-ink/55 backdrop-blur-[2px] lg:hidden"
              onClick={() => setIsMobileOpen(false)}
              initial={{ opacity: 0 }}
              animate={{ opacity: 1 }}
              exit={{ opacity: 0 }}
              transition={{ duration: prefersReducedMotion ? 0 : 0.15 }}
              aria-hidden
            />
            <motion.aside
              key="drawer"
              className="fixed inset-y-0 left-0 z-50 sidebar-gloss flex w-72 max-w-[85vw] flex-col text-sidebar-foreground shadow-float lg:hidden"
              initial={{ x: '-100%' }}
              animate={{ x: 0 }}
              exit={{ x: '-100%' }}
              transition={{ duration: prefersReducedMotion ? 0 : 0.2, ease: [0.2, 0.8, 0.2, 1] }}
              role="dialog"
              aria-modal="true"
              aria-label="Navigation"
            >
              {sidebarBody}
            </motion.aside>
          </>
        )}
      </AnimatePresence>

      {/* Main column. min-w-0 lets wide tables scroll inside their own containers. */}
      <div className="flex min-h-screen min-w-0 flex-col overflow-x-clip lg:pl-64">
        <Header />

        <main id="main-content" tabIndex={-1} className="flex min-w-0 flex-1 flex-col outline-none">
          <div className="mx-auto w-full min-w-0 max-w-[1480px] flex-1 px-4 pb-8 pt-6 sm:px-6 lg:px-8 lg:py-8">
            <AnimatePresence mode="wait" initial={false}>
              <motion.div
                key={location.pathname}
                initial={{ opacity: 0 }}
                animate={{ opacity: 1 }}
                exit={{ opacity: 0 }}
                transition={{ duration: prefersReducedMotion ? 0 : 0.12 }}
                className="min-w-0"
              >
                {children || <Outlet />}
              </motion.div>
            </AnimatePresence>
          </div>
        </main>

        <div className="pb-[calc(4rem+env(safe-area-inset-bottom))] lg:pb-0">
          <Footer />
        </div>
      </div>

      <MobileTabBar onOpenMenu={() => setIsMobileOpen(true)} />
    </div>
  )
}

export { Layout as default }
