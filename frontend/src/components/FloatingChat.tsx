"use client";

import { useState, useEffect, useRef } from "react";
import { MessageCircle, X, Send, Paperclip, Smile, Minus, Heart, Image as ImageIcon, FileText, ChevronLeft, Edit, Search as SearchIcon, Users, CheckCheck, Zap, Command, Maximize2, Minimize2, PanelRightClose, PanelRight, ArrowUpRight, Sparkles, AlertCircle, FileCheck, Layers, Bell, Pin, FolderClock, Hash, ArrowRight, ShieldCheck, FileSpreadsheet, Activity, MessageSquare, MoreVertical, Plus, Wrench } from "lucide-react";
import { HubConnection, HubConnectionBuilder, LogLevel } from "@microsoft/signalr";
import TextareaAutosize from "react-textarea-autosize";
import { motion, AnimatePresence } from "framer-motion";
import { usePathname } from "next/navigation";

// --- TYPES ---
type DisplayMode = 'floating' | 'expanded' | 'docked' | 'closed';
type SidebarSection = 'AI Assistant' | 'Notifications' | 'Approvals' | 'Pinned' | 'Recent' | 'Threads' | 'Direct';

interface Message {
  id: string;
  senderId: number;
  content: string;
  createdAt: string;
  isMe: boolean;
  type?: 'text' | 'approval' | 'file';
  attachments?: { url: string; name: string; type: string, size?: string }[];
  reactions?: string[];
  status?: 'sent' | 'delivered' | 'read';
}

interface Thread {
  id: string;
  title: string;
  type: SidebarSection;
  isGroup: boolean;
  lastMessage?: string;
  updatedAt: string;
  unreadCount?: number;
  priority?: 'high' | 'normal';
  isPinned?: boolean;
}

// --- MOCK DATA ---
const MOCK_USERS = [
  { id: 1, name: "Anh Thu", username: "athu", isOnline: true, color: "from-blue-500 to-indigo-500" },
  { id: 2, name: "Minh Hai", username: "mhai", isOnline: true, color: "from-emerald-400 to-teal-500" },
  { id: 3, name: "Supplier A", username: "sup_a", isOnline: false, color: "from-orange-400 to-rose-500" },
];

const MOCK_THREADS: Thread[] = [
  { id: 'ai_1', title: 'IQC Copilot', type: 'AI Assistant', isGroup: false, lastMessage: 'Báo cáo NCR đã sẵn sàng.', updatedAt: new Date().toISOString(), isPinned: true },
  { id: 'app_1', title: 'Phê duyệt BOM', type: 'Approvals', isGroup: true, lastMessage: 'Vui lòng kiểm duyệt BOM mới nhất.', updatedAt: new Date().toISOString(), unreadCount: 1, priority: 'high' },
  { id: 'th_1', title: 'Galaxy S26 Ultra - Camera L1', type: 'Threads', isGroup: true, lastMessage: 'Supplier đã upload file test.', updatedAt: new Date(Date.now() - 3600000).toISOString(), isPinned: true },
  { id: 'dm_1', title: 'Minh Hai (QA)', type: 'Direct', isGroup: false, lastMessage: 'Ok, tôi sẽ check ngay.', updatedAt: new Date(Date.now() - 7200000).toISOString() }
];

export function FloatingChat() {
  const [displayMode, setDisplayMode] = useState<DisplayMode>('closed');
  const [activeThread, setActiveThread] = useState<Thread | null>(null);
  const [searchQuery, setSearchQuery] = useState("");
  const [inputMessage, setInputMessage] = useState("");
  const [messages, setMessages] = useState<Message[]>([]);
  const pathname = usePathname();
  
  const messagesEndRef = useRef<HTMLDivElement>(null);

  // Scroll to bottom
  useEffect(() => {
    if (messagesEndRef.current && activeThread) {
      messagesEndRef.current.scrollIntoView({ behavior: "smooth" });
    }
  }, [messages, activeThread, displayMode]);

  // Load mock messages when opening a thread
  useEffect(() => {
    if (activeThread) {
      setMessages([
        { id: '1', senderId: 2, content: 'Đã gửi file BOM mới. Mọi người check nhé.', createdAt: new Date(Date.now() - 86400000).toISOString(), isMe: false },
        { id: '2', senderId: 1, content: 'Tôi đã xem qua, phần cảm biến bị thiếu specs.', createdAt: new Date(Date.now() - 43200000).toISOString(), isMe: true, status: 'read' },
      ]);
    }
  }, [activeThread]);

  const toggleMode = (newMode: DisplayMode) => {
    if (displayMode === newMode) {
      setDisplayMode('closed');
    } else {
      setDisplayMode(newMode);
    }
  };

  const sendMessage = (e: React.FormEvent) => {
    e.preventDefault();
    if (!inputMessage.trim() || !activeThread) return;

    setMessages(prev => [...prev, {
      id: Date.now().toString(),
      senderId: 1,
      content: inputMessage,
      createdAt: new Date().toISOString(),
      isMe: true,
      status: 'sent'
    }]);
    
    setInputMessage("");
    
    // Simulate read receipt
    setTimeout(() => {
      setMessages(prev => {
        const newMsgs = [...prev];
        newMsgs[newMsgs.length - 1].status = 'read';
        return newMsgs;
      });
    }, 1000);
  };

  // Context-Aware Suggestion
  const getContextSuggestion = () => {
    if (pathname.includes('/new-model')) return { title: 'Discuss New Models', icon: Layers, action: 'Start Thread' };
    if (pathname.includes('/hr')) return { title: 'Share Workforce Data', icon: Users, action: 'Share' };
    if (pathname.includes('/equipment')) return { title: 'Open Equipment Discussion', icon: Wrench, action: 'Discuss' };
    return { title: 'Ask IQC Copilot', icon: Sparkles, action: 'Ask AI' };
  };
  const contextData = getContextSuggestion();

  if (displayMode === 'closed') {
    return (
      <button
        onClick={() => setDisplayMode('floating')}
        className="fixed bottom-6 right-6 z-[100] w-14 h-14 rounded-2xl bg-gradient-to-tr from-[#1428A0] to-indigo-600 text-white shadow-[0_8px_30px_rgb(20,40,160,0.3)] hover:shadow-[0_12px_40px_rgb(20,40,160,0.4)] hover:-translate-y-1 flex items-center justify-center transition-all duration-300"
      >
        <MessageCircle className="w-6 h-6" />
        <span className="absolute -top-1 -right-1 w-3.5 h-3.5 bg-rose-500 border-2 border-white dark:border-[#0A0A0A] rounded-full"></span>
      </button>
    );
  }

  return (
    <div className={`fixed z-[100] flex transition-all duration-500 ease-[cubic-bezier(0.2,0,0,1)] ${
      displayMode === 'docked' 
        ? 'top-0 right-0 h-screen w-[400px] border-l border-gray-200 dark:border-white/10 shadow-2xl' 
        : displayMode === 'expanded'
        ? 'top-[10vh] left-[15vw] right-[15vw] bottom-[10vh] rounded-3xl shadow-[0_20px_60px_rgb(0,0,0,0.15)] border border-gray-200 dark:border-white/10'
        : 'bottom-6 right-6 w-[380px] h-[600px] rounded-3xl shadow-[0_12px_40px_rgb(0,0,0,0.12)] border border-gray-200 dark:border-white/10'
    }`}>
      
      <div className="bg-white dark:bg-[#121212] w-full h-full flex overflow-hidden rounded-inherit">
        
        {/* ================= SIDEBAR (Hidden in floating mode if active thread is open, to save space) ================= */}
        <div className={`flex-col border-r border-gray-100 dark:border-white/5 bg-[#F8F9FA] dark:bg-[#0A0A0A] ${
          displayMode === 'floating' && activeThread ? 'hidden' : 'flex w-[300px] flex-shrink-0'
        } ${displayMode === 'floating' && !activeThread ? 'w-full' : ''}`}>
          
          {/* Header & Controls */}
          <div className="px-4 py-3 flex items-center justify-between border-b border-gray-200 dark:border-white/5">
            <h2 className="font-bold text-gray-900 dark:text-white tracking-tight">Collaboration Hub</h2>
            <div className="flex space-x-1">
              {displayMode !== 'docked' && (
                <button onClick={() => setDisplayMode('docked')} className="p-1.5 text-gray-400 hover:text-gray-900 hover:bg-gray-200 dark:hover:text-white dark:hover:bg-white/10 rounded-lg transition-colors" title="Dock">
                  <PanelRight className="w-4 h-4" />
                </button>
              )}
              {displayMode !== 'expanded' && (
                <button onClick={() => setDisplayMode('expanded')} className="p-1.5 text-gray-400 hover:text-gray-900 hover:bg-gray-200 dark:hover:text-white dark:hover:bg-white/10 rounded-lg transition-colors" title="Expand">
                  <Maximize2 className="w-4 h-4" />
                </button>
              )}
              {displayMode !== 'floating' && (
                <button onClick={() => setDisplayMode('floating')} className="p-1.5 text-gray-400 hover:text-gray-900 hover:bg-gray-200 dark:hover:text-white dark:hover:bg-white/10 rounded-lg transition-colors" title="Floating">
                  <Minimize2 className="w-4 h-4" />
                </button>
              )}
              <button onClick={() => setDisplayMode('closed')} className="p-1.5 text-gray-400 hover:text-rose-500 hover:bg-rose-50 dark:hover:bg-rose-500/10 rounded-lg transition-colors ml-1">
                <X className="w-4 h-4" />
              </button>
            </div>
          </div>

          {/* Context Aware Banner */}
          <div className="px-4 pt-4 pb-2">
            <div className="bg-gradient-to-r from-blue-50 to-indigo-50 dark:from-blue-900/10 dark:to-indigo-900/10 border border-blue-100 dark:border-blue-500/20 rounded-xl p-3 flex items-center justify-between cursor-pointer hover:shadow-md transition-shadow">
              <div className="flex items-center space-x-3">
                <div className="p-2 bg-blue-100 dark:bg-blue-500/20 text-[#1428A0] dark:text-blue-400 rounded-lg">
                  <contextData.icon className="w-4 h-4" />
                </div>
                <div>
                  <p className="text-[10px] font-bold text-[#1428A0] dark:text-blue-400 uppercase tracking-wider">{contextData.action}</p>
                  <p className="text-xs font-semibold text-gray-900 dark:text-white mt-0.5">{contextData.title}</p>
                </div>
              </div>
              <ArrowRight className="w-4 h-4 text-blue-500" />
            </div>
          </div>

          {/* Global Search */}
          <div className="px-4 py-2">
            <div className="relative">
              <SearchIcon className="w-4 h-4 absolute left-3 top-2.5 text-gray-400" />
              <input 
                type="text" 
                placeholder="Search People, BOM, NCR..." 
                value={searchQuery}
                onChange={e => setSearchQuery(e.target.value)}
                className="w-full bg-white dark:bg-[#1A1A1A] border border-gray-200 dark:border-white/10 rounded-xl pl-9 pr-3 py-2 text-xs font-medium focus:outline-none focus:border-[#1428A0] transition-colors shadow-sm"
              />
            </div>
            
            {/* Quick Filters */}
            <div className="flex space-x-1.5 mt-3 overflow-x-auto custom-scrollbar pb-1">
              {['All', 'Unread', 'Mentions', 'Approvals'].map(f => (
                <button key={f} className="px-3 py-1 bg-white dark:bg-[#1A1A1A] border border-gray-200 dark:border-white/10 rounded-full text-[10px] font-bold text-gray-600 dark:text-gray-300 hover:text-[#1428A0] hover:border-[#1428A0] transition-colors whitespace-nowrap shadow-sm">
                  {f}
                </button>
              ))}
            </div>
          </div>

          {/* Thread List */}
          <div className="flex-1 overflow-y-auto custom-scrollbar px-2 space-y-4 pt-2">
            
            {/* Group: AI & Approvals */}
            <div>
              <h3 className="px-3 text-[10px] font-bold text-gray-400 uppercase tracking-wider mb-2">Workspace</h3>
              {MOCK_THREADS.filter(t => t.type === 'AI Assistant' || t.type === 'Approvals').map(thread => (
                <button 
                  key={thread.id} 
                  onClick={() => setActiveThread(thread)}
                  className={`w-full flex items-center p-2.5 rounded-xl transition-colors mb-1 text-left ${activeThread?.id === thread.id ? 'bg-white dark:bg-white/10 shadow-sm border border-gray-200 dark:border-white/5' : 'hover:bg-gray-100 dark:hover:bg-white/5 border border-transparent'}`}
                >
                  <div className="relative">
                    <div className={`w-9 h-9 rounded-xl flex items-center justify-center font-bold text-white shadow-sm bg-gradient-to-br ${thread.type === 'AI Assistant' ? 'from-indigo-500 to-purple-600' : 'from-amber-400 to-orange-500'}`}>
                      {thread.type === 'AI Assistant' ? <Sparkles className="w-4 h-4" /> : <ShieldCheck className="w-4 h-4" />}
                    </div>
                  </div>
                  <div className="ml-3 flex-1 overflow-hidden">
                    <div className="flex justify-between items-baseline">
                      <h4 className="font-semibold text-sm text-gray-900 dark:text-white truncate">{thread.title}</h4>
                    </div>
                    <p className={`text-xs truncate ${thread.unreadCount ? 'text-gray-900 dark:text-white font-medium' : 'text-gray-500'}`}>{thread.lastMessage}</p>
                  </div>
                  {thread.unreadCount && (
                    <div className="w-4 h-4 rounded-full bg-rose-500 text-white flex items-center justify-center text-[9px] font-bold shadow-sm ml-2">{thread.unreadCount}</div>
                  )}
                </button>
              ))}
            </div>

            {/* Group: Project Threads */}
            <div>
              <h3 className="px-3 text-[10px] font-bold text-gray-400 uppercase tracking-wider mb-2">Project Threads</h3>
              {MOCK_THREADS.filter(t => t.type === 'Threads' || t.type === 'Direct').map(thread => (
                <button 
                  key={thread.id} 
                  onClick={() => setActiveThread(thread)}
                  className={`w-full flex items-center p-2.5 rounded-xl transition-colors mb-1 text-left ${activeThread?.id === thread.id ? 'bg-white dark:bg-white/10 shadow-sm border border-gray-200 dark:border-white/5' : 'hover:bg-gray-100 dark:hover:bg-white/5 border border-transparent'}`}
                >
                  <div className="relative">
                    <div className={`w-9 h-9 rounded-full flex items-center justify-center font-bold text-white shadow-sm bg-gradient-to-br ${MOCK_USERS.find(u => u.name.includes(thread.title))?.color || 'from-[#1428A0] to-blue-500'}`}>
                      {thread.type === 'Threads' ? <Hash className="w-4 h-4" /> : thread.title.charAt(0)}
                    </div>
                    {thread.isPinned && <div className="absolute -bottom-1 -right-1 w-4 h-4 bg-white dark:bg-[#1A1A1A] rounded-full flex items-center justify-center shadow-sm"><Pin className="w-2.5 h-2.5 text-gray-500" /></div>}
                  </div>
                  <div className="ml-3 flex-1 overflow-hidden">
                    <div className="flex justify-between items-baseline">
                      <h4 className="font-semibold text-sm text-gray-900 dark:text-white truncate">{thread.title}</h4>
                      <span className="text-[9px] font-medium text-gray-400 flex-shrink-0 ml-2">
                        {new Date(thread.updatedAt).toLocaleTimeString([], {hour: '2-digit', minute:'2-digit'})}
                      </span>
                    </div>
                    <p className="text-xs text-gray-500 truncate mt-0.5">{thread.lastMessage}</p>
                  </div>
                </button>
              ))}
            </div>

          </div>
        </div>

        {/* ================= CHAT AREA ================= */}
        <div className={`flex-1 flex flex-col relative bg-white dark:bg-[#121212] ${
          displayMode === 'floating' && !activeThread ? 'hidden' : 'flex'
        }`}>
          
          {activeThread ? (
            <>
              {/* Thread Header */}
              <div className="px-5 py-3 border-b border-gray-100 dark:border-white/5 flex items-center justify-between bg-white dark:bg-[#121212]">
                <div className="flex items-center space-x-3 overflow-hidden">
                  {displayMode === 'floating' && (
                    <button onClick={() => setActiveThread(null)} className="p-1.5 hover:bg-gray-100 dark:hover:bg-white/5 rounded-lg transition-colors flex-shrink-0 text-gray-500">
                      <ChevronLeft className="w-5 h-5" />
                    </button>
                  )}
                  <div className={`w-8 h-8 rounded-full flex items-center justify-center font-bold text-white shadow-sm flex-shrink-0 bg-gradient-to-br ${activeThread.type === 'AI Assistant' ? 'from-indigo-500 to-purple-600' : 'from-[#1428A0] to-blue-500'}`}>
                    {activeThread.type === 'AI Assistant' ? <Sparkles className="w-4 h-4" /> : activeThread.type === 'Threads' ? <Hash className="w-4 h-4" /> : activeThread.title.charAt(0)}
                  </div>
                  <div className="truncate">
                    <h3 className="font-bold text-gray-900 dark:text-white text-sm truncate">{activeThread.title}</h3>
                    <p className="text-[10px] text-gray-500 truncate font-medium">
                      {activeThread.type === 'AI Assistant' ? 'Enterprise Intelligence' : activeThread.isGroup ? 'Project Thread • 4 Members' : 'Active now'}
                    </p>
                  </div>
                </div>
                <div className="flex items-center space-x-1">
                  <button className="p-2 text-gray-400 hover:text-gray-900 dark:hover:text-white hover:bg-gray-100 dark:hover:bg-white/10 rounded-lg transition-colors">
                    <SearchIcon className="w-4 h-4" />
                  </button>
                  <button className="p-2 text-gray-400 hover:text-gray-900 dark:hover:text-white hover:bg-gray-100 dark:hover:bg-white/10 rounded-lg transition-colors hidden sm:block">
                    <MoreVertical className="w-4 h-4" />
                  </button>
                </div>
              </div>

              {/* Messages Area */}
              <div className="flex-1 overflow-y-auto p-5 space-y-5 custom-scrollbar bg-[url('https://www.transparenttextures.com/patterns/cubes.png')] bg-fixed dark:opacity-10">
                <div className="text-center text-xs font-semibold text-gray-400 my-4 uppercase tracking-wider">Beginning of Thread</div>
                
                {/* Approval Workflow Card Mock */}
                {activeThread.type === 'Approvals' && (
                  <div className="mx-auto max-w-sm bg-white dark:bg-[#1A1A1A] border border-gray-200 dark:border-white/10 rounded-2xl shadow-sm overflow-hidden mb-4">
                    <div className="px-4 py-3 border-b border-gray-100 dark:border-white/5 flex items-center space-x-2 bg-amber-50/50 dark:bg-amber-900/10">
                      <ShieldCheck className="w-4 h-4 text-amber-600" />
                      <span className="text-xs font-bold text-amber-700 dark:text-amber-500">Approval Request</span>
                    </div>
                    <div className="p-4">
                      <h4 className="text-sm font-bold text-gray-900 dark:text-white mb-1">Galaxy Z Fold 8 - Final BOM</h4>
                      <p className="text-xs text-gray-500 mb-4 leading-relaxed">Please review the updated bill of materials containing 145 items. Cost increased by 2.4%.</p>
                      
                      <div className="flex items-center space-x-2 p-2 bg-gray-50 dark:bg-white/5 rounded-xl border border-gray-100 dark:border-white/5 mb-4">
                        <FileSpreadsheet className="w-8 h-8 text-emerald-500 p-1.5 bg-white dark:bg-[#1A1A1A] rounded shadow-sm" />
                        <div className="flex-1 overflow-hidden">
                          <p className="text-xs font-semibold text-gray-700 dark:text-gray-300 truncate">BOM_Fold8_v2.xlsx</p>
                          <p className="text-[10px] text-gray-400">1.2 MB</p>
                        </div>
                      </div>

                      <div className="flex space-x-2">
                        <button className="flex-1 px-3 py-2 bg-gray-100 dark:bg-white/5 text-gray-700 dark:text-gray-300 rounded-xl text-xs font-semibold hover:bg-gray-200 dark:hover:bg-white/10 transition-colors">Reject</button>
                        <button className="flex-1 px-3 py-2 bg-[#1428A0] text-white rounded-xl text-xs font-semibold hover:bg-blue-700 shadow-md transition-colors">Approve</button>
                      </div>
                    </div>
                  </div>
                )}

                {messages.map((msg, idx) => (
                  <div key={idx} className={`flex group ${msg.isMe ? "justify-end" : "justify-start"}`}>
                    {!msg.isMe && (
                      <div className="w-7 h-7 rounded-full bg-gradient-to-br from-gray-400 to-gray-600 text-white flex items-center justify-center text-[10px] font-bold mr-2 mt-auto mb-1 flex-shrink-0">
                        {MOCK_USERS.find(u => u.id === msg.senderId)?.name.charAt(0) || 'U'}
                      </div>
                    )}
                    <div className={`relative max-w-[75%] rounded-2xl px-4 py-2.5 ${
                      msg.isMe 
                        ? "bg-[#1428A0] text-white rounded-br-sm shadow-md shadow-blue-500/10" 
                        : "bg-white dark:bg-[#1C1C1C] text-gray-800 dark:text-gray-200 rounded-bl-sm border border-gray-100 dark:border-white/5 shadow-sm"
                    }`}>
                      <div className="text-sm whitespace-pre-wrap leading-relaxed">
                        {msg.content}
                      </div>
                      
                      <div className={`text-[9px] mt-1.5 flex items-center justify-end space-x-1 font-medium ${msg.isMe ? "text-blue-200" : "text-gray-400"}`}>
                        <span>{new Date(msg.createdAt).toLocaleTimeString([], {hour: '2-digit', minute:'2-digit'})}</span>
                        {msg.isMe && (
                          <CheckCheck className={`w-3.5 h-3.5 ${msg.status === 'read' ? 'text-white' : 'text-blue-300'}`} />
                        )}
                      </div>
                    </div>
                  </div>
                ))}
                <div ref={messagesEndRef} />
              </div>

              {/* Quick Actions (Contextual) */}
              {activeThread.type !== 'Approvals' && (
                <div className="px-4 py-2 bg-white dark:bg-[#121212] flex space-x-2 overflow-x-auto custom-scrollbar shrink-0">
                  {['Summarize Thread', 'Share File', 'Create NCR'].map(act => (
                    <button key={act} className="flex-shrink-0 px-3 py-1.5 bg-white dark:bg-[#1A1A1A] border border-gray-200 dark:border-white/10 rounded-full text-[10px] font-bold text-[#1428A0] dark:text-blue-400 hover:bg-blue-50 dark:hover:bg-blue-900/20 transition-colors shadow-sm">
                      {act}
                    </button>
                  ))}
                </div>
              )}

              {/* Input Area */}
              <div className="p-4 bg-white dark:bg-[#121212] border-t border-gray-100 dark:border-white/5">
                <form onSubmit={sendMessage} className="flex items-end space-x-2 bg-gray-50 dark:bg-[#1A1A1A] border border-gray-200 dark:border-white/10 rounded-2xl p-1.5 focus-within:ring-2 focus-within:ring-blue-500/20 focus-within:border-[#1428A0] transition-all shadow-inner">
                  <button type="button" className="p-2 text-gray-400 hover:text-gray-700 dark:hover:text-gray-200 transition-colors flex-shrink-0 bg-white dark:bg-[#2A2A2A] rounded-xl shadow-sm border border-gray-100 dark:border-white/5">
                    <Plus className="w-5 h-5" />
                  </button>
                  <TextareaAutosize
                    value={inputMessage}
                    onChange={(e) => setInputMessage(e.target.value)}
                    onKeyDown={(e) => { if (e.key === 'Enter' && !e.shiftKey) { e.preventDefault(); sendMessage(e); } }}
                    placeholder="Message..."
                    className="w-full bg-transparent border-none focus:ring-0 resize-none max-h-32 text-sm text-gray-800 dark:text-gray-200 outline-none px-2 py-2.5 custom-scrollbar font-medium"
                    maxRows={5}
                  />
                  <div className="flex items-center space-x-1 p-1">
                    <button type="button" className="p-1.5 text-gray-400 hover:text-[#1428A0] transition-colors flex-shrink-0">
                      <Smile className="w-5 h-5" />
                    </button>
                    <button type="submit" disabled={!inputMessage.trim()} className="w-9 h-9 rounded-xl bg-[#1428A0] flex items-center justify-center text-white hover:bg-blue-700 transition-colors disabled:opacity-50 disabled:cursor-not-allowed flex-shrink-0 shadow-md">
                      <Send className="w-4 h-4 ml-0.5" />
                    </button>
                  </div>
                </form>
              </div>
            </>
          ) : (
            /* Empty State / Welcome Screen */
            <div className="flex-1 flex flex-col items-center justify-center p-8 text-center bg-gray-50/50 dark:bg-[#121212]">
              <div className="w-20 h-20 mb-6 bg-blue-50 dark:bg-blue-900/10 rounded-full flex items-center justify-center border-8 border-white dark:border-[#1A1A1A] shadow-sm relative">
                <div className="absolute inset-0 rounded-full bg-[#1428A0]/5 animate-ping"></div>
                <MessageSquare className="w-8 h-8 text-[#1428A0] dark:text-blue-500 relative z-10" />
              </div>
              <h3 className="text-xl font-black text-gray-900 dark:text-white mb-2">Collaboration Hub</h3>
              <p className="text-sm text-gray-500 max-w-sm font-medium leading-relaxed">
                Your intelligent workspace for discussions, approvals, and AI insights. Select a thread from the sidebar to begin.
              </p>
            </div>
          )}

        </div>
      </div>
    </div>
  );
}
