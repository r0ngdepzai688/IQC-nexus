"use client";

import React from 'react';
import { BarChart3, TrendingUp, Users, Activity } from 'lucide-react';
import { useTasks } from '@/lib/contexts/TaskContext';
import { Card, CardContent } from '@/components/ui/card';
import { UserBadge } from '@/components/ui/user-badge';

export default function TasksAnalytics() {
  const { tasks } = useTasks();

  const total = tasks.length;
  const statusCounts = {
    todo: tasks.filter(t => t.columnId === 'todo').length,
    in_progress: tasks.filter(t => t.columnId === 'in_progress').length,
    review: tasks.filter(t => t.columnId === 'review').length,
    done: tasks.filter(t => t.columnId === 'done').length,
  };

  const priorityCounts = {
    critical: tasks.filter(t => t.priority === 'Critical').length,
    high: tasks.filter(t => t.priority === 'High').length,
    medium: tasks.filter(t => t.priority === 'Medium').length,
    low: tasks.filter(t => t.priority === 'Low').length,
  };

  const calculateWidth = (count: number) => {
    if (total === 0) return '0%';
    return `${(count / total) * 100}%`;
  };

  return (
    <div className="space-y-6">
      <div className="flex flex-col md:flex-row md:items-center justify-between">
        <div>
          <h1 className="text-2xl font-black text-foreground">Execution Analytics</h1>
          <p className="text-sm font-medium text-muted-foreground mt-1">Deep dive into operational metrics.</p>
        </div>
      </div>
      
      <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
        <Card className="shadow-sm overflow-hidden">
          <CardContent className="p-6">
            <div className="flex items-center mb-6">
              <TrendingUp className="w-5 h-5 text-indigo-500 mr-2" />
              <h3 className="text-sm font-bold text-foreground">Task Status Distribution</h3>
            </div>
            <div className="space-y-4">
              {[
                { label: 'To Do', count: statusCounts.todo, color: 'bg-slate-300 dark:bg-slate-700' },
                { label: 'In Progress', count: statusCounts.in_progress, color: 'bg-blue-500' },
                { label: 'Review', count: statusCounts.review, color: 'bg-amber-500' },
                { label: 'Done', count: statusCounts.done, color: 'bg-emerald-500' },
              ].map((s) => (
                <div key={s.label}>
                  <div className="flex justify-between text-xs mb-1">
                    <span className="font-semibold text-muted-foreground">{s.label}</span>
                    <span className="font-bold">{s.count}</span>
                  </div>
                  <div className="h-2 w-full bg-muted rounded-full overflow-hidden">
                    <div className={`h-full ${s.color} rounded-full transition-all duration-500`} style={{ width: calculateWidth(s.count) }}></div>
                  </div>
                </div>
              ))}
            </div>
          </CardContent>
        </Card>

        <Card className="shadow-sm overflow-hidden">
          <CardContent className="p-6">
            <div className="flex items-center mb-6">
              <Activity className="w-5 h-5 text-rose-500 mr-2" />
              <h3 className="text-sm font-bold text-foreground">Priority Breakdown</h3>
            </div>
            <div className="flex items-end h-32 space-x-4 border-b border-border pb-2 px-4 justify-around">
              {[
                { label: 'Critical', count: priorityCounts.critical, color: 'bg-rose-500' },
                { label: 'High', count: priorityCounts.high, color: 'bg-orange-500' },
                { label: 'Medium', count: priorityCounts.medium, color: 'bg-amber-400' },
                { label: 'Low', count: priorityCounts.low, color: 'bg-slate-400' },
              ].map((p) => {
                const height = total > 0 ? (p.count / total) * 100 : 0;
                return (
                  <div key={p.label} className="flex flex-col items-center w-full group">
                    <div className="text-xs font-bold mb-1 opacity-0 group-hover:opacity-100 transition-opacity">{p.count}</div>
                    <div className={`w-full max-w-[40px] rounded-t-sm ${p.color} transition-all duration-500`} style={{ height: `${Math.max(5, height)}%` }}></div>
                    <div className="text-[10px] font-bold text-muted-foreground mt-2">{p.label}</div>
                  </div>
                )
              })}
            </div>
          </CardContent>
        </Card>

        <Card className="shadow-sm overflow-hidden md:col-span-2">
          <CardContent className="p-6">
            <div className="flex items-center mb-6">
              <Users className="w-5 h-5 text-emerald-500 mr-2" />
              <h3 className="text-sm font-bold text-foreground">Workload Balancing</h3>
            </div>
            <div className="overflow-x-auto">
              <div className="flex space-x-6 pb-2 min-w-max">
                {/* Aggregate tasks by user */}
                {Array.from(new Set(tasks.map(t => t.assignee))).map(empId => {
                  const userTasks = tasks.filter(t => t.assignee === empId);
                  return (
                    <div key={empId} className="flex flex-col items-center space-y-3 p-4 bg-muted/30 rounded-xl border border-border min-w-[120px]">
                      <UserBadge name={empId} size="lg" />
                      <div className="text-center">
                        <div className="text-2xl font-black">{userTasks.length}</div>
                        <div className="text-[10px] font-bold text-muted-foreground uppercase tracking-wider">Tasks</div>
                      </div>
                    </div>
                  );
                })}
              </div>
            </div>
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
