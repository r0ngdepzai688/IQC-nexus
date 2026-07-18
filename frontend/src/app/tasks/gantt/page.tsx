"use client";

import React from 'react';
import { Plus, Settings2, Filter } from 'lucide-react';
import { Card } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { useTasks } from '@/lib/contexts/TaskContext';

export default function TasksGantt() {
  const { tasks } = useTasks();

  const getBarColor = (priorityV: string) => {
    switch (priorityV) {
      case 'danger': return 'bg-rose-500 border-rose-600';
      case 'warning': return 'bg-amber-500 border-amber-600';
      case 'info': return 'bg-blue-500 border-blue-600';
      case 'success': return 'bg-emerald-500 border-emerald-600';
      default: return 'bg-indigo-500 border-indigo-600';
    }
  };

  return (
    <div className="space-y-6 h-full flex flex-col">
      <div className="flex flex-col md:flex-row md:items-center justify-between flex-shrink-0">
        <div>
          <h1 className="text-2xl font-black text-foreground">Gantt View</h1>
          <p className="text-sm font-medium text-muted-foreground mt-1">Visualize dependencies and timelines across the enterprise.</p>
        </div>
        <div className="mt-4 md:mt-0 flex space-x-2">
          <Button variant="outline">
            <Filter className="w-4 h-4 mr-2" /> Filter
          </Button>
          <Button variant="outline">
            <Settings2 className="w-4 h-4 mr-2" /> Display
          </Button>
          <Button className="shadow-md">
            <Plus className="w-4 h-4 mr-2" /> Task
          </Button>
        </div>
      </div>
      
      <Card className="flex-1 overflow-hidden flex border-border shadow-sm min-h-[500px]">
        {/* Left pane: Task List */}
        <div className="w-[300px] border-r border-border bg-muted/20 flex flex-col flex-shrink-0">
          <div className="h-12 border-b border-border flex items-center px-4 bg-muted/50">
            <span className="text-xs font-bold text-muted-foreground uppercase tracking-wider">Task Name</span>
          </div>
          <div className="flex-1 overflow-y-auto custom-scrollbar">
            {tasks.map((t) => (
              <div key={t.id} className="h-12 border-b border-border flex items-center px-4 hover:bg-muted/50 cursor-pointer transition-colors group">
                <span className="text-[10px] font-mono text-muted-foreground mr-2 group-hover:text-primary transition-colors">{t.id}</span>
                <span className="text-sm font-semibold text-foreground truncate">{t.title}</span>
              </div>
            ))}
          </div>
        </div>
        
        {/* Right pane: Timeline Scaffold */}
        <div className="flex-1 flex flex-col bg-background relative overflow-hidden">
          <div className="h-12 border-b border-border flex bg-muted/50 relative overflow-hidden">
            {/* Mock Timeline Header (7 Days) */}
            {['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun'].map((day, i) => (
              <div key={i} className="w-32 flex-shrink-0 border-r border-border flex items-center justify-center">
                <span className="text-[10px] font-bold text-muted-foreground uppercase">{day}</span>
              </div>
            ))}
          </div>
          
          <div className="flex-1 overflow-auto relative custom-scrollbar">
            {/* Mock Grid Lines */}
            <div className="absolute inset-0 flex pointer-events-none opacity-20">
              {[...Array(7)].map((_, i) => (
                <div key={i} className="w-32 flex-shrink-0 border-r border-border h-[2000px]"></div>
              ))}
            </div>

            {/* Mock Gantt Bars */}
            <div className="absolute top-0 left-0 w-full h-full pt-3">
              {tasks.map((task, idx) => {
                // Mock width and position based on task priority/id just to show visual variation
                const leftPos = 20 + (idx * 30);
                const width = 100 + (task.title.length * 5);
                const colorClass = getBarColor(task.priorityV);
                
                return (
                  <div key={task.id} className="h-12 relative flex items-center mb-0 group hover:z-10">
                    <div 
                      className={`absolute h-6 rounded-md shadow-sm border ${colorClass} transition-transform group-hover:scale-y-110 cursor-pointer`}
                      style={{ left: `${leftPos}px`, width: `${width}px` }}
                      title={`${task.title} - ${task.assignee}`}
                    >
                      <div className="absolute inset-0 flex items-center px-2">
                        <span className="text-[10px] font-bold text-white truncate drop-shadow-sm">{task.title}</span>
                      </div>
                    </div>
                  </div>
                );
              })}
            </div>
          </div>
        </div>
      </Card>
    </div>
  );
}
