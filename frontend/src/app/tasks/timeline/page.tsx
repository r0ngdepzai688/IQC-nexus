"use client";

import React from 'react';
import { Clock, Plus, Filter, MoreHorizontal } from 'lucide-react';
import { Card, CardContent } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { useTasks } from '@/lib/contexts/TaskContext';
import { UserBadge } from '@/components/ui/user-badge';

export default function TasksTimeline() {
  const { tasks } = useTasks();

  return (
    <div className="space-y-6 h-full flex flex-col">
      <div className="flex flex-col md:flex-row md:items-center justify-between flex-shrink-0">
        <div>
          <h1 className="text-2xl font-black text-foreground">Timeline View</h1>
          <p className="text-sm font-medium text-muted-foreground mt-1">Chronological history and upcoming milestones.</p>
        </div>
        <div className="mt-4 md:mt-0 flex space-x-2">
          <Button variant="outline">
            <Filter className="w-4 h-4 mr-2" /> Filter
          </Button>
          <Button className="shadow-md">
            <Plus className="w-4 h-4 mr-2" /> Milestone
          </Button>
        </div>
      </div>
      
      <Card className="flex-1 overflow-auto border-border shadow-sm custom-scrollbar p-6">
        <div className="max-w-3xl mx-auto">
          {/* Vertical line connecting timeline nodes */}
          <div className="relative border-l-2 border-border ml-6 md:ml-[120px] space-y-8 pb-12">
            
            {/* Today marker */}
            <div className="absolute top-0 left-[-7px] w-3 h-3 rounded-full bg-primary ring-4 ring-background"></div>
            <div className="absolute top-[-4px] left-8 md:left-[-110px] text-xs font-bold text-primary">Today</div>

            {tasks.map((task, idx) => {
              const isPast = idx % 2 === 0; // Just mock logic to show past/future styling
              const dateStr = task.deadline ? new Date(task.deadline).toLocaleDateString() : 'No date';
              
              return (
                <div key={task.id} className="relative pl-8 md:pl-12 group">
                  {/* Timeline node */}
                  <div className={`absolute left-[-9px] top-1.5 w-4 h-4 rounded-full border-2 border-background ring-2 transition-all ${isPast ? 'bg-muted-foreground ring-border' : 'bg-background ring-primary'}`}></div>
                  
                  {/* Date (Left side on desktop) */}
                  <div className="hidden md:block absolute left-[-110px] top-1 text-xs font-bold text-muted-foreground w-20 text-right">
                    {dateStr}
                  </div>

                  {/* Content Card */}
                  <CardContent className="p-4 bg-muted/20 hover:bg-muted/40 transition-colors border border-border rounded-xl shadow-sm">
                    <div className="flex justify-between items-start mb-2">
                      <div className="flex items-center space-x-2">
                        <Badge variant={task.priorityV} className="text-[10px]">{task.priority}</Badge>
                        <span className="text-[10px] font-mono font-bold text-muted-foreground">{task.id}</span>
                      </div>
                      <Button variant="ghost" size="icon" className="h-6 w-6 text-muted-foreground opacity-0 group-hover:opacity-100 transition-opacity">
                        <MoreHorizontal className="w-4 h-4" />
                      </Button>
                    </div>
                    <h3 className="text-base font-semibold text-foreground mb-2">{task.title}</h3>
                    <p className="text-sm text-muted-foreground mb-4 line-clamp-2">{task.description}</p>
                    
                    <div className="flex items-center justify-between border-t border-border pt-3">
                      <div className="flex items-center space-x-4">
                        <span className="text-[10px] font-bold text-muted-foreground uppercase tracking-wider">{task.module}</span>
                        <div className="flex items-center text-[10px] font-bold text-muted-foreground">
                          <Clock className="w-3 h-3 mr-1" />
                          <span className="md:hidden">{dateStr}</span>
                          <span className="hidden md:inline">{task.deadline ? new Date(task.deadline).toLocaleTimeString() : ''}</span>
                        </div>
                      </div>
                      <UserBadge name={task.assignee} />
                    </div>
                  </CardContent>
                </div>
              );
            })}
          </div>
        </div>
      </Card>
    </div>
  );
}
