"use client";

import React, { useMemo, useState } from "react";
import { MasterPlanRecord, ProjectWorkspace, mockMasterPlanData, mockActiveWorkspaces } from "@/lib/mock-data/newModelsMock";
import { getUserById } from "@/lib/data/usersService";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { 
  Search, LayoutGrid, ArrowRight, AlertTriangle, CheckCircle2,
  MoreHorizontal, CalendarDays, BarChart3, Activity, Pin, FilePlus, X
} from "lucide-react";
// Removed Select import because it doesn't exist
import { UserBadge } from "@/components/ui/user-badge";
import { Card, CardContent } from "@/components/ui/card";

export default function EnterpriseMasterPlanPage() {
  const [records, setRecords] = useState<MasterPlanRecord[]>(mockMasterPlanData);
  const [activeProjects, setActiveProjects] = useState<ProjectWorkspace[]>(mockActiveWorkspaces);
  
  // UI States
  const [searchQuery, setSearchQuery] = useState("");
  const [filterGrade] = useState("All");
  const [filterCat] = useState("All");
  const [filterStatus, setFilterStatus] = useState("All");
  const [filterUrgentOnly, setFilterUrgentOnly] = useState(false);
  const [selectedRecord, setSelectedRecord] = useState<MasterPlanRecord | null>(null);

  // Derived KPIs
  const kpis = useMemo(() => {
    const today = new Date();
    const currentMonth = today.getMonth();
    const currentYear = today.getFullYear();
    
    let urgent = 0, ready = 0, future = 0, active = 0, thisMonth = 0;
    
    records.forEach(r => {
      if (r.actionStatus === 'Urgent') urgent++;
      else if (r.actionStatus === 'Ready') ready++;
      else if (r.actionStatus === 'Future') future++;
      else if (r.actionStatus === 'Active') active++;

      if (r.prePvrTargetDate) {
        const d = new Date(r.prePvrTargetDate);
        if (d.getMonth() === currentMonth && d.getFullYear() === currentYear) {
          thisMonth++;
        }
      }
    });

    return { total: records.length, urgent, ready, future, active, thisMonth };
  }, [records]);

  // Derived Filtered & Sorted
  const processedRecords = useMemo(() => {
    const filtered = records.filter(r => {
      const matchSearch = r.projectName.toLowerCase().includes(searchQuery.toLowerCase()) || 
                          r.basic.toLowerCase().includes(searchQuery.toLowerCase());
      const matchGrade = filterGrade === "All" || r.grade === filterGrade;
      const matchCat = filterCat === "All" || r.category === filterCat;
      const matchStatus = filterStatus === "All" || r.actionStatus === filterStatus;
      const matchUrgent = !filterUrgentOnly || r.actionStatus === 'Urgent';
      
      return matchSearch && matchGrade && matchCat && matchStatus && matchUrgent;
    });

    const priorityMap: Record<string, number> = { 'Urgent': 1, 'Ready': 2, 'Active': 3, 'Future': 4 };
    filtered.sort((a, b) => {
      const pA = priorityMap[a.actionStatus] || 99;
      const pB = priorityMap[b.actionStatus] || 99;
      if (pA !== pB) return pA - pB;
      if (a.prePvrTargetDate && b.prePvrTargetDate) {
         return new Date(a.prePvrTargetDate).getTime() - new Date(b.prePvrTargetDate).getTime();
      }
      return 0;
    });

    return filtered;
  }, [records, searchQuery, filterGrade, filterCat, filterStatus, filterUrgentOnly]);

  const getRelativeDateLabel = (dateStr: string | null) => {
    if (!dateStr) return null;
    const pvr = new Date(dateStr);
    const today = new Date();
    // Reset times to compare dates properly
    pvr.setHours(0,0,0,0); today.setHours(0,0,0,0);
    const diffTime = pvr.getTime() - today.getTime();
    const diffDays = Math.ceil(diffTime / (1000 * 60 * 60 * 24));
    
    if (diffDays < 0) return { label: `Overdue ${Math.abs(diffDays)}d`, class: 'text-red-500 font-semibold bg-red-500/10 px-1.5 py-0.5 rounded text-[10px] uppercase tracking-wider ml-2' };
    if (diffDays <= 7) return { label: 'This Wk', class: 'text-amber-500 font-semibold bg-amber-500/10 px-1.5 py-0.5 rounded text-[10px] uppercase tracking-wider ml-2' };
    if (diffDays <= 30) return { label: 'Next Mo', class: 'text-blue-500 bg-blue-500/10 px-1.5 py-0.5 rounded text-[10px] uppercase tracking-wider ml-2' };
    return { label: `In ${Math.floor(diffDays/30)}mo`, class: 'text-muted-foreground text-[10px] ml-2' };
  };

  const getStatusDot = (status: string) => {
    switch (status) {
      case 'Active': return <span className="w-2 h-2 rounded-full bg-emerald-500 shadow-[0_0_8px_rgba(16,185,129,0.5)]"></span>;
      case 'Urgent': return <span className="w-2 h-2 rounded-full bg-red-500 shadow-[0_0_8px_rgba(239,68,68,0.5)] animate-pulse"></span>;
      case 'Ready': return <span className="w-2 h-2 rounded-full bg-amber-500 shadow-[0_0_8px_rgba(245,158,11,0.5)]"></span>;
      default: return <span className="w-2 h-2 rounded-full bg-slate-400"></span>;
    }
  };

  const handleCreateProject = (record: MasterPlanRecord) => {
    const newWorkspace: ProjectWorkspace = {
      id: Date.now(),
      sourceRecordId: record.id,
      projectName: record.projectName,
      sku: record.sku,
      ownerId: record.picIqc,
      ownerName: getUserById(record.picIqc)?.name ?? record.picIqc,
      status: 'Preparation',
      activatedDate: new Date().toISOString().split('T')[0],
      completionPercentage: 0,
    };
    setActiveProjects([newWorkspace, ...activeProjects]);
    const newRecords = records.map(r => 
      r.id === record.id ? { ...r, isActivated: true, actionStatus: 'Active' as const } : r
    );
    setRecords(newRecords);
    setSelectedRecord(newRecords.find(r => r.id === record.id) || null);
  };

  return (
    <div className="flex h-full min-h-[calc(100vh-4rem)] bg-[#F8F9FA] dark:bg-[#0B0F17] text-foreground font-sans relative overflow-hidden">
      
      {/* Left Sidebar */}
      <div className="w-64 border-r border-border/40 p-5 flex flex-col gap-8 shrink-0 bg-white/50 dark:bg-card/30 backdrop-blur-md z-10 hidden lg:flex">
        <div className="space-y-4">
          <h2 className="text-xs font-bold text-muted-foreground uppercase tracking-widest">Workspace</h2>
          <div className="bg-primary/5 border border-primary/20 rounded-xl p-3 flex items-center gap-3 cursor-pointer shadow-sm">
            <div className="bg-primary/20 p-2 rounded-lg text-primary"><LayoutGrid className="w-4 h-4" /></div>
            <div>
              <p className="font-semibold text-sm text-primary">Master Plan</p>
              <p className="text-[10px] text-muted-foreground">R&D Intake</p>
            </div>
          </div>
        </div>

        <div className="flex-1 overflow-auto space-y-6">
          <div className="space-y-3">
            <h3 className="text-xs font-bold text-muted-foreground uppercase tracking-widest flex items-center gap-2"><Pin className="w-3 h-3"/> Pinned</h3>
            {activeProjects.slice(0, 1).map((p) => (
              <div key={p.id} className="text-sm font-medium hover:bg-muted/50 p-2 rounded-lg cursor-pointer transition-colors flex items-center justify-between">
                <span>{p.projectName}</span>
                <Badge variant="outline" className="text-[10px] px-1 h-4">{p.completionPercentage}%</Badge>
              </div>
            ))}
          </div>
          
          <div className="space-y-3">
            <h3 className="text-xs font-bold text-muted-foreground uppercase tracking-widest flex items-center gap-2"><Activity className="w-3 h-3"/> Active Projects</h3>
            {activeProjects.map((p) => (
              <div key={p.id} className="text-sm font-medium hover:bg-muted/50 p-2 rounded-lg cursor-pointer transition-colors flex flex-col gap-1">
                <span>{p.projectName}</span>
                <div className="w-full bg-muted rounded-full h-1">
                  <div className="bg-blue-500 h-1 rounded-full transition-all" style={{width: `${p.completionPercentage}%`}}></div>
                </div>
              </div>
            ))}
          </div>
        </div>
      </div>

      {/* Main Content Area */}
      <div className="flex-1 flex flex-col min-w-0">
        
        {/* Header & KPIs */}
        <div className="p-8 pb-4 shrink-0 border-b border-border/40 bg-white/30 dark:bg-card/10 backdrop-blur-md">
          <div className="flex justify-between items-center mb-8">
            <div>
              <h1 className="text-3xl font-extrabold tracking-tight mb-1">Master Plan</h1>
              <p className="text-sm text-muted-foreground font-medium">Smart project intake and R&D pipeline tracking.</p>
            </div>
            <div className="flex gap-3">
              <Button variant="outline" className="rounded-full shadow-sm border-border/50 bg-white dark:bg-card hover:bg-muted">
                <BarChart3 className="w-4 h-4 mr-2 text-muted-foreground" /> Analytics
              </Button>
            </div>
          </div>

          <div className="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-6 gap-4">
             <Card className="shadow-none border-border/50 bg-white/50 dark:bg-card/50">
               <CardContent className="p-4 flex flex-col justify-center">
                 <p className="text-xs text-muted-foreground font-semibold uppercase tracking-wider mb-1">Total Models</p>
                 <p className="text-2xl font-bold">{kpis.total}</p>
               </CardContent>
             </Card>
             <Card className="shadow-sm border-red-500/20 bg-red-500/5">
               <CardContent className="p-4 flex flex-col justify-center">
                 <p className="text-xs text-red-500 font-semibold uppercase tracking-wider mb-1 flex items-center gap-1"><AlertTriangle className="w-3 h-3"/> Urgent</p>
                 <p className="text-2xl font-bold text-red-600 dark:text-red-400">{kpis.urgent}</p>
               </CardContent>
             </Card>
             <Card className="shadow-none border-amber-500/20 bg-amber-500/5">
               <CardContent className="p-4 flex flex-col justify-center">
                 <p className="text-xs text-amber-600 dark:text-amber-400 font-semibold uppercase tracking-wider mb-1">Ready</p>
                 <p className="text-2xl font-bold text-amber-700 dark:text-amber-500">{kpis.ready}</p>
               </CardContent>
             </Card>
             <Card className="shadow-none border-border/50 bg-white/50 dark:bg-card/50">
               <CardContent className="p-4 flex flex-col justify-center">
                 <p className="text-xs text-muted-foreground font-semibold uppercase tracking-wider mb-1">Future</p>
                 <p className="text-2xl font-bold">{kpis.future}</p>
               </CardContent>
             </Card>
             <Card className="shadow-none border-emerald-500/20 bg-emerald-500/5">
               <CardContent className="p-4 flex flex-col justify-center">
                 <p className="text-xs text-emerald-600 dark:text-emerald-400 font-semibold uppercase tracking-wider mb-1">Active</p>
                 <p className="text-2xl font-bold text-emerald-700 dark:text-emerald-500">{kpis.active}</p>
               </CardContent>
             </Card>
             <Card className="shadow-none border-blue-500/20 bg-blue-500/5">
               <CardContent className="p-4 flex flex-col justify-center">
                 <p className="text-xs text-blue-600 dark:text-blue-400 font-semibold uppercase tracking-wider mb-1 flex items-center gap-1"><CalendarDays className="w-3 h-3"/> PVR This Mo.</p>
                 <p className="text-2xl font-bold text-blue-700 dark:text-blue-500">{kpis.thisMonth}</p>
               </CardContent>
             </Card>
          </div>
        </div>

        {/* Filter Bar */}
        <div className="px-8 py-4 shrink-0 flex items-center gap-3 overflow-x-auto no-scrollbar">
          <div className="relative w-64 shrink-0">
             <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-muted-foreground" />
             <Input placeholder="Search models..." className="pl-9 h-9 rounded-full bg-white dark:bg-card border-border/50 shadow-sm" value={searchQuery} onChange={(e) => setSearchQuery(e.target.value)} />
          </div>
          
          <select 
            value={filterStatus} 
            onChange={(e) => setFilterStatus(e.target.value)}
            className="h-9 w-32 rounded-full bg-white dark:bg-card shadow-sm border border-border/50 text-sm px-3 focus:outline-none focus:ring-1 focus:ring-primary cursor-pointer"
          >
            <option value="All">All Status</option>
            <option value="Urgent">Urgent</option>
            <option value="Ready">Ready</option>
            <option value="Active">Created</option>
          </select>

          <Button 
            variant={filterUrgentOnly ? "default" : "outline"} 
            size="sm" 
            className={`h-9 rounded-full shadow-sm text-sm ${filterUrgentOnly ? 'bg-red-500 hover:bg-red-600 text-white border-red-500' : 'bg-white dark:bg-card border-border/50 text-muted-foreground hover:text-foreground'}`}
            onClick={() => setFilterUrgentOnly(!filterUrgentOnly)}
          >
            <AlertTriangle className="w-3.5 h-3.5 mr-1.5" /> Urgent Only
          </Button>
        </div>

        {/* Table Area */}
        <div className="flex-1 overflow-auto px-8 pb-8">
          <div className="rounded-2xl border border-border/30 bg-white dark:bg-card/40 shadow-[0_8px_30px_rgb(0,0,0,0.04)] dark:shadow-[0_8px_30px_rgb(0,0,0,0.1)] overflow-hidden">
            <Table>
              <TableHeader>
                <TableRow className="border-border/30 hover:bg-transparent bg-muted/20">
                  <TableHead className="font-semibold h-11 text-xs uppercase tracking-wider text-muted-foreground pl-6 sticky left-0 z-10 bg-white/95 dark:bg-[#121826]/95 backdrop-blur-sm">Project Name</TableHead>
                  <TableHead className="font-semibold h-11 text-xs uppercase tracking-wider text-muted-foreground">Basic / SKU</TableHead>
                  <TableHead className="font-semibold h-11 text-xs uppercase tracking-wider text-muted-foreground">Region / Grade</TableHead>
                  <TableHead className="font-semibold h-11 text-xs uppercase tracking-wider text-muted-foreground">LPR / LSR</TableHead>
                  <TableHead className="font-semibold h-11 text-xs uppercase tracking-wider text-muted-foreground">PVR Target</TableHead>
                  <TableHead className="font-semibold h-11 text-xs uppercase tracking-wider text-muted-foreground">PRA / SRA</TableHead>
                  <TableHead className="font-semibold h-11 text-xs uppercase tracking-wider text-muted-foreground">IQC PIC</TableHead>
                  <TableHead className="font-semibold h-11 text-xs uppercase tracking-wider text-muted-foreground">Status</TableHead>
                  <TableHead className="w-16 sticky right-0 z-10 bg-white/95 dark:bg-[#121826]/95 backdrop-blur-sm"></TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {processedRecords.map((record) => {
                  const relPvr = getRelativeDateLabel(record.prePvrTargetDate);
                  const isSelected = selectedRecord?.id === record.id;
                  
                  return (
                    <TableRow 
                      key={record.id} 
                      onClick={() => setSelectedRecord(record)}
                      className={`border-border/20 transition-all cursor-pointer group ${isSelected ? 'bg-primary/5 dark:bg-primary/10' : 'hover:bg-muted/30'}`}
                    >
                      <TableCell className={`font-semibold py-4 pl-6 sticky left-0 z-10 transition-colors ${isSelected ? 'bg-indigo-50/90 dark:bg-indigo-900/20' : 'bg-white dark:bg-card/40 group-hover:bg-muted/10'}`}>
                        {record.projectName}
                      </TableCell>
                      <TableCell>
                        <div className="flex flex-col">
                          <span className="text-sm font-medium">{record.basic}</span>
                          <span className="text-xs text-muted-foreground font-mono">{record.sku}</span>
                        </div>
                      </TableCell>
                      <TableCell>
                        <div className="flex items-center gap-2">
                          <span className="text-sm">{record.areaRegion}</span>
                          <span className="w-1 h-1 rounded-full bg-border"></span>
                          <span className="text-xs font-medium text-muted-foreground">{record.grade}</span>
                        </div>
                      </TableCell>
                      <TableCell>
                        <div className="flex items-center gap-2">
                          <Badge variant="outline" className="text-[10px] h-5 px-1.5 font-medium border-border/50">{record.category}</Badge>
                          <span className="text-sm font-medium">{record.qtyLpr}</span>
                          <span className="text-xs text-muted-foreground">/ {record.qtyLsr}</span>
                        </div>
                      </TableCell>
                      <TableCell>
                        <div className="flex flex-col">
                           <span className="text-sm font-medium">{record.prePvrTargetDate}</span>
                           {relPvr && <span className={`w-fit mt-1 ${relPvr.class}`}>{relPvr.label}</span>}
                        </div>
                      </TableCell>
                      <TableCell>
                        <div className="flex flex-col gap-1 text-xs text-muted-foreground font-medium">
                          <span>{record.praTargetDate} <span className="opacity-50 mx-1">|</span> PRA</span>
                          <span>{record.sraTargetDate} <span className="opacity-50 mx-1">|</span> SRA</span>
                        </div>
                      </TableCell>
                      <TableCell>
                         <UserBadge name={record.picIqc} className="scale-90 origin-left" />
                      </TableCell>
                      <TableCell>
                        <div className="flex items-center gap-2">
                          {getStatusDot(record.actionStatus)}
                          <span className={`text-sm font-medium ${record.actionStatus === 'Urgent' ? 'text-red-600 dark:text-red-400' : ''}`}>
                            {record.actionStatus === 'Active' ? 'Created' : record.actionStatus}
                          </span>
                        </div>
                      </TableCell>
                      <TableCell className={`sticky right-0 z-10 text-right pr-4 transition-colors ${isSelected ? 'bg-indigo-50/90 dark:bg-indigo-900/20' : 'bg-white dark:bg-card/40 group-hover:bg-muted/10'}`}>
                         <Button variant="ghost" size="icon" className="opacity-0 group-hover:opacity-100 transition-opacity h-8 w-8 rounded-full text-muted-foreground hover:text-primary hover:bg-primary/10">
                           <MoreHorizontal className="w-4 h-4" />
                         </Button>
                      </TableCell>
                    </TableRow>
                  );
                })}
              </TableBody>
            </Table>
          </div>
        </div>
      </div>

      {/* Right Side Drawer Overlay & Panel */}
      {selectedRecord && (
        <>
          {/* Overlay */}
          <div 
            className="absolute inset-0 bg-background/80 backdrop-blur-sm z-40 transition-all animate-in fade-in"
            onClick={() => setSelectedRecord(null)}
          />
          {/* Drawer Panel */}
          <div className="absolute top-0 right-0 h-full w-[400px] bg-white dark:bg-[#121826] border-l border-border/50 shadow-2xl z-50 flex flex-col animate-in slide-in-from-right duration-300">
            <div className="flex justify-between items-center p-6 border-b border-border/30">
               <div>
                 <h2 className="text-xl font-bold">{selectedRecord.projectName}</h2>
                 <div className="flex items-center gap-2 mt-1">
                   {getStatusDot(selectedRecord.actionStatus)}
                   <span className="text-sm font-medium text-muted-foreground">
                     {selectedRecord.actionStatus === 'Active' ? 'Project Created' : `Status: ${selectedRecord.actionStatus}`}
                   </span>
                 </div>
               </div>
               <Button variant="ghost" size="icon" className="rounded-full" onClick={() => setSelectedRecord(null)}>
                 <X className="w-5 h-5 text-muted-foreground" />
               </Button>
            </div>
            
            <div className="flex-1 overflow-auto p-6 space-y-8">
               {/* Action Area */}
               <div className="bg-primary/5 rounded-2xl p-5 border border-primary/10">
                 {selectedRecord.isActivated ? (
                   <div className="text-center">
                     <CheckCircle2 className="w-10 h-10 text-emerald-500 mx-auto mb-3" />
                     <h3 className="font-semibold text-emerald-700 dark:text-emerald-400 mb-1">Project is Active</h3>
                     <p className="text-sm text-muted-foreground mb-4">This model has already been transitioned into an IQC Workspace.</p>
                     <Button className="w-full rounded-full gap-2">
                       Go to Workspace <ArrowRight className="w-4 h-4" />
                     </Button>
                   </div>
                 ) : (
                   <div>
                     <h3 className="font-semibold mb-2">Create IQC Project</h3>
                     <p className="text-sm text-muted-foreground mb-4">Initialize a workspace to start managing checklists, approvals, and quality tracking.</p>
                     <Button 
                       className="w-full rounded-full gap-2 bg-indigo-600 hover:bg-indigo-700 text-white shadow-lg shadow-indigo-500/20"
                       onClick={() => handleCreateProject(selectedRecord)}
                     >
                       <FilePlus className="w-4 h-4" /> Initialize Workspace
                     </Button>
                   </div>
                 )}
               </div>

               {/* Details Grid */}
               <div>
                 <h4 className="text-xs font-bold text-muted-foreground uppercase tracking-wider mb-4">Model Details</h4>
                 <div className="grid grid-cols-2 gap-y-4 gap-x-2 text-sm">
                   <div><p className="text-muted-foreground text-xs mb-1">Basic Code</p><p className="font-medium">{selectedRecord.basic}</p></div>
                   <div><p className="text-muted-foreground text-xs mb-1">SKU</p><p className="font-mono text-xs mt-0.5 font-medium">{selectedRecord.sku}</p></div>
                   <div><p className="text-muted-foreground text-xs mb-1">Area / Region</p><p className="font-medium">{selectedRecord.areaRegion}</p></div>
                   <div><p className="text-muted-foreground text-xs mb-1">Grade</p><p className="font-medium">{selectedRecord.grade}</p></div>
                 </div>
               </div>

               <div className="h-px bg-border/40"></div>

               <div>
                 <h4 className="text-xs font-bold text-muted-foreground uppercase tracking-wider mb-4">Schedule & Resources</h4>
                 <div className="space-y-4 text-sm">
                   <div className="flex justify-between items-center">
                     <span className="text-muted-foreground">PVR Target</span>
                     <span className="font-medium">{selectedRecord.prePvrTargetDate}</span>
                   </div>
                   <div className="flex justify-between items-center">
                     <span className="text-muted-foreground">PRA Target</span>
                     <span className="font-medium">{selectedRecord.praTargetDate}</span>
                   </div>
                   <div className="flex justify-between items-center">
                     <span className="text-muted-foreground">SRA Target</span>
                     <span className="font-medium">{selectedRecord.sraTargetDate}</span>
                   </div>
                   <div className="flex justify-between items-center pt-2">
                     <span className="text-muted-foreground">IQC PIC</span>
                     <UserBadge name={selectedRecord.picIqc} />
                   </div>
                 </div>
               </div>

               <div className="h-px bg-border/40"></div>

               <div>
                 <h4 className="text-xs font-bold text-muted-foreground uppercase tracking-wider mb-4">System</h4>
                 <div className="space-y-3 text-sm">
                   <div className="flex justify-between items-center">
                     <span className="text-muted-foreground">Last ERP Sync</span>
                     <span className="text-xs">Just now</span>
                   </div>
                   <div className="flex justify-between items-center">
                     <span className="text-muted-foreground">Category</span>
                     <Badge variant="secondary" className="text-[10px]">{selectedRecord.category}</Badge>
                   </div>
                 </div>
               </div>
            </div>
          </div>
        </>
      )}

    </div>
  );
}
