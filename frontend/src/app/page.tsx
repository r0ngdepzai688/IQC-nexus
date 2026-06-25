"use client";

import { CheckCircle2, AlertTriangle, FileSignature, Box, ArrowRight, Activity, CalendarDays, Zap } from "lucide-react";
import { AreaChart, Area, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer } from "recharts";

const data = [
  { name: "T2", pass: 4000, fail: 240 },
  { name: "T3", pass: 3000, fail: 139 },
  { name: "T4", pass: 2000, fail: 980 },
  { name: "T5", pass: 2780, fail: 390 },
  { name: "T6", pass: 1890, fail: 480 },
  { name: "T7", pass: 2390, fail: 380 },
  { name: "CN", pass: 3490, fail: 430 },
];

export default function Home() {
  return (
    <div className="space-y-4 animate-in fade-in slide-in-from-bottom-8 duration-1000 max-w-[1600px] mx-auto h-full">
      
      {/* Header Actions */}
      <div className="flex flex-col sm:flex-row justify-between items-start sm:items-end gap-4 px-2 mb-6">
        <div>
          <h2 className="text-3xl font-black text-transparent bg-clip-text bg-gradient-to-r from-gray-900 to-gray-600 dark:from-white dark:to-gray-400 tracking-tight">
            Tổng quan Chất lượng
          </h2>
          <p className="text-sm font-medium text-gray-500 dark:text-gray-400 mt-1">Hệ thống giám sát IQC thời gian thực.</p>
        </div>
        <div className="flex items-center space-x-3">
          <button className="flex items-center px-4 py-2.5 text-sm font-bold text-gray-700 dark:text-gray-200 bg-white/50 dark:bg-white/5 backdrop-blur-md border border-white/60 dark:border-white/10 rounded-full hover:bg-white/80 dark:hover:bg-white/10 transition-all shadow-sm">
            <CalendarDays className="w-4 h-4 mr-2 text-[#1428A0] dark:text-blue-400" />
            7 Ngày qua
          </button>
          <button className="px-5 py-2.5 text-sm font-bold text-white bg-gradient-to-r from-[#1428A0] to-blue-500 rounded-full hover:shadow-[0_0_20px_rgba(20,40,160,0.4)] hover:scale-105 transition-all shadow-lg shadow-blue-500/20 flex items-center">
            <Zap className="w-4 h-4 mr-2 fill-current" />
            Xuất Báo cáo
          </button>
        </div>
      </div>

      {/* Avant-Garde Bento Grid */}
      <div className="grid grid-cols-1 md:grid-cols-4 xl:grid-cols-12 gap-4 auto-rows-[minmax(120px,auto)]">
        
        {/* Pass Rate - Big Hero Card */}
        <div className="md:col-span-2 xl:col-span-3 row-span-2 bg-gradient-to-br from-[#1428A0] to-blue-700 text-white p-6 rounded-[2rem] shadow-xl shadow-blue-900/20 relative overflow-hidden group">
          <div className="absolute top-0 right-0 w-64 h-64 bg-white/10 rounded-full blur-3xl -mr-20 -mt-20 group-hover:scale-150 transition-transform duration-700" />
          <div className="relative z-10 flex flex-col h-full justify-between">
            <div className="flex justify-between items-start">
              <div className="p-3 bg-white/20 backdrop-blur-md rounded-2xl">
                <CheckCircle2 className="w-6 h-6 text-white" />
              </div>
              <span className="px-3 py-1 bg-emerald-400/20 text-emerald-100 text-xs font-bold rounded-full border border-emerald-400/30">
                +1.2% Đạt
              </span>
            </div>
            <div className="mt-8">
              <p className="text-blue-100 font-medium mb-1">Tỷ lệ Đạt (Pass Rate)</p>
              <h3 className="text-5xl font-black tracking-tighter">98.2<span className="text-2xl text-blue-200">%</span></h3>
            </div>
            <button className="mt-6 flex items-center justify-between w-full p-3 bg-black/20 hover:bg-black/30 backdrop-blur-md rounded-xl text-sm font-semibold transition-colors">
              Phân tích chuyên sâu <ArrowRight className="w-4 h-4" />
            </button>
          </div>
        </div>

        {/* Small Stat 1 */}
        <div className="md:col-span-2 xl:col-span-3 row-span-1 bg-white/60 dark:bg-[#111111]/60 backdrop-blur-xl border border-white/80 dark:border-white/10 p-5 rounded-[2rem] hover:shadow-[0_8px_30px_rgba(0,0,0,0.04)] transition-all flex flex-col justify-between group">
          <div className="flex justify-between items-start">
            <p className="text-sm font-bold text-gray-500 dark:text-gray-400">Lô hàng kiểm tra</p>
            <Box className="w-5 h-5 text-[#1428A0] dark:text-blue-400 opacity-70 group-hover:opacity-100 transition-opacity" />
          </div>
          <div className="flex items-baseline space-x-2 mt-2">
            <h3 className="text-3xl font-black text-gray-900 dark:text-white tracking-tight">12,450</h3>
          </div>
        </div>

        {/* Small Stat 2 (Alert) */}
        <div className="md:col-span-2 xl:col-span-3 row-span-1 bg-rose-50/80 dark:bg-rose-950/20 backdrop-blur-xl border border-rose-100 dark:border-rose-900/50 p-5 rounded-[2rem] hover:shadow-[0_8px_30px_rgba(225,29,72,0.1)] transition-all flex flex-col justify-between group">
          <div className="flex justify-between items-start">
            <p className="text-sm font-bold text-rose-600 dark:text-rose-400">Báo cáo lỗi (NCR)</p>
            <AlertTriangle className="w-5 h-5 text-rose-500 animate-pulse" />
          </div>
          <div className="flex items-baseline space-x-2 mt-2">
            <h3 className="text-3xl font-black text-rose-700 dark:text-rose-300 tracking-tight">24</h3>
            <span className="text-[10px] font-bold px-2 py-1 bg-rose-600 text-white rounded-full uppercase tracking-wider">
              4 Gấp
            </span>
          </div>
        </div>

        {/* Small Stat 3 */}
        <div className="md:col-span-2 xl:col-span-3 row-span-1 bg-white/60 dark:bg-[#111111]/60 backdrop-blur-xl border border-white/80 dark:border-white/10 p-5 rounded-[2rem] hover:shadow-[0_8px_30px_rgba(0,0,0,0.04)] transition-all flex flex-col justify-between group">
          <div className="flex justify-between items-start">
            <p className="text-sm font-bold text-amber-600 dark:text-amber-500">Tiêu chuẩn chờ duyệt</p>
            <FileSignature className="w-5 h-5 text-amber-500 opacity-70 group-hover:opacity-100" />
          </div>
          <div className="flex items-baseline space-x-2 mt-2">
            <h3 className="text-3xl font-black text-gray-900 dark:text-white tracking-tight">7</h3>
            <span className="text-xs font-semibold text-amber-600 dark:text-amber-400 bg-amber-100 dark:bg-amber-900/30 px-2 py-0.5 rounded-md">
              3 NPI
            </span>
          </div>
        </div>

        {/* Main Chart Area */}
        <div className="md:col-span-4 xl:col-span-9 row-span-3 bg-white/60 dark:bg-[#111111]/60 backdrop-blur-xl border border-white/80 dark:border-white/10 p-6 rounded-[2rem] shadow-sm flex flex-col relative overflow-hidden group">
          <div className="absolute top-0 right-0 w-[500px] h-[500px] bg-[#1428A0]/5 dark:bg-blue-600/10 rounded-full blur-[80px] -mr-40 -mt-40 pointer-events-none" />
          
          <div className="flex justify-between items-center mb-6 relative z-10">
            <div className="flex items-center">
              <div className="p-2 bg-[#1428A0]/10 dark:bg-blue-500/20 rounded-xl mr-3">
                <Activity className="w-5 h-5 text-[#1428A0] dark:text-blue-400" />
              </div>
              <h3 className="text-lg font-bold text-gray-900 dark:text-white">Lưu lượng Kiểm tra & Tỉ lệ Lỗi (7 ngày)</h3>
            </div>
            <div className="flex space-x-2">
              <div className="flex items-center text-xs font-semibold text-gray-500 dark:text-gray-400">
                <span className="w-2 h-2 rounded-full bg-[#1428A0] mr-2"></span> Đạt
              </div>
            </div>
          </div>
          
          <div className="flex-1 w-full min-h-[300px] relative z-10">
            <ResponsiveContainer width="100%" height="100%">
              <AreaChart data={data} margin={{ top: 10, right: 0, left: -20, bottom: 0 }}>
                <defs>
                  <linearGradient id="colorPass" x1="0" y1="0" x2="0" y2="1">
                    <stop offset="5%" stopColor="#1428A0" stopOpacity={0.3}/>
                    <stop offset="95%" stopColor="#1428A0" stopOpacity={0}/>
                  </linearGradient>
                </defs>
                <XAxis dataKey="name" stroke="#a1a1aa" fontSize={12} fontWeight={600} tickLine={false} axisLine={false} />
                <YAxis stroke="#a1a1aa" fontSize={12} fontWeight={600} tickLine={false} axisLine={false} tickFormatter={(value) => `${value}`} />
                <CartesianGrid strokeDasharray="4 4" vertical={false} stroke="#e4e4e7" className="dark:stroke-gray-800" />
                <Tooltip 
                  contentStyle={{ borderRadius: '16px', border: '1px solid rgba(255,255,255,0.2)', backgroundColor: 'rgba(255,255,255,0.8)', backdropFilter: 'blur(12px)', boxShadow: '0 10px 40px -10px rgba(0,0,0,0.1)', fontWeight: 'bold' }}
                />
                <Area type="monotone" dataKey="pass" stroke="#1428A0" strokeWidth={4} fillOpacity={1} fill="url(#colorPass)" className="dark:stroke-blue-500" />
              </AreaChart>
            </ResponsiveContainer>
          </div>
        </div>

        {/* Action Feed / NG List */}
        <div className="md:col-span-4 xl:col-span-3 row-span-4 bg-white/60 dark:bg-[#111111]/60 backdrop-blur-xl border border-white/80 dark:border-white/10 p-5 rounded-[2rem] shadow-sm flex flex-col">
          <div className="flex justify-between items-center mb-6">
            <h3 className="text-base font-bold text-gray-900 dark:text-white">Trung tâm Hành động</h3>
            <span className="text-[10px] font-black bg-rose-500 text-white px-2 py-1 rounded-full animate-pulse uppercase tracking-widest shadow-lg shadow-rose-500/40">Gấp</span>
          </div>
          
          <div className="flex-1 space-y-3">
            <TaskItem title="Duyệt Tiêu chuẩn Mainboard v2" time="10 phút trước" type="Approve" />
            <TaskItem title="Xử lý NCR Lô hàng Vỏ nhựa A" time="1 giờ trước" type="Alert" />
            <TaskItem title="Cập nhật form kiểm tra Ốc vít" time="2 giờ trước" type="Task" />
            <TaskItem title="Đánh giá năng lực NCC X" time="Hôm qua" type="Task" />
            <TaskItem title="Đánh giá năng lực NCC Y" time="Hôm qua" type="Task" />
          </div>

          <button className="mt-4 w-full py-3 text-sm font-bold text-gray-700 dark:text-gray-300 bg-black/5 dark:bg-white/5 border-transparent rounded-xl hover:bg-black/10 dark:hover:bg-white/10 transition-colors flex items-center justify-center group">
            Mở rộng <ArrowRight className="w-4 h-4 ml-2 group-hover:translate-x-1 transition-transform" />
          </button>
        </div>

      </div>
    </div>
  );
}

// Sub-components
function TaskItem({ title, time, type }: any) {
  return (
    <div className="flex items-start p-3 rounded-2xl hover:bg-white/50 dark:hover:bg-white/5 transition-colors cursor-pointer group border border-transparent hover:border-white/40 dark:hover:border-white/5">
      <div className={`mt-1 w-2.5 h-2.5 rounded-full mr-3 flex-shrink-0 shadow-sm ${type === 'Alert' ? 'bg-rose-500 shadow-rose-500/50' : type === 'Approve' ? 'bg-amber-500 shadow-amber-500/50' : 'bg-[#1428A0] dark:bg-blue-500 shadow-blue-500/50'}`} />
      <div>
        <p className="text-sm font-bold text-gray-800 dark:text-gray-200 group-hover:text-[#1428A0] dark:group-hover:text-blue-400 transition-colors leading-tight">{title}</p>
        <p className="text-xs font-semibold text-gray-400 dark:text-gray-500 mt-1">{time}</p>
      </div>
    </div>
  );
}
