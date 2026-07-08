"use client";

import React, { useState } from 'react';
import { Search, Filter, Plus, ArrowUpDown, MoreHorizontal } from 'lucide-react';
import { Card } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Badge } from '@/components/ui/badge';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { useTasks } from '@/lib/contexts/TaskContext';
import { UserBadge } from '@/components/ui/user-badge';

export default function TasksList() {
  const { tasks } = useTasks();
  const [searchTerm, setSearchTerm] = useState('');

  const filteredTasks = tasks.filter(t => t.title.toLowerCase().includes(searchTerm.toLowerCase()) || t.id.toLowerCase().includes(searchTerm.toLowerCase()));

  const getStatusFromColumn = (colId: string) => {
    switch(colId) {
      case 'todo': return { label: 'To Do', variant: 'neutral' as const };
      case 'in_progress': return { label: 'In Progress', variant: 'info' as const };
      case 'review': return { label: 'Review', variant: 'warning' as const };
      case 'done': return { label: 'Done', variant: 'success' as const };
      default: return { label: colId, variant: 'default' as const };
    }
  };

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col md:flex-row md:items-center justify-between">
        <div>
          <h1 className="text-2xl font-black text-foreground">List View</h1>
          <p className="text-sm font-medium text-muted-foreground mt-1">Manage all enterprise tasks in a tabular view.</p>
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
      
      {/* Search and Table */}
      <Card className="shadow-sm overflow-hidden">
        <div className="p-4 border-b border-border flex items-center justify-between bg-muted/50">
          <div className="relative w-full max-w-sm">
            <Search className="w-4 h-4 absolute left-3 top-2.5 text-muted-foreground" />
            <Input 
              placeholder="Search tasks..." 
              className="pl-9 h-9" 
              value={searchTerm}
              onChange={(e) => setSearchTerm(e.target.value)}
            />
          </div>
        </div>
        
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead className="w-[100px]">ID</TableHead>
              <TableHead>Title <ArrowUpDown className="w-3 h-3 ml-1 inline-block text-muted-foreground" /></TableHead>
              <TableHead>Module</TableHead>
              <TableHead>Assignee</TableHead>
              <TableHead>Status</TableHead>
              <TableHead>Priority</TableHead>
              <TableHead>Due Date</TableHead>
              <TableHead className="w-[50px]"></TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {filteredTasks.map((task) => {
              const status = getStatusFromColumn(task.columnId);
              return (
                <TableRow key={task.id} className="group cursor-pointer">
                  <TableCell className="font-mono text-xs font-semibold text-muted-foreground">{task.id}</TableCell>
                  <TableCell className="font-semibold text-foreground group-hover:text-primary transition-colors">{task.title}</TableCell>
                  <TableCell className="text-muted-foreground text-xs font-medium">{task.module}</TableCell>
                  <TableCell><UserBadge name={task.assignee} size="sm" /></TableCell>
                  <TableCell><Badge variant={status.variant}>{status.label}</Badge></TableCell>
                  <TableCell><Badge variant={task.priorityV}>{task.priority}</Badge></TableCell>
                  <TableCell className={`font-semibold text-xs text-muted-foreground`}>
                    {task.deadline ? new Date(task.deadline).toLocaleDateString() : 'N/A'}
                  </TableCell>
                  <TableCell>
                    <Button variant="ghost" size="icon" className="h-8 w-8 text-muted-foreground opacity-0 group-hover:opacity-100 transition-opacity">
                      <MoreHorizontal className="w-4 h-4" />
                    </Button>
                  </TableCell>
                </TableRow>
              );
            })}
          </TableBody>
        </Table>
      </Card>
    </div>
  );
}
