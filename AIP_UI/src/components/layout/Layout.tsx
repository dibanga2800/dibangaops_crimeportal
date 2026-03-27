import React, { useState, useEffect } from 'react'
import { Outlet, useLocation, useNavigate } from 'react-router-dom'
import { useTheme } from '../theme-provider'
import { Footer } from '../Footer'
import { ThemeToggle } from '../theme-toggle'
import { SidebarNavigation } from '@/components/dashboard/SidebarNavigation'
import { Logo } from './Logo'
import { UserMenu } from './UserMenu'
import { SearchBar } from './SearchBar'
import { motion, AnimatePresence } from 'framer-motion'
import { Cog } from 'lucide-react'
import { Header } from '../Header'

interface LayoutProps {
  children?: React.ReactNode
}

/**
 * Main layout component that provides the application structure
 * Includes sidebar navigation, header, main content area, and footer
 */
export const Layout = ({ children }: LayoutProps) => {
  const [isMobileOpen, setIsMobileOpen] = useState(false)
  const location = useLocation()
  const { theme } = useTheme()
  const navigate = useNavigate()

  // Close mobile menu on route change
  useEffect(() => {
    setIsMobileOpen(false)
  }, [location.pathname])

  return (
    <div className="min-h-screen flex flex-col overflow-hidden">
      <div className="flex flex-1">
        {/* Desktop Sidebar - hidden on mobile */}
        <aside className="hidden lg:flex w-64 flex-col fixed h-screen bg-[#1A1A1A] text-white border-r border-[#2A2A2A] z-30" style={{ backgroundColor: '#1A1A1A' }}>
          {/* Logo Section */}
          <div className="h-[var(--header-height)] border-b border-[#2A2A2A] px-6 pt-[27px] flex items-center">
            <Logo />
          </div>

          {/* Navigation */}
          <div className="flex-1 overflow-y-auto">
            <nav className="grid gap-1 p-2">
              <SidebarNavigation
                onNavigate={() => setIsMobileOpen(false)}
                isMobileOpen={isMobileOpen}
                onMobileClose={() => setIsMobileOpen(false)}
              />
            </nav>
          </div>
        </aside>

        {/* Mobile Sidebar */}
        {isMobileOpen && (
          <aside className="fixed inset-y-0 left-0 z-50 lg:hidden w-64 flex flex-col bg-[#1A1A1A] text-white border-r border-[#2A2A2A]" style={{ backgroundColor: '#1A1A1A' }}>
            {/* Logo Section */}
            <div className="h-[var(--header-height)] border-b border-[#2A2A2A] px-6 pt-[27px] flex items-center justify-between">
              <Logo />
              <button
                onClick={() => setIsMobileOpen(false)}
                className="p-1 rounded-md hover:bg-gray-600 transition-colors"
                aria-label="Close sidebar"
              >
                <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
                </svg>
              </button>
            </div>

            {/* Navigation */}
            <div className="flex-1 overflow-y-auto">
              <nav className="grid gap-1 p-2">
                <SidebarNavigation
                  onNavigate={() => setIsMobileOpen(false)}
                  isMobileOpen={isMobileOpen}
                  onMobileClose={() => setIsMobileOpen(false)}
                />
              </nav>
            </div>
          </aside>
        )}

        {/* Mobile Overlay */}
        {isMobileOpen && (
          <div 
            className="fixed inset-0 bg-black bg-opacity-50 z-40 lg:hidden"
            onClick={() => setIsMobileOpen(false)}
          />
        )}

        {/* Main Content Area - full width on mobile, adjusted on desktop. min-w-0 allows flex child to shrink and prevent overflow. */}
        <div className="flex-1 flex flex-col min-w-0 w-full lg:ml-64 overflow-x-hidden">
          {/* Header */}
          <Header onMobileMenuClick={() => setIsMobileOpen(!isMobileOpen)} />

          {/* Mobile Search */}
          <SearchBar 
            isOpen={false}
            onClose={() => {}}
          />

          {/* Page Content */}
          <main className="flex-1 bg-background text-foreground flex flex-col min-w-0 overflow-x-hidden">
            <div className="flex-1 w-full min-w-0 max-w-full mx-auto px-4 sm:px-6 lg:px-8 py-4 sm:py-6 md:py-8">
              <div className="grid gap-4 sm:gap-6 md:gap-8 min-w-0">
                <AnimatePresence mode="wait" initial={false}>
                  <motion.div
                    key={location.pathname}
                    initial={{ opacity: 0 }}
                    animate={{ opacity: 1 }}
                    exit={{ opacity: 0 }}
                    transition={{ duration: 0.15 }}
                    className="h-full min-w-0"
                  >
                    {children || <Outlet />}
                  </motion.div>
                </AnimatePresence>
              </div>
            </div>
          </main>
          
          {/* Footer - full width across all screen sizes */}
          <Footer />
        </div>
      </div>
    </div>
  )
}

export { Layout as default }