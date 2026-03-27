/// <reference types="vitest" />
import { defineConfig, UserConfig } from "vite"
import react from "@vitejs/plugin-react"
import { fileURLToPath } from 'url'
import { dirname, resolve } from 'path'
import { visualizer } from 'rollup-plugin-visualizer'

const __filename = fileURLToPath(import.meta.url)
const __dirname = dirname(__filename)

// Fix for Framer Motion and React in production
const fixFramerMotionPlugin = () => {
  return {
    name: 'fix-framer-motion',
    config(config: UserConfig) {
      return {
        optimizeDeps: {
          ...config.optimizeDeps,
          include: [
            ...(config.optimizeDeps?.include || []),
            'framer-motion',
            'framer-motion/dom',
            'react',
            'react-dom',
            'react/jsx-runtime',
            'react-redux',
            'react-router-dom',
            '@tanstack/react-query',
            '@tanstack/react-query-devtools',
            'lucide-react'
          ],
          force: true,
          esbuildOptions: {
            target: 'esnext'
          }
        }
      }
    }
  }
}

// https://vitejs.dev/config/
export default defineConfig(({ mode }) => ({
  base: mode === 'production' ? '/' : '/',
  server: {
    host: '0.0.0.0',
    port: 5173,
    strictPort: false,
  },
  plugins: [
    react({
      jsxRuntime: 'automatic',
      jsxImportSource: 'react',
      babel: {
        plugins: [
          ['@babel/plugin-transform-react-jsx', { runtime: 'automatic' }]
        ]
      }
    }),
    fixFramerMotionPlugin(),
    ...(mode === 'production' ? [visualizer({ 
      open: true, 
      gzipSize: true, 
      brotliSize: true,
      filename: 'dist/stats.html'
    })] : [])
  ],
  resolve: {
    alias: {
      '@': resolve(__dirname, 'src'),
      'react': resolve(__dirname, 'node_modules/react'),
      'react-dom': resolve(__dirname, 'node_modules/react-dom'),
      'react/jsx-runtime': resolve(__dirname, 'node_modules/react/jsx-runtime'),
    },
    dedupe: ['react', 'react-dom', 'react/jsx-runtime'],
  },
  optimizeDeps: {
    exclude: [],
    include: [
      // React core
      'react', 
      'react-dom',
      'react-redux',
      'react-router-dom',
      '@tanstack/react-query',
      '@tanstack/react-query-devtools',
      
      // Radix UI primitive packages
      '@radix-ui/react-primitive',
      '@radix-ui/react-context',
      '@radix-ui/react-use-controllable-state',
      '@radix-ui/react-use-callback-ref',
      '@radix-ui/react-use-layout-effect',
      '@radix-ui/react-compose-refs',
      '@radix-ui/react-direction',
      '@radix-ui/react-slot',
      '@radix-ui/react-id',
      '@radix-ui/react-collection',
      '@radix-ui/react-use-previous',
      '@radix-ui/react-visually-hidden',
      '@radix-ui/react-presence',
      '@radix-ui/react-portal',
      '@radix-ui/react-primitive',
      '@radix-ui/react-use-escape-keydown',
      
      // Radix UI components
      '@radix-ui/react-accordion',
      '@radix-ui/react-dropdown-menu',
      '@radix-ui/react-scroll-area',
      '@radix-ui/react-separator',
      '@radix-ui/react-popover',
      '@radix-ui/react-dialog',
      '@radix-ui/react-alert-dialog',
      '@radix-ui/react-toast',
      '@radix-ui/react-label',
      '@radix-ui/react-switch',
      '@radix-ui/react-tabs',
      '@radix-ui/react-tooltip',
      '@radix-ui/react-avatar',
      '@radix-ui/react-select',
      '@radix-ui/react-progress',
      '@radix-ui/react-checkbox',
      '@radix-ui/react-radio-group',
      
      // Other UI libraries
      'lucide-react',
      'framer-motion',
      'recharts',
      'sonner',
      'react-toastify',
      'react-day-picker',
      'react-csv',
      '@tanstack/react-table',
      
      // Utilities
      'date-fns',
      'class-variance-authority',
      'uuid',
      'tailwind-merge',
      '@zxing/library'
    ]
  },
  test: {
    globals: true,
    environment: 'jsdom',
    setupFiles: ['./src/test/setup.ts'],
    include: ['src/**/*.test.{ts,tsx}'],
  },
  build: {
    outDir: 'dist',
    assetsDir: 'assets',
    sourcemap: mode === 'production' ? false : true,
    target: 'esnext',
    minify: 'terser',
    // Disable CSS minification to avoid build-time css-syntax warnings.
    // (Vite/cssnano/esbuild warnings are non-fatal but noisy in CI logs.)
    cssMinify: false,
    reportCompressedSize: true,
    chunkSizeWarningLimit: 1000,
    terserOptions: {
      compress: {
        drop_console: mode === 'production',
        drop_debugger: true,
        pure_funcs: mode === 'production' ? ['console.log', 'console.debug', 'console.info'] : []
      },
      format: {
        comments: false
      }
    },
    rollupOptions: {
      output: {
        manualChunks: {
          'react-vendor': ['react', 'react-dom', 'react-router-dom'],
          'ui-vendor': ['framer-motion', '@radix-ui/react-primitive', 'lucide-react'],
          'state-vendor': ['@reduxjs/toolkit', 'react-redux', '@tanstack/react-query'],
          'form-vendor': ['react-hook-form', 'zod', '@hookform/resolvers'],
          'chart-vendor': ['recharts'],
        },
        assetFileNames: (assetInfo) => {
          let extType = assetInfo.name?.split('.').at(-1) || 'asset';
          if (/png|jpe?g|svg|gif|tiff|bmp|ico/i.test(extType)) {
            extType = 'images';
          } else if (/woff|woff2|eot|ttf|otf/i.test(extType)) {
            extType = 'fonts';
          }
          return `assets/${extType}/[name]-[hash][extname]`;
        },
        chunkFileNames: 'assets/js/[name]-[hash].js',
        entryFileNames: 'assets/js/[name]-[hash].js',
      }
    }
  }
}))
