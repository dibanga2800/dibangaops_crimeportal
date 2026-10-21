import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Search, UserPlus } from "lucide-react"

interface TableActionsProps {
  searchQuery: string
  onSearchChange: (value: string) => void
  onNewEmployee: () => void
}

export function TableActions({ searchQuery, onSearchChange, onNewEmployee }: TableActionsProps) {
  return (
    <div className="flex flex-col sm:flex-row justify-between items-start sm:items-center gap-4">
      <div className="flex flex-1 w-full">
        <div className="relative flex-1 w-full sm:w-[400px]">
          <Search className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
          <Input 
            placeholder="Search employees…" 
            aria-label="Search employees"
            className="pl-9" 
            value={searchQuery}
            onChange={(e) => onSearchChange(e.target.value)}
          />
        </div>
      </div>
      <Button 
        onClick={onNewEmployee} 
        className="w-full sm:w-auto"
      >
        <UserPlus className="h-4 w-4" />
        New employee
      </Button>
    </div>
  )
}