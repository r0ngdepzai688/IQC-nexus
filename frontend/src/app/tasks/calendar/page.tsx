"use client";

import React from 'react';
import { Calendar as CalendarIcon, ChevronLeft, ChevronRight, Plus, Filter } from 'lucide-react';
import { Card, CardContent, CardHeader } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { useTasks } from '@/lib/contexts/TaskContext';
import { UserBadge } from '@/components/ui/user-badge';

export default function TasksCalendar() {
  const { tasks } = useTasks();

  // Create a 5-week calendar grid (35 days) starting from some mock date
  const mockDays = Array.from({ length: 35 }, (_, i) => i + 1);

  return (
    <div className="space-y-6 h-full flex flex-col">
      <div className="flex flex-col md:flex-row md:items-center justify-between flex-shrink-0">
        <div>
          <h1 className="text-2xl font-black text-foreground">Calendar View</h1>
          <p className="text-sm font-medium text-muted-foreground mt-1">Track deadlines and milestones.</p>
        </div>
        <div className="mt-4 md:mt-0 flex space-x-2">
          <Button variant="outline">
            <Filter className="w-4 h-4 mr-2" /> Filter
          </Button>
          <Button className="shadow-md">
            <Plus className="w-4 h-4 mr-2" /> Task
          </Button>
        </div>
      </div>
      
      <Card className="flex-1 overflow-hidden flex flex-col border-border shadow-sm min-h-[600px]">
        <CardHeader className="p-4 border-b border-border bg-muted/20 flex flex-row items-center justify-between">
          <div className="flex items-center space-x-4">
            <h2 className="text-xl font-bold flex items-center">
              <CalendarIcon className="w-5 h-5 mr-2 text-primary" />
              July 2026
            </h2>
            <div className="flex space-x-1">
              <Button variant="ghost" size="icon" className="h-8 w-8"><ChevronLeft className="w-4 h-4" /></Button>
              <Button variant="ghost" size="icon" className="h-8 w-8"><ChevronRight className="w-4 h-4" /></Button>
            </div>
          </div>
          <div className="flex bg-muted rounded-lg p-1">
            <Button variant="ghost" size="sm" className="bg-background shadow-sm h-7 text-xs">Month</Button>
            <Button variant="ghost" size="sm" className="h-7 text-xs text-muted-foreground">Week</Button>
          </div>
        </CardHeader>

        <CardContent className="p-0 flex-1 overflow-auto custom-scrollbar flex flex-col">
          <div className="grid grid-cols-7 border-b border-border bg-muted/50">
            {['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun'].map(d => (
              <div key={d} className="p-2 text-center text-xs font-bold text-muted-foreground uppercase tracking-wider border-r border-border last:border-0">{d}</div>
            ))}
          </div>
          
          <div className="grid grid-cols-7 flex-1 min-h-[500px]">
            {mockDays.map(day => {
              // Distribute tasks onto days mockingly
              const dayTasks = tasks.filter(t => t.deadline && new Date(t.deadline).getDate() === (day > 31 ? day - 31 : day));
              const isToday = day === 8;
              const isCurrentMonth = day <= 31;

              return (
                <div key={day} className={`border-r border-b border-border p-2 min-h-[100px] hover:bg-muted/30 transition-colors ${!isCurrentMonth ? 'opacity-30 bg-muted/50' : ''}`}>
                  <div className="flex justify-between items-start mb-2">
                    <span className={`text-xs font-bold w-6 h-6 flex items-center justify-center rounded-full ${isToday ? 'bg-primary text-primary-foreground' : 'text-muted-foreground'}`}>
                      {day > 31 ? day - 31 : day}
                    </span>
                  </div>
                  
                  <div className="space-y-1">
                    {dayTasks.map(task => (
                      <div key={task.id} className="text-[10px] p-1.5 rounded bg-muted/50 border border-border truncate group cursor-pointer hover:border-primary/50 transition-colors flex items-center justify-between">
                        <span className="font-semibold text-foreground truncate">{task.title}</span>
                        <div className="scale-75 origin-right"><UserBadge name={task.assignee} size="sm" /></div>
                      </div>
                    ))}
                  </div>
                </div>
              );
            })}
          </div>
        </CardContent>
      </Card>
    </div>
  );
}
