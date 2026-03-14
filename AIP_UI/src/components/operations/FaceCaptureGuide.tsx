import React, { useEffect, useRef, useState, useCallback } from 'react'
import { offenderRecognitionApi } from '@/services/api/offenderRecognition'

interface FaceCaptureGuideProps {
	videoRef: React.RefObject<HTMLVideoElement | null>
	onCapture: (dataUrl: string) => void
	onCancel: () => void
	isSearching?: boolean
}

const POLL_INTERVAL_MS = 800
const FACE_STABLE_FRAMES = 2
const MIN_VIDEO_SIZE = 100
const POLL_START_DELAY_MS = 1200

/**
 * Guided face capture: video with live overlay.
 * - Red oval when no face detected
 * - Green oval when face detected
 * - Auto-captures when face is detected for FACE_STABLE_FRAMES consecutive polls
 */
export const FaceCaptureGuide: React.FC<FaceCaptureGuideProps> = ({
	videoRef,
	onCapture,
	onCancel,
	isSearching = false,
}) => {
	const overlayRef = useRef<HTMLCanvasElement | null>(null)
	const captureCanvasRef = useRef<HTMLCanvasElement | null>(null)
	const faceDetectedRef = useRef(false)
	const [faceDetected, setFaceDetected] = useState(false)
	const [isVideoReady, setIsVideoReady] = useState(false)
	const stableCountRef = useRef(0)
	const lastPollRef = useRef(0)

	faceDetectedRef.current = faceDetected

	const captureFrame = useCallback((): string | null => {
		const video = videoRef.current
		if (!video || video.videoWidth < MIN_VIDEO_SIZE || video.videoHeight < MIN_VIDEO_SIZE) return null
		const canvas = captureCanvasRef.current ?? document.createElement('canvas')
		if (!captureCanvasRef.current) captureCanvasRef.current = canvas
		const ctx = canvas.getContext('2d')
		if (!ctx) return null

		const minW = 640
		const minH = 480
		const w = video.videoWidth
		const h = video.videoHeight
		// Center crop 70% so face is larger but still fully in frame (Azure needs face clearly visible)
		const cropRatio = 0.7
		const sw = w * cropRatio
		const sh = h * cropRatio
		const sx = (w - sw) / 2
		const sy = (h - sh) / 2
		canvas.width = Math.max(minW, Math.round(sw))
		canvas.height = Math.max(minH, Math.round(sh))
		ctx.drawImage(video, sx, sy, sw, sh, 0, 0, canvas.width, canvas.height)
		return canvas.toDataURL('image/jpeg', 0.92)
	}, [videoRef])

	const doCaptureAndSearch = useCallback(() => {
		const dataUrl = captureFrame()
		if (dataUrl) onCapture(dataUrl)
	}, [captureFrame, onCapture])

	// Poll for face detection and draw overlay
	useEffect(() => {
		const video = videoRef.current
		const overlay = overlayRef.current
		if (!video || !overlay) return

		const drawOverlay = (detected: boolean) => {
			const ctx = overlay.getContext('2d')
			if (!ctx) return
			const w = overlay.width
			const h = overlay.height
			ctx.clearRect(0, 0, w, h)

			const cx = w / 2
			const cy = h / 2
			// Oval: wider than tall for face framing (rotate 90° CW from vertical)
			const rx = Math.min(w * 0.3, 120)
			const ry = Math.min(h * 0.4, 180)
			const rotation = -Math.PI / 2

			ctx.strokeStyle = detected ? '#22c55e' : '#ef4444'
			ctx.lineWidth = detected ? 4 : 3
			ctx.beginPath()
			ctx.ellipse(cx, cy, rx, ry, rotation, 0, 2 * Math.PI)
			ctx.stroke()

			if (!detected) {
				ctx.font = '14px system-ui, sans-serif'
				ctx.fillStyle = 'rgba(239, 68, 68, 0.9)'
				ctx.textAlign = 'center'
				ctx.fillText('Position face in oval', cx, cy + ry + 24)
			} else {
				ctx.font = 'bold 14px system-ui, sans-serif'
				ctx.fillStyle = '#22c55e'
				ctx.fillText('Face detected – hold steady', cx, cy + ry + 24)
			}
		}

		const syncOverlaySize = () => {
			const rect = video.getBoundingClientRect()
			if (overlay.width !== rect.width || overlay.height !== rect.height) {
				overlay.width = rect.width
				overlay.height = rect.height
			}
		}

		const pollInFlightRef = { current: false }
		const getUseMock = () => typeof localStorage !== 'undefined' && localStorage.getItem('FACE_CAPTURE_MOCK') === 'true'
		const poll = async () => {
			const now = Date.now()
			if (now - lastPollRef.current < POLL_INTERVAL_MS || pollInFlightRef.current) return
			lastPollRef.current = now
			pollInFlightRef.current = true

			const useMock = getUseMock()
			const dataUrl = captureFrame()

			// When mock is on, show green immediately (no API call); auto-capture after FACE_STABLE_FRAMES polls
			if (useMock) {
				faceDetectedRef.current = true
				setFaceDetected(true)
				stableCountRef.current += 1
				if (stableCountRef.current >= FACE_STABLE_FRAMES) {
					stableCountRef.current = 0
					doCaptureAndSearch()
				}
				pollInFlightRef.current = false
				drawOverlay(true)
				return
			}

			if (!dataUrl) {
				pollInFlightRef.current = false
				return
			}

			let detected = false
			try {
				const result = await offenderRecognitionApi.detectOnly(dataUrl)
				detected = result?.faceDetected === true || (result as { FaceDetected?: boolean })?.FaceDetected === true
				if (import.meta.env.DEV) {
					console.log('[FaceCaptureGuide] detect-only result:', { detected, raw: result })
				}
				setFaceDetected(detected)
				faceDetectedRef.current = detected

				stableCountRef.current = detected ? stableCountRef.current + 1 : 0
				if (stableCountRef.current >= FACE_STABLE_FRAMES) {
					stableCountRef.current = 0
					doCaptureAndSearch()
					pollInFlightRef.current = false
					return
				}
			} catch (err) {
				detected = false
				setFaceDetected(false)
				faceDetectedRef.current = false
				if (import.meta.env.DEV) {
					console.warn('[FaceCaptureGuide] detect-only failed:', err)
				}
			} finally {
				pollInFlightRef.current = false
			}
			drawOverlay(detected)
		}

		const drawLoop = () => {
			syncOverlaySize()
			drawOverlay(faceDetectedRef.current)
		}

		const raf = requestAnimationFrame(function loop() {
			drawLoop()
			requestAnimationFrame(loop)
		})

		// When mock is on, show green immediately (no wait for first poll)
		if (getUseMock()) {
			faceDetectedRef.current = true
			setFaceDetected(true)
		}

		// Delay poll start so camera produces real frames (avoid black frames)
		const intervalIdRef = { current: null as ReturnType<typeof setInterval> | null }
		const startTimeoutId = setTimeout(() => {
			poll()
			intervalIdRef.current = setInterval(poll, POLL_INTERVAL_MS)
		}, POLL_START_DELAY_MS)

		return () => {
			clearTimeout(startTimeoutId)
			if (intervalIdRef.current) clearInterval(intervalIdRef.current)
			cancelAnimationFrame(raf)
		}
	}, [videoRef, captureFrame, doCaptureAndSearch])

	// Observe video for ready state
	useEffect(() => {
		const video = videoRef.current
		if (!video) return

		const checkReady = () => {
			setIsVideoReady(video.videoWidth >= MIN_VIDEO_SIZE && video.videoHeight >= MIN_VIDEO_SIZE)
		}

		video.addEventListener('loadeddata', checkReady)
		video.addEventListener('playing', checkReady)
		checkReady()
		return () => {
			video.removeEventListener('loadeddata', checkReady)
			video.removeEventListener('playing', checkReady)
		}
	}, [videoRef, isVideoReady])

	return (
		<div className="space-y-3">
			<div className="relative mx-auto w-full max-w-sm sm:max-w-lg">
				<video
					ref={videoRef}
					className="w-full rounded-md bg-black object-cover"
					playsInline
					muted
				/>
				<canvas
					ref={overlayRef}
					className="absolute inset-0 w-full h-full pointer-events-none rounded-md"
					style={{ left: 0, top: 0 }}
				/>
				{!isVideoReady && (
					<div className="absolute inset-0 flex items-center justify-center rounded-md bg-black/60 text-white text-sm">
						Loading camera…
					</div>
				)}
			</div>
			<p className="text-[11px] text-center text-gray-600">
				{faceDetected
					? 'Face detected – holding steady will capture automatically'
					: 'Position your face in the red oval. Turn green to auto-capture.'}
			</p>
			{import.meta.env.DEV && (
				<p className="text-[10px] text-center text-amber-600">
					Dev: <code className="bg-amber-100 px-1 rounded">localStorage.setItem(&apos;FACE_CAPTURE_MOCK&apos;,&apos;true&apos;)</code> then refresh to test green overlay without backend.
				</p>
			)}
			<div className="flex flex-wrap gap-2 justify-center">
				<button
					type="button"
					onClick={doCaptureAndSearch}
					disabled={isSearching || !isVideoReady}
					className="inline-flex h-9 items-center justify-center rounded-md border border-input bg-background px-4 text-sm font-medium shadow-sm hover:bg-accent hover:text-accent-foreground disabled:opacity-50"
				>
					{isSearching ? 'Searching…' : 'Capture & search'}
				</button>
				<button
					type="button"
					onClick={onCancel}
					className="inline-flex h-9 items-center justify-center rounded-md px-4 text-sm font-medium hover:bg-accent"
				>
					Cancel
				</button>
			</div>
		</div>
	)
}
