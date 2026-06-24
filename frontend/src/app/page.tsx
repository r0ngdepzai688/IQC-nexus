"use client";

import { useState } from "react";
import { CheckCircle2, AlertTriangle, FileSignature, Box, ArrowRight, Activity, CalendarDays, MoreHorizontal } from "lucide-react";
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
    <div className="space-y-6 animate-in fade-in slide-in-from-bottom-4 duration-700 max-w-7xl mx-auto">
      
      {/* Header Actions */}
      <div className="flex flex-col sm:flex-row justify-between items-start sm:items-end gap-4 mb-8">
        <div>
          <h2 className="text-2xl font-semibold text-gray-900 dark:text-gray-100 tracking-tight">Tổng quan Chất lượng</h2>
          <p className="text-sm text-gray-500 dark:text-gray-400 mt-1">Theo dõi thời gian thực kết quả kiểm tra đầu vào (IQC).</p>
        </div>
        <div className="flex items-center space-x-3">
          <button className="flex items-center px-4 py-2 text-sm font-medium text-gray-600 dark:text-gray-300 bg-white dark:bg-gray-900 border border-gray-200 dark:border-gray-800 rounded-lg hover:bg-gray-50 dark:hover:bg-gray-800 transition-colors shadow-sm">
            <CalendarDays className="w-4 h-4 mr-2" />
            7 Ngày qua
          </button>
          <button className="px-4 py-2 text-sm font-medium text-white bg-[#1428A0] dark:bg-blue-600 rounded-lg hover:bg-[#101d73] dark:hover:bg-blue-700 transition-colors shadow-md shadow-[#1428A0]/20 dark:shadow-blue-900/20">
            Xuất Báo cáo
          </button>
        </div>
      </div>

      {/* Action-Oriented Stats Grid */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-5">
        <ActionableStatCard 
          title="Lô hàng kiểm tra" 
          value="12,450" 
          trend="+14% tuần này"
          trendUp={true}
          icon={<Box className="w-5 h-5" />}
          actionText="Xem lô hàng"
        />
        <ActionableStatCard 
          title="Tỷ lệ Đạt (Pass Rate)" 
          value="98.2%" 
          trend="-0.5% tuần này"
          trendUp={false}
          icon={<CheckCircle2 className="w-5 h-5 text-emerald-500 dark:text-emerald-400" />}
          actionText="Phân tích"
        />
        <ActionableStatCard 
          title="Báo cáo lỗi (NCR)" 
          value="24" 
          alert="4 Cần xử lý gấp"
          icon={<AlertTriangle className="w-5 h-5 text-rose-500 dark:text-rose-400" />}
          actionText="Xử lý NCR"
          isAlert={true}
        />
        <ActionableStatCard 
          title="Tiêu chuẩn cần duyệt" 
          value="7" 
          alert="3 NPI mới"
          icon={<FileSignature className="w-5 h-5 text-amber-500 dark:text-amber-400" />}
          actionText="Duyệt tiêu chuẩn"
          isAlert={false}
        />
      </div>

      {/* Main Content Grid */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        
        {/* Main Chart Area */}
        <div className="lg:col-span-2 bg-white dark:bg-[#09090b] rounded-xl border border-gray-100 dark:border-gray-800 shadow-[0_2px_10px_-4px_rgba(0,0,0,0.05)] p-5">
          <div className="flex justify-between items-center mb-6">
            <div className="flex items-center">
              <Activity className="w-5 h-5 text-gray-400 mr-2" />
              <h3 className="text-base font-semibold text-gray-900 dark:text-gray-100">Biểu đồ Lượng hàng (7 ngày)</h3>
            </div>
            <button className="p-1 text-gray-400 hover:text-gray-600 dark:hover:text-gray-300 rounded">
              <MoreHorizontal className="w-5 h-5" />
            </button>
          </div>
          <div className="h-[280px] w-full">
            <ResponsiveContainer width="100%" height="100%">
              <AreaChart data={data} margin={{ top: 10, right: 10, left: -20, bottom: 0 }}>
                <defs>
                  <linearGradient id="colorPass" x1="0" y1="0" x2="0" y2="1">
                    <stop offset="5%" stopColor="#1428A0" stopOpacity={0.15}/>
                    <stop offset="95%" stopColor="#1428A0" stopOpacity={0}/>
                  </linearGradient>
                </defs>
                <XAxis dataKey="name" stroke="#a1a1aa" fontSize={11} tickLine={false} axisLine={false} />
                <YAxis stroke="#a1a1aa" fontSize={11} tickLine={false} axisLine={false} tickFormatter={(value) => `${value}`} />
                <CartesianGrid strokeDasharray="3 3" vertical={false} stroke="#f4f4f5" className="dark:stroke-gray-800" />
                <Tooltip 
                  contentStyle={{ borderRadius: '8px', border: '1px solid #e4e4e7', boxShadow: '0 4px 6px -1px rgb(0 0 0 / 0.05)', fontSize: '12px' }}
                />
                <Area type="monotone" dataKey="pass" stroke="#1428A0" strokeWidth={2} fillOpacity={1} fill="url(#colorPass)" className="dark:stroke-blue-500" />
              </AreaChart>
            </ResponsiveContainer>
          </div>
        </div>

        {/* Action Feed / NG List */}
        <div className="bg-white dark:bg-[#09090b] rounded-xl border border-gray-100 dark:border-gray-800 shadow-[0_2px_10px_-4px_rgba(0,0,0,0.05)] p-5 flex flex-col">
          <div className="flex justify-between items-center mb-5">
            <h3 className="text-base font-semibold text-gray-900 dark:text-gray-100">Danh sách Cần Xử Lý</h3>
            <span className="text-xs font-medium bg-rose-100 dark:bg-rose-500/10 text-rose-600 dark:text-rose-400 px-2 py-0.5 rounded-full">Gấp</span>
          </div>
          
          <div className="flex-1 space-y-4">
            <TaskItem title="Duyệt Tiêu chuẩn Mainboard v2" time="10 phút trước" type="Approve" />
            <TaskItem title="Xử lý NCR Lô hàng Vỏ nhựa A" time="1 giờ trước" type="Alert" />
            <TaskItem title="Cập nhật form kiểm tra Ốc vít" time="2 giờ trước" type="Task" />
            <TaskItem title="Đánh giá năng lực NCC X" time="Hôm qua" type="Task" />
          </div>

          <button className="mt-5 w-full py-2 text-sm font-medium text-gray-600 dark:text-gray-400 bg-gray-50 dark:bg-gray-900 border border-gray-200 dark:border-gray-800 rounded-lg hover:bg-gray-100 dark:hover:bg-gray-800 transition-colors flex items-center justify-center">
            Xem tất cả <ArrowRight className="w-4 h-4 ml-2" />
          </button>
        </div>

      </div>
    </div>
  );
}

// Sub-components
function ActionableStatCard({ title, value, trend, trendUp, icon, alert, actionText, isAlert }: any) {
  return (
    <div className="bg-white dark:bg-[#09090b] p-5 rounded-xl border border-gray-100 dark:border-gray-800 shadow-[0_2px_10px_-4px_rgba(0,0,0,0.05)] hover:border-gray-200 dark:hover:border-gray-700 hover:shadow-md transition-all duration-300 group flex flex-col justify-between">
      <div>
        <div className="flex justify-between items-start mb-2">
          <p className="text-[13px] font-medium text-gray-500 dark:text-gray-400">{title}</p>
          <div className="p-2 bg-gray-50 dark:bg-gray-900 rounded-lg text-gray-500 dark:text-gray-400 group-hover:text-[#1428A0] dark:group-hover:text-blue-400 transition-colors">
            {icon}
          </div>
        </div>
        <div className="flex items-baseline space-x-2">
          <h3 className="text-2xl font-bold text-gray-900 dark:text-gray-100 tracking-tight">{value}</h3>
          {alert ? (
            <span className={`text-[11px] font-medium px-2 py-0.5 rounded-full ${isAlert ? 'bg-rose-100 dark:bg-rose-500/10 text-rose-600 dark:text-rose-400' : 'bg-amber-100 dark:bg-amber-500/10 text-amber-600 dark:text-amber-400'}`}>
              {alert}
            </span>
          ) : (
            <span className={`text-xs font-medium ${trendUp ? 'text-emerald-600 dark:text-emerald-400' : 'text-rose-500 dark:text-rose-400'}`}>
              {trend}
            </span>
          )}
        </div>
      </div>
      
      <div className="mt-4 pt-4 border-t border-gray-50 dark:border-gray-800/50">
        <button className="flex items-center text-[13px] font-semibold text-[#1428A0] dark:text-blue-400 hover:text-[#101d73] dark:hover:text-blue-300 transition-colors group/btn">
          {actionText} 
          <ArrowRight className="w-3.5 h-3.5 ml-1 transform group-hover/btn:translate-x-1 transition-transform" />
        </button>
      </div>
    </div>
  );
}

function TaskItem({ title, time, type }: any) {
  return (
    <div className="flex items-start group cursor-pointer">
      <div className={`mt-0.5 w-2 h-2 rounded-full mr-3 ${type === 'Alert' ? 'bg-rose-500' : type === 'Approve' ? 'bg-amber-500' : 'bg-[#1428A0] dark:bg-blue-500'}`} />
      <div>
        <p className="text-sm font-medium text-gray-700 dark:text-gray-300 group-hover:text-[#1428A0] dark:group-hover:text-blue-400 transition-colors">{title}</p>
        <p className="text-[11px] text-gray-400 dark:text-gray-500 mt-0.5">{time}</p>
      </div>
    </div>
  );
}
