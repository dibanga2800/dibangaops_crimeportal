import { useEffect, useState } from "react"
import { Label } from "@/components/ui/label"
import { Button } from "@/components/ui/button"
import { Checkbox } from "@/components/ui/checkbox"
import { lookupTableService, type LookupTableItem } from "@/services/lookupTableService"

interface PositionStepProps {
  onNext: () => void
  onBack: () => void
}

export function PositionStep({ onNext, onBack }: PositionStepProps) {
  const [positions, setPositions] = useState<LookupTableItem[]>([])
  const [selectedPositions, setSelectedPositions] = useState<string[]>([])
  const [isLoading, setIsLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    let isMounted = true

    const loadPositions = async () => {
      setIsLoading(true)
      setError(null)

      try {
        const items = await lookupTableService.getByCategory("Positions")
        if (!isMounted) return

        // Only show active positions
        setPositions(items.filter(item => item.isActive))
      } catch (err) {
        console.error("Failed to load Positions lookup", err)
        if (!isMounted) return
        setError("Failed to load positions")
      } finally {
        if (isMounted) {
          setIsLoading(false)
        }
      }
    }

    loadPositions()

    return () => {
      isMounted = false
    }
  }, [])

  const handleTogglePosition = (value: string) => {
    setSelectedPositions(prev => 
      prev.includes(value)
        ? prev.filter(v => v !== value)
        : [...prev, value]
    )
  }

  return (
    <div className="space-y-6">
      <div className="space-y-4">
        <Label>Position</Label>
        {isLoading && (
          <p className="text-sm text-muted-foreground">Loading positions...</p>
        )}
        {error && (
          <p className="text-sm text-red-600">{error}</p>
        )}
        <div className="grid grid-cols-2 gap-4">
          {positions.map(position => (
            <div key={position.lookupId} className="flex items-center space-x-2">
              <Checkbox
                id={`position-${position.lookupId}`}
                checked={selectedPositions.includes(position.value)}
                onCheckedChange={() => handleTogglePosition(position.value)}
              />
              <Label htmlFor={`position-${position.lookupId}`}>{position.value}</Label>
            </div>
          ))}
        </div>
      </div>

      <div className="flex justify-between">
        <Button variant="outline" onClick={onBack}>Previous Step</Button>
        <Button onClick={onNext} disabled={isLoading || (!!error && positions.length === 0)}>
          Next Step
        </Button>
      </div>
    </div>
  )
}