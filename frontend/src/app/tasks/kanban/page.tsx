"use client";

import React, { useState } from 'react';
import { Plus, MoreHorizontal, Calendar as CalendarIcon, UploadCloud, MessageSquare, Paperclip, Clock } from 'lucide-react';
import { Card, CardContent } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { useLanguage } from '@/lib/contexts/LanguageContext';
import { useTasks, TaskItem } from '@/lib/contexts/TaskContext';
import { UserBadge } from '@/components/ui/user-badge';
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogDescription, DialogFooter } from '@/components/ui/dialog';
import { UserSearchInput } from '@/components/ui/user-search-input';

export default function TasksKanban() {
  const { t } = useLanguage();
  const { tasks, moveTask, addTask, addProgressUpdate } = useTasks();

  const [draggingTaskId, setDraggingTaskId] = useState<string | null>(null);
  
  // Modals state
  const [selectedTask, setSelectedTask] = useState<TaskItem | null>(null);
  const [isAddModalOpen, setIsAddModalOpen] = useState(false);

  // Form states for Add Task
  const [newTaskTitle, setNewTaskTitle] = useState("");
  const [newTaskDesc, setNewTaskDesc] = useState("");
  const [newTaskAssignee, setNewTaskAssignee] = useState("");
  const [newUpdateText, setNewUpdateText] = useState("");

  const COLUMNS = [
    { id: "todo", title: t.task.columns.todo },
    { id: "in_progress", title: t.task.columns.inProgress },
    { id: "review", title: t.task.columns.review },
    { id: "done", title: t.task.columns.done }
  ];

  const handleDragStart = (e: React.DragEvent, taskId: string) => {
    setDraggingTaskId(taskId);
    e.dataTransfer.setData('text/plain', taskId);
    e.dataTransfer.effectAllowed = 'move';
  };

  const handleDragOver = (e: React.DragEvent) => {
    e.preventDefault();
    e.dataTransfer.dropEffect = 'move';
  };

  const handleDrop = (e: React.DragEvent, columnId: string) => {
    e.preventDefault();
    const taskId = e.dataTransfer.getData('text/plain');
    if (taskId && taskId !== '') {
      moveTask(taskId, columnId);
    }
    setDraggingTaskId(null);
  };

  const handleCreateTask = () => {
    if (!newTaskTitle || !newTaskAssignee) {
      alert("Vui lòng nhập đủ Tiêu đề và Người phụ trách!");
      return;
    }
    addTask({
      title: newTaskTitle,
      description: newTaskDesc,
      module: "General",
      assignee: newTaskAssignee,
      priority: "Medium",
      priorityV: "warning",
      columnId: "todo",
      deadline: new Date(Date.now() + 86400000).toISOString()
    });
    setIsAddModalOpen(false);
    setNewTaskTitle("");
    setNewTaskDesc("");
    setNewTaskAssignee("");
  };

  const handleAddUpdate = () => {
    if (!selectedTask || !newUpdateText) return;
    addProgressUpdate(selectedTask.id, newUpdateText, "Current User");
    setNewUpdateText("");
  };

  // Keep selectedTask in sync if it gets updated in Context
  React.useEffect(() => {
    if (selectedTask) {
      const updated = tasks.find(t => t.id === selectedTask.id);
      if (updated) setSelectedTask(updated);
    }
  }, [tasks]);

  return (
    <div className="space-y-6 h-full flex flex-col overflow-hidden">
      <div className="flex flex-col md:flex-row md:items-center justify-between flex-shrink-0">
        <div>
          <h1 className="text-2xl font-black text-foreground">{t.task.kanbanTitle}</h1>
          <p className="text-sm font-medium text-muted-foreground mt-1">{t.task.kanbanSubtitle}</p>
        </div>
        <Button onClick={() => setIsAddModalOpen(true)} className="mt-4 md:mt-0 shadow-md">
          <Plus className="w-4 h-4 mr-2" /> {t.task.addTask}
        </Button>
      </div>
      
      <div className="flex-1 flex space-x-4 overflow-x-auto pb-4 custom-scrollbar">
        {COLUMNS.map(col => {
          const colTasks = tasks.filter(t => t.columnId === col.id);
          
          return (
            <div 
              key={col.id} 
              onDragOver={handleDragOver}
              onDrop={(e) => handleDrop(e, col.id)}
              className="w-[320px] flex-shrink-0 bg-muted/30 rounded-2xl p-4 flex flex-col border border-border"
            >
              <div className="flex justify-between items-center mb-4">
                <h3 className="text-sm font-bold text-foreground flex items-center">
                  {col.title}
                  <Badge variant="neutral" className="ml-2">{colTasks.length}</Badge>
                </h3>
                <Button variant="ghost" size="icon" className="h-6 w-6 text-muted-foreground">
                  <MoreHorizontal className="w-4 h-4" />
                </Button>
              </div>
              
              <div className="flex-1 overflow-y-auto space-y-3 custom-scrollbar">
                {colTasks.map(task => (
                  <Card 
                    key={task.id} 
                    draggable
                    onDragStart={(e) => handleDragStart(e, task.id)}
                    onClick={() => setSelectedTask(task)}
                    className={`shadow-sm hover:shadow-md transition-all cursor-grab active:cursor-grabbing border-border ${draggingTaskId === task.id ? 'opacity-50' : 'opacity-100'}`}
                  >
                    <CardContent className="p-4">
                      <div className="flex justify-between items-start mb-2">
                        <Badge variant={task.priorityV} className="text-[10px] px-1.5 py-0">{task.priority}</Badge>
                        <span className="text-[10px] font-mono font-bold text-muted-foreground">{task.id}</span>
                      </div>
                      <p className="text-sm font-semibold text-foreground mb-3 leading-snug">{task.title}</p>
                      
                      {task.deadline && (
                        <div className="flex items-center text-[10px] font-medium text-muted-foreground mb-3">
                          <Clock className="w-3 h-3 mr-1" />
                          {new Date(task.deadline).toLocaleDateString()}
                        </div>
                      )}

                      <div className="flex justify-between items-center">
                        <span className="text-[10px] font-bold text-muted-foreground bg-muted px-2 py-1 rounded-md">{task.module}</span>
                        <UserBadge name={task.assignee} />
                      </div>
                    </CardContent>
                  </Card>
                ))}
              </div>
              
              <Button onClick={() => setIsAddModalOpen(true)} variant="ghost" className="w-full mt-3 text-muted-foreground hover:text-foreground border border-dashed border-border hover:border-muted-foreground/50">
                <Plus className="w-4 h-4 mr-2" /> {t.task.addCard}
              </Button>
            </div>
          )
        })}
      </div>

      {/* Task Detail Modal */}
      <Dialog open={!!selectedTask} onOpenChange={(open) => !open && setSelectedTask(null)}>
        <DialogContent className="sm:max-w-3xl">
          {selectedTask && (
            <>
              <DialogHeader>
                <div className="flex items-center space-x-2 mb-2">
                  <span className="text-xs font-mono font-bold text-muted-foreground">{selectedTask.id}</span>
                  <Badge variant={selectedTask.priorityV}>{selectedTask.priority}</Badge>
                  <Badge variant="neutral">{selectedTask.module}</Badge>
                </div>
                <DialogTitle className="text-xl">{selectedTask.title}</DialogTitle>
                <DialogDescription>
                  Chỉ thị / Mô tả chi tiết công việc.
                </DialogDescription>
              </DialogHeader>

              <div className="grid grid-cols-1 md:grid-cols-3 gap-6 py-4">
                <div className="col-span-1 md:col-span-2 space-y-6">
                  <div>
                    <h4 className="text-sm font-bold mb-2">Nội dung chỉ thị</h4>
                    <p className="text-sm text-gray-700 dark:text-gray-300 leading-relaxed bg-muted/50 p-3 rounded-lg border border-border">
                      {selectedTask.description}
                    </p>
                  </div>

                  <div>
                    <h4 className="text-sm font-bold mb-3 flex items-center">
                      <MessageSquare className="w-4 h-4 mr-2" /> Cập nhật tiến độ
                    </h4>
                    <div className="space-y-3 mb-3 max-h-[150px] overflow-y-auto custom-scrollbar">
                      {selectedTask.progressUpdates?.map(update => (
                        <div key={update.id} className="text-sm bg-muted/30 p-2 rounded-lg border border-border/50">
                          <div className="flex justify-between items-center mb-1">
                            <span className="font-semibold text-xs">{update.user}</span>
                            <span className="text-[10px] text-muted-foreground">{new Date(update.date).toLocaleString()}</span>
                          </div>
                          <p className="text-gray-600 dark:text-gray-400 text-xs">{update.text}</p>
                        </div>
                      ))}
                      {(!selectedTask.progressUpdates || selectedTask.progressUpdates.length === 0) && (
                        <p className="text-xs text-muted-foreground italic">Chưa có cập nhật nào.</p>
                      )}
                    </div>
                    <div className="flex gap-2">
                      <input 
                        type="text" 
                        value={newUpdateText}
                        onChange={(e) => setNewUpdateText(e.target.value)}
                        onKeyDown={(e) => e.key === 'Enter' && handleAddUpdate()}
                        placeholder="Nhập tiến độ mới..." 
                        className="flex-1 text-sm rounded-lg border border-border bg-background px-3 py-2 outline-none focus:border-primary focus:ring-1 focus:ring-primary/20 transition-all"
                      />
                      <Button onClick={handleAddUpdate} size="sm">Gửi</Button>
                    </div>
                  </div>
                </div>

                <div className="col-span-1 space-y-6">
                  <div>
                    <h4 className="text-sm font-bold mb-2 text-muted-foreground">Người phụ trách</h4>
                    <UserBadge name={selectedTask.assignee} size="md" />
                  </div>

                  <div>
                    <h4 className="text-sm font-bold mb-2 text-muted-foreground flex items-center">
                      <CalendarIcon className="w-4 h-4 mr-2" /> Hạn chót
                    </h4>
                    <p className="text-sm font-medium bg-muted/30 px-3 py-2 rounded-lg border border-border">
                      {selectedTask.deadline ? new Date(selectedTask.deadline).toLocaleDateString() : "Không có"}
                    </p>
                  </div>

                  <div>
                    <h4 className="text-sm font-bold mb-2 text-muted-foreground flex items-center">
                      <Paperclip className="w-4 h-4 mr-2" /> Bằng chứng
                    </h4>
                    <div className="space-y-2 mb-3">
                      {selectedTask.evidenceFiles?.map(f => (
                        <div key={f.id} className="flex justify-between items-center p-2 text-xs bg-muted/50 rounded border border-border">
                          <span className="truncate max-w-[120px] font-medium">{f.name}</span>
                          <span className="text-muted-foreground">{f.size}</span>
                        </div>
                      ))}
                    </div>
                    <div className="border-2 border-dashed border-border rounded-xl p-4 flex flex-col items-center justify-center text-center cursor-pointer hover:bg-muted/50 hover:border-primary/50 transition-colors group">
                      <UploadCloud className="w-6 h-6 text-muted-foreground group-hover:text-primary mb-2 transition-colors" />
                      <span className="text-xs text-muted-foreground font-medium group-hover:text-primary transition-colors">Kéo thả file vào đây</span>
                    </div>
                  </div>
                </div>
              </div>
            </>
          )}
        </DialogContent>
      </Dialog>

      {/* Add Task Modal */}
      <Dialog open={isAddModalOpen} onOpenChange={setIsAddModalOpen}>
        <DialogContent className="overflow-visible">
          <DialogHeader>
            <DialogTitle>Thêm Công Việc Mới</DialogTitle>
            <DialogDescription>
              Tạo một chỉ thị mới và giao cho nhân sự.
            </DialogDescription>
          </DialogHeader>
          <div className="space-y-4 py-4 overflow-visible">
            <div className="space-y-2">
              <label className="text-sm font-bold text-foreground">Tiêu đề</label>
              <input 
                value={newTaskTitle}
                onChange={e => setNewTaskTitle(e.target.value)}
                className="w-full text-sm rounded-lg border border-border bg-background px-3 py-2 outline-none focus:border-primary focus:ring-1 focus:ring-primary/20 transition-all" 
                placeholder="VD: Cập nhật tài liệu kỹ thuật" 
              />
            </div>
            
            <div className="space-y-2 relative">
              <label className="text-sm font-bold text-foreground">Người phụ trách (Tìm kiếm)</label>
              <UserSearchInput 
                value={newTaskAssignee}
                onChange={setNewTaskAssignee}
              />
            </div>

            <div className="space-y-2">
              <label className="text-sm font-bold text-foreground">Chỉ thị chi tiết</label>
              <textarea 
                value={newTaskDesc}
                onChange={e => setNewTaskDesc(e.target.value)}
                className="w-full text-sm rounded-lg border border-border bg-background px-3 py-2 outline-none focus:border-primary focus:ring-1 focus:ring-primary/20 transition-all min-h-[100px] resize-none" 
                placeholder="Nhập mô tả chi tiết yêu cầu..."
              />
            </div>
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setIsAddModalOpen(false)}>Hủy</Button>
            <Button onClick={handleCreateTask}>Tạo Công Việc</Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

    </div>
  );
}
