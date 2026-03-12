import { Button } from "@/components/ui/button"
import { Label } from "@/components/ui/label"

interface LicensingStepProps {
  onNext: () => void
  onBack: () => void
}

export function LicensingStep({ onNext, onBack }: LicensingStepProps) {
  return (
    <div className="space-y-6">
      <div className="space-y-4">
        <Label className="text-sm text-muted-foreground">
          No additional licensing information is required.
        </Label>
      </div>

      <div className="flex justify-between">
        <Button variant="outline" onClick={onBack}>Previous Step</Button>
        <Button onClick={onNext}>Next Step</Button>
      </div>
    </div>
  )
}
