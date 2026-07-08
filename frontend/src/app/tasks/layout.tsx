"use client";

import { ReactNode, useEffect, useState } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { LayoutDashboard, List, KanbanSquare, Calendar, Clock, GanttChartSquare, BarChart3 } from "lucide-react";
import { Header } from "@/components/Header";
import { FloatingChat } from "@/components/FloatingChat";
import { AuthProvider } from "@/lib/contexts/AuthContext";

import { TaskProvider } from "@/lib/contexts/TaskContext";

export default function TasksLayout({ children }: { children: ReactNode }) {
  const router = useRouter();
  const [isAuthenticated, setIsAuthenticated] = useState(false);

  useEffect(() => {
    const token = localStorage.getItem("token");
    if (!token) {
      router.push("/login");
    } else {
      // eslint-disable-next-line react-hooks/set-state-in-effect
      setIsAuthenticated(true);
    }
  }, [router]);

  if (!isAuthenticated) return null;

  return (
    <AuthProvider>
      <TaskProvider>
        <div className="flex flex-col h-screen overflow-hidden p-4 gap-4 relative z-10">
          <Header />
          <div className="flex-1 flex overflow-hidden gap-4">
            <aside className="w-full md:w-64 bg-card border border-border p-4 flex flex-col space-y-2 rounded-[2rem] shadow-sm">
              <div className="mb-4 pl-2 pt-2">
                <h2 className="text-sm font-black text-foreground uppercase tracking-wider">Tasks Center</h2>
              </div>
              <Link href="/tasks" className="flex items-center px-3 py-2 text-sm font-semibold text-muted-foreground hover:text-foreground hover:bg-muted rounded-xl transition-colors">
                <LayoutDashboard className="w-4 h-4 mr-3" /> Dashboard
              </Link>
              <Link href="/tasks/list" className="flex items-center px-3 py-2 text-sm font-semibold text-muted-foreground hover:text-foreground hover:bg-muted rounded-xl transition-colors">
                <List className="w-4 h-4 mr-3" /> List View
              </Link>
              <Link href="/tasks/kanban" className="flex items-center px-3 py-2 text-sm font-semibold text-muted-foreground hover:text-foreground hover:bg-muted rounded-xl transition-colors">
                <KanbanSquare className="w-4 h-4 mr-3" /> Kanban Board
              </Link>
              <Link href="/tasks/calendar" className="flex items-center px-3 py-2 text-sm font-semibold text-muted-foreground hover:text-foreground hover:bg-muted rounded-xl transition-colors">
                <Calendar className="w-4 h-4 mr-3" /> Calendar
              </Link>
              <Link href="/tasks/timeline" className="flex items-center px-3 py-2 text-sm font-semibold text-muted-foreground hover:text-foreground hover:bg-muted rounded-xl transition-colors">
                <Clock className="w-4 h-4 mr-3" /> Timeline
              </Link>
              <Link href="/tasks/gantt" className="flex items-center px-3 py-2 text-sm font-semibold text-muted-foreground hover:text-foreground hover:bg-muted rounded-xl transition-colors">
                <GanttChartSquare className="w-4 h-4 mr-3" /> Gantt View
              </Link>
              <Link href="/tasks/analytics" className="flex items-center px-3 py-2 text-sm font-semibold text-muted-foreground hover:text-foreground hover:bg-muted rounded-xl transition-colors">
                <BarChart3 className="w-4 h-4 mr-3" /> Analytics
              </Link>
            </aside>
            <main className="flex-1 overflow-y-auto bg-transparent rounded-[2rem] pb-8 pr-1 custom-scrollbar">
              {children}
            </main>
          </div>
        </div>
        <FloatingChat />
      </TaskProvider>
    </AuthProvider>
  );
}
