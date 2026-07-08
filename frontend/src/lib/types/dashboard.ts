// Domain strictly typed contracts for Dashboard Data
// These interfaces serve as the bridge between Mock data and Real API data

export interface DashboardKPIs {
  factoryYield: number; // e.g. 98.4
  qualityCost: number; // e.g. 1200000 (1.2M)
  blockedTasks: number; // e.g. 2
  passRate: number; // e.g. 94.2
  activeProjects: number; // e.g. 12
  criticalRisks: number; // e.g. 3
}

export interface ProjectSummary {
  id: string;
  name: string; // e.g. Galaxy Z Fold 8
  stage: string; // e.g. PVR, DVR
  progress: number; // 0-100
  status: 'OnTrack' | 'AtRisk' | 'Delayed';
}

export interface ActionItem {
  id: string;
  title: string;
  description: string;
  dueDate: string; // ISO date string
  priority: 'High' | 'Medium' | 'Low';
  status: 'Pending' | 'InProgress' | 'Completed';
  type: 'Approval' | 'Review' | 'Meeting' | 'Issue';
}

export interface RecentActivity {
  id: string;
  actor: string;
  action: string;
  target: string;
  timestamp: string; // ISO date string
}

export interface DashboardData {
  kpis: DashboardKPIs;
  projects: ProjectSummary[];
  actions: ActionItem[];
  activities: RecentActivity[];
}
