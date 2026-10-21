import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { Card, CardContent } from "@/components/ui/card"
import { formatDistanceToNow } from 'date-fns'
import { Link } from 'react-router-dom'

interface Task {
  id: string
  title: string
  description: string
  assignee: string
  dueDate: Date
  priority: 'high' | 'medium' | 'low'
  status: 'pending' | 'in-progress' | 'completed'
}

interface TaskCardProps {
  task: Task
}

export const TaskCard = ({ task }: TaskCardProps) => {
  const formattedDueDate = formatDistanceToNow(task.dueDate, { addSuffix: true })
  
  const priorityColors = {
    high: 'bg-destructive/10 text-loss border-loss/20',
    medium: 'bg-warning/10 text-warning border-warning/20',
    low: 'bg-recovered/10 text-recovered border-recovered/20'
  }

  const statusColors = {
    'pending': 'bg-gray-500/10 text-muted-foreground border-gray-500/20',
    'in-progress': 'bg-primary/10 text-primary border-primary/20',
    'completed': 'bg-recovered/10 text-recovered border-recovered/20'
  }

  return (
    <Link to="/dashboard" className="block">
      <Card className="hover:bg-accent/5 transition-colors cursor-pointer">
        <CardContent className="p-4">
          <div className="flex items-start justify-between">
            <div className="space-y-3">
              <div>
                <h3 className="font-medium text-foreground">{task.title}</h3>
                <p className="text-sm text-muted-foreground">{task.description}</p>
              </div>
              <div className="flex items-center gap-2 text-sm text-muted-foreground">
                <span>Assigned to: {task.assignee}</span>
                <span>•</span>
                <span>Due {formattedDueDate}</span>
              </div>
            </div>
            <div className="flex flex-col items-end gap-2">
              <div className="flex gap-2">
                <Badge variant="outline" className={priorityColors[task.priority]}>
                  {task.priority}
                </Badge>
                <Badge variant="outline" className={statusColors[task.status]}>
                  {task.status}
                </Badge>
              </div>
              <div className="flex gap-2 mt-2">
                <Button 
                  variant="outline" 
                  size="sm" 
                  onClick={(e) => {
                    // Prevent the click from bubbling up
                    // This way, when the button is clicked, it doesn't trigger the Link twice
                    e.preventDefault();
                    // Navigate programmatically if needed (not needed with Link wrapper)
                  }}
                >
                  Details
                </Button>
              </div>
            </div>
          </div>
        </CardContent>
      </Card>
    </Link>
  )
}