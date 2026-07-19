"use client";

import { useEffect, useState } from "react";
import { 
  Plus, Search, Filter, CheckCircle2, ChevronRight, Target, Wrench, AlertCircle, 
  Check, LayoutDashboard, History, ArrowRight, Upload, AlertTriangle, FileText, Download
} from "lucide-react";
import { motion } from "framer-motion";
import { Card } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { UserBadge } from "@/components/ui/user-badge";
import Link from "next/link";
import { fetchMasterPlanRecords, MasterPlanDisplayDto } from "@/lib/api/masterPlanApi";

// --- MOCK DATA ---
const STAGES = [
  { id: "dv", name: "Design Validation (DV)", desc: "Xác nhận thiết kế." },
  { id: "pv", name: "Process Validation (PV)", desc: "Xác nhận quy trình sản xuất." },
  { id: "pr", name: "Production Readiness (PR)", desc: "Sẵn sàng sản xuất đại trà." },
  { id: "sr", name: "System Release (SR)", desc: "Sản xuất hàng loạt." },
];

const MOCK_PROJECTS = [
  {
    id: "proj_1",
    name: "Galaxy Z Fold 7",
    segment: "Flagship",
    baseModel: "Galaxy Z Fold 6",
    stage: "pv",
    dueDate: "2026-08-15",
    status: "Initiated",
    owner: { name: "Anh Thu", initials: "AT", color: "bg-primary" },
    kpis: {
      approvalSheet: 45,
      issCoverage: 60,
      sampleStatus: 30,
      classARisk: 5,
      jigReady: "Pending"
    }
  },
  {
    id: "proj_2",
    name: "Galaxy S26 Ultra",
    segment: "Flagship",
    baseModel: "Galaxy S25 Ultra",
    stage: "dv",
    dueDate: "2026-09-01",
    status: "Initiated",
    owner: { name: "Minh Hai", initials: "MH", color: "bg-rose-600" },
    kpis: {
      approvalSheet: 10,
      issCoverage: 20,
      sampleStatus: 0,
      classARisk: 12,
      jigReady: "Pending"
    }
  },
  {
    id: "proj_3",
    name: "Galaxy A56 5G",
    segment: "Mid-tier",
    baseModel: "Galaxy A55",
    stage: "pending",
    dueDate: "2026-11-20",
    status: "Pending",
    owner: null,
    kpis: {
      approvalSheet: 0,
      issCoverage: 0,
      sampleStatus: 0,
      classARisk: 0,
      jigReady: "N/A"
    }
  },
];

const MOCK_RISK_ITEMS = [
  { partCode: "GH96-12345A", desc: "Nhiệt độ hoạt động quá cao khi sạc nhanh (>45 độ)", stage: "PV", countermeasure: "Vendor tối ưu lại IC quản lý nguồn và thay đổi thiết kế pad tản nhiệt", status: "Open", pic: "Hải Nam" },
  { partCode: "GH98-98765B", desc: "Tỉ lệ lỗi ngoại quan viền camera > 2% do vết xước gia công", stage: "DV", countermeasure: "Cập nhật lại thông số máy phay CNC tại chuyền sản xuất của Vendor", status: "Closed", pic: "Anh Thư" },
];

export default function NewModelPage() {
  // Set to null to show Master Plan by default
  const [activeProject, setActiveProject] = useState<typeof MOCK_PROJECTS[0] | null>(null);
  const [activeTab, setActiveTab] = useState("bom");
  const [hasRiskData, setHasRiskData] = useState(false);
  const [masterPlans, setMasterPlans] = useState<MasterPlanDisplayDto[]>([]);
  const [isLoadingMasterPlans, setIsLoadingMasterPlans] = useState(true);

  useEffect(() => {
    if (activeProject !== null) return;

    let cancelled = false;

    fetchMasterPlanRecords()
      .then((data) => {
        if (!cancelled) setMasterPlans(data);
      })
      .catch((error) => console.error(error))
      .finally(() => {
        if (!cancelled) setIsLoadingMasterPlans(false);
      });

    return () => {
      cancelled = true;
    };
  }, [activeProject]);

  const handleProjectSelect = (project: typeof MOCK_PROJECTS[0] | null) => {
    if (project === null) {
      setIsLoadingMasterPlans(true);
    }

    setActiveProject(project);
    if (project) {
      setActiveTab("bom"); // Default tab for PIC
    }
  };

  return (
    <div className="h-[calc(100vh-64px)] flex bg-transparent relative z-0 overflow-hidden">
      {/* Ambient Pastel Background */}
      <div className="absolute inset-0 overflow-hidden pointer-events-none -z-10 bg-gradient-to-br from-indigo-50/40 via-white to-blue-50/40 dark:from-indigo-950/20 dark:via-[#0a0a0a] dark:to-blue-900/10"></div>
      
      {/* ================= LEFT PANEL (PROJECT LIST) ================= */}
      <div className="w-[380px] flex-shrink-0 flex flex-col border-r border-border bg-white/60 dark:bg-background/60 backdrop-blur-xl z-10 shadow-sm">
        <div className="px-5 py-4 border-b border-border">
          <div className="flex justify-between items-center mb-4">
            <h1 className="text-lg font-black text-foreground tracking-tight">Dự án (Projects)</h1>
          </div>
          <div className="relative mb-3">
            <Search className="w-4 h-4 absolute left-3 top-2.5 text-muted-foreground" />
            <Input placeholder="Tìm kiếm project..." className="pl-9 h-9" />
          </div>
        </div>

        <div className="flex-1 overflow-y-auto custom-scrollbar p-3 space-y-2">
          
          {/* Pinned "Master Plan" Item */}
          <Card
            onClick={() => handleProjectSelect(null)} 
            className={`p-3 rounded-2xl cursor-pointer transition-all flex items-center mb-4 shadow-sm border-2 ${activeProject === null ? 'bg-indigo-50/50 dark:bg-indigo-900/20 border-indigo-200 dark:border-indigo-800' : 'bg-transparent border-transparent hover:border-border'}`}
          >
            <div className={`w-10 h-10 rounded-xl flex items-center justify-center mr-3 shadow-sm ${activeProject === null ? 'bg-primary text-primary-foreground' : 'bg-muted text-muted-foreground'}`}>
              <LayoutDashboard className="w-5 h-5" />
            </div>
            <div>
              <h3 className={`font-black text-sm ${activeProject === null ? 'text-primary dark:text-blue-400' : 'text-foreground'}`}>Master Plan</h3>
              <p className="text-xs text-muted-foreground font-medium">Kế hoạch phát triển chung</p>
            </div>
          </Card>

          <div className="px-2 pb-1 text-xs font-bold text-gray-400 uppercase">Active Projects</div>

          {/* Project List */}
          {MOCK_PROJECTS.filter(p => p.status === 'Initiated').map(project => {
            const isSelected = activeProject?.id === project.id;
            
            return (
              <motion.div whileHover={{ scale: 1.01 }} whileTap={{ scale: 0.99 }} key={project.id}>
                <Card 
                  onClick={() => handleProjectSelect(project)}
                  className={`p-4 rounded-2xl cursor-pointer transition-all border-2 ${
                    isSelected 
                      ? 'bg-white dark:bg-muted border-primary shadow-md' 
                      : 'bg-white/40 dark:bg-muted/40 border-transparent hover:border-border hover:bg-white/80 dark:hover:bg-muted/80'
                  }`}
                >
                  <div className="flex justify-between items-start mb-3">
                    <div className="flex items-center space-x-2">
                      <div className={`w-2 h-2 rounded-full bg-blue-500`} />
                      <h3 className="font-bold text-sm text-foreground">{project.name}</h3>
                    </div>
                  </div>
                  
                  <div className="flex items-center justify-between mt-3 text-xs text-muted-foreground font-medium">
                    <div className="flex items-center">
                      <span className="uppercase tracking-wider">{project.stage}</span>
                    </div>
                    <div className="flex items-center">
                      <span>{project.kpis.approvalSheet}% Approved</span>
                    </div>
                  </div>
                  
                  {isSelected && (
                    <div className="mt-3 pt-3 border-t border-border flex justify-between items-center">
                      <div className="flex -space-x-2 items-center">
                        {project.owner && (
                          <UserBadge name={project.owner.name} size="sm" avatarOnly className="z-10 border border-white dark:border-background" />
                        )}
                        <div className="w-6 h-6 rounded-full bg-gray-100 dark:bg-gray-800 text-gray-500 flex items-center justify-center text-[10px] font-bold border border-white dark:border-background z-0">
                          +2
                        </div>
                      </div>
                      <ChevronRight className="w-4 h-4 text-primary" />
                    </div>
                  )}
                </Card>
              </motion.div>
            );
          })}
        </div>
      </div>

      {/* ================= RIGHT PANEL (CONTENT AREA) ================= */}
      <div className="flex-1 flex flex-col min-w-0 bg-white/40 dark:bg-background/40 backdrop-blur-md relative">
        
        {activeProject === null ? (
          /* ================= MASTER PLAN VIEW ================= */
          <div className="flex-1 overflow-y-auto p-8 custom-scrollbar">
            <div className="flex justify-between items-end mb-8">
              <div>
                <h1 className="text-3xl font-black tracking-tight text-foreground">Kế Hoạch Tổng Thể (Master Plan)</h1>
                <p className="text-muted-foreground mt-2 font-medium">Theo dõi kế hoạch từ R&D và khởi tạo dự án mới cho IQC.</p>
              </div>
              <Button nativeButton={false} render={<Link href="/new-model/import" />} className="rounded-xl shadow-lg shadow-primary/20 bg-primary hover:bg-primary/90 text-white font-bold h-11 px-6">
                <Download className="w-4 h-4 mr-2" /> Import Master Plan
              </Button>
            </div>

            <Card className="rounded-[2rem] border-0 shadow-xl shadow-gray-200/50 dark:shadow-none bg-white dark:bg-[#121826] overflow-hidden">
              <div className="overflow-x-auto">
                <table className="w-full text-sm text-left">
                  <thead className="bg-gray-50 dark:bg-white/5 border-b border-gray-100 dark:border-white/10">
                    <tr>
                      <th className="px-6 py-4 font-semibold text-gray-500 dark:text-gray-400">Project Name</th>
                      <th className="px-6 py-4 font-semibold text-gray-500 dark:text-gray-400">Basic</th>
                      <th className="px-6 py-4 font-semibold text-gray-500 dark:text-gray-400">Area</th>
                      <th className="px-6 py-4 font-semibold text-gray-500 dark:text-gray-400">Grade</th>
                      <th className="px-6 py-4 font-semibold text-gray-500 dark:text-gray-400">SKU</th>
                      <th className="px-6 py-4 font-semibold text-gray-500 dark:text-gray-400">Q&apos;ty (LPR)</th>
                      <th className="px-6 py-4 font-semibold text-gray-500 dark:text-gray-400">Q&apos;ty (LSR)</th>
                      <th className="px-6 py-4 font-semibold text-gray-500 dark:text-gray-400">PVR Target</th>
                      <th className="px-6 py-4 font-semibold text-gray-500 dark:text-gray-400">PRA Target</th>
                      <th className="px-6 py-4 font-semibold text-gray-500 dark:text-gray-400">SRA Target</th>
                      <th className="px-6 py-4 font-semibold text-gray-500 dark:text-gray-400">IQC PIC</th>
                      <th className="px-6 py-4 font-semibold text-gray-500 dark:text-gray-400">Status</th>
                      <th className="px-6 py-4 font-semibold text-gray-500 dark:text-gray-400">Action</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-gray-100 dark:divide-white/10">
                    {isLoadingMasterPlans ? (
                      <tr>
                        <td colSpan={13} className="px-6 py-8 text-center text-gray-500">
                          <div className="flex items-center justify-center">
                            <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-primary"></div>
                          </div>
                        </td>
                      </tr>
                    ) : masterPlans.length === 0 ? (
                      <tr>
                        <td colSpan={13} className="px-6 py-12 text-center text-gray-500">
                          Chưa có dữ liệu Master Plan. Vui lòng Import.
                        </td>
                      </tr>
                    ) : masterPlans.map((mp) => (
                      <tr key={mp.id} className="hover:bg-gray-50/50 dark:hover:bg-white/5 transition-colors">
                        <td className="px-6 py-4 font-bold text-gray-900 dark:text-white">{mp.projectName}</td>
                        <td className="px-6 py-4 text-gray-500 dark:text-gray-400">{mp.basic}</td>
                        <td className="px-6 py-4 text-gray-500 dark:text-gray-400">{mp.area}</td>
                        <td className="px-6 py-4 text-gray-500 dark:text-gray-400">{mp.grade}</td>
                        <td className="px-6 py-4 text-gray-500 dark:text-gray-400">{mp.sku}</td>
                        <td className="px-6 py-4 text-gray-500 dark:text-gray-400">{mp.qtyLpr}</td>
                        <td className="px-6 py-4 text-gray-500 dark:text-gray-400">{mp.qtyLsr}</td>
                        <td className="px-6 py-4 text-gray-500 dark:text-gray-400">{mp.pvrTargetDate ? new Date(mp.pvrTargetDate).toLocaleDateString('vi-VN') : '-'}</td>
                        <td className="px-6 py-4 text-gray-500 dark:text-gray-400">{mp.praTargetDate ? new Date(mp.praTargetDate).toLocaleDateString('vi-VN') : '-'}</td>
                        <td className="px-6 py-4 text-gray-500 dark:text-gray-400">{mp.sraTargetDate ? new Date(mp.sraTargetDate).toLocaleDateString('vi-VN') : '-'}</td>
                        <td className="px-6 py-4">
                          <UserBadge name={mp.hwPic} size="sm" />
                        </td>
                        <td className="px-6 py-4">
                          {mp.displayStatus === 'Created' && (
                            <Badge className="bg-emerald-100 text-emerald-700 dark:bg-emerald-900/30 dark:text-emerald-400 hover:bg-emerald-100 border-0 rounded-full px-3 py-1 whitespace-nowrap">
                              Created
                            </Badge>
                          )}
                          {mp.displayStatus === 'Urgent' && (
                            <Badge className="bg-rose-100 text-rose-700 dark:bg-rose-900/30 dark:text-rose-400 hover:bg-rose-100 border-0 rounded-full px-3 py-1 whitespace-nowrap">
                              Urgent
                            </Badge>
                          )}
                          {mp.displayStatus === 'Ready' && (
                            <Badge className="bg-amber-100 text-amber-700 dark:bg-amber-900/30 dark:text-amber-400 hover:bg-amber-100 border-0 rounded-full px-3 py-1 whitespace-nowrap">
                              Ready
                            </Badge>
                          )}
                          {mp.displayStatus === 'Future' && (
                            <Badge className="bg-blue-50 text-blue-700 dark:bg-blue-900/30 dark:text-blue-400 hover:bg-blue-50 border-0 rounded-full px-3 py-1 whitespace-nowrap">
                              Future
                            </Badge>
                          )}
                          {mp.displayStatus === 'Review Required' && (
                            <Badge className="bg-gray-100 text-gray-700 dark:bg-gray-800 dark:text-gray-400 hover:bg-gray-100 border-0 rounded-full px-3 py-1 whitespace-nowrap">
                              Review Required
                            </Badge>
                          )}
                        </td>
                        <td className="px-6 py-4 whitespace-nowrap">
                          {mp.displayAction === 'View Project' && (
                            <Button variant="ghost" className="text-primary hover:text-primary hover:bg-primary/10 font-semibold h-8 px-3">
                              View Project <ArrowRight className="w-3 h-3 ml-1" />
                            </Button>
                          )}
                          {mp.displayAction === 'Create Project' && (
                            <Button className="bg-indigo-600 hover:bg-indigo-700 text-white shadow-md font-semibold h-8 px-3">
                              <Plus className="w-3 h-3 mr-1" /> Create Project
                            </Button>
                          )}
                          {mp.displayAction === 'Fix Data' && (
                            <Button variant="outline" className="border-rose-200 text-rose-600 hover:bg-rose-50 hover:text-rose-700 font-semibold h-8 px-3">
                              Fix Data
                            </Button>
                          )}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </Card>
          </div>
        ) : (
          /* ================= PROJECT WORKSPACE VIEW ================= */
          <div className="flex-1 overflow-y-auto p-6 lg:p-8 custom-scrollbar">
            
            {/* Header */}
            <div className="flex flex-col sm:flex-row sm:justify-between sm:items-end mb-8 gap-4">
              <div>
                <div className="flex items-center space-x-2 text-sm font-semibold text-primary mb-2 tracking-wide uppercase">
                  <span>Dự án</span>
                  <ChevronRight className="w-4 h-4 text-gray-300" />
                  <span>{activeProject.baseModel} (Base)</span>
                </div>
                <h1 className="text-3xl font-black tracking-tight text-foreground">{activeProject.name}</h1>
              </div>
            </div>

            {/* KPIs */}
            <div className="grid grid-cols-2 lg:grid-cols-5 gap-4 mb-8">
              <Card className="rounded-2xl border-0 shadow-sm bg-white dark:bg-muted p-5 relative overflow-hidden group">
                <div className="flex justify-between items-start mb-2">
                  <span className="text-xs font-bold text-gray-500 uppercase tracking-wider">Approval Sheet</span>
                  <div className="w-8 h-8 rounded-full bg-blue-50 dark:bg-blue-900/20 flex items-center justify-center text-blue-600">
                    <CheckCircle2 className="w-4 h-4" />
                  </div>
                </div>
                <div className="flex items-end space-x-2">
                  <span className="text-3xl font-black text-gray-900 dark:text-white">{activeProject.kpis.approvalSheet}%</span>
                </div>
                <div className="w-full bg-gray-100 dark:bg-gray-800 h-1.5 rounded-full mt-3 overflow-hidden">
                  <div className="bg-blue-600 h-full rounded-full transition-all duration-1000" style={{ width: `${activeProject.kpis.approvalSheet}%` }}></div>
                </div>
              </Card>

              <Card className="rounded-2xl border-0 shadow-sm bg-white dark:bg-muted p-5 relative overflow-hidden group">
                <div className="flex justify-between items-start mb-2">
                  <span className="text-xs font-bold text-gray-500 uppercase tracking-wider">ISS Coverage</span>
                  <div className="w-8 h-8 rounded-full bg-indigo-50 dark:bg-indigo-900/20 flex items-center justify-center text-indigo-600">
                    <FileText className="w-4 h-4" />
                  </div>
                </div>
                <div className="flex items-end space-x-2">
                  <span className="text-3xl font-black text-gray-900 dark:text-white">{activeProject.kpis.issCoverage}%</span>
                </div>
                <div className="w-full bg-gray-100 dark:bg-gray-800 h-1.5 rounded-full mt-3 overflow-hidden">
                  <div className="bg-indigo-600 h-full rounded-full transition-all duration-1000" style={{ width: `${activeProject.kpis.issCoverage}%` }}></div>
                </div>
              </Card>

              <Card className="rounded-2xl border-0 shadow-sm bg-white dark:bg-muted p-5 relative overflow-hidden group">
                <div className="flex justify-between items-start mb-2">
                  <span className="text-xs font-bold text-gray-500 uppercase tracking-wider">Master Sample</span>
                  <div className="w-8 h-8 rounded-full bg-emerald-50 dark:bg-emerald-900/20 flex items-center justify-center text-emerald-600">
                    <Target className="w-4 h-4" />
                  </div>
                </div>
                <div className="flex items-end space-x-2">
                  <span className="text-3xl font-black text-gray-900 dark:text-white">{activeProject.kpis.sampleStatus}%</span>
                </div>
                <div className="w-full bg-gray-100 dark:bg-gray-800 h-1.5 rounded-full mt-3 overflow-hidden">
                  <div className="bg-emerald-600 h-full rounded-full transition-all duration-1000" style={{ width: `${activeProject.kpis.sampleStatus}%` }}></div>
                </div>
              </Card>

              <Card className="rounded-2xl border-0 shadow-sm bg-white dark:bg-muted p-5 relative overflow-hidden group">
                <div className="flex justify-between items-start mb-2">
                  <span className="text-xs font-bold text-gray-500 uppercase tracking-wider">Class A Risk</span>
                  <div className="w-8 h-8 rounded-full bg-rose-50 dark:bg-rose-900/20 flex items-center justify-center text-rose-600">
                    <AlertTriangle className="w-4 h-4" />
                  </div>
                </div>
                <div className="flex items-end space-x-2">
                  <span className="text-3xl font-black text-rose-600">{activeProject.kpis.classARisk}</span>
                  <span className="text-sm font-semibold text-gray-500 mb-1">Items</span>
                </div>
              </Card>
              
              <Card className="rounded-2xl border-0 shadow-sm bg-white dark:bg-muted p-5 relative overflow-hidden group">
                <div className="flex justify-between items-start mb-2">
                  <span className="text-xs font-bold text-gray-500 uppercase tracking-wider">JIG / Equip</span>
                  <div className="w-8 h-8 rounded-full bg-orange-50 dark:bg-orange-900/20 flex items-center justify-center text-orange-600">
                    <Wrench className="w-4 h-4" />
                  </div>
                </div>
                <div className="flex items-end space-x-2">
                  <span className={`text-xl font-black ${activeProject.kpis.jigReady === 'Pending' ? 'text-orange-600' : 'text-emerald-600'} mt-2`}>
                    {activeProject.kpis.jigReady}
                  </span>
                </div>
              </Card>
            </div>

            {/* Stepper */}
            <div className="mb-8 overflow-hidden bg-white dark:bg-muted rounded-[2rem] border border-gray-100 dark:border-white/5 shadow-sm p-2 flex flex-col md:flex-row relative">
              {STAGES.map((stage, idx) => {
                const isActive = activeProject.stage === stage.id;
                const isPast = STAGES.findIndex(s => s.id === activeProject.stage) > idx;
                
                return (
                  <div 
                    key={stage.id} 
                    className={`flex-1 relative p-4 rounded-[1.5rem] transition-all duration-500 ${
                      isActive 
                        ? "bg-blue-50 dark:bg-blue-900/20 shadow-inner" 
                        : isPast 
                          ? "opacity-60" 
                          : "opacity-40"
                    }`}
                  >
                    <div className="flex items-center space-x-3 mb-2">
                      <div className={`w-8 h-8 rounded-full flex items-center justify-center flex-shrink-0 transition-colors ${
                        isActive 
                          ? "bg-blue-600 text-white shadow-md shadow-blue-500/30" 
                          : isPast 
                            ? "bg-emerald-500 text-white" 
                            : "bg-gray-200 dark:bg-gray-800 text-gray-400"
                      }`}>
                        {isPast ? <Check className="w-4 h-4" /> : <span className="text-xs font-bold">{idx + 1}</span>}
                      </div>
                      <h4 className={`font-bold text-sm ${isActive ? "text-blue-900 dark:text-blue-100" : "text-gray-900 dark:text-gray-100"}`}>
                        {stage.name}
                      </h4>
                    </div>
                  </div>
                );
              })}
            </div>

            {/* Tabs */}
            <div className="flex space-x-2 mb-6 overflow-x-auto custom-scrollbar pb-1">
              {[
                { id: "overview", label: "Overview", icon: LayoutDashboard },
                { id: "risk", label: "Risk & DFx", icon: AlertCircle },
                { id: "bom", label: "BOM & Part Tracking", icon: FileText },
                { id: "mppr", label: "MPPR & PR Lot", icon: History },
              ].map(t => (
                <button
                  key={t.id}
                  onClick={() => setActiveTab(t.id)}
                  className={`flex items-center px-5 py-3 rounded-xl font-bold text-sm transition-all whitespace-nowrap ${
                    activeTab === t.id 
                      ? "bg-primary text-white shadow-md shadow-primary/20" 
                      : "bg-white dark:bg-muted text-gray-500 hover:text-gray-900 hover:bg-gray-50 dark:hover:bg-white/5"
                  }`}
                >
                  <t.icon className={`w-4 h-4 mr-2 ${activeTab === t.id ? "opacity-100" : "opacity-50"}`} />
                  {t.label}
                </button>
              ))}
            </div>

            {/* Tab Contents */}
            <div className="flex-1 bg-white dark:bg-muted rounded-[2rem] border border-gray-100 dark:border-white/5 shadow-sm overflow-hidden p-6">
              
              {activeTab === 'bom' && (
                <div className="animate-in fade-in slide-in-from-bottom-4 duration-500">
                  <div className="flex justify-between items-center mb-6">
                    <div>
                      <h3 className="text-lg font-black text-gray-900 dark:text-white">BOM & Part Tracking</h3>
                      <p className="text-sm text-gray-500">Quản lý trạng thái Approval Sheet, ISS, Sample cho từng linh kiện.</p>
                    </div>
                    <div className="flex space-x-3">
                      <Button variant="outline" className="rounded-xl border-gray-200 shadow-sm font-semibold h-10">
                        <Filter className="w-4 h-4 mr-2" /> Filter (Class A)
                      </Button>
                      <Button className="rounded-xl shadow-md bg-indigo-600 hover:bg-indigo-700 text-white font-bold h-10">
                        <Upload className="w-4 h-4 mr-2" /> Upload BOM (Excel)
                      </Button>
                    </div>
                  </div>

                  {/* Data Table */}
                  <div className="overflow-x-auto rounded-xl border border-gray-200 dark:border-white/10">
                    <table className="w-full text-sm text-left whitespace-nowrap">
                      <thead className="bg-gray-50 dark:bg-white/5 border-b border-gray-200 dark:border-white/10">
                        <tr>
                          <th className="px-4 py-3 font-semibold text-gray-500 dark:text-gray-400">Part Code</th>
                          <th className="px-4 py-3 font-semibold text-gray-500 dark:text-gray-400">Vendor</th>
                          <th className="px-4 py-3 font-semibold text-gray-500 dark:text-gray-400 text-center">Class A</th>
                          <th className="px-4 py-3 font-semibold text-gray-500 dark:text-gray-400">PIC</th>
                          <th className="px-4 py-3 font-semibold text-gray-500 dark:text-gray-400 text-center">Approval Sheet</th>
                          <th className="px-4 py-3 font-semibold text-gray-500 dark:text-gray-400 text-center">ISS</th>
                          <th className="px-4 py-3 font-semibold text-gray-500 dark:text-gray-400 text-center">Master Sample</th>
                          <th className="px-4 py-3 font-semibold text-gray-500 dark:text-gray-400 text-center">JIG</th>
                        </tr>
                      </thead>
                      <tbody className="divide-y divide-gray-100 dark:divide-white/10">
                        {[
                          { code: "GH96-12345A", vendor: "Samsung Electro-Mechanics", classA: true, pic: "Hải Nam", app: "Approved", iss: "Yes", sample: "Yes", jig: "Done" },
                          { code: "GH98-98765B", vendor: "Partron", classA: true, pic: "Anh Thư", app: "Pending", iss: "No", sample: "Pending", jig: "Pending" },
                          { code: "GH81-45678C", vendor: "MCNEX", classA: false, pic: "Minh Hải", app: "Approved", iss: "Yes", sample: "Yes", jig: "N/A" },
                        ].map((part, i) => (
                          <tr key={i} className="hover:bg-gray-50/50 dark:hover:bg-white/5">
                            <td className="px-4 py-3 font-bold text-gray-900 dark:text-white">{part.code}</td>
                            <td className="px-4 py-3 text-gray-600 dark:text-gray-300">{part.vendor}</td>
                            <td className="px-4 py-3 text-center">
                              {part.classA && <Badge className="bg-rose-100 text-rose-700 border-0">Class A</Badge>}
                            </td>
                            <td className="px-4 py-3">
                              <UserBadge name={part.pic} size="sm" />
                            </td>
                            <td className="px-4 py-3 text-center">
                              {part.app === 'Approved' ? <span className="text-emerald-600 font-bold">Approved</span> : <span className="text-orange-500 font-bold">Pending</span>}
                            </td>
                            <td className="px-4 py-3 text-center">
                              {part.iss === 'Yes' ? <CheckCircle2 className="w-5 h-5 text-emerald-500 mx-auto" /> : <div className="w-5 h-5 rounded-full border-2 border-gray-300 mx-auto" />}
                            </td>
                            <td className="px-4 py-3 text-center">
                              {part.sample === 'Yes' ? <CheckCircle2 className="w-5 h-5 text-emerald-500 mx-auto" /> : <span className="text-orange-500 font-semibold">Pending</span>}
                            </td>
                            <td className="px-4 py-3 text-center">
                              {part.jig === 'Done' ? <span className="text-emerald-600 font-bold">Done</span> : part.jig === 'N/A' ? <span className="text-gray-400">N/A</span> : <span className="text-orange-500 font-bold">Order</span>}
                            </td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                </div>
              )}

              {activeTab === 'risk' && (
                <div className="animate-in fade-in slide-in-from-bottom-4 duration-500">
                  {hasRiskData ? (
                    <>
                      <div className="flex justify-between items-center mb-6">
                        <div>
                          <h3 className="text-lg font-black text-gray-900 dark:text-white">Risk & DFx Tracking</h3>
                          <p className="text-sm text-gray-500">Theo dõi các hạng mục rủi ro từ Base Model và phản hồi khắc phục.</p>
                        </div>
                        <div className="flex space-x-3">
                          <Button variant="outline" className="rounded-xl border-gray-200 shadow-sm font-semibold h-10">
                            <Upload className="w-4 h-4 mr-2" /> Upload bổ sung
                          </Button>
                          <Button className="rounded-xl shadow-md bg-indigo-600 hover:bg-indigo-700 text-white font-bold h-10">
                            <Plus className="w-4 h-4 mr-2" /> Thêm từ Thư viện
                          </Button>
                        </div>
                      </div>

                      {/* Data Table */}
                      <div className="overflow-x-auto rounded-xl border border-gray-200 dark:border-white/10">
                        <table className="w-full text-sm text-left">
                          <thead className="bg-gray-50 dark:bg-white/5 border-b border-gray-200 dark:border-white/10">
                            <tr>
                              <th className="px-4 py-3 font-semibold text-gray-500 dark:text-gray-400">Part Code</th>
                              <th className="px-4 py-3 font-semibold text-gray-500 dark:text-gray-400">Mô tả Rủi ro / Lỗi</th>
                              <th className="px-4 py-3 font-semibold text-gray-500 dark:text-gray-400 text-center">Stage</th>
                              <th className="px-4 py-3 font-semibold text-gray-500 dark:text-gray-400">Biện pháp khắc phục (Countermeasure)</th>
                              <th className="px-4 py-3 font-semibold text-gray-500 dark:text-gray-400 text-center">Trạng thái</th>
                              <th className="px-4 py-3 font-semibold text-gray-500 dark:text-gray-400">PIC</th>
                            </tr>
                          </thead>
                          <tbody className="divide-y divide-gray-100 dark:divide-white/10">
                            {MOCK_RISK_ITEMS.map((item, i) => (
                              <tr key={i} className="hover:bg-gray-50/50 dark:hover:bg-white/5">
                                <td className="px-4 py-4 font-bold text-gray-900 dark:text-white align-top">{item.partCode}</td>
                                <td className="px-4 py-4 text-gray-600 dark:text-gray-300 max-w-xs whitespace-normal align-top leading-relaxed">{item.desc}</td>
                                <td className="px-4 py-4 text-center align-top">
                                  <Badge className="bg-blue-50 text-blue-700 border-blue-200 dark:bg-blue-900/30 dark:text-blue-400">{item.stage}</Badge>
                                </td>
                                <td className="px-4 py-4 text-gray-600 dark:text-gray-300 max-w-sm whitespace-normal align-top leading-relaxed">{item.countermeasure}</td>
                                <td className="px-4 py-4 text-center align-top">
                                  {item.status === 'Open' ? (
                                    <Badge className="bg-orange-100 text-orange-700 border-0 dark:bg-orange-900/30 dark:text-orange-400">Open</Badge>
                                  ) : (
                                    <Badge className="bg-emerald-100 text-emerald-700 border-0 dark:bg-emerald-900/30 dark:text-emerald-400">Closed</Badge>
                                  )}
                                </td>
                                <td className="px-4 py-4 align-top">
                                  <UserBadge name={item.pic} size="sm" />
                                </td>
                              </tr>
                            ))}
                          </tbody>
                        </table>
                      </div>
                    </>
                  ) : (
                    <div className="flex flex-col items-center justify-center py-12 text-center bg-white dark:bg-muted border border-dashed border-gray-300 dark:border-white/10 rounded-2xl">
                      <div className="w-20 h-20 bg-orange-50 dark:bg-orange-900/20 text-orange-500 rounded-full flex items-center justify-center mb-6 shadow-sm border border-orange-100 dark:border-orange-500/30">
                        <AlertTriangle className="w-10 h-10" />
                      </div>
                      <h3 className="text-2xl font-black text-gray-900 dark:text-white mb-3">Risk & DFx Tracking</h3>
                      <p className="text-gray-500 max-w-lg mb-8 leading-relaxed">
                        Chưa có dữ liệu rủi ro nào được khởi tạo cho dự án này. Vui lòng tải lên từ file Excel (Base Model) hoặc chọn các lỗi điển hình từ Thư viện.
                      </p>
                      
                      <div className="flex flex-col sm:flex-row gap-4">
                        <Button 
                          onClick={() => setHasRiskData(true)}
                          className="rounded-xl shadow-lg shadow-indigo-600/20 bg-indigo-600 hover:bg-indigo-700 text-white font-bold h-12 px-8 flex flex-col justify-center gap-1"
                        >
                          <div className="flex items-center"><Upload className="w-5 h-5 mr-2" /> Tải lên từ Excel</div>
                        </Button>
                        <Button 
                          onClick={() => setHasRiskData(true)}
                          variant="outline" 
                          className="rounded-xl border-gray-200 dark:border-white/10 shadow-sm font-bold h-12 px-8 text-gray-700 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-white/5"
                        >
                          <LayoutDashboard className="w-5 h-5 mr-2 text-gray-400" /> Chọn từ Thư viện
                        </Button>
                      </div>
                      <a href="#" className="mt-6 text-sm text-indigo-500 hover:underline font-semibold flex items-center">
                        <Download className="w-4 h-4 mr-1" /> Tải Template Excel mẫu
                      </a>
                    </div>
                  )}
                </div>
              )}

              {activeTab === 'mppr' && (
                <div className="animate-in fade-in slide-in-from-bottom-4 duration-500 flex flex-col items-center justify-center h-64 text-center">
                  <div className="w-16 h-16 bg-blue-50 dark:bg-blue-900/20 text-blue-500 rounded-full flex items-center justify-center mb-4">
                    <History className="w-8 h-8" />
                  </div>
                  <h3 className="text-xl font-bold text-gray-900 dark:text-white mb-2">MPPR & PR Lot Tracking</h3>
                  <p className="text-gray-500 max-w-md">Khu vực báo cáo kết quả và theo dõi tiến độ lô hàng PR thực tế.</p>
                </div>
              )}

              {activeTab === 'overview' && (
                <div className="animate-in fade-in slide-in-from-bottom-4 duration-500 flex flex-col items-center justify-center h-64 text-center">
                  <div className="w-16 h-16 bg-indigo-50 dark:bg-indigo-900/20 text-indigo-500 rounded-full flex items-center justify-center mb-4">
                    <LayoutDashboard className="w-8 h-8" />
                  </div>
                  <h3 className="text-xl font-bold text-gray-900 dark:text-white mb-2">Tổng quan Dự án</h3>
                  <p className="text-gray-500 max-w-md">Hiển thị timeline chung và gán PIC cho các cụm linh kiện.</p>
                </div>
              )}

            </div>
          </div>
        )}
      </div>
    </div>
  );
}
