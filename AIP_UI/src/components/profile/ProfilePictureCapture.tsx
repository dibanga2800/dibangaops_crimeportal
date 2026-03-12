import { useState, useRef, useCallback, useEffect } from 'react'
import { Button } from '@/components/ui/button'
import { Avatar, AvatarFallback, AvatarImage } from '@/components/ui/avatar'
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription,
  DialogFooter,
} from '@/components/ui/dialog'
import { Camera, Upload, RotateCcw, Check, X, User, Loader2, SwitchCamera } from 'lucide-react'
import { cn } from '@/lib/utils'

interface ProfilePictureCaptureProps {
  currentImage?: string | null
  onSave: (imageDataUrl: string) => void
  onRemove?: () => void
  userName?: string
  userInitials?: string
}

type CaptureMode = 'idle' | 'camera' | 'preview'

/** Max file size for upload input (4 MB) */
const MAX_UPLOAD_SIZE_BYTES = 4 * 1024 * 1024

/** Max size of image data sent to DB (1 MB) - keeps storage reasonable */
const MAX_OUTPUT_SIZE_BYTES = 1 * 1024 * 1024

/** Base64 overhead ~4/3, so max chars ≈ (bytes * 4/3); use 1.4 multiplier for safety */
const MAX_OUTPUT_CHARS = Math.floor(MAX_OUTPUT_SIZE_BYTES * (4 / 3))

const DEFAULT_CANVAS_SIZE = 512
const DEFAULT_JPEG_QUALITY = 0.82

/**
 * Resizes image to square and compresses to stay under max output size.
 * Iteratively reduces quality and canvas size until within limit.
 */
const resizeAndCompressImage = (dataUrl: string): Promise<string> => {
  return new Promise((resolve, reject) => {
    const img = new Image()
    img.onload = () => {
      const canvas = document.createElement('canvas')
      const ctx = canvas.getContext('2d')
      if (!ctx) {
        reject(new Error('Could not create canvas context'))
        return
      }

      const srcSize = Math.min(img.width, img.height)
      const offsetX = (img.width - srcSize) / 2
      const offsetY = (img.height - srcSize) / 2

      const tryOutput = (dim: number, quality: number): string => {
        canvas.width = dim
        canvas.height = dim
        ctx.drawImage(img, offsetX, offsetY, srcSize, srcSize, 0, 0, dim, dim)
        return canvas.toDataURL('image/jpeg', quality)
      }

      const qualitySteps = [DEFAULT_JPEG_QUALITY, 0.7, 0.55, 0.4, 0.3]
      const sizeSteps = [DEFAULT_CANVAS_SIZE, 400, 320, 256, 200]

      for (const dim of sizeSteps) {
        for (const q of qualitySteps) {
          const result = tryOutput(dim, q)
          if (result.length <= MAX_OUTPUT_CHARS) {
            resolve(result)
            return
          }
        }
      }

      resolve(tryOutput(160, 0.2))
    }
    img.onerror = () => reject(new Error('Failed to load image'))
    img.src = dataUrl
  })
}

export function ProfilePictureCapture({
  currentImage,
  onSave,
  onRemove,
  userName = 'User',
  userInitials = 'U',
}: ProfilePictureCaptureProps) {
  const [isDialogOpen, setIsDialogOpen] = useState(false)
  const [mode, setMode] = useState<CaptureMode>('idle')
  const [capturedImage, setCapturedImage] = useState<string | null>(null)
  const [isSaving, setIsSaving] = useState(false)
  const [isZoomed, setIsZoomed] = useState(false)
  const [cameraError, setCameraError] = useState<string | null>(null)
  const [facingMode, setFacingMode] = useState<'user' | 'environment'>('user')
  const [captureSource, setCaptureSource] = useState<'camera' | 'file'>('camera')
  const [pendingStream, setPendingStream] = useState<MediaStream | null>(null)

  const videoRef = useRef<HTMLVideoElement>(null)
  const streamRef = useRef<MediaStream | null>(null)
  const fileInputRef = useRef<HTMLInputElement>(null)

  const stopCamera = useCallback(() => {
    if (streamRef.current) {
      streamRef.current.getTracks().forEach(track => track.stop())
      streamRef.current = null
    }
    setPendingStream(null)
  }, [])

  // Attach stream to video element once it's rendered in the DOM
  useEffect(() => {
    if (mode !== 'camera' || !pendingStream) return

    const attachStream = async () => {
      // Small delay to ensure DOM paint is complete
      await new Promise(r => setTimeout(r, 50))

      if (videoRef.current && pendingStream.active) {
        videoRef.current.srcObject = pendingStream
        try {
          await videoRef.current.play()
        } catch {
          setCameraError('Failed to start camera playback. Please try again.')
          stopCamera()
          setMode('idle')
        }
      }
    }

    attachStream()
  }, [mode, pendingStream, stopCamera])

  const startCamera = useCallback(async (facing: 'user' | 'environment' = facingMode) => {
    setCameraError(null)
    stopCamera()

    try {
      const stream = await navigator.mediaDevices.getUserMedia({
        video: { facingMode: facing, width: { ideal: 640 }, height: { ideal: 640 } },
        audio: false,
      })
      streamRef.current = stream
      setPendingStream(stream)
      setMode('camera')
    } catch {
      setCameraError('Unable to access camera. Please check permissions or try uploading a file instead.')
      setMode('idle')
    }
  }, [facingMode, stopCamera])

  const handleSwitchCamera = useCallback(() => {
    const newFacing = facingMode === 'user' ? 'environment' : 'user'
    setFacingMode(newFacing)
    startCamera(newFacing)
  }, [facingMode, startCamera])

  const capturePhoto = useCallback(() => {
    if (!videoRef.current) return

    const video = videoRef.current
    const canvas = document.createElement('canvas')
    const size = Math.min(video.videoWidth, video.videoHeight)
    const offsetX = (video.videoWidth - size) / 2
    const offsetY = (video.videoHeight - size) / 2

    canvas.width = DEFAULT_CANVAS_SIZE
    canvas.height = DEFAULT_CANVAS_SIZE
    const ctx = canvas.getContext('2d')
    if (!ctx) return

    if (facingMode === 'user') {
      ctx.translate(DEFAULT_CANVAS_SIZE, 0)
      ctx.scale(-1, 1)
    }
    ctx.drawImage(video, offsetX, offsetY, size, size, 0, 0, DEFAULT_CANVAS_SIZE, DEFAULT_CANVAS_SIZE)

    const dataUrl = canvas.toDataURL('image/jpeg', DEFAULT_JPEG_QUALITY)
    setCapturedImage(dataUrl)
    setCaptureSource('camera')
    stopCamera()
    setMode('preview')
  }, [stopCamera, facingMode])

  const handleFileSelect = useCallback(async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0]
    if (!file) return

    if (!file.type.startsWith('image/')) {
      setCameraError('Please select an image file (JPG, PNG, GIF).')
      return
    }

    if (file.size > MAX_UPLOAD_SIZE_BYTES) {
      setCameraError(`Image must be smaller than ${MAX_UPLOAD_SIZE_BYTES / (1024 * 1024)} MB.`)
      return
    }

    setCameraError(null)

    const reader = new FileReader()
    reader.onload = async (event) => {
      try {
        const dataUrl = event.target?.result as string
        const resized = await resizeAndCompressImage(dataUrl)
        setCapturedImage(resized)
        setCaptureSource('file')
        setMode('preview')
      } catch {
        setCameraError('Failed to process image. Please try a different file.')
      }
    }
    reader.onerror = () => {
      setCameraError('Failed to read file. Please try again.')
    }
    reader.readAsDataURL(file)

    if (fileInputRef.current) {
      fileInputRef.current.value = ''
    }
  }, [])

  const handleRetake = useCallback(() => {
    setCapturedImage(null)
    setCameraError(null)
    if (captureSource === 'camera') {
      startCamera()
    } else {
      setMode('idle')
    }
  }, [captureSource, startCamera])

  const handleSave = useCallback(async () => {
    if (!capturedImage) return
    setIsSaving(true)
    try {
      const toSave =
        capturedImage.length > MAX_OUTPUT_CHARS
          ? await resizeAndCompressImage(capturedImage)
          : capturedImage
      onSave(toSave)
      setIsDialogOpen(false)
      setMode('idle')
      setCapturedImage(null)
      setIsZoomed(false)
    } catch (err) {
      setCameraError('Failed to process image. Please try again.')
    } finally {
      setIsSaving(false)
    }
  }, [capturedImage, onSave])

  const handleClose = useCallback(() => {
    stopCamera()
    setMode('idle')
    setCapturedImage(null)
    setCameraError(null)
    setIsZoomed(false)
    setIsDialogOpen(false)
  }, [stopCamera])

  useEffect(() => {
    return () => { stopCamera() }
  }, [stopCamera])

  return (
    <>
      <div className="relative group">
        <Avatar
          className="h-24 w-24 border-2 border-primary/10 cursor-pointer transition-opacity group-hover:opacity-80"
          onClick={() => setIsDialogOpen(true)}
        >
          <AvatarImage src={currentImage || ''} alt={userName} className="object-cover" />
          <AvatarFallback className="bg-primary/10 text-primary text-lg">
            {userInitials}
          </AvatarFallback>
        </Avatar>
        <button
          type="button"
          onClick={() => setIsDialogOpen(true)}
          className="absolute -bottom-1 -right-1 h-8 w-8 rounded-full bg-primary text-primary-foreground shadow-lg flex items-center justify-center hover:bg-primary/90 transition-colors"
          aria-label="Change profile picture"
        >
          <Camera className="h-4 w-4" />
        </button>
      </div>

      <Dialog open={isDialogOpen} onOpenChange={(open) => { if (!open) handleClose() }}>
        <DialogContent className="sm:max-w-md">
          <DialogHeader>
            <DialogTitle>Update Profile Picture</DialogTitle>
            <DialogDescription>
              Take a photo with your camera or upload an image file.
            </DialogDescription>
          </DialogHeader>

          <div className="flex flex-col items-center gap-4 py-4">
            {mode === 'idle' && (
              <div className="flex flex-col items-center gap-4">
                <Avatar className="h-32 w-32 border-2 border-muted">
                  <AvatarImage src={currentImage || ''} alt={userName} className="object-cover" />
                  <AvatarFallback className="bg-muted text-muted-foreground">
                    <User className="h-12 w-12" />
                  </AvatarFallback>
                </Avatar>

                {cameraError && (
                  <p className="text-sm text-destructive text-center px-4">{cameraError}</p>
                )}

                <div className="flex gap-3">
                  <Button variant="outline" onClick={() => startCamera()} className="gap-2">
                    <Camera className="h-4 w-4" />
                    Take Photo
                  </Button>
                  <Button variant="outline" onClick={() => fileInputRef.current?.click()} className="gap-2">
                    <Upload className="h-4 w-4" />
                    Upload
                  </Button>
                </div>

                {currentImage && onRemove && (
                  <Button
                    variant="ghost"
                    size="sm"
                    onClick={() => { onRemove(); handleClose() }}
                    className="text-destructive hover:text-destructive hover:bg-destructive/10"
                  >
                    Remove Photo
                  </Button>
                )}
              </div>
            )}

            {mode === 'camera' && (
              <div className="flex flex-col items-center gap-3 w-full">
                <div className="relative w-full max-w-[320px] aspect-square rounded-xl overflow-hidden bg-black">
                  <video
                    ref={videoRef}
                    autoPlay
                    playsInline
                    muted
                    className={cn(
                      'w-full h-full object-cover',
                      facingMode === 'user' && '-scale-x-100'
                    )}
                  />
                  <div className="absolute inset-0 border-[3px] border-white/20 rounded-xl pointer-events-none" />
                </div>

                <div className="flex items-center gap-3">
                  <Button variant="outline" size="icon" onClick={handleSwitchCamera} aria-label="Switch camera">
                    <SwitchCamera className="h-4 w-4" />
                  </Button>
                  <Button
                    size="lg"
                    onClick={capturePhoto}
                    className="rounded-full h-14 w-14 p-0"
                    aria-label="Capture photo"
                  >
                    <div className="h-10 w-10 rounded-full border-2 border-primary-foreground" />
                  </Button>
                  <Button variant="outline" size="icon" onClick={handleClose} aria-label="Cancel">
                    <X className="h-4 w-4" />
                  </Button>
                </div>
              </div>
            )}

            {mode === 'preview' && capturedImage && (
              <div className="flex flex-col items-center gap-4 w-full">
                <div
                  className={cn(
                    'w-full max-w-[320px] sm:max-w-[420px] aspect-square rounded-xl overflow-hidden bg-black cursor-zoom-in',
                    isZoomed && 'cursor-zoom-out'
                  )}
                  onClick={() => setIsZoomed((prev) => !prev)}
                >
                  <img
                    src={capturedImage}
                    alt="Preview"
                    className={cn(
                      'w-full h-full object-cover transition-transform duration-200 ease-out',
                      isZoomed && 'scale-150'
                    )}
                  />
                </div>
                <p className="text-xs text-muted-foreground">
                  Click the image to {isZoomed ? 'shrink' : 'zoom in'}.
                </p>
              </div>
            )}
          </div>

          {mode === 'preview' && (
            <DialogFooter className="flex-row gap-2 sm:justify-center">
              <Button variant="outline" onClick={handleRetake} className="gap-2">
                <RotateCcw className="h-4 w-4" />
                Retake
              </Button>
              <Button onClick={handleSave} disabled={isSaving} className="gap-2">
                {isSaving ? (
                  <Loader2 className="h-4 w-4 animate-spin" />
                ) : (
                  <Check className="h-4 w-4" />
                )}
                Save Photo
              </Button>
            </DialogFooter>
          )}

          <input
            ref={fileInputRef}
            type="file"
            accept="image/jpeg,image/png,image/gif,image/webp"
            className="hidden"
            onChange={handleFileSelect}
          />
        </DialogContent>
      </Dialog>
    </>
  )
}
