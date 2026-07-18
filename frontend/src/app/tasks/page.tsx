"use client";

import React from 'react';
import { Clock, AlertTriangle, Activity, ListChecks, Plus } from 'lucide-react';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { useTasks } from '@/lib/contexts/TaskContext';
import { UserBadge } from '@/components/ui/user-badge';

export default function TasksExecutiveDashboard() {
  const { tasks } = useTasks();

  const totalTasks = tasks.length;
  const completedTasks = tasks.filter(t => t.columnId === 'done').length;
  const blockedTasks = tasks.filter(t => t.columnId === 'in_progress' && t.priority === 'Critical').length; // Mock logic for blocked

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col md:flex-row md:items-center justify-between">
        <div>
          <h1 className="text-2xl font-black text-foreground">Enterprise Execution Center</h1>
          <p className="text-sm font-medium text-muted-foreground mt-1">Cross-module operational dashboard.</p>
        </div>
        <Button className="mt-4 md:mt-0 shadow-md">
          <Plus className="w-4 h-4 mr-2" /> Global Task
        </Button>
      </div>

      {/* Advanced KPI Cards */}
      <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
        {[
          { title: "Completion Rate", value: totalTasks > 0 ? `${Math.round((completedTasks/totalTasks)*100)}%` : "0%", trend: "+2%", icon: Activity, color: "text-blue-500", bg: "bg-blue-50 dark:bg-blue-500/10", up: true },
          { title: "Average Delay", value: "1.2d", trend: "-0.3d", icon: Clock, color: "text-amber-500", bg: "bg-amber-50 dark:bg-amber-500/10", up: true },
          { title: "Critical Tasks", value: blockedTasks.toString(), trend: "-1", icon: AlertTriangle, color: "text-rose-500", bg: "bg-rose-50 dark:bg-rose-500/10", up: true },
          { title: "Total Executed", value: totalTasks.toString(), trend: "+4", icon: ListChecks, color: "text-emerald-500", bg: "bg-emerald-50 dark:bg-emerald-500/10", up: true }
        ].map((kpi, idx) => (
          <Card key={idx} className="shadow-sm hover:shadow-md transition-shadow flex flex-col justify-between">
            <CardContent className="p-5">
              <div className="flex justify-between items-start">
                <p className="text-[10px] font-bold text-muted-foreground uppercase tracking-wider">{kpi.title}</p>
                <div className={`w-6 h-6 rounded-md flex items-center justify-center ${kpi.bg}`}>
                  <kpi.icon className={`w-3.5 h-3.5 ${kpi.color}`} />
                </div>
              </div>
              <div className="mt-4 flex items-baseline space-x-2">
                <h4 className="text-3xl font-black text-foreground leading-none">{kpi.value}</h4>
                <span className={`text-xs font-bold ${kpi.up ? 'text-emerald-500' : 'text-rose-500'}`}>{kpi.trend}</span>
              </div>
            </CardContent>
          </Card>
        ))}
      </div>

      {/* Lists */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        <Card className="shadow-sm">
          <CardHeader className="pb-4 flex flex-row items-center justify-between border-b border-border">
            <CardTitle className="text-sm font-bold">Waiting My Approval</CardTitle>
            <Badge variant="neutral">2 Items</Badge>
          </CardHeader>
          <CardContent className="p-5">
            <div className="space-y-3">
              {[
                { title: 'Approve MPPR Report', module: 'New Models', reqBy: 'SYN-0001' },
                { title: 'Sign off FAI Inspection', module: 'Inspections', reqBy: 'SYN-0002' }
              ].map((i, idx) => (
                <div key={idx} className="flex items-center justify-between p-3 bg-muted/50 rounded-xl hover:bg-muted transition-colors">
                  <div>
                    <h5 className="text-sm font-semibold text-foreground">{i.title}</h5>
                    <div className="text-[10px] font-medium text-muted-foreground mt-0.5 flex items-center gap-1">
                      <span>{i.module} â€¢ Requested by</span>
                      <UserBadge name={i.reqBy} size="sm" />
                    </div>
                  </div>
                  <div className="flex space-x-2 ml-4">
                    <Button variant="outline" size="xs" className="text-rose-600 dark:text-rose-400 bg-rose-50 dark:bg-rose-500/10 border-transparent hover:bg-rose-100">Reject</Button>
                    <Button variant="outline" size="xs" className="text-emerald-600 dark:text-emerald-400 bg-emerald-50 dark:bg-emerald-500/10 border-transparent hover:bg-emerald-100">Approve</Button>
                  </div>
                </div>
              ))}
            </div>
          </CardContent>
        </Card>

        <Card className="shadow-sm">
          <CardHeader className="pb-4 flex flex-row items-center justify-between border-b border-border">
            <CardTitle className="text-sm font-bold">Assigned By Me</CardTitle>
            <Button variant="link" size="sm" className="h-auto p-0 text-xs">View All</Button>
          </CardHeader>
          <CardContent className="p-5">
            <div className="space-y-3">
              {tasks.slice(0, 4).map((task) => (
                <div key={task.id} className="flex items-center justify-between p-3 bg-muted/50 rounded-xl hover:bg-muted transition-colors">
                  <div>
                    <h5 className="text-sm font-semibold text-foreground">{task.title}</h5>
                    <div className="text-[10px] font-medium text-muted-foreground mt-0.5 flex items-center gap-1">
                      <span>Assigned to</span>
                      <UserBadge name={task.assignee} size="sm" />
                    </div>
                  </div>
                  <Badge variant={task.priorityV}>{task.priority}</Badge>
                </div>
              ))}
            </div>
          </CardContent>
        </Card>
      </div>
    </div>
  );
}

