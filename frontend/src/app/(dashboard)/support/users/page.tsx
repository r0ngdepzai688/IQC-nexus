"use client";

import React, { useState, useEffect } from "react";
import { useAuth, DashboardProfile } from "@/lib/contexts/AuthContext";
import { useRouter } from "next/navigation";
import { 
  ShieldAlert, Search, Filter, Plus, Users, UserCheck, UserX,
  ChevronLeft, ChevronRight, Edit2, Shield, Building, Briefcase, Download, Upload, Lock, Unlock, CheckCircle, Clock
} from "lucide-react";
import { motion, AnimatePresence } from "framer-motion";

export default function UserManagementPage() {
  const { user, loginAs } = useAuth();
  const router = useRouter();
  const [search, setSearch] = useState("");
  const [selectedUser, setSelectedUser] = useState<any>(null);
  const [usersList, setUsersList] = useState<any[]>([]);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    fetch("http://localhost:5215/api/users")
      .then(res => res.json())
      .then(data => {
        setUsersList(data);
        setIsLoading(false);
      })
      .catch(err => {
        console.error("Failed to fetch users", err);
        setIsLoading(false);
      });
  }, []);

  // Security Check: Access Denied for non-admins
  if (user.systemRole !== "Administrator") {
    return (
      <div className="min-h-[80vh] flex flex-col items-center justify-center p-10 bg-transparent rounded-3xl">
        <div className="w-24 h-24 bg-rose-100 dark:bg-rose-900/20 rounded-full flex items-center justify-center mb-6">
          <ShieldAlert className="w-12 h-12 text-rose-600 dark:text-rose-500" />
        </div>
        <h1 className="text-3xl font-black text-gray-900 dark:text-white mb-3">Access Denied</h1>
        <p className="text-gray-500 max-w-md text-center font-medium">
          You do not have the required system permissions to view the User Management module. 
          Please contact a System Administrator.
        </p>
      </div>
    );
  }

  const filteredUsers = usersList.filter(u => 
    (u.name || "").toLowerCase().includes(search.toLowerCase()) || 
    (u.id || "").includes(search) ||
    (u.dept || "").toLowerCase().includes(search.toLowerCase())
  );

  const activeUsers = usersList.filter(u => u.status === 'Active').length;
  const lockedUsers = usersList.filter(u => u.status === 'Locked' || u.status === 'Inactive').length;
  const adminUsers = usersList.filter(u => u.systemRole === 'Administrator').length;

  const getStatusStyle = (status: string) => {
    switch(status) {
      case 'Active': return "bg-emerald-50 text-emerald-600 dark:bg-emerald-900/20 dark:text-emerald-400";
      case 'Inactive': return "bg-gray-100 text-gray-600 dark:bg-gray-800 dark:text-gray-400";
      case 'Locked': return "bg-rose-50 text-rose-600 dark:bg-rose-900/20 dark:text-rose-400";
      case 'Pending': return "bg-yellow-50 text-yellow-600 dark:bg-yellow-900/20 dark:text-yellow-400";
      default: return "bg-gray-100 text-gray-600";
    }
  };

  const getStatusIcon = (status: string) => {
    switch(status) {
      case 'Active': return <CheckCircle className="w-3 h-3 mr-1" />;
      case 'Inactive': return <Clock className="w-3 h-3 mr-1" />;
      case 'Locked': return <Lock className="w-3 h-3 mr-1" />;
      case 'Pending': return <UserCheck className="w-3 h-3 mr-1" />;
      default: return null;
    }
  };

  const handlePreviewDashboard = () => {
    // Inject the target user's profile to current local session temporarily
    loginAs({
      position: selectedUser.position,
      scope: selectedUser.scope,
      dashboardProfile: selectedUser.dashboardProfile as DashboardProfile,
    });
    router.push("/overview");
  };

  return (
    <div className="min-h-full bg-transparent p-6 lg:p-10 pb-20 relative">
      {/* Header */}
      <div className="flex flex-col md:flex-row justify-between items-start md:items-center mb-8 gap-4">
        <div>
          <h1 className="text-3xl font-black text-gray-900 dark:text-white flex items-center">
            <Users className="w-8 h-8 mr-3 text-[#1428A0] dark:text-blue-500" /> Enterprise Directory
          </h1>
          <p className="text-gray-500 font-medium mt-1">Manage business roles, scopes, account statuses, and system permissions.</p>
        </div>
        <div className="flex gap-2">
          <button className="bg-white dark:bg-[#1A1A1A] border border-gray-200 dark:border-white/10 hover:bg-gray-50 dark:hover:bg-white/5 px-4 py-2.5 rounded-xl text-sm font-bold flex items-center transition-colors">
            <Download className="w-4 h-4 mr-2" /> Export
          </button>
          <button className="bg-white dark:bg-[#1A1A1A] border border-gray-200 dark:border-white/10 hover:bg-gray-50 dark:hover:bg-white/5 px-4 py-2.5 rounded-xl text-sm font-bold flex items-center transition-colors">
            <Upload className="w-4 h-4 mr-2" /> Import
          </button>
          <button className="bg-[#1428A0] hover:bg-blue-800 text-white px-5 py-2.5 rounded-xl text-sm font-bold shadow-lg flex items-center transition-colors">
            <Plus className="w-4 h-4 mr-2" /> Add User
          </button>
        </div>
      </div>

      {/* KPI Cards */}
      <div className="grid grid-cols-1 md:grid-cols-4 gap-4 mb-8">
        <div className="bg-white dark:bg-[#121212] p-5 rounded-2xl border border-gray-200 dark:border-white/10 shadow-sm flex items-center space-x-4">
          <div className="p-3 bg-blue-50 dark:bg-blue-900/20 rounded-xl text-blue-600"><Users className="w-5 h-5"/></div>
          <div><p className="text-xs font-bold text-gray-500 uppercase tracking-wider">Total Users</p><p className="text-2xl font-black">{usersList.length}</p></div>
        </div>
        <div className="bg-white dark:bg-[#121212] p-5 rounded-2xl border border-gray-200 dark:border-white/10 shadow-sm flex items-center space-x-4">
          <div className="p-3 bg-emerald-50 dark:bg-emerald-900/20 rounded-xl text-emerald-600"><UserCheck className="w-5 h-5"/></div>
          <div><p className="text-xs font-bold text-gray-500 uppercase tracking-wider">Active</p><p className="text-2xl font-black">{activeUsers}</p></div>
        </div>
        <div className="bg-white dark:bg-[#121212] p-5 rounded-2xl border border-gray-200 dark:border-white/10 shadow-sm flex items-center space-x-4">
          <div className="p-3 bg-rose-50 dark:bg-rose-900/20 rounded-xl text-rose-600"><UserX className="w-5 h-5"/></div>
          <div><p className="text-xs font-bold text-gray-500 uppercase tracking-wider">Locked / Inactive</p><p className="text-2xl font-black">{lockedUsers}</p></div>
        </div>
        <div className="bg-white dark:bg-[#121212] p-5 rounded-2xl border border-gray-200 dark:border-white/10 shadow-sm flex items-center space-x-4">
          <div className="p-3 bg-purple-50 dark:bg-purple-900/20 rounded-xl text-purple-600"><Shield className="w-5 h-5"/></div>
          <div><p className="text-xs font-bold text-gray-500 uppercase tracking-wider">Admins</p><p className="text-2xl font-black">{adminUsers}</p></div>
        </div>
      </div>

      {/* Main Table Card */}
      <div className="bg-gradient-to-b from-white/90 to-white/40 dark:from-white/5 dark:to-transparent backdrop-blur-xl rounded-3xl border border-white/60 border-t-white dark:border-white/10 dark:border-t-white/20 shadow-[0_8px_30px_rgb(0,0,0,0.04)] dark:shadow-none hover:shadow-[0_8px_30px_rgb(0,0,0,0.08)] transition-all duration-300 p-5 md:p-6 overflow-hidden flex flex-col min-h-[500px]">
        {/* Toolbar */}
        <div className="p-4 border-b border-gray-200 dark:border-white/10 flex flex-col md:flex-row justify-between gap-4">
          <div className="relative w-full md:w-96">
            <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-gray-400" />
            <input 
              type="text" 
              placeholder="Global Search (Name, ID, Dept, Scope)..."
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              className="w-full pl-9 pr-4 py-2.5 bg-gray-50 dark:bg-white/5 border border-gray-200 dark:border-white/10 rounded-xl text-sm font-medium focus:outline-none focus:ring-2 focus:ring-[#1428A0]"
            />
          </div>
          <div className="flex space-x-2">
            <button className="flex items-center px-4 py-2.5 bg-gray-50 dark:bg-white/5 border border-gray-200 dark:border-white/10 rounded-xl text-sm font-bold text-gray-700 dark:text-gray-300 hover:bg-gray-100 transition-colors">
              <Filter className="w-4 h-4 mr-2" /> Add Filter
            </button>
            <select className="px-4 py-2.5 bg-gray-50 dark:bg-white/5 border border-gray-200 dark:border-white/10 rounded-xl text-sm font-bold text-gray-700 dark:text-gray-300 outline-none">
              <option>Bulk Actions</option>
              <option>Activate Selected</option>
              <option>Lock Selected</option>
            </select>
          </div>
        </div>

        {/* Table */}
        <div className="overflow-x-auto flex-1">
          <table className="w-full text-left border-collapse whitespace-nowrap">
            <thead>
              <tr className="bg-gray-50/50 dark:bg-white/5 border-b border-gray-200 dark:border-white/10 text-xs font-bold text-gray-500 uppercase tracking-wider">
                <th className="px-6 py-4">Employee</th>
                <th className="px-6 py-4">Organization</th>
                <th className="px-6 py-4">Business Role</th>
                <th className="px-6 py-4">System Role</th>
                <th className="px-6 py-4">Account Status</th>
                <th className="px-6 py-4 text-right">Actions</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-100 dark:divide-white/5">
              {filteredUsers.map((u, i) => (
                <tr key={i} className={`hover:bg-blue-50/50 dark:hover:bg-white/5 transition-colors group ${u.status === 'Inactive' ? 'opacity-60' : ''}`}>
                  <td className="px-6 py-4">
                    <div className="flex items-center space-x-3">
                      <div className="w-10 h-10 rounded-full bg-gradient-to-br from-gray-200 to-gray-300 dark:from-gray-800 dark:to-gray-900 flex items-center justify-center text-sm font-black text-gray-600 dark:text-gray-400">
                        {u.name.charAt(0)}
                      </div>
                      <div>
                        <p className={`text-sm font-bold ${u.status === 'Locked' ? 'text-rose-600' : 'text-gray-900 dark:text-white'}`}>{u.name}</p>
                        <p className="text-[10px] font-medium text-gray-500">ID: {u.id} • Last login: {u.lastLogin}</p>
                      </div>
                    </div>
                  </td>
                  <td className="px-6 py-4">
                    <p className="text-sm font-bold text-gray-900 dark:text-white">{u.dept}</p>
                    <p className="text-[10px] font-medium text-gray-500">{u.org}</p>
                  </td>
                  <td className="px-6 py-4">
                    <p className="text-sm font-bold text-gray-900 dark:text-white">{u.position}</p>
                    <p className="text-[10px] font-medium text-gray-500">Scope: {u.scope}</p>
                  </td>
                  <td className="px-6 py-4">
                    {u.systemRole === "Administrator" ? (
                      <span className="inline-flex items-center px-2.5 py-1 rounded-md text-[10px] font-bold bg-purple-100 dark:bg-purple-900/30 text-purple-700 dark:text-purple-400 uppercase tracking-widest border border-purple-200 dark:border-purple-800">
                        <Shield className="w-3 h-3 mr-1" /> Admin
                      </span>
                    ) : (
                      <span className="inline-flex items-center px-2.5 py-1 rounded-md text-[10px] font-bold bg-gray-100 dark:bg-gray-800 text-gray-600 dark:text-gray-400 uppercase tracking-widest border border-gray-200 dark:border-gray-700">
                        User
                      </span>
                    )}
                  </td>
                  <td className="px-6 py-4">
                    <span className={`inline-flex items-center px-2.5 py-1 rounded-full text-[10px] font-bold uppercase tracking-wider ${getStatusStyle(u.status)}`}>
                      {getStatusIcon(u.status)} {u.status}
                    </span>
                  </td>
                  <td className="px-6 py-4 text-right">
                    <button 
                      onClick={() => setSelectedUser(u)}
                      className="p-2 text-gray-400 hover:text-[#1428A0] dark:hover:text-white hover:bg-blue-50 dark:hover:bg-white/10 rounded-lg transition-colors"
                    >
                      <Edit2 className="w-4 h-4" />
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>

        {/* Pagination */}
        <div className="p-4 border-t border-gray-200 dark:border-white/10 flex items-center justify-between text-sm">
          <span className="text-gray-500 font-medium">Showing {filteredUsers.length} of {usersList.length} users</span>
          <div className="flex space-x-1">
            <button className="p-2 border border-gray-200 dark:border-white/10 rounded-lg text-gray-400 hover:bg-gray-50 dark:hover:bg-white/5"><ChevronLeft className="w-4 h-4" /></button>
            <button className="px-3.5 py-1 bg-[#1428A0] text-white rounded-lg font-bold">1</button>
            <button className="px-3.5 py-1 text-gray-600 dark:text-gray-400 hover:bg-gray-50 dark:hover:bg-white/5 rounded-lg font-bold">2</button>
            <button className="px-3.5 py-1 text-gray-600 dark:text-gray-400 hover:bg-gray-50 dark:hover:bg-white/5 rounded-lg font-bold">3</button>
            <button className="p-2 border border-gray-200 dark:border-white/10 rounded-lg text-gray-400 hover:bg-gray-50 dark:hover:bg-white/5"><ChevronRight className="w-4 h-4" /></button>
          </div>
        </div>
      </div>

      {/* Edit User Modal (Mock) */}
      <AnimatePresence>
        {selectedUser && (
          <>
            <motion.div 
              initial={{ opacity: 0 }} animate={{ opacity: 1 }} exit={{ opacity: 0 }}
              onClick={() => setSelectedUser(null)}
              className="fixed inset-0 bg-black/40 backdrop-blur-sm z-[100]"
            />
            <motion.div 
              initial={{ x: '100%', opacity: 0 }} animate={{ x: 0, opacity: 1 }} exit={{ x: '100%', opacity: 0 }}
              transition={{ type: 'spring', damping: 25, stiffness: 200 }}
              className="fixed top-0 right-0 w-full md:w-[600px] h-full bg-white dark:bg-[#121212] z-[110] shadow-2xl border-l border-gray-200 dark:border-white/10 overflow-y-auto custom-scrollbar"
            >
              <div className="p-6 border-b border-gray-200 dark:border-white/10 flex justify-between items-center sticky top-0 bg-white/90 dark:bg-[#121212]/90 backdrop-blur-md z-10">
                <h2 className="text-xl font-black text-gray-900 dark:text-white">Edit User Account</h2>
                <div className="flex items-center gap-2">
                  <button 
                    onClick={handlePreviewDashboard}
                    className="px-3 py-1.5 bg-gray-100 dark:bg-white/10 text-xs font-bold rounded-lg hover:bg-gray-200 transition-colors flex items-center"
                  >
                    Preview Dashboard
                  </button>
                  <button onClick={() => setSelectedUser(null)} className="p-2 bg-gray-100 dark:bg-white/10 rounded-full hover:bg-gray-200 transition-colors">
                    <ChevronRight className="w-5 h-5" />
                  </button>
                </div>
              </div>

              <div className="p-6 space-y-8">
                {/* Identity */}
                <div className="flex items-center space-x-4">
                  <div className="w-16 h-16 rounded-full bg-gradient-to-tr from-gray-200 to-gray-400 text-gray-700 flex items-center justify-center text-xl font-black">
                    {selectedUser.name.charAt(0)}
                  </div>
                  <div className="flex-1">
                    <h3 className="text-lg font-bold text-gray-900 dark:text-white">{selectedUser.name}</h3>
                    <p className="text-sm text-gray-500 font-medium">Employee ID: {selectedUser.id}</p>
                  </div>
                  <div className="text-right text-xs text-gray-400 font-medium">
                    <p>Created: {selectedUser.createdDate}</p>
                    <p>Last Login: {selectedUser.lastLogin}</p>
                  </div>
                </div>

                {/* Status Switcher */}
                <div className="p-4 bg-gray-50 dark:bg-white/5 border border-gray-200 dark:border-white/10 rounded-2xl">
                  <label className="block text-[10px] font-bold text-gray-500 uppercase mb-2">Account Status</label>
                  <select defaultValue={selectedUser.status} className="w-full bg-white dark:bg-[#1A1A1A] border border-gray-200 dark:border-white/10 rounded-xl px-3 py-2 text-sm font-medium focus:border-blue-500 focus:outline-none">
                    <option value="Active">Active - Normal Access</option>
                    <option value="Inactive">Inactive - Soft Deleted</option>
                    <option value="Pending">Pending - Awaiting Activation</option>
                    <option value="Locked">Locked - Security Restriction</option>
                  </select>
                </div>

                {/* Forms */}
                <div className="space-y-6">
                  <div>
                    <h4 className="text-xs font-bold text-gray-500 uppercase tracking-widest mb-4 flex items-center"><Building className="w-4 h-4 mr-2" /> Organization</h4>
                    <div className="grid grid-cols-2 gap-4">
                      <div>
                        <label className="block text-[10px] font-bold text-gray-500 uppercase mb-1">Department</label>
                        <input type="text" defaultValue={selectedUser.dept} className="w-full bg-gray-50 border border-gray-200 rounded-xl px-3 py-2 text-sm font-medium focus:border-blue-500 focus:outline-none dark:bg-white/5 dark:border-white/10" />
                      </div>
                      <div>
                        <label className="block text-[10px] font-bold text-gray-500 uppercase mb-1">Organization</label>
                        <input type="text" defaultValue={selectedUser.org} className="w-full bg-gray-50 border border-gray-200 rounded-xl px-3 py-2 text-sm font-medium focus:border-blue-500 focus:outline-none dark:bg-white/5 dark:border-white/10" />
                      </div>
                    </div>
                  </div>

                  <div>
                    <h4 className="text-xs font-bold text-gray-500 uppercase tracking-widest mb-4 flex items-center"><Briefcase className="w-4 h-4 mr-2" /> Business Role (Operational)</h4>
                    <div className="grid grid-cols-2 gap-4">
                      <div>
                        <label className="block text-[10px] font-bold text-gray-500 uppercase mb-1">Position</label>
                        <select defaultValue={selectedUser.position} className="w-full bg-gray-50 border border-gray-200 rounded-xl px-3 py-2 text-sm font-medium focus:border-blue-500 focus:outline-none dark:bg-white/5 dark:border-white/10">
                          <option>Team Leader</option><option>Group Leader</option><option>Part Leader</option><option>Cell Leader</option><option>Staff</option>
                        </select>
                      </div>
                      <div>
                        <label className="block text-[10px] font-bold text-gray-500 uppercase mb-1">Scope</label>
                        <input type="text" defaultValue={selectedUser.scope} className="w-full bg-gray-50 border border-gray-200 rounded-xl px-3 py-2 text-sm font-medium focus:border-blue-500 focus:outline-none dark:bg-white/5 dark:border-white/10" />
                      </div>
                    </div>
                  </div>

                  <div className="p-5 bg-purple-50 dark:bg-purple-900/10 border border-purple-100 dark:border-purple-900/30 rounded-2xl">
                    <h4 className="text-xs font-bold text-purple-600 dark:text-purple-400 uppercase tracking-widest mb-4 flex items-center"><Shield className="w-4 h-4 mr-2" /> System Permissions</h4>
                    <div className="space-y-4">
                      <div>
                        <label className="block text-[10px] font-bold text-purple-500 uppercase mb-1">System Role</label>
                        <select defaultValue={selectedUser.systemRole} className="w-full bg-white dark:bg-[#1A1A1A] border border-purple-200 dark:border-purple-900/50 rounded-xl px-3 py-2 text-sm font-medium focus:border-purple-500 focus:outline-none">
                          <option>Administrator</option><option>User</option>
                        </select>
                      </div>
                      <div>
                        <label className="block text-[10px] font-bold text-purple-500 uppercase mb-1">Dashboard Profile Override</label>
                        <select defaultValue={selectedUser.dashboardProfile} className="w-full bg-white dark:bg-[#1A1A1A] border border-purple-200 dark:border-purple-900/50 rounded-xl px-3 py-2 text-sm font-medium focus:border-purple-500 focus:outline-none">
                          <option>Auto (Default)</option><option>Executive</option><option>Team Leader</option><option>Group Leader</option><option>Part Leader</option><option>Cell Leader</option><option>Staff</option>
                        </select>
                      </div>
                    </div>
                  </div>

                  <div>
                    <label className="block text-[10px] font-bold text-gray-500 uppercase mb-1">Admin Notes (Hidden from user)</label>
                    <textarea 
                      defaultValue={selectedUser.notes}
                      rows={3} 
                      className="w-full bg-gray-50 border border-gray-200 rounded-xl px-3 py-2 text-sm font-medium focus:border-blue-500 focus:outline-none dark:bg-white/5 dark:border-white/10" 
                    />
                  </div>
                </div>

                {/* Actions */}
                <div className="pt-6 flex gap-3 border-t border-gray-200 dark:border-white/10 mt-6">
                  <button className="flex-1 bg-[#1428A0] text-white py-3 rounded-xl font-bold shadow-lg hover:bg-blue-800 transition-colors" onClick={() => setSelectedUser(null)}>Save Changes</button>
                  <button className="flex-1 bg-gray-100 dark:bg-white/5 text-gray-700 dark:text-gray-300 py-3 rounded-xl font-bold hover:bg-gray-200 dark:hover:bg-white/10 transition-colors" onClick={() => setSelectedUser(null)}>Cancel</button>
                </div>
              </div>
            </motion.div>
          </>
        )}
      </AnimatePresence>
    </div>
  );
}
