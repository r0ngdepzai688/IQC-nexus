"use client";

import React from "react";
import { ProjectWorkspace } from "@/lib/mock-data/newModelsMock";
import { Card, CardContent, CardFooter, CardHeader, CardTitle } from "@/components/ui/card";
import { UserBadge } from "@/components/ui/user-badge";
import { Badge } from "@/components/ui/badge";
import { Calendar, CheckCircle2, Clock, PlayCircle } from "lucide-react";

interface ActiveWorkspacesBoardProps {
  workspaces: ProjectWorkspace[];
}

export function ActiveWorkspacesBoard({ workspaces }: ActiveWorkspacesBoardProps) {
  
  const getStatusDisplay = (status: string) => {
    switch(status) {
      case 'Preparation': return { label: 'Preparation', icon: Clock, color: 'text-amber-500' };
      case 'InProgress': return { label: 'In Progress', icon: PlayCircle, color: 'text-blue-500' }; // Wait, PlayCircle from lucide
      case 'Completed': return { label: 'Completed', icon: CheckCircle2, color: 'text-emerald-500' };
      default: return { label: status, icon: Clock, color: 'text-muted-foreground' };
    }
  };

  if (workspaces.length === 0) {
    return (
      <div className="flex flex-col items-center justify-center p-12 text-center border border-dashed rounded-xl border-border/50 bg-card/10">
        <Clock className="w-12 h-12 mb-4 text-muted-foreground/50" />
        <h3 className="text-lg font-medium">No Active Workspaces</h3>
        <p className="text-sm text-muted-foreground max-w-sm mt-2">
          Activate a project from the Candidate Pool to create a new workspace and track its progress.
        </p>
      </div>
    );
  }

  return (
    <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-6">
      {workspaces.map((ws) => {
        // Need to import PlayCircle if used, let's just stick to what we have or import it at top
        const isCompleted = ws.status === 'Completed';
        
        return (
          <Card key={ws.id} className="relative overflow-hidden border-border/50 hover:border-primary/30 transition-all duration-300 hover:shadow-md hover:shadow-primary/5 bg-card/50 backdrop-blur-sm group">
            {/* Top decorative line based on status */}
            <div className={`absolute top-0 left-0 right-0 h-1 ${ws.status === 'InProgress' ? 'bg-blue-500' : ws.status === 'Completed' ? 'bg-emerald-500' : 'bg-amber-500'}`} />
            
            <CardHeader className="pb-3 pt-5">
              <div className="flex justify-between items-start mb-2">
                <Badge variant="outline" className="font-normal text-xs bg-background/50 backdrop-blur">
                  {ws.sku}
                </Badge>
                {isCompleted ? (
                   <CheckCircle2 className="w-5 h-5 text-emerald-500" />
                ) : (
                   <span className="text-xs font-medium text-muted-foreground">{ws.status}</span>
                )}
              </div>
              <CardTitle className="text-lg font-semibold tracking-tight">{ws.projectName}</CardTitle>
            </CardHeader>
            
            <CardContent className="pb-4">
              <div className="space-y-4">
                {/* PIC */}
                <div>
                  <p className="text-xs text-muted-foreground mb-1.5 uppercase tracking-wider">PIC (IQC)</p>
                  <UserBadge 
                    userId={ws.ownerId} 
                    name={ws.ownerName} 
                    avatarUrl={`https://api.dicebear.com/7.x/initials/svg?seed=${ws.ownerName}&backgroundColor=1d4ed8`}
                    className="w-full justify-start border bg-background/50 hover:bg-muted/50"
                  />
                </div>
                
                {/* Progress */}
                <div>
                  <div className="flex justify-between items-end mb-1.5">
                     <p className="text-xs text-muted-foreground uppercase tracking-wider">Preparation Progress</p>
                     <span className="text-xs font-medium">{ws.completionPercentage}%</span>
                  </div>
                  <div className="w-full h-2 bg-muted rounded-full overflow-hidden">
                    <div 
                      className={`h-full rounded-full transition-all duration-500 ${ws.status === 'Completed' ? 'bg-emerald-500' : 'bg-primary'}`}
                      style={{ width: `${ws.completionPercentage}%` }}
                    />
                  </div>
                </div>
              </div>
            </CardContent>
            
            <CardFooter className="pt-0 pb-4">
              <div className="w-full flex items-center gap-1.5 text-xs text-muted-foreground pt-4 border-t border-border/50">
                <Calendar className="w-3.5 h-3.5" />
                <span>Activated: {ws.activatedDate}</span>
              </div>
            </CardFooter>
          </Card>
        );
      })}
    </div>
  );
}
