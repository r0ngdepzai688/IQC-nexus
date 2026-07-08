"use client";

import React, { createContext, useContext, useState, ReactNode } from "react";

export type Priority = "Low" | "Medium" | "High" | "Critical";
export type PriorityV = "neutral" | "warning" | "danger" | "default";

export interface TaskItem {
  id: string;
  title: string;
  description: string;
  module: string;
  assignee: string; // Full name
  priority: Priority;
  priorityV: PriorityV;
  deadline?: string;
  columnId: string;
  progressUpdates?: { id: string; user: string; text: string; date: string }[];
  evidenceFiles?: { id: string; name: string; url: string; size: string }[];
}

interface TaskContextType {
  tasks: TaskItem[];
  moveTask: (taskId: string, targetColumnId: string) => void;
  addTask: (task: Omit<TaskItem, "id">) => void;
  updateTask: (taskId: string, updates: Partial<TaskItem>) => void;
  addProgressUpdate: (taskId: string, text: string, user: string) => void;
  addEvidence: (taskId: string, file: { name: string; url: string; size: string }) => void;
}

const INITIAL_TASKS: TaskItem[] = [
  { 
    id: "T-1", 
    title: "Sign-off Final BOM", 
    description: "Review and sign off the final Bill of Materials for the new X90 model. Ensure all components are verified by the sourcing team.",
    module: "New Models", 
    assignee: "12578026", // Bùi Thị Vân
    priority: "Low", 
    priorityV: "neutral",
    deadline: "2026-07-15T17:00:00",
    columnId: "todo" 
  },
  { 
    id: "T-2", 
    title: "Complete MPPR Analysis", 
    description: "Perform the Mass Production Preparation Review. Upload the final spreadsheet.",
    module: "New Models", 
    assignee: "10548876", // Chu Văn Tú
    priority: "High", 
    priorityV: "danger",
    deadline: "2026-07-10T12:00:00",
    columnId: "in_progress",
    progressUpdates: [
      { id: "u1", user: "10548876", text: "Started gathering data from suppliers.", date: "2026-07-08T09:00:00" }
    ]
  },
  { 
    id: "T-3", 
    title: "Update Compliance Policy", 
    description: "Update the Q3 compliance policy document according to the new safety regulations.",
    module: "Compliance", 
    assignee: "10548898", // Dương Xuân Văn
    priority: "Medium", 
    priorityV: "warning",
    columnId: "in_progress" 
  },
  { 
    id: "T-4", 
    title: "Update Dashboard UI", 
    description: "Implement the new Design System requirements on the main dashboard.",
    module: "IT", 
    assignee: "14824007", // Lê Văn Bậc
    priority: "High", 
    priorityV: "danger",
    columnId: "review" 
  },
  { 
    id: "T-5", 
    title: "Review Supplier Audit", 
    description: "Review the quarterly supplier audit logs.",
    module: "Compliance", 
    assignee: "14502286", // Nguyễn Ngọc Hùng
    priority: "Medium", 
    priorityV: "warning",
    columnId: "done",
    evidenceFiles: [
      { id: "f1", name: "Audit_Q2_Final.pdf", url: "#", size: "2.4 MB" }
    ]
  },
];

const TaskContext = createContext<TaskContextType | undefined>(undefined);

export function TaskProvider({ children }: { children: ReactNode }) {
  const [tasks, setTasks] = useState<TaskItem[]>(INITIAL_TASKS);

  const moveTask = (taskId: string, targetColumnId: string) => {
    setTasks((prev) => 
      prev.map(t => t.id === taskId ? { ...t, columnId: targetColumnId } : t)
    );
  };

  const addTask = (taskData: Omit<TaskItem, "id">) => {
    const newTask: TaskItem = {
      ...taskData,
      id: `T-${tasks.length + 100}`, 
    };
    setTasks(prev => [...prev, newTask]);
  };

  const updateTask = (taskId: string, updates: Partial<TaskItem>) => {
    setTasks(prev => prev.map(t => t.id === taskId ? { ...t, ...updates } : t));
  };

  const addProgressUpdate = (taskId: string, text: string, user: string) => {
    setTasks(prev => prev.map(t => {
      if (t.id === taskId) {
        const newUpdate = { id: `u-${Date.now()}`, user, text, date: new Date().toISOString() };
        return { ...t, progressUpdates: [...(t.progressUpdates || []), newUpdate] };
      }
      return t;
    }));
  };

  const addEvidence = (taskId: string, file: { name: string; url: string; size: string }) => {
    setTasks(prev => prev.map(t => {
      if (t.id === taskId) {
        const newFile = { id: `f-${Date.now()}`, ...file };
        return { ...t, evidenceFiles: [...(t.evidenceFiles || []), newFile] };
      }
      return t;
    }));
  };

  return (
    <TaskContext.Provider value={{ tasks, moveTask, addTask, updateTask, addProgressUpdate, addEvidence }}>
      {children}
    </TaskContext.Provider>
  );
}

export function useTasks() {
  const context = useContext(TaskContext);
  if (context === undefined) {
    throw new Error("useTasks must be used within a TaskProvider");
  }
  return context;
}
