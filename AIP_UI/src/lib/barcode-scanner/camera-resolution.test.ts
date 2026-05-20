import { describe, it, expect } from 'vitest'
import {
	meetsMinimumScanResolution,
	formatResolution,
	buildLowResolutionWarning,
	buildUpgradeConstraintsFromCapabilities,
} from '@/lib/barcode-scanner/camera-resolution'

describe('camera-resolution', () => {
	it('passes portrait mobile resolutions such as 1080×1920', () => {
		expect(meetsMinimumScanResolution({ width: 1080, height: 1920 })).toBe(true)
	})

	it('passes landscape 1920×1080', () => {
		expect(meetsMinimumScanResolution({ width: 1920, height: 1080 })).toBe(true)
	})

	it('passes exact minimum 1280×720', () => {
		expect(meetsMinimumScanResolution({ width: 1280, height: 720 })).toBe(true)
	})

	it('fails VGA 640×480', () => {
		expect(meetsMinimumScanResolution({ width: 640, height: 480 })).toBe(false)
	})

	it('fails when short side is below 720', () => {
		expect(meetsMinimumScanResolution({ width: 1280, height: 480 })).toBe(false)
	})

	it('fails when long side is below 1280', () => {
		expect(meetsMinimumScanResolution({ width: 800, height: 600 })).toBe(false)
	})

	it('fails invalid dimensions', () => {
		expect(meetsMinimumScanResolution({ width: 0, height: 1920 })).toBe(false)
	})

	it('formats resolution for display', () => {
		expect(formatResolution(1080, 1920)).toBe('1080 × 1920')
	})

	it('builds warning with short and long side guidance', () => {
		const message = buildLowResolutionWarning(640, 480)
		expect(message).toContain('640 × 480')
		expect(message).toContain('short side 480px')
		expect(message).toContain('long side 640px')
	})

	it('builds upgrade constraints from capabilities max dimensions', () => {
		const constraints = buildUpgradeConstraintsFromCapabilities({
			width: { min: 640, max: 1920 },
			height: { min: 480, max: 1080 },
		})
		expect(constraints).toEqual({
			width: { ideal: 1920 },
			height: { ideal: 1080 },
		})
	})

	it('returns null when capabilities lack max dimensions', () => {
		expect(buildUpgradeConstraintsFromCapabilities({})).toBeNull()
	})
})
