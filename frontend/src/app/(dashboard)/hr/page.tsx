"use client";

import { useState } from "react";
import { motion, AnimatePresence } from "framer-motion";
import { 
  Users, ShieldAlert, TrendingUp, AlertTriangle, Folder, 
  Activity, Zap, Clock, FileWarning, X
} from "lucide-react";

// ============================================================================
// MOCK DATA: DIGITAL TWIN
// ============================================================================
type HealthStatus = 'Healthy' | 'Attention' | 'Risk' | 'Critical';

interface OrgNode {
  id: string;
  name: string;
  level: string;
  leader: string;
  employeeCount: number;
  healthScore: number;
  status: HealthStatus;
  metrics: {
    qualityScore: number;
    projects: number;
    openNcr: number;
    supplierRisk: number;
    delayedTasks: number;
    pendingApproval: number;
  };
  lastUpdated: string;
  children?: OrgNode[];
}

const IQC_ORG: OrgNode = {
  id: "iqc-g",
  name: "IQC G",
  level: "Group",
  leader: "Trần Lê Vinh",
  employeeCount: 345,
  healthScore: 88,
  status: "Attention",
  metrics: { qualityScore: 92, projects: 45, openNcr: 28, supplierRisk: 5, delayedTasks: 12, pendingApproval: 8 },
  lastUpdated: "Just now",
  children: [
    {
      id: "iqc-1p",
      name: "IQC 1P",
      level: "Part",
      leader: "Nguyễn Thị Quỳnh",
      employeeCount: 82,
      healthScore: 96,
      status: "Healthy",
      metrics: { qualityScore: 96, projects: 18, openNcr: 5, supplierRisk: 1, delayedTasks: 2, pendingApproval: 1 },
      lastUpdated: "5 mins ago"
    },
    {
      id: "iqc-2p",
      name: "IQC 2P",
      level: "Part",
      leader: "Phan Thanh Ba",
      employeeCount: 115,
      healthScore: 82,
      status: "Attention",
      metrics: { qualityScore: 89, projects: 12, openNcr: 12, supplierRisk: 3, delayedTasks: 5, pendingApproval: 4 },
      lastUpdated: "12 mins ago"
    },
    {
      id: "iqc-3p",
      name: "IQC 3P",
      level: "Part",
      leader: "Nguyễn Văn Thịnh",
      employeeCount: 148,
      healthScore: 68,
      status: "Critical",
      metrics: { qualityScore: 84, projects: 15, openNcr: 11, supplierRisk: 1, delayedTasks: 5, pendingApproval: 3 },
      lastUpdated: "2 mins ago"
    }
  ]
};

// ============================================================================
// HELPER FUNCTIONS
// ============================================================================
const getHealthColor = (status: HealthStatus) => {
  switch (status) {
    case 'Healthy': return 'bg-emerald-500 border-emerald-400 text-emerald-900 dark:bg-emerald-500/20 dark:border-emerald-500/50 dark:text-emerald-400';
    case 'Attention': return 'bg-yellow-400 border-yellow-300 text-yellow-900 dark:bg-yellow-500/20 dark:border-yellow-500/50 dark:text-yellow-400';
    case 'Risk': return 'bg-orange-500 border-orange-400 text-orange-900 dark:bg-orange-500/20 dark:border-orange-500/50 dark:text-orange-400';
    case 'Critical': return 'bg-rose-500 border-rose-400 text-rose-900 dark:bg-rose-500/20 dark:border-rose-500/50 dark:text-rose-400';
  }
};

const getHealthBadge = (status: HealthStatus) => {
  switch (status) {
    case 'Healthy': return <span className="inline-flex items-center px-2 py-0.5 rounded-full text-[10px] font-bold bg-emerald-100 text-emerald-700 dark:bg-emerald-900/40 dark:text-emerald-400"><span className="w-1.5 h-1.5 rounded-full bg-emerald-500 mr-1.5 animate-pulse"/>HEALTHY</span>;
    case 'Attention': return <span className="inline-flex items-center px-2 py-0.5 rounded-full text-[10px] font-bold bg-yellow-100 text-yellow-700 dark:bg-yellow-900/40 dark:text-yellow-400"><span className="w-1.5 h-1.5 rounded-full bg-yellow-500 mr-1.5"/>ATTENTION</span>;
    case 'Risk': return <span className="inline-flex items-center px-2 py-0.5 rounded-full text-[10px] font-bold bg-orange-100 text-orange-700 dark:bg-orange-900/40 dark:text-orange-400"><span className="w-1.5 h-1.5 rounded-full bg-orange-500 mr-1.5"/>RISK</span>;
    case 'Critical': return <span className="inline-flex items-center px-2 py-0.5 rounded-full text-[10px] font-bold bg-rose-100 text-rose-700 dark:bg-rose-900/40 dark:text-rose-400"><span className="w-1.5 h-1.5 rounded-full bg-rose-500 mr-1.5 animate-pulse"/>CRITICAL</span>;
  }
};

// ============================================================================
// COMPONENT: HEALTH NODE CARD
// ============================================================================
const HealthNode = ({ node, isRoot = false, isSelected, onClick }: { node: OrgNode, isRoot?: boolean, isSelected: boolean, onClick: () => void }) => {
  const colorClasses = getHealthColor(node.status);
  
  return (
    <motion.div 
      whileHover={{ y: -5, scale: 1.02 }}
      whileTap={{ scale: 0.98 }}
      onClick={onClick}
      className={`relative w-72 rounded-3xl p-5 cursor-pointer shadow-lg transition-all duration-300 border-2 ${isSelected ? 'ring-4 ring-primary/30 dark:ring-white/20 border-primary dark:border-white' : 'border-transparent'} ${isRoot ? 'bg-white dark:bg-popover' : 'bg-white dark:bg-popover'}`}
    >
      {/* Top Header */}
      <div className="flex justify-between items-start mb-4">
        <div>
          <h3 className="text-xl font-black text-gray-900 dark:text-white">{node.name}</h3>
          <p className="text-xs font-bold text-gray-500 uppercase tracking-widest">{node.level}</p>
        </div>
        {getHealthBadge(node.status)}
      </div>

      {/* Leader & Size */}
      <div className="flex items-center space-x-3 mb-6 p-2 bg-gray-50 dark:bg-white/5 rounded-xl">
        <div className="w-8 h-8 rounded-full bg-gradient-to-tr from-blue-100 to-blue-200 dark:from-blue-900/40 dark:to-blue-800/40 flex items-center justify-center text-blue-700 dark:text-blue-400 font-black text-xs">
          {node.leader.charAt(0)}
        </div>
        <div>
          <p className="text-sm font-bold text-gray-900 dark:text-white">{node.leader}</p>
          <p className="text-[10px] font-medium text-gray-500">{node.employeeCount} Employees</p>
        </div>
      </div>

      {/* Key Metrics Grid */}
      <div className="grid grid-cols-2 gap-2 mb-4">
        <div className="p-2 bg-gray-50 dark:bg-white/5 rounded-lg border border-border">
          <p className="text-[10px] font-bold text-gray-500 uppercase">Health Score</p>
          <p className={`text-lg font-black ${node.healthScore >= 90 ? 'text-emerald-600' : node.healthScore >= 75 ? 'text-yellow-600' : 'text-rose-600'}`}>{node.healthScore}%</p>
        </div>
        <div className="p-2 bg-gray-50 dark:bg-white/5 rounded-lg border border-border">
          <p className="text-[10px] font-bold text-gray-500 uppercase">Quality</p>
          <p className="text-lg font-black text-gray-900 dark:text-white">{node.metrics.qualityScore}%</p>
        </div>
        <div className="p-2 bg-gray-50 dark:bg-white/5 rounded-lg border border-border">
          <p className="text-[10px] font-bold text-gray-500 uppercase">Open NCR</p>
          <p className="text-lg font-black text-gray-900 dark:text-white">{node.metrics.openNcr}</p>
        </div>
        <div className="p-2 bg-gray-50 dark:bg-white/5 rounded-lg border border-border">
          <p className="text-[10px] font-bold text-gray-500 uppercase">Projects</p>
          <p className="text-lg font-black text-gray-900 dark:text-white">{node.metrics.projects}</p>
        </div>
      </div>

      <div className="text-center text-[10px] font-medium text-gray-400">
        Updated {node.lastUpdated}
      </div>

      {/* Color Accent Line */}
      <div className={`absolute bottom-0 left-0 w-full h-1.5 rounded-b-3xl ${colorClasses.split(' ')[0]}`} />
    </motion.div>
  );
};

// ============================================================================
// COMPONENT: EXPANDED DASHBOARD
// ============================================================================
const ExpandedDashboard = ({ node, onClose }: { node: OrgNode, onClose: () => void }) => {
  return (
    <motion.div 
      initial={{ opacity: 0, y: 50 }}
      animate={{ opacity: 1, y: 0 }}
      exit={{ opacity: 0, y: 50 }}
      className="bg-white dark:bg-card rounded-3xl border border-border shadow-xl overflow-hidden mt-12 mb-20"
    >
      {/* Dashboard Header */}
      <div className="p-6 md:p-8 border-b border-border flex flex-col md:flex-row justify-between items-start md:items-center gap-4 bg-gray-50/50 dark:bg-white/[0.02]">
        <div>
          <div className="flex items-center space-x-3 mb-2">
            <h2 className="text-3xl font-black text-gray-900 dark:text-white">{node.name}</h2>
            {getHealthBadge(node.status)}
          </div>
          <p className="text-gray-500 font-medium flex items-center">
            <Users className="w-4 h-4 mr-1.5" /> Led by {node.leader} • {node.employeeCount} Employees
          </p>
        </div>
        <button onClick={onClose} className="p-2 bg-gray-200/50 dark:bg-white/10 hover:bg-gray-300/50 dark:hover:bg-white/20 rounded-full transition-colors">
          <X className="w-5 h-5 text-gray-600 dark:text-gray-300" />
        </button>
      </div>

      {/* AI Summary Banner */}
      <div className="m-6 md:m-8 p-6 bg-gradient-to-br from-blue-50 to-indigo-50 dark:from-blue-900/10 dark:to-indigo-900/10 border border-blue-100 dark:border-blue-800/30 rounded-2xl relative overflow-hidden">
        <div className="absolute top-0 right-0 p-4 opacity-10">
          <Zap className="w-24 h-24 text-blue-500" />
        </div>
        <div className="relative z-10">
          <h3 className="text-sm font-black text-blue-800 dark:text-blue-400 uppercase tracking-widest mb-3 flex items-center">
            <Zap className="w-4 h-4 mr-2" /> AI Executive Summary
          </h3>
          <p className="text-blue-900 dark:text-gray-600 dark:text-blue-100 font-medium leading-relaxed max-w-4xl text-sm md:text-base">
            {node.status === 'Healthy' 
              ? `${node.name} is operating in Healthy condition. Quality Score improved by 2% this week. Inspection pass rate remains stable. Supplier dependencies are well-managed with minimal risk.`
              : node.status === 'Critical'
              ? `URGENT: ${node.name} requires immediate intervention. Health Score has dropped to ${node.healthScore}%. ${node.metrics.openNcr} Open NCRs are blocking ${node.metrics.delayedTasks} tasks. Recommend allocating additional capacity to address compliance risks.`
              : `${node.name} requires attention. While Quality Score is ${node.metrics.qualityScore}%, there are ${node.metrics.delayedTasks} delayed tasks affecting ${node.metrics.projects} projects. Supplier risks are currently at ${node.metrics.supplierRisk}.`}
          </p>
        </div>
      </div>

      <div className="p-6 md:p-8 pt-0 grid grid-cols-1 lg:grid-cols-3 gap-8">
        
        {/* Left Col: KPI Cards */}
        <div className="lg:col-span-2 space-y-8">
          <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
            <div className="p-5 bg-gray-50 dark:bg-popover rounded-2xl border border-border">
              <p className="text-xs font-bold text-gray-500 uppercase">Quality Score</p>
              <div className="flex items-end mt-2"><p className="text-3xl font-black">{node.metrics.qualityScore}%</p><TrendingUp className="w-4 h-4 ml-2 mb-1 text-emerald-500" /></div>
            </div>
            <div className="p-5 bg-rose-50 dark:bg-rose-900/10 rounded-2xl border border-rose-100 dark:border-rose-900/30">
              <p className="text-xs font-bold text-rose-500 uppercase">Open NCR</p>
              <div className="flex items-end mt-2"><p className="text-3xl font-black text-rose-600 dark:text-rose-400">{node.metrics.openNcr}</p><AlertTriangle className="w-4 h-4 ml-2 mb-1 text-rose-500" /></div>
            </div>
            <div className="p-5 bg-gray-50 dark:bg-popover rounded-2xl border border-border">
              <p className="text-xs font-bold text-gray-500 uppercase">Active Projects</p>
              <div className="flex items-end mt-2"><p className="text-3xl font-black">{node.metrics.projects}</p></div>
            </div>
            <div className="p-5 bg-gray-50 dark:bg-popover rounded-2xl border border-border">
              <p className="text-xs font-bold text-gray-500 uppercase">Delayed Tasks</p>
              <div className="flex items-end mt-2"><p className="text-3xl font-black">{node.metrics.delayedTasks}</p></div>
            </div>
          </div>

          {/* Project Pipeline */}
          <div>
            <h3 className="text-lg font-black text-gray-900 dark:text-white mb-4 flex items-center">
              <Folder className="w-5 h-5 mr-2 text-primary dark:text-blue-500" /> Project Pipeline
            </h3>
            <div className="bg-gray-50 dark:bg-popover p-6 rounded-2xl border border-border">
              <div className="flex flex-col space-y-4">
                {[
                  { stage: 'Planning', count: Math.floor(node.metrics.projects * 0.1), progress: 100 },
                  { stage: 'DVR', count: Math.floor(node.metrics.projects * 0.2), progress: 80 },
                  { stage: 'PVR', count: Math.floor(node.metrics.projects * 0.4), progress: 60 },
                  { stage: 'PRA', count: Math.floor(node.metrics.projects * 0.2), progress: 40 },
                  { stage: 'Mass Production', count: Math.floor(node.metrics.projects * 0.1), progress: 20 },
                ].map(p => (
                  <div key={p.stage}>
                    <div className="flex justify-between text-sm font-bold mb-1">
                      <span className="text-gray-700 dark:text-gray-300">{p.stage} ({p.count})</span>
                    </div>
                    <div className="w-full bg-gray-200 dark:bg-gray-800 rounded-full h-2">
                      <div className="bg-primary dark:bg-blue-500 h-2 rounded-full" style={{ width: `${p.progress}%` }}></div>
                    </div>
                  </div>
                ))}
              </div>
            </div>
          </div>
        </div>

        {/* Right Col: Timeline & Alerts */}
        <div className="space-y-8">
          <div>
            <h3 className="text-lg font-black text-gray-900 dark:text-white mb-4 flex items-center">
              <Activity className="w-5 h-5 mr-2 text-primary dark:text-blue-500" /> Smart Alerts
            </h3>
            <div className="space-y-3">
              {node.metrics.openNcr > 10 && (
                <div className="p-4 bg-rose-50 dark:bg-rose-900/10 border border-rose-100 dark:border-rose-900/30 rounded-xl flex items-start">
                  <FileWarning className="w-5 h-5 text-rose-500 mr-3 mt-0.5 flex-shrink-0" />
                  <div>
                    <p className="text-sm font-bold text-rose-800 dark:text-rose-400">High NCR Volume</p>
                    <p className="text-xs font-medium text-rose-600/80 dark:text-rose-400/80 mt-1">Over 10 open non-conformances require immediate review.</p>
                  </div>
                </div>
              )}
              {node.metrics.supplierRisk > 2 && (
                <div className="p-4 bg-orange-50 dark:bg-orange-900/10 border border-orange-100 dark:border-orange-900/30 rounded-xl flex items-start">
                  <ShieldAlert className="w-5 h-5 text-orange-500 mr-3 mt-0.5 flex-shrink-0" />
                  <div>
                    <p className="text-sm font-bold text-orange-800 dark:text-orange-400">Supplier Risk Elevated</p>
                    <p className="text-xs font-medium text-orange-600/80 dark:text-orange-400/80 mt-1">{node.metrics.supplierRisk} suppliers flagged for delayed responses.</p>
                  </div>
                </div>
              )}
              <div className="p-4 bg-blue-50 dark:bg-blue-900/10 border border-blue-100 dark:border-blue-900/30 rounded-xl flex items-start">
                <Clock className="w-5 h-5 text-blue-500 mr-3 mt-0.5 flex-shrink-0" />
                <div>
                  <p className="text-sm font-bold text-blue-800 dark:text-blue-400">Pending Approvals</p>
                  <p className="text-xs font-medium text-blue-600/80 dark:text-blue-400/80 mt-1">{node.metrics.pendingApproval} documents awaiting signature.</p>
                </div>
              </div>
            </div>
          </div>

          <div>
            <h3 className="text-lg font-black text-gray-900 dark:text-white mb-4 flex items-center">
              <Clock className="w-5 h-5 mr-2 text-gray-400" /> Recent Activities
            </h3>
            <div className="relative border-l-2 border-border ml-3 space-y-6">
              {[
                { time: '2 hours ago', title: 'NCR Approved', desc: 'QMS-2024-089 was closed.' },
                { time: '5 hours ago', title: 'Inspection Failed', desc: 'Lot #882 rejected for dimension error.' },
                { time: '1 day ago', title: 'Supplier Update', desc: 'Supplier ABC uploaded PPAP documents.' }
              ].map((act, i) => (
                <div key={i} className="pl-6 relative">
                  <div className="absolute w-3 h-3 bg-white dark:bg-card border-2 border-primary dark:border-blue-500 rounded-full -left-[7px] top-1.5" />
                  <p className="text-xs font-bold text-gray-400 mb-1">{act.time}</p>
                  <p className="text-sm font-bold text-gray-900 dark:text-white">{act.title}</p>
                  <p className="text-xs font-medium text-gray-500">{act.desc}</p>
                </div>
              ))}
            </div>
          </div>
        </div>

      </div>
    </motion.div>
  );
}


// ============================================================================
// MAIN PAGE EXPORT
// ============================================================================
export default function OrganizationHealthPage() {
  const [selectedNodeId, setSelectedNodeId] = useState<string | null>(null);

  // Flatten the tree to find selected node easily
  const getAllNodes = (root: OrgNode): OrgNode[] => {
    let nodes = [root];
    if (root.children) {
      root.children.forEach(c => nodes = nodes.concat(getAllNodes(c)));
    }
    return nodes;
  };
  const allNodes = getAllNodes(IQC_ORG);
  const selectedNode = selectedNodeId ? allNodes.find(n => n.id === selectedNodeId) : null;

  return (
    <div className="min-h-full bg-transparent p-6 lg:p-10 relative overflow-hidden">
      
      {/* Background Decor */}
      <div className="absolute top-0 right-0 w-[800px] h-[800px] bg-gradient-to-bl from-blue-100/50 via-transparent to-transparent dark:from-blue-900/10 rounded-full blur-3xl pointer-events-none -z-10 transform translate-x-1/3 -translate-y-1/3" />
      
      {/* Header */}
      <div className="mb-12">
        <h1 className="text-4xl font-black text-transparent bg-clip-text bg-gradient-to-r from-gray-900 to-gray-600 dark:from-white dark:to-gray-400 mb-3 flex items-center">
          <Activity className="w-10 h-10 mr-4 text-primary dark:text-blue-500" />
          Organization Health Center
        </h1>
        <p className="text-lg font-medium text-gray-500 max-w-3xl leading-relaxed">
          The digital twin of the IQC Organization. Monitor live operational health, bottlenecks, and KPIs across the entire factory hierarchy.
        </p>
      </div>

      {/* Global Health Map */}
      <div className="bg-transparent mb-12 flex flex-col items-center justify-center py-10 relative">
        
        {/* Connection Lines (CSS simulated via borders for simplicity in React without canvas) */}
        <div className="absolute top-1/2 left-1/2 -translate-x-1/2 -translate-y-1/2 w-px h-24 bg-gray-300 dark:bg-white/20 -z-10 hidden md:block" />
        <div className="absolute top-[calc(50%+48px)] left-[15%] right-[15%] h-px bg-gray-300 dark:bg-white/20 -z-10 hidden md:block" />
        <div className="absolute top-[calc(50%+48px)] left-[15%] w-px h-12 bg-gray-300 dark:bg-white/20 -z-10 hidden md:block" />
        <div className="absolute top-[calc(50%+48px)] left-[50%] w-px h-12 bg-gray-300 dark:bg-white/20 -z-10 hidden md:block" />
        <div className="absolute top-[calc(50%+48px)] right-[15%] w-px h-12 bg-gray-300 dark:bg-white/20 -z-10 hidden md:block" />

        {/* Root Node */}
        <div className="z-10 flex justify-center mb-16 md:mb-24">
          <HealthNode 
            node={IQC_ORG} 
            isRoot={true} 
            isSelected={selectedNodeId === IQC_ORG.id}
            onClick={() => setSelectedNodeId(IQC_ORG.id)} 
          />
        </div>

        {/* Child Nodes */}
        <div className="z-10 flex flex-col md:flex-row justify-center gap-8 md:gap-16 w-full px-4">
          {IQC_ORG.children?.map(child => (
            <HealthNode 
              key={child.id} 
              node={child} 
              isSelected={selectedNodeId === child.id}
              onClick={() => setSelectedNodeId(child.id)} 
            />
          ))}
        </div>
      </div>

      {/* Expanded Dashboard */}
      <AnimatePresence mode="wait">
        {selectedNode && (
          <ExpandedDashboard 
            key={selectedNode.id} 
            node={selectedNode} 
            onClose={() => setSelectedNodeId(null)} 
          />
        )}
      </AnimatePresence>

    </div>
  );
}
