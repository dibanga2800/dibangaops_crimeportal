import { useLayoutEffect } from 'react'

/**
 * Marks the document as rendering inside the redesigned app chrome
 * (`<html data-ui="app">`). Global styles that change the base look of the
 * document — the Inter and Plus Jakarta Sans typefaces and form-control defaults — are scoped to
 * this attribute so the login page, which never mounts this hook, keeps its
 * original appearance.
 *
 * A counter makes nested shells (e.g. the 404 page rendered inside Layout)
 * safe: the attribute is removed only when the last shell unmounts.
 */
let mountedShells = 0

export function useAppChrome(): void {
	useLayoutEffect(() => {
		mountedShells += 1
		document.documentElement.setAttribute('data-ui', 'app')
		return () => {
			mountedShells = Math.max(0, mountedShells - 1)
			if (mountedShells === 0) {
				document.documentElement.removeAttribute('data-ui')
			}
		}
	}, [])
}
