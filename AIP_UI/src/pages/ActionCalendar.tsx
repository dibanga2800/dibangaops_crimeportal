import React, { useEffect, useState } from "react";
import {
  Card,
  CardContent,
  CardHeader,
  CardTitle,
  CardDescription,
  CardFooter,
} from "@/components/ui/card";
import { Button, buttonVariants } from "@/components/ui/button";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
  DialogDescription,
} from "@/components/ui/dialog";
import { Calendar } from "@/components/ui/calendar";
import { AddTaskForm } from "@/components/action-calendar/AddTaskForm";
import { TaskList } from "@/components/action-calendar/TaskList";
import { TaskProgressSheet } from "@/components/action-calendar/TaskProgressSheet";
import { usePageAccess } from "@/contexts/PageAccessContext";
import { 
  Plus, 
  CalendarDays, 
  ListTodo, 
  CheckCircle, 
  Clock, 
  AlertCircle,
  ArrowUpCircle, 
  MinusCircle, 
  ArrowDownCircle,
  Calendar as CalendarIcon,
  Filter,
  Lock,
  Loader2,
  ChevronLeft,
  ChevronRight,
} from "lucide-react";
import { useToast } from "@/hooks/use-toast";
import { startOfWeek, endOfWeek, isSameWeek, isSameDay, isSameMonth, format, isToday } from "date-fns";
import { Badge } from "@/components/ui/badge";
import { Separator } from "@/components/ui/separator";
import { cn } from "@/lib/utils";
import { actionCalendarService, ActionCalendarStatusUpdate } from "@/services/actionCalendarService";
import useAuth from "@/hooks/useAuth";

export type Task = {
  id: string;
  title: string;
  description: string;
  date: Date;
  priority: "low" | "medium" | "high";
  assignee: string;
  assigneeName?: string;
  status: "pending" | "in-progress" | "completed" | "blocked";
  statusNotes?: string;
  email?: string;
  createdById?: string;
  createdByName?: string;
  modifiedById?: string;
  modifiedByName?: string;
  dateCreated?: Date;
  dateModified?: Date;
};

export type TaskStatusUpdate = {
  id: string;
  taskId: string;
  status: Task["status"];
  comment?: string;
  updateDate: Date;
  updatedBy?: string;
  updatedByName: string;
};

const mapStatusUpdate = (update: ActionCalendarStatusUpdate): TaskStatusUpdate => ({
  id: update.actionCalendarStatusUpdateId.toString(),
  taskId: update.actionCalendarId.toString(),
  status: update.status,
  comment: update.comment,
  updateDate: new Date(update.updateDate),
  updatedBy: update.updatedBy,
  updatedByName: update.updatedByUserName,
});

const ActionCalendar: React.FC = () => {
  const [date, setDate] = useState<Date>(new Date());
  const [tasks, setTasks] = useState<Task[]>([]);
  const [loading, setLoading] = useState(true);
  const [statistics, setStatistics] = useState({
    total: 0,
    completed: 0,
    inProgress: 0,
    pending: 0,
    blocked: 0,
    highPriority: 0,
    dueToday: 0,
    overdue: 0,
  });
  const [activeTab, setActiveTab] = useState<string>("day");
  const { toast } = useToast();
  const { currentRole } = usePageAccess();
  const { user } = useAuth();
  const isAdmin = currentRole === "administrator";
  const currentUserId = user?.id;

  const [statusUpdates, setStatusUpdates] = useState<Record<string, TaskStatusUpdate[]>>({});
  const [statusUpdatesLoading, setStatusUpdatesLoading] = useState<Record<string, boolean>>({});
  const [statusUpdatesError, setStatusUpdatesError] = useState<Record<string, string | null>>({});
  const [activeTask, setActiveTask] = useState<Task | null>(null);
  const [isProgressSheetOpen, setIsProgressSheetOpen] = useState(false);

  const canManageTasks = isAdmin;
  const canUpdateTaskStatus = (task: Task) => {
    if (isAdmin) return true;
    if (task.assignee && currentUserId && task.assignee === currentUserId) return true;
    if (task.createdById && currentUserId && task.createdById === currentUserId) return true;
    return false;
  };

  const loadTasks = async () => {
    try {
      setLoading(true);
      const response = await actionCalendarService.getTasks();
      if (response.success) {
        const convertedTasks = response.data.map((task: any) => actionCalendarService.convertToFrontendFormat(task));
        setTasks(convertedTasks);
      } else {
        toast({ title: "Error", description: response.message || "Failed to load tasks", variant: "destructive" });
      }
    } catch (error) {
      console.error("Error loading tasks:", error);
      toast({ title: "Error", description: "Failed to load tasks from server", variant: "destructive" });
    } finally {
      setLoading(false);
    }
  };

  const loadStatistics = async () => {
    try {
      const stats = await actionCalendarService.getStatistics();
      setStatistics(stats);
    } catch (error) {
      console.error("Error loading statistics:", error);
    }
  };

  const loadTaskStatusUpdates = async (taskId: string, force = false) => {
    if (!force && statusUpdates[taskId]) return;
    setStatusUpdatesLoading((p) => ({ ...p, [taskId]: true }));
    setStatusUpdatesError((p) => ({ ...p, [taskId]: null }));

    try {
      const res = await actionCalendarService.getStatusUpdates(parseInt(taskId));
      if (res.success) {
        setStatusUpdates((p) => ({ ...p, [taskId]: res.data.map(mapStatusUpdate) }));
      } else {
        throw new Error(res.message || "Failed to load status updates");
      }
    } catch (error) {
      console.error(error);
      setStatusUpdatesError((p) => ({ ...p, [taskId]: "Unable to load progress updates. Please try again." }));
      toast({ title: "Error", description: "Failed to load task progress history.", variant: "destructive" });
    } finally {
      setStatusUpdatesLoading((p) => ({ ...p, [taskId]: false }));
    }
  };

  const handleOpenProgress = async (task: Task) => {
    setActiveTask(task);
    setIsProgressSheetOpen(true);
    await loadTaskStatusUpdates(task.id);
  };

  const handleSubmitProgressUpdate = async (taskId: string, payload: { status: Task["status"]; comment?: string }) => {
    try {
      setStatusUpdatesError((p) => ({ ...p, [taskId]: null }));
      const response = await actionCalendarService.createStatusUpdate(parseInt(taskId), payload);
      if (response.success) {
        const mappedUpdate = mapStatusUpdate(response.data);
        setStatusUpdates((p) => ({ ...p, [taskId]: [mappedUpdate, ...(p[taskId] || [])] }));
        await loadTasks();
        await loadStatistics();
        toast({ title: "Progress Updated", description: "Task progress has been shared with the task creator and assignee." });
      } else {
        throw new Error(response.message || "Failed to submit status update");
      }
    } catch (error) {
      console.error("Error updating task progress:", error);
      setStatusUpdatesError((p) => ({ ...p, [taskId]: "Unable to submit update. Please try again." }));
      toast({ title: "Error", description: "Failed to submit task progress.", variant: "destructive" });
    }
  };

  const handleCloseProgressSheet = (open: boolean) => {
    setIsProgressSheetOpen(open);
    if (!open) setActiveTask(null);
  };

  const handleRefreshActiveTaskUpdates = () => {
    if (activeTask) loadTaskStatusUpdates(activeTask.id, true);
  };

  const handleAddTask = async (newTask: Omit<Task, "id" | "status">) => {
    if (!isAdmin) {
      toast({ title: "Access Denied", description: "Only administrators can create and assign tasks.", variant: "destructive" });
      return;
    }
    try {
      const backendTask = actionCalendarService.convertToBackendFormat({ ...newTask, status: "pending" });
      const response = await actionCalendarService.createTask(backendTask);
      if (response.success) {
        const convertedTask = actionCalendarService.convertToFrontendFormat(response.data);
        setTasks((t) => [...t, convertedTask]);
        await loadStatistics();
        toast({ title: "Task Added", description: "Your task has been successfully created." });
      } else {
        toast({ title: "Error", description: response.message || "Failed to create task", variant: "destructive" });
      }
    } catch (error) {
      console.error("Error creating task:", error);
      toast({ title: "Error", description: "Failed to create task", variant: "destructive" });
    }
  };

  const handleUpdateTask = async (taskId: string, updatedTask: Partial<Task>) => {
    if (!isAdmin) {
      toast({ title: "Access Denied", description: "Only administrators can update tasks.", variant: "destructive" });
      return;
    }
    try {
      const task = tasks.find((t) => t.id === taskId);
      if (!task) return;
      const backendTask = actionCalendarService.convertToBackendFormat({ ...task, ...updatedTask });
      const response = await actionCalendarService.updateTask(parseInt(taskId), backendTask);
      if (response.success) {
        const updated = actionCalendarService.convertToFrontendFormat(response.data);
        setTasks((t) => t.map((x) => (x.id === taskId ? updated : x)));
        await loadStatistics();
        toast({ title: "Task Updated", description: "Task has been successfully updated." });
      } else {
        toast({ title: "Error", description: response.message || "Failed to update task", variant: "destructive" });
      }
    } catch (error) {
      console.error("Error updating task:", error);
      toast({ title: "Error", description: "Failed to update task", variant: "destructive" });
    }
  };

  const handleDeleteTask = async (taskId: string) => {
    if (!isAdmin) {
      toast({ title: "Access Denied", description: "Only administrators can delete tasks.", variant: "destructive" });
      return;
    }
    try {
      const response = await actionCalendarService.deleteTask(parseInt(taskId));
      if (response.success) {
        setTasks((t) => t.filter((task) => task.id !== taskId));
        await loadStatistics();
        toast({ title: "Task Deleted", description: "Task has been successfully deleted." });
      } else {
        toast({ title: "Error", description: response.message || "Failed to delete task", variant: "destructive" });
      }
    } catch (error) {
      console.error("Error deleting task:", error);
      toast({ title: "Error", description: "Failed to delete task", variant: "destructive" });
    }
  };

  useEffect(() => {
    loadTasks();
    loadStatistics();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  if (loading) {
    return (
      <div className="min-h-screen flex flex-col bg-gray-50">
        <header className="bg-white border-b">
          <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-4">
            <div className="flex items-center justify-between gap-4">
              <div>
                <h1 className="text-2xl font-semibold text-gray-900">Action Calendar</h1>
                <p className="text-sm text-gray-500 mt-1">{isAdmin ? "Plan, organize, and assign tasks efficiently" : "View your assigned tasks and their status"}</p>
              </div>
            </div>
          </div>
        </header>

        <main className="flex-1 flex items-center justify-center p-6">
          <div className="text-center">
            <Loader2 className="h-8 w-8 animate-spin text-gray-500 mx-auto mb-4" />
            <p className="text-sm text-gray-500">Loading tasks...</p>
          </div>
        </main>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-gray-50">
      {/* Header */}
      <header className="bg-white border-b">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-4">
          <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
            <div>
              <h1 className="text-2xl font-semibold text-gray-900">Action Calendar</h1>
              <p className="text-sm text-gray-500 mt-1">{isAdmin ? "Plan, organize, and assign tasks efficiently" : "View your assigned tasks and their status"}</p>
            </div>

            <div className="flex items-center gap-2">
                <Button variant="outline" size="sm" className="gap-2 border-gray-200 text-gray-700 hover:bg-gray-50 hover:text-gray-900">
                  <Filter className="h-4 w-4" />
                <span className="sr-only sm:not-sr-only">Filter</span>
                </Button>

                {isAdmin ? (
                  <Dialog>
                    <DialogTrigger asChild>
                    <Button size="sm" className="gap-2 bg-[#111827] hover:bg-[#1f2937]">
                        <Plus className="h-4 w-4" />
                      <span className="hidden sm:inline">Create Task</span>
                      </Button>
                    </DialogTrigger>
                    <DialogContent className="sm:max-w-[550px]">
          <DialogHeader>
            <DialogTitle>Create New Task</DialogTitle>
                      <DialogDescription>Fill in the details below to create a new task. All fields are required.</DialogDescription>
          </DialogHeader>
                      <AddTaskForm onSubmit={handleAddTask} selectedDate={date} />
                    </DialogContent>
                  </Dialog>
                ) : (
                <Button size="sm" variant="outline" className="gap-2 border-gray-200 text-gray-500 cursor-not-allowed" disabled>
                    <Lock className="h-4 w-4" />
                  <span className="hidden sm:inline">Create Task</span>
                  </Button>
                )}
              </div>
          </div>
        </div>
      </header>

      {/* Main content area */}
      <main className="py-6">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          {/* Stats */}
          <section className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 gap-3 mb-6">
            <Card className="border-0 shadow-sm bg-blue-600 text-white">
              <CardContent className="p-3 sm:p-4 flex flex-col">
                <div className="flex items-center justify-between">
                  <p className="text-xs sm:text-sm font-medium text-blue-100">Total Tasks</p>
                  <ListTodo className="h-4 w-4 sm:h-5 sm:w-5 text-blue-100" />
                </div>
                <p className="text-xl sm:text-2xl font-bold mt-1 sm:mt-2">{statistics.total}</p>
                <p className="text-[10px] sm:text-xs text-blue-100 mt-0.5 sm:mt-1">All time</p>
              </CardContent>
            </Card>
            
            <Card className="border-0 shadow-sm bg-green-600 text-white">
              <CardContent className="p-3 sm:p-4 flex flex-col">
                <div className="flex items-center justify-between">
                  <p className="text-xs sm:text-sm font-medium text-green-100">Completed</p>
                  <CheckCircle className="h-4 w-4 sm:h-5 sm:w-5 text-green-100" />
                </div>
                <p className="text-xl sm:text-2xl font-bold mt-1 sm:mt-2">{statistics.completed}</p>
                <p className="text-[10px] sm:text-xs text-green-100 mt-0.5 sm:mt-1">{statistics.total > 0 ? Math.round((statistics.completed / statistics.total) * 100) : 0}% of total</p>
              </CardContent>
            </Card>
            
            <Card className="border-0 shadow-sm bg-amber-600 text-white">
              <CardContent className="p-3 sm:p-4 flex flex-col">
                <div className="flex items-center justify-between">
                  <p className="text-xs sm:text-sm font-medium text-amber-100">In Progress</p>
                  <Clock className="h-4 w-4 sm:h-5 sm:w-5 text-amber-100" />
                </div>
                <p className="text-xl sm:text-2xl font-bold mt-1 sm:mt-2">{statistics.inProgress}</p>
                <p className="text-[10px] sm:text-xs text-amber-100 mt-0.5 sm:mt-1">{statistics.total > 0 ? Math.round((statistics.inProgress / statistics.total) * 100) : 0}% of total</p>
              </CardContent>
            </Card>
            
            <Card className="border-0 shadow-sm bg-red-600 text-white">
              <CardContent className="p-3 sm:p-4 flex flex-col">
                <div className="flex items-center justify-between">
                  <p className="text-xs sm:text-sm font-medium text-red-100">Due Today</p>
                  <CalendarIcon className="h-4 w-4 sm:h-5 sm:w-5 text-red-100" />
                </div>
                <p className="text-xl sm:text-2xl font-bold mt-1 sm:mt-2">{statistics.dueToday}</p>
                <p className="text-[10px] sm:text-xs text-red-100 font-medium mt-0.5 sm:mt-1">Urgent</p>
              </CardContent>
            </Card>
          </section>

          {/* Two column responsive layout: calendar + tasks */}
          <section className="grid grid-cols-1 md:grid-cols-12 gap-6">
            {/* Calendar column */}
            <div className="md:col-span-4 lg:col-span-3 order-2 md:order-1">
              <div className="flex items-center justify-between mb-2">
                <div className="flex items-center gap-2">
                  <CalendarDays className="h-5 w-5 text-gray-700" />
                  <h2 className="text-sm font-medium text-gray-900">Calendar</h2>
                </div>

                <div className="flex items-center gap-2 text-gray-500">
                  <p className="text-sm">{format(date, "MMMM yyyy")}</p>
                </div>
              </div>

              <Card className="border-0 shadow-sm bg-white overflow-hidden">
                <CardContent className="p-3 sm:p-4">
                  {/* Centered calendar with responsive sizing */}
                  <div className="w-full flex justify-center">
                    <div className="w-full max-w-[360px] sm:max-w-[420px]">
                      <div className="flex items-center justify-center mb-2">
                        <button onClick={() => setDate(new Date(date.getFullYear(), date.getMonth() - 1, 1))} aria-label="Previous month" className="p-1">
                          <ChevronLeft className="h-5 w-5 text-gray-700" />
                        </button>

                        <div className="flex-1 text-center font-medium">{format(date, "MMMM yyyy")}</div>

                        <button onClick={() => setDate(new Date(date.getFullYear(), date.getMonth() + 1, 1))} aria-label="Next month" className="p-1">
                          <ChevronRight className="h-5 w-5 text-gray-700" />
                        </button>
                      </div>

                    <Calendar
                      mode="single"
                      selected={date}
                      onSelect={(d) => d && setDate(d)}
                      className="w-full"
                      showOutsideDays
                      hideNavigation
                    />

                    </div>
                  </div>
                  
                  <Separator className="my-3" />
                  
                  <div className="bg-gray-50 p-3 rounded-md">
                    <h3 className="text-sm font-medium mb-3 text-gray-700">Priority Legend</h3>
                    <div className="space-y-2">
                      <div className="flex items-center justify-between p-2 bg-white rounded border">
                        <div className="flex items-center gap-2">
                          <div className="w-3 h-3 rounded-full bg-red-500" />
                          <span className="text-sm text-gray-700">High Priority</span>
                        </div>
                        <Badge variant="outline" className="text-sm bg-white text-gray-700 border-gray-200">{statistics.highPriority}</Badge>
                      </div>

                      <div className="flex items-center justify-between p-2 bg-white rounded border">
                        <div className="flex items-center gap-2">
                          <div className="w-3 h-3 rounded-full bg-amber-500" />
                          <span className="text-sm text-gray-700">Medium Priority</span>
                        </div>
                        <Badge variant="outline" className="text-sm bg-white text-gray-700 border-gray-200">{tasks.filter((t) => t.priority === "medium").length}</Badge>
                      </div>

                      <div className="flex items-center justify-between p-2 bg-white rounded border">
                        <div className="flex items-center gap-2">
                          <div className="w-3 h-3 rounded-full bg-green-500" />
                          <span className="text-sm text-gray-700">Low Priority</span>
                        </div>
                        <Badge variant="outline" className="text-sm bg-white text-gray-700 border-gray-200">{tasks.filter((t) => t.priority === "low").length}</Badge>
                      </div>
                    </div>
                  </div>
                </CardContent>
              </Card>
            </div>

            {/* Tasks column */}
            <div className="md:col-span-8 lg:col-span-9 order-1 md:order-2">
              <div className="flex items-center justify-between mb-2">
                <div className="flex items-center gap-2">
                  <ListTodo className="h-5 w-5 text-gray-700" />
                  <h2 className="text-sm font-medium text-gray-900">Tasks</h2>
                </div>

                <div className="flex items-center gap-2">
                  {isToday(date) && (
                    <Badge variant="outline" className="bg-green-50 text-green-700 border-green-100">
                      Today
                    </Badge>
                  )}
                  <Button variant="outline" size="sm" className="gap-2 border-gray-200 text-gray-700">Daily View</Button>
                </div>
              </div>
              
              <Card className="border-0 shadow-sm bg-white">
                <CardHeader className="p-0">
                  <Tabs defaultValue="day" className="w-full" onValueChange={setActiveTab}>
                    <TabsList className="w-full grid grid-cols-3 h-10 bg-white border-b rounded-none p-0 text-xs sm:text-sm">
                      <TabsTrigger value="day" className="min-w-0 px-2 sm:px-3">Day</TabsTrigger>
                      <TabsTrigger value="week" className="min-w-0 px-2 sm:px-3">Week</TabsTrigger>
                      <TabsTrigger value="month" className="min-w-0 px-2 sm:px-3">Month</TabsTrigger>
                    </TabsList>
                  </Tabs>
                </CardHeader>

                <CardContent className="p-0">
                  <Tabs defaultValue="day" className="w-full" value={activeTab} onValueChange={setActiveTab}>
                    <TabsContent value="day" className="m-0">
                      <div className="px-4 py-3 bg-gray-50 border-b flex items-center justify-between">
                        <p className="text-sm font-medium text-gray-700">{format(date, "EEEE, MMMM d, yyyy")}</p>
                        {isToday(date) && <Badge className="bg-green-500 text-white">Today</Badge>}
                      </div>

                      <div className="p-4">
                        {tasks.filter((task) => isSameDay(new Date(task.date), date)).length > 0 ? (
                          <TaskList 
                            tasks={tasks.filter((task) => isSameDay(new Date(task.date), date))}
                            onOpenProgress={handleOpenProgress}
                            onUpdateTask={handleUpdateTask}
                            onDeleteTask={handleDeleteTask}
                            canManageTasks={canManageTasks}
                            canUpdateStatus={canUpdateTaskStatus}
                          />
                        ) : (
                          <div className="flex flex-col items-center justify-center py-12 text-center">
                            <div className="rounded-full bg-gray-100 p-4 mb-4">
                              <ListTodo className="h-6 w-6 text-gray-400" />
                            </div>
                            <h3 className="text-lg font-medium mb-2 text-gray-900">No Tasks Scheduled</h3>
                            <p className="text-sm text-gray-500 max-w-md">
                              {isAdmin ? "No tasks are scheduled for this period. Create a new task to get started." : "No tasks have been assigned to you for this period."}
                            </p>

                            {isAdmin && (
                              <Dialog>
                                <DialogTrigger asChild>
                                  <Button className="mt-4 gap-2 bg-[#111827] hover:bg-[#1f2937]">
                                    <Plus className="h-4 w-4" />
                                    <span>Create Task</span>
                                  </Button>
                                </DialogTrigger>
                                <DialogContent className="sm:max-w-[550px]">
          <DialogHeader>
            <DialogTitle>Create New Task</DialogTitle>
                                    <DialogDescription>Fill in the details below to create a new task. All fields are required.</DialogDescription>
                                  </DialogHeader>
                                  <AddTaskForm onSubmit={handleAddTask} selectedDate={date} />
                                </DialogContent>
                              </Dialog>
                            )}
                          </div>
                        )}
                      </div>
                    </TabsContent>

                    <TabsContent value="week" className="m-0">
                      <div className="px-3 sm:px-4 py-2 sm:py-3 bg-gray-50 border-b flex items-center justify-between">
                        <p className="text-xs sm:text-sm font-medium text-gray-700">Week of {format(startOfWeek(date), "MMM d")} - {format(endOfWeek(date), "MMM d, yyyy")}</p>
                        <Badge variant="outline" className="text-[10px] sm:text-xs bg-blue-50 text-blue-700 border-blue-100">{tasks.filter((task) => isSameWeek(new Date(task.date), date)).length} tasks</Badge>
                      </div>

                      <div className="p-4">
                        {tasks.filter((task) => isSameWeek(new Date(task.date), date)).length > 0 ? (
                          <TaskList 
                            tasks={tasks.filter((task) => isSameWeek(new Date(task.date), date))}
                            onOpenProgress={handleOpenProgress}
                            onUpdateTask={handleUpdateTask}
                            onDeleteTask={handleDeleteTask}
                            canManageTasks={canManageTasks}
                            canUpdateStatus={canUpdateTaskStatus}
                          />
                        ) : (
                          <div className="flex flex-col items-center justify-center py-12 text-center">
                            <div className="rounded-full bg-gray-100 p-4 mb-4">
                              <ListTodo className="h-6 w-6 text-gray-400" />
                            </div>
                            <h3 className="text-lg font-medium mb-2 text-gray-900">No Tasks Scheduled</h3>
                            <p className="text-sm text-gray-500 max-w-md">{isAdmin ? "No tasks are scheduled for this period. Create a new task to get started." : "No tasks have been assigned to you for this period."}</p>

                            {isAdmin && (
                              <Dialog>
                                <DialogTrigger asChild>
                                  <Button className="mt-4 gap-2 bg-[#111827] hover:bg-[#1f2937]">
                                    <Plus className="h-4 w-4" />
                                    <span>Create Task</span>
                                  </Button>
                                </DialogTrigger>
                                <DialogContent className="sm:max-w-[550px]">
          <DialogHeader>
            <DialogTitle>Create New Task</DialogTitle>
                                    <DialogDescription>Fill in the details below to create a new task. All fields are required.</DialogDescription>
                                  </DialogHeader>
                                  <AddTaskForm onSubmit={handleAddTask} selectedDate={date} />
                                </DialogContent>
                              </Dialog>
                            )}
                          </div>
                        )}
                      </div>
                    </TabsContent>

                    <TabsContent value="month" className="m-0">
                      <div className="px-4 py-3 bg-gray-50 border-b flex items-center justify-between">
                        <p className="text-sm font-medium text-gray-700">{format(date, "MMMM yyyy")}</p>
                        <Badge variant="outline" className="bg-blue-50 text-blue-700 border-blue-100">{tasks.filter((task) => isSameMonth(new Date(task.date), date)).length} tasks</Badge>
                      </div>

                      <div className="p-4">
                        {tasks.filter((task) => isSameMonth(new Date(task.date), date)).length > 0 ? (
                          <TaskList 
                            tasks={tasks.filter((task) => isSameMonth(new Date(task.date), date))}
                            onOpenProgress={handleOpenProgress}
                            onUpdateTask={handleUpdateTask}
                            onDeleteTask={handleDeleteTask}
                            canManageTasks={canManageTasks}
                            canUpdateStatus={canUpdateTaskStatus}
                          />
                        ) : (
                          <div className="flex flex-col items-center justify-center py-12 text-center">
                            <div className="rounded-full bg-gray-100 p-4 mb-4">
                              <ListTodo className="h-6 w-6 text-gray-400" />
                            </div>
                            <h3 className="text-lg font-medium mb-2 text-gray-900">No Tasks Scheduled</h3>
                            <p className="text-sm text-gray-500 max-w-md">{isAdmin ? "No tasks are scheduled for this period. Create a new task to get started." : "No tasks have been assigned to you for this period."}</p>

                            {isAdmin && (
                              <Dialog>
                                <DialogTrigger asChild>
                                  <Button className="mt-4 gap-2 bg-[#111827] hover:bg-[#1f2937]">
                                    <Plus className="h-4 w-4" />
                                    <span>Create Task</span>
                                  </Button>
                                </DialogTrigger>
                                <DialogContent className="sm:max-w-[550px]">
          <DialogHeader>
            <DialogTitle>Create New Task</DialogTitle>
                                    <DialogDescription>Fill in the details below to create a new task. All fields are required.</DialogDescription>
                                  </DialogHeader>
                                  <AddTaskForm onSubmit={handleAddTask} selectedDate={date} />
                                </DialogContent>
                              </Dialog>
                            )}
                          </div>
                        )}
                      </div>
                    </TabsContent>
                  </Tabs>
                </CardContent>
              </Card>
            </div>
          </section>
        </div>
      </main>

      <TaskProgressSheet
        open={isProgressSheetOpen}
        onOpenChange={handleCloseProgressSheet}
        task={activeTask}
        statusUpdates={activeTask ? statusUpdates[activeTask.id] ?? [] : []}
        isLoading={activeTask ? statusUpdatesLoading[activeTask.id] ?? false : false}
        error={activeTask ? statusUpdatesError[activeTask.id] : undefined}
        onRefresh={handleRefreshActiveTaskUpdates}
        onSubmitProgress={handleSubmitProgressUpdate}
        canUpdate={activeTask ? canUpdateTaskStatus(activeTask) : false}
        isAdmin={isAdmin}
      />
    </div>
  );
};

export default ActionCalendar;
