import React, { useState } from 'react';
import { Plus, CheckSquare } from 'lucide-react';
import Link from 'next/link';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';

interface RelatedTasksPanelProps {
  sourceModule: string;
  relatedEntityId: string;
  relatedEntityName: string;
}

export function RelatedTasksPanel({ sourceModule, relatedEntityName }: RelatedTasksPanelProps) {
  
  const [tasks] = useState([
    { id: '1', title: 'Prepare PV ISS', status: 'In Progress', type: 'Business', v: 'info' as const },
    { id: '2', title: 'Verify Material Check', status: 'Draft', type: 'Assigned', v: 'neutral' as const }
  ]);
  
  const completed = tasks.filter(t => t.status === 'Completed').length;
  const progress = tasks.length === 0 ? 0 : Math.round((completed / tasks.length) * 100);

  return (
    <Card className="w-full shadow-sm hover:shadow-md transition-shadow">
      <CardHeader className="pb-4 flex flex-row items-center justify-between">
        <div>
          <CardTitle className="text-sm font-bold flex items-center">
            <CheckSquare className="w-4 h-4 mr-2 text-indigo-500" />
            Execution Tasks
          </CardTitle>
          <p className="text-xs text-muted-foreground mt-1">Linked to {sourceModule} / {relatedEntityName}</p>
        </div>
        <Button variant="outline" size="sm" className="h-8 text-indigo-600 dark:text-indigo-400 bg-indigo-50 dark:bg-indigo-500/10 hover:bg-indigo-100 border-transparent">
          <Plus className="w-3.5 h-3.5 mr-1" /> Add Task
        </Button>
      </CardHeader>

      <CardContent>
        <div className="mb-4">
          <div className="flex justify-between text-xs font-bold mb-1.5">
            <span className="text-muted-foreground">Overall Progress</span>
            <span className="text-indigo-600 dark:text-indigo-400">{progress}%</span>
          </div>
          <div className="w-full h-2 bg-muted rounded-full overflow-hidden">
            <div className="h-full bg-indigo-500 rounded-full" style={{ width: `${progress}%` }}></div>
          </div>
        </div>

        <div className="space-y-2">
          {tasks.map(task => (
            <div key={task.id} className="flex items-center justify-between p-2.5 bg-muted/50 rounded-xl group hover:bg-muted transition-colors cursor-pointer border border-transparent hover:border-border">
              <div className="flex flex-col">
                <span className="text-xs font-semibold text-foreground">{task.title}</span>
                <div className="flex items-center space-x-2 mt-1">
                  <span className="text-[10px] text-muted-foreground">{task.type}</span>
                  <Badge variant={task.v} className="px-1.5 py-0">{task.status}</Badge>
                </div>
              </div>
              <Link href={`/tasks/${task.id}`} className="opacity-0 group-hover:opacity-100 text-xs font-bold text-indigo-500 transition-opacity">
                Open
              </Link>
            </div>
          ))}
        </div>
      </CardContent>
    </Card>
  );
}
