export interface CameraResolution {
	width: number
	height: number
}

const MIN_SHORT_SIDE = 720
const MIN_LONG_SIDE = 1280

/** True when stream meets at least 1280×720 in either orientation */
export const meetsMinimumScanResolution = ({ width, height }: CameraResolution): boolean => {
	if (width <= 0 || height <= 0) {
		return false
	}
	const shortSide = Math.min(width, height)
	const longSide = Math.max(width, height)
	return shortSide >= MIN_SHORT_SIDE && longSide >= MIN_LONG_SIDE
}

export const formatResolution = (width: number, height: number): string =>
	`${width} × ${height}`

export const buildLowResolutionWarning = (width: number, height: number): string => {
	const shortSide = Math.min(width, height)
	const longSide = Math.max(width, height)
	return (
		`Detected resolution ${formatResolution(width, height)} (short side ${shortSide}px, long side ${longSide}px). ` +
		`Need at least ${MIN_SHORT_SIDE}px on the short side and ${MIN_LONG_SIDE}px on the long side. ` +
		'Move the barcode closer so it fills most of the frame, or switch to a higher-resolution camera if available.'
	)
}

/** Best-effort constraints from device capabilities when current stream is below minimum */
export const buildUpgradeConstraintsFromCapabilities = (
	capabilities: MediaTrackCapabilities
): MediaTrackConstraints | null => {
	const maxWidth = capabilities.width?.max
	const maxHeight = capabilities.height?.max
	if (!maxWidth || !maxHeight) {
		return null
	}
	return {
		width: { ideal: maxWidth },
		height: { ideal: maxHeight },
	}
}
