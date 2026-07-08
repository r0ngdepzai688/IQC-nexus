"use client";

import { motion, AnimatePresence } from "framer-motion";
import { 
  Sparkles, Target, AlertTriangle, CheckCircle2, Factory, Activity, 
  Users, ArrowRight, BookOpen, Layers, BarChart3, TrendingUp, Briefcase, Calendar, ShieldCheck
} from "lucide-react";
import Link from "next/link";

import { useAuth } from "@/lib/contexts/AuthContext";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";

export default function OverviewPage() {
  const { user, activeRoleLens } = useAuth();
  const activeRole = activeRoleLens;
  const activeScope = user.scope;

  return (
    <div className="min-h-full bg-transparent p-4 lg:p-6 pb-20 relative z-0">
      {/* Ambient Pastel Background for Light Mode */}
      <div className="absolute inset-0 overflow-hidden pointer-events-none z-[-1] dark:hidden">
        <div className="absolute top-[-10%] right-[10%] w-[600px] h-[600px] bg-blue-200/40 rounded-full blur-[100px]"></div>
        <div className="absolute top-[20%] left-[0%] w-[500px] h-[500px] bg-indigo-200/40 rounded-full blur-[100px]"></div>
        <div className="absolute bottom-[-10%] right-[20%] w-[700px] h-[700px] bg-purple-200/30 rounded-full blur-[120px]"></div>
      </div>

      {/* ================= 1. HEADER & AI BRIEFING ================= */}
      <div className="mb-6">
        <h1 className="text-3xl font-black text-gray-900 dark:text-white mb-1 tracking-tight">Enterprise Command Center</h1>
        <p className="text-gray-500 font-medium text-sm mb-4">System-wide situational awareness & operational priorities.</p>

        <Card className="relative overflow-hidden bg-white/70 dark:bg-card/40 backdrop-blur-3xl border-border shadow-xl shadow-blue-900/5 dark:shadow-none hover:border-border transition-all duration-500">
          <div className="absolute top-0 right-0 w-[500px] h-[500px] bg-blue-500/5 dark:bg-blue-500/10 rounded-full blur-3xl translate-x-1/3 -translate-y-1/3 pointer-events-none"></div>
          <div className="absolute bottom-0 left-0 w-[400px] h-[400px] bg-indigo-500/5 dark:bg-indigo-500/10 rounded-full blur-3xl -translate-x-1/3 translate-y-1/3 pointer-events-none"></div>
          
          <CardContent className="relative z-10 p-5 md:p-6 flex flex-col lg:flex-row gap-6 items-start lg:items-center justify-between">
            <div className="flex-1 min-w-[200px]">
              <div className="flex items-center space-x-2 mb-3">
                <Sparkles className="w-4 h-4 text-blue-600 dark:text-blue-300" />
                <span className="text-[10px] font-bold text-blue-700 dark:text-blue-200 uppercase tracking-widest">AI Executive Briefing • {activeRole}</span>
              </div>
              
              {/* Dynamic AI Briefing Content */}
              {activeRole === 'Staff' && (
                <>
                  <h2 className="text-xl md:text-2xl font-bold mb-4 leading-snug">
                    Good morning.<br/>You have 5 tasks to finish today.
                  </h2>
                  <p className="text-gray-600 dark:text-blue-100 font-medium max-w-2xl text-sm md:text-base opacity-90 leading-relaxed mb-6">
                    Next recommended action: Approve Jig Design for Galaxy Z Fold 8. Estimated work duration: 3.5 hours for today&apos;s queue.
                  </p>
                  <Button className="shadow-lg hover:shadow-xl transition-all">
                    Start Next Task <ArrowRight className="w-4 h-4 ml-2" />
                  </Button>
                </>
              )}

              {activeRole === 'Cell Leader' && (
                <>
                  <h2 className="text-xl md:text-2xl font-bold mb-4 leading-snug">
                    Good morning.<br/>3 Projects in {activeScope} require attention.
                  </h2>
                  <p className="text-gray-600 dark:text-blue-100 font-medium max-w-2xl text-sm md:text-base opacity-90 leading-relaxed mb-6">
                    Critical blocker identified in PVR stage. Supplier ABC has delayed readiness. 2 approvals are pending in your queue.
                  </p>
                  <Button className="shadow-lg hover:shadow-xl transition-all">
                    Resolve Blockers <ArrowRight className="w-4 h-4 ml-2" />
                  </Button>
                </>
              )}

              {activeRole === 'Part Leader' && (
                <>
                  <h2 className="text-xl md:text-2xl font-bold mb-4 leading-snug">
                    {activeScope} Department Performance:<br/>Yield improved by 1.2% this week.
                  </h2>
                  <p className="text-gray-600 dark:text-blue-100 font-medium max-w-2xl text-sm md:text-base opacity-90 leading-relaxed mb-6">
                    Resource utilization is at 94%. Project Galaxy S26 Ultra poses a high risk due to Supplier capacity. Management recommendation: Reallocate 2 engineers to NPI cell.
                  </p>
                </>
              )}

              {activeRole === 'Group Leader' && (
                <>
                  <h2 className="text-xl md:text-2xl font-bold mb-4 leading-snug">
                    Organization Health: 92/100<br/>Factory yield at 98.4%.
                  </h2>
                  <p className="text-gray-600 dark:text-blue-100 font-medium max-w-2xl text-sm md:text-base opacity-90 leading-relaxed mb-6">
                    Strategic recommendation: Supplier consolidation required for acoustic components to reduce Quality Cost by 4%. Major compliance audit upcoming in 14 days.
                  </p>
                  <Button className="shadow-lg hover:shadow-xl transition-all">
                    View Monthly Executive Report <ArrowRight className="w-4 h-4 ml-2" />
                  </Button>
                </>
              )}
            </div>

            {/* Dynamic AI KPI Badges */}
            <div className="flex gap-4">
              {activeRole === 'Group Leader' ? (
                <>
                  <Card className="bg-black/5 dark:bg-black/20 backdrop-blur-md p-4 text-center min-w-[100px] border-border shadow-none">
                    <span className="block text-3xl font-black mb-1 text-emerald-600 dark:text-emerald-400">98%</span>
                    <span className="text-[10px] uppercase tracking-wider font-bold text-gray-500 dark:text-blue-200">Factory Yield</span>
                  </Card>
                  <Card className="bg-black/5 dark:bg-black/20 backdrop-blur-md p-4 text-center min-w-[100px] border-border shadow-none">
                    <span className="block text-3xl font-black mb-1 text-amber-500 dark:text-amber-400">$1.2M</span>
                    <span className="text-[10px] uppercase tracking-wider font-bold text-gray-500 dark:text-blue-200">Quality Cost</span>
                  </Card>
                </>
              ) : (
                <>
                  <Card className="bg-black/5 dark:bg-black/20 backdrop-blur-md p-4 text-center min-w-[100px] border-border shadow-none">
                    <span className="block text-3xl font-black mb-1">2</span>
                    <span className="text-[10px] uppercase tracking-wider font-bold text-gray-500 dark:text-blue-200">Blocked</span>
                  </Card>
                  <Card className="bg-black/5 dark:bg-black/20 backdrop-blur-md p-4 text-center min-w-[100px] border-border shadow-none">
                    <span className="block text-3xl font-black mb-1 text-emerald-600 dark:text-emerald-400">94%</span>
                    <span className="text-[10px] uppercase tracking-wider font-bold text-gray-500 dark:text-blue-200">Pass Rate</span>
                  </Card>
                </>
              )}
            </div>
          </CardContent>
        </Card>
      </div>

      {/* ================= 2. MODULAR DASHBOARD GRID ================= */}
      <AnimatePresence mode="wait">
        <motion.div 
          key={`${activeRole}-${activeScope}`}
          initial={{ opacity: 0, y: 20 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ duration: 0.3 }}
          className="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-6 lg:gap-8"
        >
          
          {/* ----------------- COLUMN 1: ACTION CENTER & PERSONAL WORK ----------------- */}
          <div className="space-y-6 lg:space-y-8 flex flex-col">
            
            {/* My Action Center */}
            <Card className="flex-1 bg-gradient-to-b from-rose-50/80 to-white/60 dark:from-rose-500/10 dark:to-transparent backdrop-blur-xl border-t-[3px] border-t-rose-500 shadow-sm hover:shadow-md transition-all duration-300 relative overflow-hidden">
              <div className="absolute top-0 right-0 w-32 h-32 bg-rose-500/5 rounded-full blur-2xl -translate-y-1/2 translate-x-1/2 pointer-events-none"></div>
              <CardHeader className="pb-4 relative z-10">
                <CardTitle className="text-lg font-black flex items-center">
                  <Target className="w-5 h-5 mr-2 text-rose-600 dark:text-rose-500" /> 
                  My Action Center
                  <span className="relative flex h-3 w-3 ml-3">
                    <span className="animate-ping absolute inline-flex h-full w-full rounded-full bg-rose-400 opacity-75"></span>
                    <span className="relative inline-flex rounded-full h-3 w-3 bg-rose-500"></span>
                  </span>
                </CardTitle>
              </CardHeader>
              <CardContent className="space-y-4">
                <div className="p-4 bg-rose-50/80 dark:bg-rose-500/10 backdrop-blur-md border border-rose-200/50 dark:border-rose-500/20 rounded-xl flex items-start space-x-3 cursor-pointer hover:border-rose-300 dark:hover:border-rose-500/40 transition-all duration-300">
                  <AlertTriangle className="w-5 h-5 text-rose-600 dark:text-rose-400 mt-0.5" />
                  <div>
                    <h4 className="text-sm font-bold text-gray-900 dark:text-white">Approve Jig Design (Z Fold 8)</h4>
                    <p className="text-xs text-rose-600 dark:text-rose-400 font-medium mt-1">Overdue by 2 days</p>
                  </div>
                </div>
                <div className="p-4 bg-muted border border-border rounded-xl flex items-start space-x-3 cursor-pointer hover:border-gray-300 dark:hover:border-white/20 transition-colors">
                  <CheckCircle2 className="w-5 h-5 text-gray-400 mt-0.5" />
                  <div>
                    <h4 className="text-sm font-bold text-gray-900 dark:text-white">Review QMS Audit Report</h4>
                    <p className="text-xs text-gray-500 font-medium mt-1">Due today at 17:00</p>
                  </div>
                </div>
                {['Staff', 'Cell Leader'].includes(activeRole) && (
                  <div className="p-4 bg-muted border border-border rounded-xl flex items-start space-x-3 cursor-pointer hover:border-gray-300 dark:hover:border-white/20 transition-colors">
                    <Calendar className="w-5 h-5 text-blue-500 mt-0.5" />
                    <div>
                      <h4 className="text-sm font-bold text-gray-900 dark:text-white">My Calendar</h4>
                      <p className="text-xs text-gray-500 font-medium mt-1">2 meetings today</p>
                    </div>
                  </div>
                )}
              </CardContent>
            </Card>

            {/* Workforce/Capacity */}
            {activeRole !== 'Staff' && (
              <Card className="shadow-sm hover:shadow-md transition-all duration-300">
                <CardHeader className="pb-4">
                  <CardTitle className="text-base font-black flex items-center">
                    <Users className="w-4 h-4 mr-2 text-gray-400" /> {activeRole === 'Group Leader' ? 'Org Capacity' : 'Team Capacity'}
                  </CardTitle>
                </CardHeader>
                <CardContent className="grid grid-cols-2 gap-4">
                  <div>
                    <p className="text-xs font-bold text-gray-500 uppercase tracking-widest mb-1">Over Capacity</p>
                    <p className="text-2xl font-black text-rose-600">3 <span className="text-sm font-medium text-gray-500">Eng.</span></p>
                  </div>
                  <div>
                    <p className="text-xs font-bold text-gray-500 uppercase tracking-widest mb-1">Tasks Due</p>
                    <p className="text-2xl font-black text-gray-900 dark:text-white">45</p>
                  </div>
                </CardContent>
              </Card>
            )}
          </div>

          {/* ----------------- COLUMN 2: DOMAIN ANALYTICS ----------------- */}
          <div className="space-y-6 lg:space-y-8 flex flex-col">
            
            {activeRole === 'Staff' ? (
              <Card className="flex-1 shadow-sm hover:shadow-md transition-all duration-300">
                <CardHeader className="pb-4 flex flex-row items-center justify-between">
                  <CardTitle className="text-base font-black flex items-center">
                    <Briefcase className="w-4 h-4 mr-2 text-indigo-500" /> Assigned Projects
                  </CardTitle>
                </CardHeader>
                <CardContent className="space-y-4">
                  <div className="p-4 bg-muted rounded-xl flex justify-between items-center">
                    <div>
                      <h4 className="font-bold text-sm">Galaxy Z Fold 8</h4>
                      <p className="text-[10px] text-gray-500">Stage: PVR</p>
                    </div>
                    <CircularProgress value={45} size={30} />
                  </div>
                  <div className="p-4 bg-muted rounded-xl flex justify-between items-center">
                    <div>
                      <h4 className="font-bold text-sm">Galaxy S26 Ultra</h4>
                      <p className="text-[10px] text-gray-500">Stage: DVR</p>
                    </div>
                    <CircularProgress value={20} size={30} colorClass="text-rose-500" />
                  </div>
                  
                  <div className="mt-8">
                    <h3 className="text-sm font-black text-gray-900 dark:text-white flex items-center mb-4">
                      <Activity className="w-4 h-4 mr-2 text-gray-400" /> Recent Activities
                    </h3>
                    <div className="space-y-3">
                      <div className="text-xs text-gray-500"><span className="font-bold text-gray-800 dark:text-gray-200">You</span> approved BOM v2 <span className="text-[10px]">2h ago</span></div>
                      <div className="text-xs text-gray-500"><span className="font-bold text-gray-800 dark:text-gray-200">Minh Hai</span> replied to your comment <span className="text-[10px]">5h ago</span></div>
                    </div>
                  </div>
                </CardContent>
              </Card>
            ) : activeRole === 'Group Leader' ? (
              <Card className="flex-1 shadow-sm hover:shadow-md transition-all duration-300">
                <CardHeader className="pb-4">
                  <CardTitle className="text-base font-black flex items-center">
                    <BarChart3 className="w-4 h-4 mr-2 text-indigo-500" /> Organization KPI
                  </CardTitle>
                </CardHeader>
                <CardContent className="space-y-5">
                  <div>
                    <div className="flex justify-between items-end mb-1">
                      <span className="text-xs font-bold text-gray-500">IQC 1P</span>
                      <span className="text-xs font-bold text-emerald-600">96.5% Yield</span>
                    </div>
                    <div className="h-2 w-full bg-muted rounded-full overflow-hidden"><div className="h-full bg-emerald-500 rounded-full" style={{width: '96.5%'}}></div></div>
                  </div>
                  <div>
                    <div className="flex justify-between items-end mb-1">
                      <span className="text-xs font-bold text-gray-500">IQC 2P</span>
                      <span className="text-xs font-bold text-amber-500">92.1% Yield</span>
                    </div>
                    <div className="h-2 w-full bg-muted rounded-full overflow-hidden"><div className="h-full bg-amber-500 rounded-full" style={{width: '92.1%'}}></div></div>
                  </div>
                  <div>
                    <div className="flex justify-between items-end mb-1">
                      <span className="text-xs font-bold text-gray-500">IQC 3P</span>
                      <span className="text-xs font-bold text-emerald-600">98.2% Yield</span>
                    </div>
                    <div className="h-2 w-full bg-muted rounded-full overflow-hidden"><div className="h-full bg-emerald-500 rounded-full" style={{width: '98.2%'}}></div></div>
                  </div>
                  <div className="mt-8 grid grid-cols-2 gap-4">
                    <div className="p-4 bg-rose-50 dark:bg-rose-900/10 rounded-xl">
                      <p className="text-2xl font-black text-rose-600 dark:text-rose-400">12</p>
                      <p className="text-[10px] font-bold text-rose-900/60 dark:text-rose-300/60 uppercase tracking-wider mt-1">Major Incidents</p>
                    </div>
                    <div className="p-4 bg-indigo-50 dark:bg-indigo-900/10 rounded-xl">
                      <p className="text-2xl font-black text-indigo-700 dark:text-indigo-400">45</p>
                      <p className="text-[10px] font-bold text-indigo-900/60 dark:text-indigo-300/60 uppercase tracking-wider mt-1">Top Risks</p>
                    </div>
                  </div>
                </CardContent>
              </Card>
            ) : (
              <>
                {activeScope === 'New Models' || activeRole === 'Part Leader' ? (
                  <Card className="flex-1 shadow-sm hover:shadow-md transition-all duration-300">
                    <CardHeader className="pb-4 flex flex-row items-center justify-between">
                      <CardTitle className="text-base font-black flex items-center">
                        <Layers className="w-4 h-4 mr-2 text-indigo-500" /> NPI Lifecycle
                      </CardTitle>
                      <Link href="/new-model" className="text-xs font-bold text-primary dark:text-blue-400 hover:underline">View All</Link>
                    </CardHeader>
                    <CardContent>
                      <div className="grid grid-cols-2 gap-4 mb-6">
                        <div className="p-4 bg-indigo-50 dark:bg-indigo-900/10 rounded-xl">
                          <p className="text-2xl font-black text-indigo-700 dark:text-indigo-400">12</p>
                          <p className="text-xs font-bold text-indigo-900/60 dark:text-indigo-300/60 uppercase tracking-wider mt-1">Active Projects</p>
                        </div>
                        <div className="p-4 bg-rose-50 dark:bg-rose-900/10 rounded-xl">
                          <p className="text-2xl font-black text-rose-600 dark:text-rose-400">2</p>
                          <p className="text-xs font-bold text-rose-900/60 dark:text-rose-300/60 uppercase tracking-wider mt-1">Blocked Stages</p>
                        </div>
                      </div>
                      <div className="space-y-4 text-sm">
                        <div className="flex justify-between border-b border-border pb-2">
                          <span className="text-gray-600 font-medium">Pending Approvals</span><Badge variant="warning">8</Badge>
                        </div>
                        <div className="flex justify-between border-b border-border pb-2">
                          <span className="text-gray-600 font-medium">Critical Risks</span><Badge variant="danger">3</Badge>
                        </div>
                      </div>
                    </CardContent>
                  </Card>
                ) : null}

                {activeScope === 'Inspection' || activeRole === 'Part Leader' ? (
                  <Card className="flex-1 shadow-sm hover:shadow-md transition-all duration-300">
                    <CardHeader className="pb-4 flex flex-row items-center justify-between">
                      <CardTitle className="text-base font-black flex items-center">
                        <Activity className="w-4 h-4 mr-2 text-emerald-500" /> Quality Inspections
                      </CardTitle>
                    </CardHeader>
                    <CardContent>
                      <div className="flex items-end space-x-4 mb-6">
                        <div>
                          <p className="text-[10px] font-bold text-gray-500 uppercase tracking-widest mb-1">Pass Rate</p>
                          <div className="flex items-center space-x-2">
                            <span className="text-3xl font-black text-emerald-600 dark:text-emerald-400">94.2%</span>
                            <Badge variant="success" className="px-2 py-0.5"><TrendingUp className="w-3 h-3 mr-1" /> 1.8%</Badge>
                          </div>
                        </div>
                      </div>
                      <div className="space-y-4 text-sm">
                        <div className="flex justify-between border-b border-border pb-2">
                          <span className="text-gray-600 font-medium">Today&apos;s Lots</span><span className="font-bold">124</span>
                        </div>
                        <div className="flex justify-between border-b border-border pb-2">
                          <span className="text-gray-600 font-medium">Open NCRs</span><Badge variant="warning">12</Badge>
                        </div>
                      </div>
                    </CardContent>
                  </Card>
                ) : null}
              </>
            )}
          </div>

          {/* ----------------- COLUMN 3: SUPPLIERS & COMPLIANCE ----------------- */}
          {activeRole !== 'Staff' && (
            <div className="space-y-6 lg:space-y-8 flex flex-col xl:col-span-1 md:col-span-2">
              
              <Card className="shadow-sm hover:shadow-md transition-all duration-300 xl:flex-1 md:grid xl:block md:grid-cols-2 md:gap-6 xl:gap-0">
                <CardHeader className="col-span-2 xl:col-span-1 pb-4">
                  <CardTitle className="text-base font-black flex items-center">
                    <Factory className="w-4 h-4 mr-2 text-amber-500" /> Supplier Performance
                  </CardTitle>
                </CardHeader>
                <CardContent className="col-span-2">
                  <div className="mb-6 xl:mb-6">
                    <div className="p-4 bg-amber-50 dark:bg-amber-900/10 border border-amber-100 dark:border-amber-900/30 rounded-xl flex justify-between items-center">
                      <div>
                        <h4 className="text-sm font-bold text-amber-900 dark:text-amber-400">High Risk Suppliers</h4>
                        <p className="text-xs text-amber-700 dark:text-amber-500 font-medium mt-1">Requires immediate audit</p>
                      </div>
                      <span className="text-2xl font-black text-amber-600">3</span>
                    </div>
                  </div>
                  <div className="space-y-4 text-sm">
                    <div className="flex justify-between border-b border-border pb-2">
                      <span className="text-gray-600 font-medium">Delayed Responses</span><Badge variant="danger">8</Badge>
                    </div>
                    <div className="flex justify-between border-b border-border pb-2">
                      <span className="text-gray-600 font-medium">Incoming IQC Issues</span><span className="font-bold">14 Lots</span>
                    </div>
                  </div>
                </CardContent>
              </Card>

              <Card className="shadow-sm hover:shadow-md transition-all duration-300 xl:flex-1 md:grid xl:block md:grid-cols-2 md:gap-6 xl:gap-0">
                <CardHeader className="col-span-2 xl:col-span-1 pb-4">
                  <CardTitle className="text-base font-black flex items-center">
                    <ShieldCheck className="w-4 h-4 mr-2 text-purple-500" /> Compliance Health
                  </CardTitle>
                </CardHeader>
                <CardContent className="space-y-4 col-span-2">
                  <div className="p-3 bg-muted rounded-xl border border-border flex items-center justify-between">
                    <div className="flex items-center space-x-3">
                      <div className="p-2 bg-purple-100 text-purple-600 rounded-lg"><BookOpen className="w-4 h-4" /></div>
                      <div>
                        <h4 className="text-sm font-bold text-gray-900 dark:text-white">ISO 9001 Audit</h4>
                        <p className="text-[10px] text-gray-500 font-medium">Upcoming in 14 days</p>
                      </div>
                    </div>
                  </div>
                  <div className="p-3 bg-muted rounded-xl border border-border flex items-center justify-between">
                    <div className="flex items-center space-x-3">
                      <div className="p-2 bg-rose-100 text-rose-600 rounded-lg"><AlertTriangle className="w-4 h-4" /></div>
                      <div>
                        <h4 className="text-sm font-bold text-gray-900 dark:text-white">Expiring Certificates</h4>
                        <p className="text-[10px] text-gray-500 font-medium">2 Suppliers need renewal</p>
                      </div>
                    </div>
                  </div>
                  <div className="flex justify-between text-sm pt-2">
                    <span className="text-gray-600 font-medium">Calibration Due</span><span className="font-bold text-gray-900 dark:text-white">12 Jigs</span>
                  </div>
                </CardContent>
              </Card>

            </div>
          )}

        </motion.div>
      </AnimatePresence>
    </div>
  );
}

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
