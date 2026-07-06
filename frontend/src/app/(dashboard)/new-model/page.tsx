"use client";

import { useState } from "react";
import { Plus, Search, Filter, Calendar, CheckCircle2, Clock, AlertTriangle, Layers, ChevronRight, X, Target, Wrench, Package, Sparkles, Activity, FileText, LayoutList, ShieldAlert, Check, Paperclip, Briefcase, ChevronDown, Circle, AlertCircle, ArrowDownToLine, Users2, PanelRight, TrendingUp, TrendingDown, Settings, Bell, Zap, BookOpen, LayoutDashboard, SearchIcon, History, Lightbulb, Link2, ArrowRight, Home } from "lucide-react";
import { motion, AnimatePresence } from "framer-motion";
import { useAuth } from "@/lib/contexts/AuthContext";

// --- MOCK DATA ---
const STAGES = [
  { id: "planning", name: "Planning", desc: "Initial project scoping and feasibility analysis." },
  { id: "dvr", name: "Design Validation Review (DVR)", desc: "Engineering design and spec confirmation." },
  { id: "pvr", name: "Process Validation Review (PVR)", desc: "Manufacturing process readiness verification." },
  { id: "pra", name: "Production Readiness Assessment (PRA)", desc: "Pilot run and final quality sign-off." },
  { id: "sra", name: "Supplier Readiness Assessment (SRA)", desc: "Supplier capacity and quality verification." },
  { id: "mass", name: "Mass Production", desc: "Full scale production ramp-up." },
];

const MOCK_PROJECTS = [
  {
    id: "proj_1",
    name: "Galaxy Z Fold 7",
    customer: "Samsung",
    stage: "pvr",
    progress: 45,
    dueDate: "2026-08-15",
    healthStatus: "Attention",
    priority: "High",
    owner: { name: "Anh Thu", initials: "AT", color: "bg-[#1428A0]" },
    openIssues: 12,
    supplierCount: 45,
    pendingApprovals: 3,
    unreadDiscussions: 5,
    risk: "Medium",
    aiSummary: "PVR gate is slightly delayed due to Camera Module L1 calibration issues. Supplier is addressing the yield rate. Recommend expediting the jig validation to recover the 3-day delay."
  },
  {
    id: "proj_2",
    name: "Galaxy S26 Ultra",
    customer: "Samsung",
    stage: "dvr",
    progress: 20,
    dueDate: "2026-09-01",
    healthStatus: "Critical",
    priority: "Critical",
    owner: { name: "Minh Hai", initials: "MH", color: "bg-rose-600" },
    openIssues: 34,
    supplierCount: 82,
    pendingApprovals: 8,
    unreadDiscussions: 12,
    risk: "High",
    aiSummary: "Critical delay in JIG fabrication for final assembly. Immediate intervention required to meet DVR gate deadline. Supplier B is flagged for capacity risk."
  },
  {
    id: "proj_3",
    name: "Galaxy Buds 4 Pro",
    customer: "Samsung",
    stage: "pra",
    progress: 75,
    dueDate: "2026-07-20",
    healthStatus: "Healthy",
    priority: "Medium",
    owner: { name: "Hoang Nam", initials: "HN", color: "bg-emerald-600" },
    openIssues: 2,
    supplierCount: 15,
    pendingApprovals: 1,
    unreadDiscussions: 0,
    risk: "Low",
    aiSummary: "PRA stage validation passed 98% of acoustic tests. Ready for final SRA sign-off."
  },
  {
    id: "proj_4",
    name: "Galaxy Watch 7",
    customer: "Samsung",
    stage: "planning",
    progress: 10,
    dueDate: "2026-11-20",
    healthStatus: "Healthy",
    priority: "Medium",
    owner: { name: "Quang Vinh", initials: "QV", color: "bg-purple-600" },
    openIssues: 5,
    supplierCount: 12,
    pendingApprovals: 2,
    unreadDiscussions: 8,
    risk: "Low",
    aiSummary: "Project kickoff completed. Supplier selection matrix is under review."
  },
];

const MOCK_KNOWLEDGE_RESULTS = [
  { id: "k1", type: "Lesson Learned", title: "Camera L1 Calibration Drift in Mass Production", project: "Galaxy Z Fold 7", date: "Oct 2025", author: "Anh Thu", tags: ["Camera", "Calibration", "PVR"] },
  { id: "k2", type: "NCR Resolution", title: "Supplier B Capacity Shortfall during SRA", project: "Galaxy S25 Ultra", date: "Jan 2025", author: "Minh Hai", tags: ["Supplier", "Capacity", "SRA"] },
  { id: "k3", type: "Document", title: "Standard Operating Procedure: Acoustic Testing v3", project: "Global NPI Standard", date: "Mar 2026", author: "Hoang Nam", tags: ["Acoustic", "PRA", "SOP"] },
];

const CircularProgress = ({ value, size = 36, strokeWidth = 3, colorClass = "text-blue-600" }: { value: number, size?: number, strokeWidth?: number, colorClass?: string }) => {
  const radius = (size - strokeWidth) / 2;
  const circumference = radius * 2 * Math.PI;
  const offset = circumference - (value / 100) * circumference;
  return (
    <div className="relative flex items-center justify-center" style={{ width: size, height: size }}>
      <svg className="transform -rotate-90 w-full h-full">
        <circle cx={size / 2} cy={size / 2} r={radius} className="stroke-gray-200 dark:stroke-white/10" strokeWidth={strokeWidth} fill="transparent" />
        <circle cx={size / 2} cy={size / 2} r={radius} className={`stroke-current ${colorClass} transition-all duration-1000 ease-in-out`} strokeWidth={strokeWidth} fill="transparent" strokeDasharray={circumference} strokeDashoffset={offset} strokeLinecap="round" />
      </svg>
    </div>
  );
};

export default function NewModelPage() {
  const { user, activeRoleLens } = useAuth();
  const [appMode, setAppMode] = useState<'workspace' | 'knowledge'>('workspace');
  
  // Set to null to show Operational Home Screen by default
  const [activeProject, setActiveProject] = useState<any | null>(null);
  
  const [activeTab, setActiveTab] = useState("overview");
  const [expandedStep, setExpandedStep] = useState<string | null>(null);
  const [showRightPanel, setShowRightPanel] = useState(true);
  
  // Checklist states
  const [checklist, setChecklist] = useState({ bom: true, qa: false, jig: false, supp: false });
  const allChecked = Object.values(checklist).every(v => v);

  const handleProjectSelect = (project: any) => {
    setActiveProject(project);
    if(project) {
      setExpandedStep(project.stage);
      setActiveTab("overview");
    }
  };

  return (
    <div className="h-[calc(100vh-64px)] flex bg-transparent relative z-0 overflow-hidden">
      {/* Ambient Pastel Background for Light Mode */}
      <div className="absolute inset-0 overflow-hidden pointer-events-none -z-10 bg-gradient-to-br from-indigo-50/40 via-white to-blue-50/40 dark:from-indigo-950/20 dark:via-[#0a0a0a] dark:to-blue-900/10"></div>
      
      {/* ================= MODE TOGGLE NAV ================= */}
      <div className="w-16 bg-white dark:bg-[#0A0A0A] border-r border-gray-200 dark:border-white/10 flex flex-col items-center py-6 z-20">
        <button 
          onClick={() => setAppMode('workspace')}
          className={`w-10 h-10 rounded-xl flex items-center justify-center transition-all mb-4 ${appMode === 'workspace' ? 'bg-[#1428A0] text-white shadow-md' : 'text-gray-400 hover:bg-gray-100 dark:hover:bg-white/5'}`}
          title="Execution Workspace"
        >
          <LayoutDashboard className="w-5 h-5" />
        </button>
        <button 
          onClick={() => setAppMode('knowledge')}
          className={`w-10 h-10 rounded-xl flex items-center justify-center transition-all ${appMode === 'knowledge' ? 'bg-[#1428A0] text-white shadow-md' : 'text-gray-400 hover:bg-gray-100 dark:hover:bg-white/5'}`}
          title="Knowledge Base"
        >
          <BookOpen className="w-5 h-5" />
        </button>
      </div>

      {appMode === 'workspace' ? (
        <>
          {/* ================= LEFT PANEL (MASTER LIST) ================= */}
          <div className="w-[380px] flex-shrink-0 flex flex-col border-r border-white/40 dark:border-white/10 bg-white/60 dark:bg-[#0A0A0A]/60 backdrop-blur-xl z-10 shadow-[1px_0_10px_rgba(0,0,0,0.02)]">
            <div className="px-5 py-4 border-b border-gray-100 dark:border-white/5">
              <div className="flex justify-between items-center mb-4">
                <h1 className="text-lg font-black text-gray-900 dark:text-white tracking-tight">Active Projects</h1>
                <button className="flex items-center justify-center w-8 h-8 bg-gray-900 dark:bg-white text-white dark:text-black rounded-lg hover:bg-black transition-colors shadow-sm"><Plus className="w-5 h-5" /></button>
              </div>
              <div className="relative mb-3">
                <Search className="w-4 h-4 absolute left-3 top-2.5 text-gray-400" />
                <input type="text" placeholder="Search Projects..." className="w-full bg-gray-50 dark:bg-[#151515] border border-gray-200 dark:border-white/10 rounded-xl pl-9 pr-3 py-2 text-sm focus:outline-none focus:border-[#1428A0] transition-all" />
              </div>
              <div className="flex items-center space-x-2 overflow-x-auto custom-scrollbar pb-1">
                <button className="px-3 py-1 bg-gray-100 dark:bg-white/10 text-gray-700 dark:text-gray-300 rounded-lg text-[10px] font-bold"><Filter className="w-3 h-3 inline-block mr-1" />Filter</button>
                {['All', 'Critical', 'My Projects'].map(f => (
                  <button key={f} className="px-3 py-1 bg-white dark:bg-[#151515] border border-gray-200 dark:border-white/10 text-gray-600 dark:text-gray-400 rounded-lg text-[10px] font-bold hover:text-[#1428A0] transition-colors">{f}</button>
                ))}
              </div>
            </div>

            <div className="flex-1 overflow-y-auto custom-scrollbar p-3 space-y-2">
              
              {/* Pinned "My Operations" Item */}
              <div 
                onClick={() => handleProjectSelect(null)} 
                className={`p-3 rounded-2xl cursor-pointer transition-all border flex items-center mb-4 ${activeProject === null ? 'bg-indigo-50/50 dark:bg-indigo-900/20 border-indigo-200 dark:border-indigo-800 shadow-sm' : 'bg-gray-50 dark:bg-[#151515] border-transparent hover:border-gray-200 dark:hover:border-white/10'}`}
              >
                <div className={`w-10 h-10 rounded-xl flex items-center justify-center mr-3 shadow-sm ${activeProject === null ? 'bg-[#1428A0] text-white' : 'bg-white dark:bg-[#1A1A1A] text-gray-500'}`}>
                  <Home className="w-5 h-5" />
                </div>
                <div>
                  <h3 className={`font-black text-sm ${activeProject === null ? 'text-[#1428A0] dark:text-blue-400' : 'text-gray-900 dark:text-white'}`}>My Operations</h3>
                  <p className="text-xs text-gray-500 font-medium">Daily summary & tasks</p>
                </div>
              </div>

              {/* Project List */}
              {MOCK_PROJECTS.map(project => {
                const isSelected = activeProject?.id === project.id;
                const isHealthy = project.healthStatus === 'Healthy';
                const isAtRisk = project.healthStatus === 'Attention';
                
                return (
                  <div key={project.id} onClick={() => handleProjectSelect(project)} className={`p-3 rounded-2xl cursor-pointer transition-all border flex flex-col ${isSelected ? 'bg-blue-50/50 dark:bg-blue-900/10 border-[#1428A0]/30 shadow-sm' : 'bg-white dark:bg-[#151515] border-gray-200 dark:border-white/5 hover:border-gray-300 dark:hover:border-white/10 hover:shadow-sm'}`}>
                    <div className="flex justify-between items-start mb-1">
                      <div className="flex items-center space-x-1.5">
                        <div className={`w-1.5 h-1.5 rounded-full ${isHealthy ? 'bg-emerald-500' : isAtRisk ? 'bg-amber-500' : 'bg-rose-500'}`}></div>
                        <h3 className={`font-bold text-sm truncate max-w-[200px] ${isSelected ? 'text-[#1428A0] dark:text-blue-400' : 'text-gray-900 dark:text-white'}`}>{project.name}</h3>
                      </div>
                      <span className="text-[10px] text-gray-500 font-medium whitespace-nowrap">ETA: {new Date(project.dueDate).toLocaleDateString('en-GB', {day:'2-digit', month:'short'})}</span>
                    </div>
                    <div className="flex justify-between items-center mt-2">
                      <div className="flex items-center space-x-3 text-[10px] text-gray-500">
                        <span className="font-bold text-gray-800 dark:text-gray-200">{STAGES.find(s => s.id === project.stage)?.name.split(' (')[0]}</span>
                        <span className="flex items-center" title="Open Issues"><ShieldAlert className="w-3 h-3 mr-1" />{project.openIssues}</span>
                        <span className="flex items-center" title="Suppliers"><Users2 className="w-3 h-3 mr-1" />{project.supplierCount}</span>
                      </div>
                      <div className="flex items-center space-x-2">
                        <CircularProgress value={project.progress} size={18} strokeWidth={3} colorClass={isHealthy ? 'text-emerald-500' : isAtRisk ? 'text-amber-500' : 'text-rose-500'} />
                      </div>
                    </div>
                  </div>
                )
              })}
            </div>
          </div>

          {/* ================= RIGHT PANEL (WORKSPACE OR HOME) ================= */}
          <div className="flex-1 flex flex-col bg-transparent overflow-hidden relative">
            
            {/* If NO PROJECT is selected -> Show Operational Home Screen */}
            {!activeProject ? (
              <div className="flex-1 overflow-y-auto custom-scrollbar p-4 lg:p-6 pb-20 relative z-0">
                
                <div className="max-w-6xl mx-auto space-y-4">
                  
                  {/* Greeting & AI Summary Banner */}
                  <div className="bg-white/70 dark:bg-white/[0.03] backdrop-blur-3xl border border-white dark:border-white/10 rounded-[2rem] p-5 md:p-6 text-gray-900 dark:text-white shadow-xl shadow-blue-900/5 dark:shadow-none relative overflow-hidden transition-all duration-500 hover:border-white/50 dark:hover:border-white/20 flex justify-between items-center">
                    <div className="absolute top-0 right-0 w-[500px] h-[500px] bg-blue-500/5 dark:bg-blue-500/10 rounded-full blur-3xl translate-x-1/3 -translate-y-1/3 pointer-events-none"></div>
                    <div className="relative z-10 max-w-2xl">
                      <div className="flex items-center space-x-2 mb-3">
                        <Sparkles className="w-4 h-4 text-blue-600 dark:text-blue-300" />
                        <span className="text-[10px] font-bold text-blue-700 dark:text-blue-200 uppercase tracking-widest">AI Executive Briefing • {activeRoleLens}</span>
                      </div>
                      <h2 className="text-xl md:text-2xl font-bold mb-1 tracking-tight">Good morning, {user.name}!</h2>
                      <p className="text-gray-600 dark:text-blue-100 font-medium text-xs md:text-sm">
                        You have 12 tasks requiring attention today. 2 critical blockers identified.
                      </p>
                    </div>
                    <div className="relative z-10 flex space-x-3 text-center">
                      <div className="bg-white/80 dark:bg-white/5 backdrop-blur-md rounded-2xl px-4 py-2 min-w-[80px] border border-white/50 dark:border-white/10 shadow-sm">
                        <p className="text-xl font-black text-rose-600 dark:text-rose-400">2</p>
                        <p className="text-[10px] font-bold text-gray-500 uppercase tracking-widest mt-0.5">Blocked</p>
                      </div>
                      <div className="bg-white/80 dark:bg-white/5 backdrop-blur-md rounded-2xl px-4 py-2 min-w-[80px] border border-white/50 dark:border-white/10 shadow-sm">
                        <p className="text-xl font-black text-indigo-600 dark:text-indigo-400">12</p>
                        <p className="text-[10px] font-bold text-gray-500 uppercase tracking-widest mt-0.5">Pending</p>
                      </div>
                    </div>
                  </div>

                  <div className="grid grid-cols-1 xl:grid-cols-3 gap-6">
                    {/* Urgent Action Grid (2/3 width) */}
                    <div className="xl:col-span-2 space-y-6">
                      
                      {activeRoleLens === 'Staff' && (
                        <>
                          <div className="bg-gradient-to-b from-white/90 to-white/40 dark:from-[#1A1A1A]/90 dark:to-[#151515]/40 backdrop-blur-xl rounded-3xl border border-white/60 dark:border-white/10 shadow-[0_8px_30px_rgb(0,0,0,0.04)] dark:shadow-[0_8px_30px_rgb(0,0,0,0.2)] p-6">
                            <div className="flex justify-between items-center mb-6">
                              <h3 className="text-lg font-black text-gray-900 dark:text-white flex items-center"><Target className="w-5 h-5 mr-2 text-[#1428A0] dark:text-blue-500" /> My Tasks</h3>
                              <button className="text-sm font-bold text-[#1428A0] dark:text-blue-400 hover:underline">View all (12)</button>
                            </div>
                            <div className="space-y-3">
                              {[
                                { title: "Upload PVR Jig Test Validation", project: "Galaxy Z Fold 7", time: "Due Today", c: "bg-rose-50 text-rose-700 dark:bg-rose-900/20 dark:text-rose-400" },
                                { title: "Review Supplier B Acoustic Capacity", project: "Galaxy S26 Ultra", time: "Overdue", c: "bg-rose-50 text-rose-700 dark:bg-rose-900/20 dark:text-rose-400" },
                                { title: "Sign-off Final BOM Document", project: "Galaxy Buds 4 Pro", time: "Tomorrow", c: "bg-gray-100 text-gray-700 dark:bg-white/10 dark:text-gray-300" }
                              ].map((t, i) => (
                                <div key={i} className="flex items-center justify-between p-4 border border-gray-100 dark:border-white/5 rounded-2xl hover:border-gray-300 dark:hover:border-white/10 transition-colors cursor-pointer group bg-gray-50/50 dark:bg-[#1A1A1A]">
                                  <div className="flex items-center space-x-4">
                                    <div className="w-5 h-5 rounded-md border-2 border-gray-300 dark:border-gray-600 group-hover:border-[#1428A0] transition-colors"></div>
                                    <div>
                                      <h4 className="text-sm font-bold text-gray-900 dark:text-white group-hover:text-[#1428A0] dark:group-hover:text-blue-400 transition-colors">{t.title}</h4>
                                      <p className="text-xs text-gray-500 font-medium">{t.project}</p>
                                    </div>
                                  </div>
                                  <span className={`text-[10px] font-bold px-2 py-1 rounded-md ${t.c}`}>{t.time}</span>
                                </div>
                              ))}
                            </div>
                          </div>

                          <div className="grid grid-cols-2 gap-6">
                            <div className="bg-gradient-to-b from-white/90 to-white/40 dark:from-[#1A1A1A]/90 dark:to-[#151515]/40 backdrop-blur-xl rounded-3xl border border-white/60 dark:border-white/10 shadow-[0_8px_30px_rgb(0,0,0,0.04)] dark:shadow-[0_8px_30px_rgb(0,0,0,0.2)] p-6">
                              <h3 className="text-sm font-black text-gray-500 uppercase tracking-wider mb-4 flex items-center"><CheckCircle2 className="w-4 h-4 mr-2" /> Pending Approvals</h3>
                              <div className="space-y-4">
                                <div className="flex items-center justify-between">
                                  <div className="flex items-center space-x-3">
                                    <div className="w-10 h-10 rounded-xl bg-indigo-50 dark:bg-indigo-900/20 flex items-center justify-center text-indigo-600 dark:text-indigo-400"><FileText className="w-5 h-5" /></div>
                                    <div><p className="text-xs font-bold text-gray-900 dark:text-white">Camera Spec v2</p><p className="text-[10px] text-gray-500">Z Fold 7 • 2h ago</p></div>
                                  </div>
                                  <button className="text-[10px] font-bold bg-[#1428A0] text-white px-3 py-1.5 rounded-lg shadow-sm">Review</button>
                                </div>
                              </div>
                            </div>
                            <div className="bg-gradient-to-b from-white/90 to-white/40 dark:from-[#1A1A1A]/90 dark:to-[#151515]/40 backdrop-blur-xl rounded-3xl border border-white/60 dark:border-white/10 shadow-[0_8px_30px_rgb(0,0,0,0.04)] dark:shadow-[0_8px_30px_rgb(0,0,0,0.2)] p-6">
                              <h3 className="text-sm font-black text-gray-500 uppercase tracking-wider mb-4 flex items-center"><AlertTriangle className="w-4 h-4 mr-2" /> Open NCRs</h3>
                              <div className="space-y-4">
                                <div className="flex items-center justify-between">
                                  <div className="flex items-center space-x-3">
                                    <div className="w-10 h-10 rounded-xl bg-rose-50 dark:bg-rose-900/20 flex items-center justify-center text-rose-600 dark:text-rose-400"><ShieldAlert className="w-5 h-5" /></div>
                                    <div><p className="text-xs font-bold text-gray-900 dark:text-white">Yield Rate Drop</p><p className="text-[10px] text-gray-500">Supplier B • Critical</p></div>
                                  </div>
                                  <button className="text-[10px] font-bold bg-gray-100 dark:bg-white/10 text-gray-700 dark:text-gray-300 px-3 py-1.5 rounded-lg">Solve</button>
                                </div>
                              </div>
                            </div>
                          </div>
                        </>
                      )}

                      {activeRoleLens === 'Cell Leader' && (
                        <>
                          <div className="bg-gradient-to-b from-white/90 to-white/40 dark:from-[#1A1A1A]/90 dark:to-[#151515]/40 backdrop-blur-xl rounded-3xl border border-white/60 dark:border-white/10 shadow-[0_8px_30px_rgb(0,0,0,0.04)] dark:shadow-[0_8px_30px_rgb(0,0,0,0.2)] p-6">
                            <div className="flex justify-between items-center mb-6">
                              <h3 className="text-lg font-black text-gray-900 dark:text-white flex items-center"><Layers className="w-5 h-5 mr-2 text-[#1428A0] dark:text-blue-500" /> Projects in Scope ({user.scope})</h3>
                            </div>
                            <div className="space-y-3">
                              {MOCK_PROJECTS.slice(0, 2).map((p, i) => (
                                <div key={i} className="flex items-center justify-between p-4 border border-gray-100 dark:border-white/5 rounded-2xl hover:border-[#1428A0]/30 transition-colors cursor-pointer bg-gray-50/50 dark:bg-[#1A1A1A]">
                                  <div>
                                    <h4 className="text-sm font-bold text-gray-900 dark:text-white">{p.name}</h4>
                                    <p className="text-xs text-gray-500 font-medium">Stage: {p.stage.toUpperCase()}</p>
                                  </div>
                                  <CircularProgress value={p.progress} size={30} strokeWidth={4} />
                                </div>
                              ))}
                            </div>
                          </div>

                          <div className="grid grid-cols-2 gap-6">
                            <div className="bg-gradient-to-b from-white/90 to-white/40 dark:from-[#1A1A1A]/90 dark:to-[#151515]/40 backdrop-blur-xl rounded-3xl border border-white/60 dark:border-white/10 shadow-[0_8px_30px_rgb(0,0,0,0.04)] dark:shadow-[0_8px_30px_rgb(0,0,0,0.2)] p-6">
                              <h3 className="text-sm font-black text-gray-500 uppercase tracking-wider mb-4 flex items-center"><Clock className="w-4 h-4 mr-2" /> Pending Stage Approvals</h3>
                              <p className="text-3xl font-black text-gray-900 dark:text-white mb-2">4</p>
                              <p className="text-xs text-gray-500">Awaiting your sign-off to proceed.</p>
                            </div>
                            <div className="bg-rose-50 dark:bg-rose-900/10 rounded-3xl border border-rose-200 dark:border-rose-900/30 shadow-sm p-6">
                              <h3 className="text-sm font-black text-rose-600 dark:text-rose-400 uppercase tracking-wider mb-4 flex items-center"><ShieldAlert className="w-4 h-4 mr-2" /> Blocked Stages</h3>
                              <p className="text-3xl font-black text-rose-700 dark:text-rose-400 mb-2">1</p>
                              <p className="text-xs text-rose-600/80 dark:text-rose-400/80">Galaxy S26 Ultra - Supplier Readiness.</p>
                            </div>
                          </div>
                        </>
                      )}

                      {['Part Leader', 'Group Leader', 'Team Leader'].includes(activeRoleLens) && (
                        <>
                          <div className="bg-gradient-to-b from-white/90 to-white/40 dark:from-[#1A1A1A]/90 dark:to-[#151515]/40 backdrop-blur-xl rounded-3xl border border-white/60 dark:border-white/10 shadow-[0_8px_30px_rgb(0,0,0,0.04)] dark:shadow-[0_8px_30px_rgb(0,0,0,0.2)] p-6">
                            <div className="flex justify-between items-center mb-6">
                              <h3 className="text-lg font-black text-gray-900 dark:text-white flex items-center"><Activity className="w-5 h-5 mr-2 text-[#1428A0] dark:text-blue-500" /> NPI Strategic Analytics ({user.scope})</h3>
                            </div>
                            <div className="h-48 flex items-center justify-center bg-gray-50 dark:bg-white/5 rounded-2xl border border-gray-100 dark:border-white/5 border-dashed">
                              <p className="text-gray-400 font-bold text-sm">Strategic Project Health Chart Placeholder</p>
                            </div>
                          </div>

                          <div className="grid grid-cols-2 gap-6">
                            <div className="bg-gradient-to-b from-white/90 to-white/40 dark:from-[#1A1A1A]/90 dark:to-[#151515]/40 backdrop-blur-xl rounded-3xl border border-white/60 dark:border-white/10 shadow-[0_8px_30px_rgb(0,0,0,0.04)] dark:shadow-[0_8px_30px_rgb(0,0,0,0.2)] p-6">
                              <h3 className="text-sm font-black text-gray-500 uppercase tracking-wider mb-4 flex items-center"><Users2 className="w-4 h-4 mr-2" /> Resource Allocation</h3>
                              <p className="text-3xl font-black text-gray-900 dark:text-white mb-2">94%</p>
                              <p className="text-xs text-gray-500 flex items-center"><ArrowDownToLine className="w-3 h-3 text-emerald-500 mr-1"/> Optimized load across teams</p>
                            </div>
                            <div className="bg-gradient-to-b from-white/90 to-white/40 dark:from-[#1A1A1A]/90 dark:to-[#151515]/40 backdrop-blur-xl rounded-3xl border border-white/60 dark:border-white/10 shadow-[0_8px_30px_rgb(0,0,0,0.04)] dark:shadow-[0_8px_30px_rgb(0,0,0,0.2)] p-6">
                              <h3 className="text-sm font-black text-gray-500 uppercase tracking-wider mb-4 flex items-center"><TrendingUp className="w-4 h-4 mr-2" /> Overall Yield Trend</h3>
                              <p className="text-3xl font-black text-emerald-600 dark:text-emerald-400 mb-2">+1.2%</p>
                              <p className="text-xs text-gray-500">Compared to last month</p>
                            </div>
                          </div>
                        </>
                      )}

                    </div>

                    {/* Global Monitoring Column (1/3 width) */}
                    <div className="space-y-6">
                      <div className="bg-gradient-to-b from-white/90 to-white/40 dark:from-[#1A1A1A]/90 dark:to-[#151515]/40 backdrop-blur-xl rounded-3xl border border-white/60 dark:border-white/10 shadow-[0_8px_30px_rgb(0,0,0,0.04)] dark:shadow-[0_8px_30px_rgb(0,0,0,0.2)] p-6">
                        <h3 className="text-lg font-black text-gray-900 dark:text-white mb-6">Monitoring</h3>
                        
                        <div className="space-y-5">
                          <div>
                            <div className="flex justify-between items-end mb-2">
                              <span className="text-xs font-bold text-gray-500 uppercase tracking-wider">Delayed Projects</span>
                              <span className="text-sm font-black text-rose-600">2</span>
                            </div>
                            <div className="h-1.5 w-full bg-gray-100 dark:bg-white/5 rounded-full overflow-hidden"><div className="h-full bg-rose-500 rounded-full w-1/4"></div></div>
                          </div>
                          <div>
                            <div className="flex justify-between items-end mb-2">
                              <span className="text-xs font-bold text-gray-500 uppercase tracking-wider">Supplier Alerts</span>
                              <span className="text-sm font-black text-amber-500">5</span>
                            </div>
                            <div className="h-1.5 w-full bg-gray-100 dark:bg-white/5 rounded-full overflow-hidden"><div className="h-full bg-amber-500 rounded-full w-1/2"></div></div>
                          </div>
                        </div>

                        <div className="mt-8">
                          <h4 className="text-[10px] font-bold text-gray-500 uppercase tracking-wider mb-3">Recently Updated</h4>
                          <div className="space-y-3">
                            <div className="flex items-center space-x-3 p-3 bg-gray-50 dark:bg-white/5 rounded-xl border border-gray-100 dark:border-white/5">
                              <div className="w-8 h-8 rounded-full bg-blue-100 dark:bg-blue-900/30 flex items-center justify-center"><CheckCircle2 className="w-4 h-4 text-blue-600 dark:text-blue-400" /></div>
                              <div><p className="text-xs font-bold text-gray-800 dark:text-gray-200">S26 Ultra PVR passed</p><p className="text-[10px] text-gray-500">10 mins ago by Minh Hai</p></div>
                            </div>
                            <div className="flex items-center space-x-3 p-3 bg-gray-50 dark:bg-white/5 rounded-xl border border-gray-100 dark:border-white/5">
                              <div className="w-8 h-8 rounded-full bg-amber-100 dark:bg-amber-900/30 flex items-center justify-center"><Bell className="w-4 h-4 text-amber-600 dark:text-amber-400" /></div>
                              <div><p className="text-xs font-bold text-gray-800 dark:text-gray-200">2 New messages in Z Fold 7</p><p className="text-[10px] text-gray-500">1 hr ago</p></div>
                            </div>
                          </div>
                        </div>
                      </div>
                    </div>
                  </div>
                </div>
              </div>
            ) : (
              /* If PROJECT IS SELECTED -> Show Workspace */
              <>
                {/* Assistant & Execution Side Panel */}
                <AnimatePresence>
                  {showRightPanel && (
                    <motion.div initial={{ width: 0, opacity: 0 }} animate={{ width: 340, opacity: 1 }} exit={{ width: 0, opacity: 0 }} className="absolute right-0 top-0 bottom-0 bg-white dark:bg-[#0A0A0A] border-l border-gray-200 dark:border-white/10 z-20 shadow-2xl flex flex-col">
                      <div className="p-4 border-b border-gray-100 dark:border-white/5 flex justify-between items-center bg-gray-50 dark:bg-transparent">
                        <h3 className="font-bold text-sm text-gray-900 dark:text-white flex items-center"><Zap className="w-4 h-4 mr-2 text-indigo-500" /> Action Center</h3>
                        <button onClick={() => setShowRightPanel(false)} className="p-1 hover:bg-gray-200 dark:hover:bg-white/10 rounded-md text-gray-400"><X className="w-4 h-4" /></button>
                      </div>
                      <div className="flex-1 overflow-y-auto p-4 custom-scrollbar">
                        
                        {/* 1. EXECUTE: Today's Tasks */}
                        <div className="mb-6">
                          <h4 className="text-[10px] font-bold text-gray-500 uppercase tracking-wider mb-3">Today's Tasks</h4>
                          <div className="space-y-2">
                            <button className="w-full flex items-center justify-between p-3 bg-blue-50/50 dark:bg-blue-900/20 border border-blue-100 dark:border-blue-500/20 rounded-xl hover:bg-blue-100/50 transition-colors group">
                              <div className="flex items-center"><div className="w-2 h-2 rounded-full bg-blue-500 mr-2"></div><span className="text-xs font-bold text-blue-900 dark:text-blue-600 dark:text-blue-300">Upload PVR Jig Test</span></div>
                              <ArrowRight className="w-3 h-3 text-blue-500 opacity-0 group-hover:opacity-100 transition-opacity" />
                            </button>
                            <button className="w-full flex items-center justify-between p-3 bg-white dark:bg-[#151515] border border-gray-200 dark:border-white/10 rounded-xl hover:border-gray-300 transition-colors group">
                              <div className="flex items-center"><div className="w-2 h-2 rounded-full bg-amber-500 mr-2"></div><span className="text-xs font-bold text-gray-700 dark:text-gray-300">Approve Supplier B Capacity</span></div>
                              <ArrowRight className="w-3 h-3 text-gray-400 opacity-0 group-hover:opacity-100 transition-opacity" />
                            </button>
                          </div>
                        </div>

                        {/* 2. KNOWLEDGE: AI Insight */}
                        <div className="bg-indigo-50/80 dark:bg-indigo-900/10 border border-indigo-100 dark:border-indigo-500/20 rounded-2xl p-4 mb-6">
                          <h4 className="text-[11px] font-black text-indigo-800 dark:text-indigo-300 mb-2 uppercase tracking-wide flex items-center"><Sparkles className="w-3 h-3 mr-1.5" /> AI Summary</h4>
                          <p className="text-xs text-indigo-900 dark:text-indigo-200 leading-relaxed font-medium">{activeProject.aiSummary}</p>
                        </div>
                        
                      </div>
                    </motion.div>
                  )}
                </AnimatePresence>

                {/* Main Content Area */}
                <div className="flex-1 overflow-y-auto custom-scrollbar">
                  
                  {/* Header */}
                  <div className="bg-white dark:bg-[#0A0A0A] px-8 py-6 border-b border-gray-200 dark:border-white/5">
                    <div className="flex justify-between items-start mb-6">
                      <div>
                        <div className="flex items-center space-x-2 text-xs font-bold text-gray-500 uppercase tracking-widest mb-1.5">
                          <span>{activeProject.customer}</span><ChevronRight className="w-3 h-3" /><span className="px-2 py-0.5 bg-gray-100 dark:bg-white/10 text-gray-700 dark:text-gray-300 rounded">ID: {activeProject.id}</span>
                        </div>
                        <h2 className="text-3xl font-black text-gray-900 dark:text-white">{activeProject.name}</h2>
                      </div>
                      <div className="flex items-center space-x-2">
                        {!showRightPanel && (
                          <button onClick={() => setShowRightPanel(true)} className="p-2 bg-indigo-50 text-indigo-600 dark:bg-indigo-900/20 dark:text-indigo-400 rounded-xl hover:bg-indigo-100 transition-colors mr-2 shadow-sm"><PanelRight className="w-5 h-5" /></button>
                        )}
                      </div>
                    </div>

                    {/* KPI Row (MONITOR Pillar) */}
                    <div className="grid grid-cols-2 md:grid-cols-4 lg:grid-cols-6 gap-3">
                      {[
                        { label: "Completion", val: `${activeProject.progress}%`, trend: "+5%", up: true, icon: Target, c: "text-blue-600" },
                        { label: "Open Issues", val: activeProject.openIssues, trend: "-2", up: true, icon: ShieldAlert, c: "text-rose-600" },
                        { label: "Suppliers", val: activeProject.supplierCount, trend: "+1", up: true, icon: Users2, c: "text-purple-600" },
                        { label: "BOM Readiness", val: "92%", trend: "+12%", up: true, icon: Layers, c: "text-emerald-600" },
                        { label: "Jig Status", val: "Delayed", trend: "Critical", up: false, icon: Wrench, c: "text-rose-600" },
                        { label: "Pending Appr", val: activeProject.pendingApprovals, trend: "Stable", up: true, icon: CheckCircle2, c: "text-amber-600" }
                      ].map((k, i) => (
                        <div key={i} className="p-3 bg-white dark:bg-[#151515] rounded-2xl border border-gray-200 dark:border-white/5 flex flex-col justify-between h-24 shadow-sm">
                          <div className="flex justify-between items-start">
                            <span className="text-[10px] font-bold text-gray-500 uppercase tracking-wider">{k.label}</span>
                            <div className={`p-1.5 rounded-md bg-gray-50 dark:bg-white/5 ${k.c}`}><k.icon className="w-3.5 h-3.5" /></div>
                          </div>
                          <div className="flex items-end justify-between">
                            <span className="text-xl font-black text-gray-900 dark:text-white leading-none">{k.val}</span>
                            <div className={`flex items-center text-[10px] font-bold ${k.up ? 'text-emerald-500' : 'text-rose-500'}`}>
                              {k.up ? <TrendingUp className="w-3 h-3 mr-0.5" /> : <TrendingDown className="w-3 h-3 mr-0.5" />} {k.trend}
                            </div>
                          </div>
                        </div>
                      ))}
                    </div>
                  </div>

                  {/* Body & Tabs */}
                  <div className="p-8 max-w-5xl mx-auto">
                    <div className="flex space-x-2 border-b border-gray-200 dark:border-white/10 mb-8 overflow-x-auto custom-scrollbar">
                      {[
                        { id: 'overview', label: 'Lifecycle Stepper' }, { id: 'lessons', label: 'Lessons Learned (KMS)' }, { id: 'bom', label: 'BOM' }, { id: 'docs', label: 'Documents' },
                      ].map(t => (
                        <button key={t.id} onClick={() => setActiveTab(t.id)} className={`px-4 py-2.5 text-sm font-bold border-b-2 whitespace-nowrap transition-colors flex items-center ${activeTab === t.id ? 'border-[#1428A0] text-[#1428A0] dark:border-blue-400 dark:text-blue-400' : 'border-transparent text-gray-500 hover:text-gray-900 dark:hover:text-gray-300'}`}>
                          {t.id === 'lessons' && <Lightbulb className="w-4 h-4 mr-2" />}
                          {t.label}
                        </button>
                      ))}
                    </div>

                    {activeTab === 'overview' && (
                      <div className="space-y-4">
                        {STAGES.map((s, idx) => {
                          const currentStageIdx = STAGES.findIndex(x => x.id === activeProject.stage);
                          const isPast = idx < currentStageIdx;
                          const isActive = idx === currentStageIdx;
                          const isFuture = idx > currentStageIdx;
                          const isExpanded = expandedStep === s.id;
                          
                          if (isPast) {
                            return (
                              <div key={s.id} onClick={() => setExpandedStep(isExpanded ? null : s.id)} className="bg-emerald-50 dark:bg-emerald-900/20 rounded-2xl p-4 flex items-center justify-between cursor-pointer h-[88px]">
                                <div className="flex items-center space-x-4">
                                  <div className="w-10 h-10 rounded-full bg-emerald-500 flex items-center justify-center text-white"><Check className="w-5 h-5" /></div>
                                  <div>
                                    <h4 className="text-sm font-bold text-emerald-900 dark:text-emerald-300">{s.name}</h4>
                                    <p className="text-xs text-emerald-700/80 dark:text-emerald-400/80 font-medium mt-0.5">Completed</p>
                                  </div>
                                </div>
                                <div className="flex items-center space-x-4">
                                  <button className="text-[10px] font-bold text-emerald-700 bg-white dark:bg-emerald-900/50 px-3 py-1.5 rounded-lg border border-emerald-200 dark:border-emerald-700 flex items-center">
                                    <Lightbulb className="w-3 h-3 mr-1" /> View Lessons
                                  </button>
                                  <ChevronDown className={`w-5 h-5 text-emerald-600 transition-transform ${isExpanded ? 'rotate-180' : ''}`} />
                                </div>
                              </div>
                            );
                          }

                          if (isActive) {
                            return (
                              <motion.div key={s.id} layout className="bg-white dark:bg-[#151515] rounded-3xl border-2 border-[#1428A0]/20 dark:border-blue-500/30 shadow-lg overflow-hidden relative">
                                <div className="absolute top-0 left-0 w-1.5 h-full bg-[#1428A0] dark:bg-blue-500"></div>
                                <div onClick={() => setExpandedStep(isExpanded ? null : s.id)} className="p-5 flex items-center justify-between cursor-pointer ml-1.5">
                                  <div className="flex items-center space-x-4">
                                    <div className="w-12 h-12 rounded-full bg-blue-50 dark:bg-blue-900/20 border border-blue-200 dark:border-blue-800 flex items-center justify-center text-[#1428A0] dark:text-blue-500"><Circle className="w-4 h-4 fill-current" /></div>
                                    <div>
                                      <h4 className="text-base font-black text-gray-900 dark:text-white">{s.name}</h4>
                                      <p className="text-xs text-gray-500 font-medium mt-0.5">{s.desc}</p>
                                    </div>
                                  </div>
                                  <span className="text-[10px] font-bold text-[#1428A0] dark:text-blue-400 bg-blue-50 dark:bg-blue-900/20 px-2 py-1 rounded-md">Action Required</span>
                                </div>
                                
                                <AnimatePresence>
                                  {isExpanded && (
                                    <motion.div initial={{ height: 0, opacity: 0 }} animate={{ height: 'auto', opacity: 1 }} exit={{ height: 0, opacity: 0 }} className="overflow-hidden ml-1.5 border-t border-gray-100 dark:border-white/5">
                                      <div className="px-6 py-6">
                                        
                                        {/* EXECUTE: Required Checklist */}
                                        <div className="mb-6">
                                          <h4 className="text-[10px] font-bold text-gray-500 uppercase tracking-wider mb-3">Required Actions for Gate Sign-off</h4>
                                          <div className="grid grid-cols-2 gap-3">
                                            <label className={`flex items-center p-3 rounded-xl border cursor-pointer transition-colors ${checklist.bom ? 'bg-blue-50/50 border-blue-200 dark:bg-blue-900/20 dark:border-blue-800' : 'bg-gray-50 border-gray-200 dark:bg-white/5 dark:border-white/10 hover:border-gray-300'}`}>
                                              <input type="checkbox" checked={checklist.bom} onChange={e => setChecklist(c => ({...c, bom: e.target.checked}))} className="rounded w-4 h-4 text-[#1428A0] mr-3" />
                                              <span className="text-sm font-bold text-gray-700 dark:text-gray-300">BOM Validation Complete</span>
                                            </label>
                                            <label className={`flex items-center p-3 rounded-xl border cursor-pointer transition-colors ${checklist.qa ? 'bg-blue-50/50 border-blue-200 dark:bg-blue-900/20 dark:border-blue-800' : 'bg-gray-50 border-gray-200 dark:bg-white/5 dark:border-white/10 hover:border-gray-300'}`}>
                                              <input type="checkbox" checked={checklist.qa} onChange={e => setChecklist(c => ({...c, qa: e.target.checked}))} className="rounded w-4 h-4 text-[#1428A0] mr-3" />
                                              <span className="text-sm font-bold text-gray-700 dark:text-gray-300">Quality Gate Approval</span>
                                            </label>
                                            <label className={`flex items-center p-3 rounded-xl border cursor-pointer transition-colors ${checklist.jig ? 'bg-blue-50/50 border-blue-200 dark:bg-blue-900/20 dark:border-blue-800' : 'bg-gray-50 border-gray-200 dark:bg-white/5 dark:border-white/10 hover:border-gray-300'}`}>
                                              <input type="checkbox" checked={checklist.jig} onChange={e => setChecklist(c => ({...c, jig: e.target.checked}))} className="rounded w-4 h-4 text-[#1428A0] mr-3" />
                                              <span className="text-sm font-bold text-gray-700 dark:text-gray-300">Jig & Fixture Sign-off</span>
                                            </label>
                                            <label className={`flex items-center p-3 rounded-xl border cursor-pointer transition-colors ${checklist.supp ? 'bg-blue-50/50 border-blue-200 dark:bg-blue-900/20 dark:border-blue-800' : 'bg-gray-50 border-gray-200 dark:bg-white/5 dark:border-white/10 hover:border-gray-300'}`}>
                                              <input type="checkbox" checked={checklist.supp} onChange={e => setChecklist(c => ({...c, supp: e.target.checked}))} className="rounded w-4 h-4 text-[#1428A0] mr-3" />
                                              <span className="text-sm font-bold text-gray-700 dark:text-gray-300">Supplier Capacity Verified</span>
                                            </label>
                                          </div>
                                        </div>

                                        {/* Quick Actions & Final Submit */}
                                        <div className="flex justify-between items-center pt-6 border-t border-gray-100 dark:border-white/5">
                                          <div className="flex space-x-2">
                                            <button className="px-4 py-2 bg-gray-50 dark:bg-[#1A1A1A] border border-gray-200 dark:border-white/10 rounded-xl text-xs font-bold text-gray-700 dark:text-gray-300 hover:bg-gray-100 transition-colors flex items-center"><ArrowDownToLine className="w-3.5 h-3.5 mr-1.5" /> Upload Report</button>
                                            <button className="px-4 py-2 bg-gray-50 dark:bg-[#1A1A1A] border border-gray-200 dark:border-white/10 rounded-xl text-xs font-bold text-gray-700 dark:text-gray-300 hover:bg-gray-100 transition-colors flex items-center"><Lightbulb className="w-3.5 h-3.5 mr-1.5" /> Log Lesson Learned</button>
                                          </div>
                                          <button disabled={!allChecked} className={`px-6 py-2.5 rounded-xl text-sm font-bold transition-all shadow-md ${allChecked ? 'bg-[#1428A0] text-white hover:bg-blue-700' : 'bg-gray-200 dark:bg-white/10 text-gray-400 cursor-not-allowed'}`}>
                                            {allChecked ? 'Submit Gate Approval' : 'Complete checklist to submit'}
                                          </button>
                                        </div>
                                      </div>
                                    </motion.div>
                                  )}
                                </AnimatePresence>
                              </motion.div>
                            );
                          }

                          return (
                            <div key={s.id} className="bg-transparent rounded-2xl p-4 flex items-center justify-between border border-gray-200 dark:border-white/10 h-[88px] opacity-70">
                              <div className="flex items-center space-x-4">
                                <div className="w-10 h-10 rounded-full bg-gray-100 dark:bg-[#1A1A1A] flex items-center justify-center text-gray-500 font-bold border border-gray-200 dark:border-white/10">{idx + 1}</div>
                                <div><h4 className="text-sm font-bold text-gray-500">{s.name}</h4><p className="text-xs text-gray-400 font-medium mt-0.5">Pending</p></div>
                              </div>
                            </div>
                          );
                        })}
                      </div>
                    )}

                    {/* KNOWLEDGE: Lessons Learned Tab inside Workspace */}
                    {activeTab === 'lessons' && (
                      <div className="bg-white dark:bg-[#121212] rounded-3xl border border-gray-200 dark:border-white/5 shadow-sm p-8">
                        <div className="flex justify-between items-center mb-6">
                          <div>
                            <h3 className="text-xl font-black text-gray-900 dark:text-white">Project Knowledge Base</h3>
                            <p className="text-sm text-gray-500 mt-1">Documented root causes and solutions for reuse.</p>
                          </div>
                          <button className="px-4 py-2 bg-blue-50 text-blue-700 dark:bg-blue-900/20 dark:text-blue-400 rounded-xl text-sm font-bold flex items-center"><Plus className="w-4 h-4 mr-1.5" /> New Entry</button>
                        </div>
                        
                        <div className="space-y-4">
                          <div className="p-5 border border-gray-200 dark:border-white/10 rounded-2xl">
                            <div className="flex items-center space-x-2 mb-3">
                              <span className="px-2 py-0.5 bg-rose-100 text-rose-700 dark:bg-rose-900/30 dark:text-rose-400 text-[10px] font-bold rounded">Problem</span>
                              <span className="text-xs text-gray-500">Logged during PVR by Anh Thu</span>
                            </div>
                            <h4 className="text-sm font-bold text-gray-900 dark:text-white mb-2">Camera L1 Calibration Drift in High Temp</h4>
                            <p className="text-sm text-gray-600 dark:text-gray-400 mb-4">Yield rate dropped by 12% during high temp stress testing. Jig #45 was losing calibration.</p>
                            <div className="bg-gray-50 dark:bg-white/5 p-4 rounded-xl">
                              <h5 className="text-[10px] font-bold text-gray-500 uppercase tracking-wider mb-1">Preventive Solution</h5>
                              <p className="text-sm text-gray-800 dark:text-gray-200">Replaced polymer jig mounts with aluminum. Added automated re-calibration script every 100 units.</p>
                            </div>
                          </div>
                        </div>
                      </div>
                    )}
                  </div>
                </div>
              </>
            )}
          </div>
        </>
      ) : (
        /* ================= KNOWLEDGE BASE MODE ================= */
        <div className="flex-1 bg-white dark:bg-[#0A0A0A] flex flex-col items-center pt-24 px-8 overflow-y-auto custom-scrollbar">
          
          <div className="w-full max-w-4xl text-center mb-12">
            <h1 className="text-4xl font-black text-gray-900 dark:text-white mb-4 tracking-tight">Enterprise Knowledge Search</h1>
            <p className="text-lg text-gray-500 max-w-2xl mx-auto">Query the collective intelligence of all past NPI projects, NCRs, and Lessons Learned.</p>
          </div>

          <div className="w-full max-w-3xl relative mb-16">
            <div className="absolute inset-y-0 left-0 pl-5 flex items-center pointer-events-none">
              <SearchIcon className="h-6 w-6 text-gray-400" />
            </div>
            <input 
              type="text" 
              placeholder="e.g. Find all PVR failures related to Supplier A..." 
              className="w-full bg-gray-50 dark:bg-[#151515] border-2 border-gray-200 dark:border-white/10 rounded-full py-5 pl-14 pr-6 text-lg focus:outline-none focus:border-[#1428A0] focus:ring-4 focus:ring-[#1428A0]/10 transition-all shadow-sm"
            />
            <div className="absolute inset-y-0 right-3 flex items-center">
              <button className="bg-[#1428A0] text-white p-3 rounded-full hover:bg-blue-700 shadow-md transition-colors">
                <ArrowRight className="w-5 h-5" />
              </button>
            </div>
          </div>

          <div className="w-full max-w-5xl">
            <h3 className="text-sm font-bold text-gray-500 uppercase tracking-widest mb-6 border-b border-gray-100 dark:border-white/5 pb-2">Recent Discoveries</h3>
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
              {MOCK_KNOWLEDGE_RESULTS.map(k => (
                <div key={k.id} className="p-6 bg-white dark:bg-[#151515] border border-gray-200 dark:border-white/10 rounded-3xl hover:shadow-xl transition-shadow cursor-pointer group">
                  <div className="flex items-center space-x-2 mb-3">
                    <span className="px-2 py-0.5 bg-indigo-50 text-indigo-700 dark:bg-indigo-900/30 dark:text-indigo-400 text-[9px] font-bold uppercase rounded">{k.type}</span>
                  </div>
                  <h4 className="text-lg font-bold text-gray-900 dark:text-white mb-2 group-hover:text-[#1428A0] transition-colors line-clamp-2">{k.title}</h4>
                  <p className="text-xs text-gray-500 mb-4 flex items-center"><History className="w-3.5 h-3.5 mr-1.5" /> {k.project} • {k.date}</p>
                  <div className="flex flex-wrap gap-1.5">
                    {k.tags.map(t => <span key={t} className="px-2 py-1 bg-gray-50 dark:bg-white/5 text-gray-600 dark:text-gray-400 text-[10px] font-bold rounded-md">#{t}</span>)}
                  </div>
                </div>
              ))}
            </div>
          </div>

        </div>
      )}
    </div>
  );
}

