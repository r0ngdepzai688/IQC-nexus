"use client";

import { useState, useEffect, useRef, useMemo } from "react";
import { MessageCircle, X, Send, Paperclip, Smile, Search as SearchIcon, CheckCheck, Sparkles, Hash, Plus, MessageSquare, ChevronLeft, Maximize2, Minimize2, PanelRight, Check, Image as ImageIcon, Mic, FileText, AtSign, Video, Info, Trash2, UserPlus, UserMinus, ChevronDown, ChevronRight, Star } from "lucide-react";
import TextareaAutosize from "react-textarea-autosize";
import { motion, AnimatePresence } from "framer-motion";
import { usePathname } from "next/navigation";
import usersData from "@/data/users.json";

// --- PARSE USERS FROM JSON ---
const DB_USERS = (usersData as Record<string, string>[]).slice(1).map((u, idx) => ({
  id: u["Unnamed: 1"]?.toString() || `user_${idx}`,
  name: u["Unnamed: 2"] || "Unknown",
  department: u["Unnamed: 3"] || "Khác",
  org: u["Unnamed: 4"] || "",
  clName: u["Unnamed: 5"] || "",
  email: u["Unnamed: 6"] || "",
  position: u["Unnamed: 7"] || "",
  scope: u["Unnamed: 8"] || "",
  color: ["from-blue-500 to-indigo-500", "from-emerald-400 to-teal-500", "from-orange-400 to-rose-500", "from-purple-500 to-pink-500", "from-cyan-400 to-blue-500"][idx % 5]
}));

// --- CONSTANTS ---
const EMOJIS = ['👍','❤️','😂','😲','😢','🙏','🎉','🔥','😊','✅','❌','⚠️','✨','👀','💯','👌'];

// --- TYPES ---
type DisplayMode = 'floating' | 'expanded' | 'docked' | 'closed';
type SidebarSection = 'AI Assistant' | 'Notifications' | 'Approvals' | 'Pinned' | 'Recent' | 'Threads' | 'Direct';
type TabType = 'chats' | 'contacts';
type ChatFilter = 'All' | 'Unread' | 'Mentions' | 'Favorite';

interface Message {
  id: string;
  senderId: string;
  content: string;
  createdAt: string;
  isMe: boolean;
  status?: 'sent' | 'delivered' | 'read';
  attachments?: { type: 'image' | 'file'; url: string; name: string }[];
}

interface Thread {
  id: string;
  title: string;
  type: SidebarSection;
  isGroup: boolean;
  lastMessage?: string;
  updatedAt: string;
  unreadCount?: number;
  hasMention?: boolean;
  isPinned?: boolean;
  isFavorite?: boolean;
  members?: string[];
  color?: string;
}

const INITIAL_THREADS: Thread[] = [
  { id: 'ai_1', title: 'IQC Copilot', type: 'AI Assistant', isGroup: false, lastMessage: 'Báo cáo NCR đã sẵn sàng.', updatedAt: new Date().toISOString(), isPinned: true },
  { id: 'app_1', title: 'Phê duyệt BOM', type: 'Approvals', isGroup: true, lastMessage: 'Vui lòng kiểm duyệt BOM mới nhất.', updatedAt: new Date().toISOString(), unreadCount: 1, hasMention: true, members: [DB_USERS[0]?.id, DB_USERS[1]?.id] },
  { id: 'th_mock', title: 'Team QA 1P', type: 'Threads', isGroup: true, lastMessage: 'Đã nhận được file báo cáo', updatedAt: new Date(Date.now() - 3600000).toISOString(), unreadCount: 3, hasMention: false, color: 'from-purple-500 to-pink-500', members: [DB_USERS[2]?.id, DB_USERS[3]?.id, DB_USERS[4]?.id] }
];

export function FloatingChat() {
  const [displayMode, setDisplayMode] = useState<DisplayMode>('closed');
  const [activeTab, setActiveTab] = useState<TabType>('chats');
  const [chatFilter, setChatFilter] = useState<ChatFilter>('All');
  const [activeThread, setActiveThread] = useState<Thread | null>(null);
  const [searchQuery, setSearchQuery] = useState("");
  const [inputMessage, setInputMessage] = useState("");
  const [messages, setMessages] = useState<Message[]>([]);
  const [threads, setThreads] = useState<Thread[]>(INITIAL_THREADS);
  
  // Contacts View State
  const [collapsedDepts, setCollapsedDepts] = useState<Record<string, boolean>>({});

  // Modals & Menus
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [modalType, setModalType] = useState<'create' | 'add_member'>('create');
  const [modalSearch, setModalSearch] = useState("");
  const [selectedUsers, setSelectedUsers] = useState<string[]>([]);
  const [groupName, setGroupName] = useState("");

  // Input Menus
  const [isAttachmentMenuOpen, setIsAttachmentMenuOpen] = useState(false);
  const [isEmojiPickerOpen, setIsEmojiPickerOpen] = useState(false);
  
  // Thread Settings Menu
  const [isSettingsOpen, setIsSettingsOpen] = useState(false);
  const [editThreadName, setEditThreadName] = useState("");

  // Mention Menu
  const [mentionSearch, setMentionSearch] = useState<{ query: string, position: number } | null>(null);
  const textareaRef = useRef<HTMLTextAreaElement>(null);

  const pathname = usePathname(); // eslint-disable-line @typescript-eslint/no-unused-vars
  const messagesEndRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (messagesEndRef.current && activeThread) {
      messagesEndRef.current.scrollIntoView({ behavior: "smooth" });
    }
  }, [messages, activeThread, displayMode]);

  useEffect(() => {
    if (activeThread && activeThread.id.startsWith('th_')) {
      setTimeout(() => setMessages([
        { id: '1', senderId: 'system', content: 'Đã tạo cuộc trò chuyện.', createdAt: new Date().toISOString(), isMe: false },
      ]), 0);
    } else if (activeThread) {
      setTimeout(() => setMessages([
        { id: '1', senderId: 'user', content: 'Tin nhắn cũ mô phỏng...', createdAt: new Date(Date.now() - 86400000).toISOString(), isMe: false },
      ]), 0);
    }
    setTimeout(() => setIsSettingsOpen(false), 0); // close settings when switching threads
  }, [activeThread]);

  const toggleMode = /* eslint-disable-line @typescript-eslint/no-unused-vars */ (newMode: DisplayMode) => {
    setDisplayMode(displayMode === newMode ? 'closed' : newMode);
  };

  const handleSendMessage = (e: React.FormEvent) => {
    e.preventDefault();
    if (!inputMessage.trim() && !isAttachmentMenuOpen && !isEmojiPickerOpen) return; 

    setMessages(prev => [...prev, {
      id: Date.now().toString(),
      senderId: 'me',
      content: inputMessage,
      createdAt: new Date().toISOString(),
      isMe: true,
      status: 'sent'
    }]);
    
    setInputMessage("");
    setIsAttachmentMenuOpen(false);
    setIsEmojiPickerOpen(false);
    
    setTimeout(() => {
      setMessages(prev => {
        const newMsgs = [...prev];
        if (newMsgs.length > 0) newMsgs[newMsgs.length - 1].status = 'read';
        return newMsgs;
      });
    }, 1000);
  };

  // --- ACTIONS ---
  const handleCreateChat = () => {
    if (selectedUsers.length === 0) return;
    
    const isGroup = selectedUsers.length > 1;
    let title = groupName;
    let color = "from-blue-500 to-indigo-500";

    if (!isGroup) {
      const user = DB_USERS.find(u => u.id === selectedUsers[0]);
      title = user ? user.name : "Chat";
      color = user?.color || color;
    } else if (!title) {
      title = `Group (${selectedUsers.length})`;
    }

    const newThread: Thread = {
      id: `th_${Date.now()}`,
      title,
      type: isGroup ? 'Threads' : 'Direct',
      isGroup,
      members: selectedUsers,
      updatedAt: new Date().toISOString(),
      lastMessage: "Cuộc trò chuyện mới được tạo",
      color
    };

    setThreads([newThread, ...threads]);
    setIsModalOpen(false);
    setSelectedUsers([]);
    setGroupName("");
    setActiveTab('chats');
    setActiveThread(newThread);
  };

  const handleAddMembersToThread = () => {
    if (!activeThread || selectedUsers.length === 0) return;
    const updatedMembers = [...(activeThread.members || []), ...selectedUsers];
    
    setThreads(threads.map(t => t.id === activeThread.id ? { ...t, members: updatedMembers, isGroup: updatedMembers.length > 1 } : t));
    setActiveThread(prev => prev ? { ...prev, members: updatedMembers, isGroup: updatedMembers.length > 1 } : null);
    
    setIsModalOpen(false);
    setSelectedUsers([]);
  };

  const handleRenameThread = () => {
    if (!activeThread || !editThreadName.trim()) return;
    setThreads(threads.map(t => t.id === activeThread.id ? { ...t, title: editThreadName } : t));
    setActiveThread(prev => prev ? { ...prev, title: editThreadName } : null);
  };

  const handleRemoveMember = (userId: string) => {
    if (!activeThread) return;
    const updatedMembers = activeThread.members?.filter(m => m !== userId) || [];
    setThreads(threads.map(t => t.id === activeThread.id ? { ...t, members: updatedMembers } : t));
    setActiveThread(prev => prev ? { ...prev, members: updatedMembers } : null);
  };

  const handleClearChat = () => {
    if(confirm("Bạn có chắc chắn muốn xóa toàn bộ tin nhắn trong nhóm này?")) {
      setMessages([]);
    }
  };

  const handleDeleteThread = (e?: React.MouseEvent, threadId?: string) => {
    if(e) e.stopPropagation();
    const idToDelete = threadId || activeThread?.id;
    if(!idToDelete) return;

    if(confirm("Bạn có chắc chắn muốn xóa cuộc trò chuyện này khỏi danh sách?")) {
      setThreads(threads.filter(t => t.id !== idToDelete));
      if(activeThread?.id === idToDelete) {
        setActiveThread(null);
      }
    }
  };

  const handleToggleFavorite = (e?: React.MouseEvent, threadId?: string) => {
    if(e) e.stopPropagation();
    const idToToggle = threadId || activeThread?.id;
    if(!idToToggle) return;

    setThreads(threads.map(t => t.id === idToToggle ? { ...t, isFavorite: !t.isFavorite } : t));
    if(activeThread?.id === idToToggle) {
      setActiveThread(prev => prev ? { ...prev, isFavorite: !prev.isFavorite } : null);
    }
  };

  // --- FILTER & GROUPING DATA ---

  // Filter contacts
  const filteredContacts = useMemo(() => {
    if (!searchQuery) return DB_USERS;
    const lowerQ = searchQuery.toLowerCase();
    return DB_USERS.filter(u => u.name.toLowerCase().includes(lowerQ) || u.department.toLowerCase().includes(lowerQ));
  }, [searchQuery]);

  // Group contacts by department
  const groupedContacts = useMemo(() => {
    const groups: Record<string, typeof DB_USERS> = {};
    filteredContacts.forEach(u => {
      const dept = u.department || 'Khác';
      if (!groups[dept]) groups[dept] = [];
      groups[dept].push(u);
    });
    // Sort departments alphabetically
    return Object.keys(groups).sort().reduce((acc, key) => {
      acc[key] = groups[key];
      return acc;
    }, {} as Record<string, typeof DB_USERS>);
  }, [filteredContacts]);

  // Filter modal contacts
  const modalContacts = useMemo(() => {
    const existingMembers = modalType === 'add_member' && activeThread ? activeThread.members || [] : [];
    const available = DB_USERS.filter(u => !existingMembers.includes(u.id));
    
    if (!modalSearch) return available.slice(0, 50);
    const lowerQ = modalSearch.toLowerCase();
    return available.filter(u => u.name.toLowerCase().includes(lowerQ) || u.department.toLowerCase().includes(lowerQ)).slice(0, 50);
  }, [modalSearch, modalType, activeThread]);

  const mentionCandidates = useMemo(() => {
    if (!mentionSearch || !activeThread || !activeThread.isGroup) return [];
    const groupMembers = activeThread.members || [];
    const lowerQuery = mentionSearch.query.toLowerCase();
    return DB_USERS.filter(u => groupMembers.includes(u.id) && u.name.toLowerCase().includes(lowerQuery));
  }, [mentionSearch, activeThread]);

  const handleMentionSelect = (userName: string) => {
    if (!mentionSearch || !textareaRef.current) return;
    const text = inputMessage;
    const start = mentionSearch.position;
    const end = textareaRef.current.selectionStart;
    const newText = text.slice(0, start) + `@${userName} ` + text.slice(end);
    setInputMessage(newText);
    setMentionSearch(null);
    setTimeout(() => {
      textareaRef.current?.focus();
    }, 0);
  };

  // Filter chats based on search and chatFilter
  const filteredThreads = useMemo(() => {
    let result = threads;
    if (searchQuery) {
      const lowerQ = searchQuery.toLowerCase();
      result = result.filter(t => t.title.toLowerCase().includes(lowerQ));
    }
    if (chatFilter === 'Unread') {
      result = result.filter(t => (t.unreadCount || 0) > 0);
    } else if (chatFilter === 'Mentions') {
      result = result.filter(t => t.hasMention);
    } else if (chatFilter === 'Favorite') {
      result = result.filter(t => t.isFavorite);
    }
    return result;
  }, [threads, searchQuery, chatFilter]);

  if (displayMode === 'closed') {
    return (
      <button
        onClick={() => setDisplayMode('floating')}
        className="fixed bottom-6 right-6 z-[100] w-14 h-14 rounded-2xl bg-gradient-to-tr from-primary to-indigo-600 text-white shadow-[0_8px_30px_rgb(20,40,160,0.3)] hover:shadow-[0_12px_40px_rgb(20,40,160,0.4)] hover:-translate-y-1 flex items-center justify-center transition-all duration-300 group"
      >
        <MessageCircle className="w-6 h-6 group-hover:scale-110 transition-transform" />
        <span className="absolute -top-1 -right-1 w-3.5 h-3.5 bg-rose-500 border-2 border-white dark:border-[#0A0A0A] rounded-full animate-pulse"></span>
      </button>
    );
  }

  return (
    <div className={`fixed z-[100] flex transition-all duration-500 ease-[cubic-bezier(0.2,0,0,1)] ${
      displayMode === 'docked' 
        ? 'top-0 right-0 h-screen w-[400px] border-l border-border shadow-2xl' 
        : displayMode === 'expanded'
        ? 'top-[10vh] left-[15vw] right-[15vw] bottom-[10vh] rounded-3xl shadow-[0_20px_60px_rgb(0,0,0,0.15)] border border-border'
        : 'bottom-6 right-6 w-[380px] h-[600px] rounded-3xl shadow-[0_12px_40px_rgb(0,0,0,0.12)] border border-border'
    }`}>
      
      <div className="bg-white dark:bg-card w-full h-full flex overflow-hidden rounded-inherit relative">
        
        {/* ================= SIDEBAR ================= */}
        <div className={`flex-col border-r border-border bg-muted dark:bg-background ${
          displayMode === 'floating' && activeThread ? 'hidden' : 'flex w-[300px] flex-shrink-0'
        } ${displayMode === 'floating' && !activeThread ? 'w-full' : ''}`}>
          
          {/* Header */}
          <div className="px-4 py-3 flex items-center justify-between border-b border-border">
            <h2 className="font-bold text-gray-900 dark:text-white tracking-tight">Collaboration Hub</h2>
            <div className="flex space-x-1">
              <button onClick={() => {setModalType('create'); setIsModalOpen(true);}} className="p-1.5 text-primary hover:bg-blue-50 dark:text-blue-400 dark:hover:bg-white/10 rounded-lg transition-colors" title="New Chat">
                <Plus className="w-4 h-4" />
              </button>
              {displayMode !== 'docked' && (
                <button onClick={() => setDisplayMode('docked')} className="p-1.5 text-gray-400 hover:text-gray-900 hover:bg-gray-200 dark:hover:text-white dark:hover:bg-white/10 rounded-lg transition-colors">
                  <PanelRight className="w-4 h-4" />
                </button>
              )}
              {displayMode !== 'expanded' && (
                <button onClick={() => setDisplayMode('expanded')} className="p-1.5 text-gray-400 hover:text-gray-900 hover:bg-gray-200 dark:hover:text-white dark:hover:bg-white/10 rounded-lg transition-colors">
                  <Maximize2 className="w-4 h-4" />
                </button>
              )}
              {displayMode !== 'floating' && (
                <button onClick={() => setDisplayMode('floating')} className="p-1.5 text-gray-400 hover:text-gray-900 hover:bg-gray-200 dark:hover:text-white dark:hover:bg-white/10 rounded-lg transition-colors">
                  <Minimize2 className="w-4 h-4" />
                </button>
              )}
              <button onClick={() => setDisplayMode('closed')} className="p-1.5 text-gray-400 hover:text-rose-500 hover:bg-rose-50 dark:hover:bg-rose-500/10 rounded-lg transition-colors ml-1">
                <X className="w-4 h-4" />
              </button>
            </div>
          </div>

          {/* TABS */}
          <div className="flex p-2 bg-white dark:bg-card border-b border-border">
            <button 
              onClick={() => setActiveTab('chats')} 
              className={`flex-1 py-1.5 text-xs font-bold rounded-lg transition-colors ${activeTab === 'chats' ? 'bg-gray-100 dark:bg-white/10 text-gray-900 dark:text-white' : 'text-gray-500 hover:bg-gray-50 dark:hover:bg-white/5'}`}
            >
              Chats
            </button>
            <button 
              onClick={() => setActiveTab('contacts')} 
              className={`flex-1 py-1.5 text-xs font-bold rounded-lg transition-colors ${activeTab === 'contacts' ? 'bg-gray-100 dark:bg-white/10 text-gray-900 dark:text-white' : 'text-gray-500 hover:bg-gray-50 dark:hover:bg-white/5'}`}
            >
              Contacts
            </button>
          </div>

          {/* Search */}
          <div className="px-4 py-2 mt-2">
            <div className="relative">
              <SearchIcon className="w-4 h-4 absolute left-3 top-2.5 text-gray-400" />
              <input 
                type="text" 
                placeholder={activeTab === 'chats' ? "Search chats..." : "Search contacts..."} 
                value={searchQuery}
                onChange={e => setSearchQuery(e.target.value)}
                className="w-full bg-white dark:bg-popover border border-border rounded-xl pl-9 pr-3 py-2 text-xs font-medium focus:outline-none focus:border-primary transition-colors shadow-sm"
              />
            </div>
            
            {/* Quick Filters - Only for Chats Tab */}
            {activeTab === 'chats' && (
              <div className="flex space-x-1.5 mt-3 overflow-x-auto custom-scrollbar pb-1">
                {(['All', 'Unread', 'Mentions', 'Favorite'] as ChatFilter[]).map(f => (
                  <button 
                    key={f} 
                    onClick={() => setChatFilter(f)}
                    className={`px-3 py-1 border rounded-full text-[10px] font-bold transition-colors whitespace-nowrap shadow-sm
                      ${chatFilter === f 
                        ? 'bg-primary text-white border-primary dark:bg-blue-600 dark:border-blue-600' 
                        : 'bg-white dark:bg-popover text-gray-600 dark:text-gray-300 border-border hover:text-primary hover:border-primary'}
                    `}
                  >
                    {f}
                  </button>
                ))}
              </div>
            )}
          </div>

          {/* List Content */}
          <div className="flex-1 overflow-y-auto custom-scrollbar px-2 space-y-4 pt-2 pb-4">
            
            {activeTab === 'chats' && (
              <>
                {filteredThreads.length === 0 ? (
                  <div className="text-center py-10 text-xs text-gray-400 font-medium">
                    Không tìm thấy cuộc trò chuyện nào.
                  </div>
                ) : (
                  <div>
                    <h3 className="px-3 text-[10px] font-bold text-gray-400 uppercase tracking-wider mb-2">Recent</h3>
                    {filteredThreads.map(thread => (
                      <div key={thread.id} className="relative group/thread mb-1">
                        <button 
                          onClick={() => setActiveThread(thread)}
                          className={`w-full flex items-center p-2.5 rounded-xl transition-colors text-left pr-10 ${activeThread?.id === thread.id ? 'bg-white dark:bg-white/10 shadow-sm border border-border' : 'hover:bg-gray-100 dark:hover:bg-white/5 border border-transparent'}`}
                        >
                          <div className="relative">
                            <div className={`w-9 h-9 rounded-xl flex items-center justify-center font-bold text-white shadow-sm bg-gradient-to-br ${thread.color || 'from-amber-400 to-orange-500'}`}>
                              {thread.type === 'AI Assistant' ? <Sparkles className="w-4 h-4" /> : thread.isGroup ? <Hash className="w-4 h-4" /> : thread.title.charAt(0)}
                            </div>
                            {thread.hasMention && (
                              <div className="absolute -bottom-1 -right-1 w-4 h-4 bg-indigo-500 text-white rounded-full flex items-center justify-center border border-white dark:border-[#0A0A0A]">
                                <AtSign className="w-2.5 h-2.5" />
                              </div>
                            )}
                          </div>
                          <div className="ml-3 flex-1 overflow-hidden">
                            <div className="flex items-center justify-between">
                              <div className="flex items-center space-x-1 overflow-hidden">
                                <h4 className={`font-semibold text-sm truncate ${thread.unreadCount ? 'text-gray-900 dark:text-white' : 'text-gray-700 dark:text-gray-300'}`}>{thread.title}</h4>
                                {thread.isFavorite && <Star className="w-3 h-3 text-amber-500 fill-current shrink-0" />}
                              </div>
                            </div>
                            <p className={`text-xs truncate mt-0.5 ${thread.unreadCount ? 'text-gray-900 dark:text-white font-semibold' : 'text-gray-500'}`}>{thread.lastMessage}</p>
                          </div>
                          {thread.unreadCount && (
                            <div className="w-4 h-4 rounded-full bg-rose-500 text-white flex items-center justify-center text-[9px] font-bold shadow-sm ml-2 shrink-0">{thread.unreadCount}</div>
                          )}
                        </button>
                        <div className="absolute right-2 top-1/2 -translate-y-1/2 flex items-center space-x-0.5 opacity-0 group-hover/thread:opacity-100 transition-all z-10 bg-white/80 dark:bg-popover/80 backdrop-blur-sm rounded-lg p-0.5 shadow-sm border border-border">
                          <button 
                            onClick={(e) => handleToggleFavorite(e, thread.id)}
                            className={`p-1.5 rounded-md transition-colors ${thread.isFavorite ? 'text-amber-500 hover:bg-amber-50 dark:hover:bg-amber-500/10' : 'text-gray-400 hover:text-amber-500 hover:bg-amber-50 dark:hover:bg-amber-500/10'}`}
                            title={thread.isFavorite ? "Remove from Favorites" : "Add to Favorites"}
                          >
                            <Star className={`w-3.5 h-3.5 ${thread.isFavorite ? 'fill-current' : ''}`} />
                          </button>
                          <button 
                            onClick={(e) => handleDeleteThread(e, thread.id)}
                            className="p-1.5 text-gray-400 hover:text-rose-500 hover:bg-rose-50 dark:hover:bg-rose-500/10 rounded-md transition-colors"
                            title="Delete Chat"
                          >
                            <Trash2 className="w-3.5 h-3.5" />
                          </button>
                        </div>
                      </div>
                    ))}
                  </div>
                )}
              </>
            )}

            {activeTab === 'contacts' && (
              <div className="pb-4">
                {Object.keys(groupedContacts).map(dept => {
                  const isCollapsed = collapsedDepts[dept];
                  const deptUsers = groupedContacts[dept];
                  
                  return (
                    <div key={dept} className="mb-2">
                      <button 
                        onClick={() => setCollapsedDepts(prev => ({...prev, [dept]: !isCollapsed}))}
                        className="w-full flex items-center justify-between px-3 py-1.5 hover:bg-gray-50 dark:hover:bg-white/5 rounded-lg transition-colors group"
                      >
                        <span className="text-[11px] font-bold text-gray-500 dark:text-gray-400 uppercase tracking-wider">{dept} ({deptUsers.length})</span>
                        {isCollapsed ? <ChevronRight className="w-3.5 h-3.5 text-gray-400" /> : <ChevronDown className="w-3.5 h-3.5 text-gray-400" />}
                      </button>
                      
                      {!isCollapsed && (
                        <div className="mt-1 space-y-1">
                          {deptUsers.slice(0, 20).map(user => (
                            <div key={user.id} className="w-full flex items-center p-2 rounded-xl hover:bg-gray-100 dark:hover:bg-white/5 transition-colors group/item">
                              <div className={`w-8 h-8 rounded-full flex items-center justify-center font-bold text-white shadow-sm bg-gradient-to-br ${user.color}`}>
                                {user.name.charAt(0)}
                              </div>
                              <div className="ml-3 flex-1 overflow-hidden flex flex-col justify-center">
                                <h4 className="font-semibold text-sm text-gray-900 dark:text-white truncate leading-tight">{user.name}</h4>
                                <p className="text-[10px] text-gray-500 truncate">{user.position || 'Nhân viên'}</p>
                              </div>
                              <button 
                                onClick={() => {
                                  setSelectedUsers([user.id]);
                                  setModalType('create');
                                  setIsModalOpen(true);
                                }} 
                                className="opacity-0 group-hover/item:opacity-100 p-1.5 bg-blue-50 dark:bg-blue-900/20 text-primary dark:text-blue-400 rounded-lg transition-all hover:scale-105"
                                title="Start Chat"
                              >
                                <MessageSquare className="w-3.5 h-3.5" />
                              </button>
                            </div>
                          ))}
                          {deptUsers.length > 20 && (
                            <div className="px-3 py-1 text-[10px] text-gray-400 font-medium italic">
                              + {deptUsers.length - 20} more... (Search to find)
                            </div>
                          )}
                        </div>
                      )}
                    </div>
                  );
                })}
              </div>
            )}
          </div>
        </div>

        {/* ================= CHAT AREA ================= */}
        <div className={`flex-1 flex flex-col relative bg-white dark:bg-card ${
          displayMode === 'floating' && !activeThread ? 'hidden' : 'flex'
        }`}>
          
          {activeThread ? (
            <>
              {/* Thread Header */}
              <div className="px-4 md:px-5 py-3 border-b border-border flex items-center justify-between bg-white dark:bg-card/90 backdrop-blur-md sticky top-0 z-10">
                <div className="flex items-center space-x-3 overflow-hidden">
                  {displayMode === 'floating' && (
                    <button onClick={() => setActiveThread(null)} className="p-1.5 hover:bg-gray-100 dark:hover:bg-white/5 rounded-lg transition-colors flex-shrink-0 text-gray-500 mr-1">
                      <ChevronLeft className="w-5 h-5" />
                    </button>
                  )}
                  <div className={`w-9 h-9 rounded-full flex items-center justify-center font-bold text-white shadow-sm flex-shrink-0 bg-gradient-to-br ${activeThread.color || 'from-primary to-blue-500'}`}>
                    {activeThread.type === 'AI Assistant' ? <Sparkles className="w-4 h-4" /> : activeThread.isGroup ? <Hash className="w-4 h-4" /> : activeThread.title.charAt(0)}
                  </div>
                  <div className="truncate">
                    <h3 className="font-bold text-gray-900 dark:text-white text-sm truncate">{activeThread.title}</h3>
                    <div className="flex items-center text-[10px] text-gray-500 font-medium">
                      {activeThread.type === 'AI Assistant' ? 'Enterprise Intelligence' : activeThread.isGroup ? (
                        <><span className="w-1.5 h-1.5 rounded-full bg-emerald-500 mr-1.5"></span> {activeThread.members?.length || 0} Members</>
                      ) : (
                        <><span className="w-1.5 h-1.5 rounded-full bg-emerald-500 mr-1.5"></span> Active now</>
                      )}
                    </div>
                  </div>
                </div>
                <div className="flex items-center space-x-1">
                  <button className="p-2 text-gray-400 hover:text-primary dark:hover:text-blue-400 hover:bg-blue-50 dark:hover:bg-blue-900/20 rounded-lg transition-colors">
                    <Video className="w-4 h-4" />
                  </button>
                  <button onClick={() => {setIsSettingsOpen(!isSettingsOpen); setEditThreadName(activeThread.title);}} className={`p-2 rounded-lg transition-colors ${isSettingsOpen ? 'bg-blue-50 text-primary dark:bg-white/10 dark:text-white' : 'text-gray-400 hover:text-gray-900 dark:hover:text-white hover:bg-gray-100 dark:hover:bg-white/10'}`}>
                    <Info className="w-4 h-4" />
                  </button>
                </div>
              </div>

              {/* Chat Layout: Messages + Settings Sidebar */}
              <div className="flex-1 flex overflow-hidden relative">
                
                {/* Messages Container */}
                <div className="flex-1 overflow-y-auto p-4 md:p-5 space-y-5 custom-scrollbar bg-muted/50 dark:bg-background/50 bg-[url('https://www.transparenttextures.com/patterns/cubes.png')] bg-fixed dark:opacity-10">
                  <div className="text-center text-xs font-semibold text-gray-400 my-4 uppercase tracking-wider">Beginning of Thread</div>
                  
                  {messages.map((msg, idx) => (
                    <div key={idx} className={`flex group ${msg.isMe ? "justify-end" : "justify-start"}`}>
                      {!msg.isMe && (
                        <div className="w-8 h-8 rounded-full bg-gradient-to-br from-gray-400 to-gray-600 text-white flex items-center justify-center text-[11px] font-bold mr-2 mt-auto mb-1 flex-shrink-0 shadow-sm">
                          S
                        </div>
                      )}
                      <div className={`relative max-w-[80%] rounded-[1.25rem] px-4 py-2.5 ${
                        msg.isMe 
                          ? "bg-primary text-white rounded-br-sm shadow-md shadow-blue-500/10" 
                          : "bg-white dark:bg-[#1C1C1C] text-gray-800 dark:text-gray-200 rounded-bl-sm border border-border shadow-sm"
                      }`}>
                        <div className="text-[13px] whitespace-pre-wrap leading-relaxed font-medium">
                          {msg.content}
                        </div>
                        <div className={`text-[9px] mt-1.5 flex items-center justify-end space-x-1 font-semibold ${msg.isMe ? "text-blue-200" : "text-gray-400"}`}>
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

                {/* Settings Panel */}
                <AnimatePresence>
                  {isSettingsOpen && (
                    <motion.div 
                      initial={{ x: "100%", opacity: 0 }}
                      animate={{ x: 0, opacity: 1 }}
                      exit={{ x: "100%", opacity: 0 }}
                      transition={{ type: "spring", damping: 25, stiffness: 250 }}
                      className="absolute inset-0 bg-white/95 dark:bg-card/95 backdrop-blur-md overflow-y-auto custom-scrollbar z-30 flex flex-col shadow-[-10px_0_30px_rgba(0,0,0,0.05)]"
                    >
                      <div className="p-4 md:p-5 w-full h-full flex flex-col">
                        <div className="flex items-center justify-between mb-5 border-b border-border pb-3">
                          <h4 className="font-bold text-sm text-gray-900 dark:text-white">Chat Settings</h4>
                          <button onClick={() => setIsSettingsOpen(false)} className="p-1.5 text-gray-400 hover:text-gray-900 dark:hover:text-white bg-gray-50 dark:bg-white/5 hover:bg-gray-100 dark:hover:bg-white/10 rounded-lg transition-colors">
                            <X className="w-4 h-4" />
                          </button>
                        </div>
                        
                        {/* Rename */}
                        <div className="mb-6">
                          <label className="text-[10px] font-bold text-gray-500 uppercase tracking-wider mb-2 block">Group Name</label>
                          <div className="flex space-x-2">
                            <input 
                              type="text" 
                              value={editThreadName}
                              onChange={e => setEditThreadName(e.target.value)}
                              className="flex-1 bg-gray-50 dark:bg-popover border border-border rounded-xl px-3 py-2 text-sm font-medium focus:outline-none focus:border-primary transition-colors"
                            />
                            <button onClick={handleRenameThread} className="p-2.5 bg-primary text-white rounded-xl hover:bg-blue-700 shadow-md shadow-blue-500/20 transition-all">
                              <Check className="w-4 h-4"/>
                            </button>
                          </div>
                        </div>

                        {/* Members */}
                        <div className="mb-6 flex-1 flex flex-col min-h-0">
                          <div className="flex items-center justify-between mb-3">
                            <label className="text-[10px] font-bold text-gray-500 uppercase tracking-wider">Members ({activeThread.members?.length || 0})</label>
                            <button 
                              onClick={() => { setModalType('add_member'); setIsModalOpen(true); }}
                              className="text-primary dark:text-blue-400 hover:bg-blue-50 dark:hover:bg-blue-900/20 p-1.5 rounded-lg transition-colors"
                            >
                              <UserPlus className="w-4 h-4" />
                            </button>
                          </div>
                          <div className="space-y-2 flex-1 overflow-y-auto custom-scrollbar pr-1">
                            {activeThread.members?.map(memberId => {
                              const u = DB_USERS.find(user => user.id === memberId);
                              if (!u) return null;
                              return (
                                <div key={u.id} className="flex items-center justify-between group p-1.5 rounded-xl hover:bg-gray-50 dark:hover:bg-white/5 transition-colors">
                                  <div className="flex items-center space-x-3 overflow-hidden">
                                    <div className={`w-8 h-8 rounded-full flex items-center justify-center text-xs font-bold text-white shadow-sm bg-gradient-to-br ${u.color}`}>{u.name.charAt(0)}</div>
                                    <div className="flex flex-col truncate">
                                      <span className="text-sm font-bold text-gray-900 dark:text-white truncate">{u.name}</span>
                                      <span className="text-[10px] text-gray-500 truncate font-medium">{u.department}</span>
                                    </div>
                                  </div>
                                  <button onClick={() => handleRemoveMember(u.id)} className="opacity-0 group-hover:opacity-100 p-2 text-gray-400 hover:bg-rose-50 hover:text-rose-500 rounded-lg transition-all" title="Remove member">
                                    <UserMinus className="w-4 h-4"/>
                                  </button>
                                </div>
                              );
                            })}
                          </div>
                        </div>

                        {/* Actions */}
                        <div className="pt-4 border-t border-border mt-auto shrink-0 space-y-2">
                          <button onClick={handleClearChat} className="w-full flex items-center justify-center space-x-2 py-3 rounded-xl bg-orange-50 dark:bg-orange-500/10 text-orange-600 dark:text-orange-400 hover:bg-orange-100 dark:hover:bg-orange-500/20 transition-colors text-sm font-bold">
                            <Trash2 className="w-4 h-4" />
                            <span>Clear Chat History</span>
                          </button>
                          <button onClick={() => handleDeleteThread()} className="w-full flex items-center justify-center space-x-2 py-3 rounded-xl bg-rose-50 dark:bg-rose-500/10 text-rose-600 dark:text-rose-400 hover:bg-rose-100 dark:hover:bg-rose-500/20 transition-colors text-sm font-bold">
                            <Trash2 className="w-4 h-4" />
                            <span>Delete Chat</span>
                          </button>
                        </div>
                      </div>
                    </motion.div>
                  )}
                </AnimatePresence>
              </div>

              {/* Modern Input Area */}
              <div className="p-3 md:p-4 bg-white dark:bg-card border-t border-border relative z-20">
                {/* Floating Attachments Menu */}
                <AnimatePresence>
                  {isAttachmentMenuOpen && (
                    <motion.div 
                      initial={{ opacity: 0, y: 10, scale: 0.95 }}
                      animate={{ opacity: 1, y: 0, scale: 1 }}
                      exit={{ opacity: 0, y: 10, scale: 0.95 }}
                      className="absolute bottom-full left-4 mb-2 bg-white dark:bg-popover border border-border shadow-xl rounded-2xl p-2 flex space-x-1 z-50"
                    >
                      <button className="p-3 hover:bg-gray-100 dark:hover:bg-white/10 rounded-xl flex flex-col items-center justify-center transition-colors group">
                        <div className="w-10 h-10 rounded-full bg-blue-50 dark:bg-blue-900/20 text-primary dark:text-blue-400 flex items-center justify-center mb-1 group-hover:scale-110 transition-transform"><ImageIcon className="w-5 h-5" /></div>
                        <span className="text-[9px] font-bold text-gray-600 dark:text-gray-400">Photos</span>
                      </button>
                      <button className="p-3 hover:bg-gray-100 dark:hover:bg-white/10 rounded-xl flex flex-col items-center justify-center transition-colors group">
                        <div className="w-10 h-10 rounded-full bg-purple-50 dark:bg-purple-900/20 text-purple-600 dark:text-purple-400 flex items-center justify-center mb-1 group-hover:scale-110 transition-transform"><FileText className="w-5 h-5" /></div>
                        <span className="text-[9px] font-bold text-gray-600 dark:text-gray-400">Document</span>
                      </button>
                      <button className="p-3 hover:bg-gray-100 dark:hover:bg-white/10 rounded-xl flex flex-col items-center justify-center transition-colors group">
                        <div className="w-10 h-10 rounded-full bg-amber-50 dark:bg-amber-900/20 text-amber-600 dark:text-amber-400 flex items-center justify-center mb-1 group-hover:scale-110 transition-transform"><Paperclip className="w-5 h-5" /></div>
                        <span className="text-[9px] font-bold text-gray-600 dark:text-gray-400">File</span>
                      </button>
                    </motion.div>
                  )}
                </AnimatePresence>

                {/* Emoji Picker Popup */}
                <AnimatePresence>
                  {isEmojiPickerOpen && (
                    <motion.div 
                      initial={{ opacity: 0, y: 10, scale: 0.95 }}
                      animate={{ opacity: 1, y: 0, scale: 1 }}
                      exit={{ opacity: 0, y: 10, scale: 0.95 }}
                      className="absolute bottom-full right-16 mb-2 bg-white dark:bg-popover border border-border shadow-xl rounded-2xl p-3 z-50 w-[240px]"
                    >
                      <div className="grid grid-cols-4 gap-2">
                        {EMOJIS.map(emoji => (
                          <button 
                            key={emoji}
                            type="button"
                            onClick={() => { setInputMessage(prev => prev + emoji); setIsEmojiPickerOpen(false); }}
                            className="w-10 h-10 text-xl flex items-center justify-center hover:bg-gray-100 dark:hover:bg-white/10 rounded-xl transition-colors hover:scale-110"
                          >
                            {emoji}
                          </button>
                        ))}
                      </div>
                    </motion.div>
                  )}
                </AnimatePresence>

                <AnimatePresence>
                  {mentionSearch && mentionCandidates.length > 0 && (
                    <motion.div 
                      initial={{ opacity: 0, y: 10, scale: 0.95 }}
                      animate={{ opacity: 1, y: 0, scale: 1 }}
                      exit={{ opacity: 0, y: 10, scale: 0.95 }}
                      className="absolute bottom-full left-4 mb-2 w-64 bg-white dark:bg-popover border border-border shadow-xl rounded-2xl py-2 z-50 max-h-48 overflow-y-auto custom-scrollbar"
                    >
                      <div className="px-3 pb-2 mb-1 text-[10px] font-bold text-gray-500 uppercase tracking-wider border-b border-border">
                        Mentions ({mentionCandidates.length})
                      </div>
                      {mentionCandidates.map(u => (
                        <button
                          key={u.id}
                          type="button"
                          onClick={() => handleMentionSelect(u.name)}
                          className="w-full text-left px-3 py-1.5 hover:bg-gray-100 dark:hover:bg-white/10 flex items-center space-x-3 transition-colors group"
                        >
                          <div className={`w-7 h-7 rounded-full flex items-center justify-center text-[10px] font-bold text-white shadow-sm bg-gradient-to-br ${u.color}`}>{u.name.charAt(0)}</div>
                          <div className="flex flex-col overflow-hidden">
                            <span className="text-sm font-semibold text-gray-900 dark:text-white truncate">{u.name}</span>
                            <span className="text-[10px] text-gray-500 truncate">{u.department}</span>
                          </div>
                        </button>
                      ))}
                    </motion.div>
                  )}
                </AnimatePresence>

                <form onSubmit={handleSendMessage} className="flex items-end space-x-2 bg-gray-50 dark:bg-popover border border-border rounded-3xl p-1.5 focus-within:ring-2 focus-within:ring-blue-500/20 focus-within:border-primary transition-all shadow-sm">
                  
                  <button 
                    type="button" 
                    onClick={() => {setIsAttachmentMenuOpen(!isAttachmentMenuOpen); setIsEmojiPickerOpen(false);}}
                    className={`p-2.5 rounded-full transition-colors flex-shrink-0 ml-1 ${isAttachmentMenuOpen ? 'bg-primary text-white rotate-45' : 'text-gray-500 hover:text-gray-900 dark:hover:text-white bg-white dark:bg-[#2A2A2A] shadow-sm border border-border'}`}
                  >
                    <Plus className="w-5 h-5" />
                  </button>

                  <TextareaAutosize
                    ref={textareaRef}
                    value={inputMessage}
                    onChange={(e) => {
                      const val = e.target.value;
                      setInputMessage(val);
                      const cursor = e.target.selectionStart;
                      const textBeforeCursor = val.slice(0, cursor);
                      const match = /(?:^|\s)@([^@\s]*)$/.exec(textBeforeCursor);
                      if (match) {
                        const atIndex = match.index + (match[0].startsWith(' ') || match[0].startsWith('\n') ? 1 : 0);
                        setMentionSearch({ query: match[1], position: atIndex });
                      } else {
                        setMentionSearch(null);
                      }
                    }}
                    onKeyDown={(e) => { if (e.key === 'Enter' && !e.shiftKey) { e.preventDefault(); handleSendMessage(e); } }}
                    placeholder="Type a message..."
                    className="w-full bg-transparent border-none focus:ring-0 resize-none max-h-32 text-sm text-gray-800 dark:text-gray-200 outline-none px-3 py-3 custom-scrollbar font-medium placeholder:text-gray-400"
                    maxRows={5}
                  />

                  <div className="flex items-center space-x-1 p-1">
                    <button 
                      type="button" 
                      onClick={() => {setIsEmojiPickerOpen(!isEmojiPickerOpen); setIsAttachmentMenuOpen(false);}}
                      className={`p-2 transition-colors flex-shrink-0 hidden sm:block rounded-full ${isEmojiPickerOpen ? 'bg-amber-100 text-amber-600 dark:bg-amber-500/20 dark:text-amber-400' : 'text-gray-400 hover:text-amber-500'}`}
                    >
                      <Smile className="w-5 h-5" />
                    </button>
                    {!inputMessage.trim() ? (
                      <button type="button" className="w-10 h-10 rounded-full text-gray-400 hover:bg-gray-200 dark:hover:bg-white/10 hover:text-gray-700 dark:hover:text-white transition-colors flex items-center justify-center flex-shrink-0">
                        <Mic className="w-5 h-5" />
                      </button>
                    ) : (
                      <button type="submit" className="w-10 h-10 rounded-full bg-gradient-to-r from-primary to-blue-600 flex items-center justify-center text-white hover:shadow-lg hover:shadow-blue-500/30 transition-all flex-shrink-0 hover:scale-105">
                        <Send className="w-4 h-4 ml-0.5" />
                      </button>
                    )}
                  </div>
                </form>
              </div>
            </>
          ) : (
            <div className="flex-1 flex flex-col items-center justify-center p-8 text-center bg-gray-50/50 dark:bg-card">
              <div className="w-24 h-24 mb-6 bg-blue-50 dark:bg-blue-900/10 rounded-full flex items-center justify-center border-8 border-white dark:border-[#1A1A1A] shadow-sm relative">
                <div className="absolute inset-0 rounded-full bg-primary/5 animate-ping"></div>
                <MessageSquare className="w-10 h-10 text-primary dark:text-blue-500 relative z-10" />
              </div>
              <h3 className="text-xl font-black text-gray-900 dark:text-white mb-2 tracking-tight">Enterprise Messaging</h3>
              <p className="text-sm text-gray-500 max-w-sm font-medium leading-relaxed">
                Your intelligent workspace for cross-functional discussions. Select a thread or create a new chat to begin.
              </p>
            </div>
          )}

        </div>

        {/* ================= MODALS ================= */}
        <AnimatePresence>
          {isModalOpen && (
            <motion.div 
              initial={{ opacity: 0 }} 
              animate={{ opacity: 1 }} 
              exit={{ opacity: 0 }}
              className="absolute inset-0 bg-white/95 dark:bg-card/95 backdrop-blur-md z-50 flex flex-col"
            >
              <div className="px-4 py-3 border-b border-border flex items-center justify-between bg-white/50 dark:bg-black/20">
                <h3 className="font-bold text-gray-900 dark:text-white">
                  {modalType === 'create' ? 'New Chat' : 'Add Members'}
                </h3>
                <button onClick={() => { setIsModalOpen(false); setSelectedUsers([]); setGroupName(""); }} className="p-1.5 text-gray-400 hover:text-rose-500 rounded-lg transition-colors">
                  <X className="w-5 h-5" />
                </button>
              </div>
              
              <div className="p-4 border-b border-border">
                <div className="relative">
                  <SearchIcon className="w-4 h-4 absolute left-3 top-3 text-gray-400" />
                  <input 
                    type="text" 
                    placeholder="Search personnel by name or department..." 
                    value={modalSearch}
                    onChange={e => setModalSearch(e.target.value)}
                    className="w-full bg-gray-50 dark:bg-popover border border-border rounded-xl pl-10 pr-4 py-2.5 text-sm font-medium focus:outline-none focus:border-primary transition-colors"
                  />
                </div>
                
                {modalType === 'create' && selectedUsers.length > 1 && (
                  <motion.input 
                    initial={{ opacity: 0, height: 0 }}
                    animate={{ opacity: 1, height: 'auto' }}
                    type="text" 
                    placeholder="Enter group name (optional)..." 
                    value={groupName}
                    onChange={e => setGroupName(e.target.value)}
                    className="w-full bg-white dark:bg-secondary border border-border shadow-sm rounded-xl px-4 py-2.5 text-sm font-bold focus:outline-none focus:border-primary focus:ring-2 focus:ring-blue-500/20 transition-colors mt-3"
                  />
                )}
              </div>

              <div className="flex-1 overflow-y-auto custom-scrollbar p-2">
                <div className="text-xs font-bold text-gray-400 px-3 pt-1 pb-2 uppercase tracking-wider">Suggested Contacts</div>
                {modalContacts.map(user => (
                  <button 
                    key={user.id}
                    onClick={() => {
                      if (selectedUsers.includes(user.id)) {
                        setSelectedUsers(prev => prev.filter(id => id !== user.id));
                      } else {
                        setSelectedUsers(prev => [...prev, user.id]);
                      }
                    }}
                    className={`w-full flex items-center p-2.5 rounded-xl transition-colors mb-1 text-left ${selectedUsers.includes(user.id) ? 'bg-blue-50/50 dark:bg-blue-900/10' : 'hover:bg-gray-50 dark:hover:bg-white/5'}`}
                  >
                    <div className="relative">
                      <div className={`w-10 h-10 rounded-full flex items-center justify-center font-bold text-white shadow-sm bg-gradient-to-br ${user.color}`}>
                        {user.name.charAt(0)}
                      </div>
                      {selectedUsers.includes(user.id) && (
                        <div className="absolute -bottom-1 -right-1 w-4.5 h-4.5 bg-emerald-500 border-2 border-white dark:border-[#121212] rounded-full flex items-center justify-center">
                          <Check className="w-2.5 h-2.5 text-white" />
                        </div>
                      )}
                    </div>
                    <div className="ml-3 flex-1 overflow-hidden">
                      <h4 className={`font-bold text-sm truncate ${selectedUsers.includes(user.id) ? 'text-primary dark:text-blue-400' : 'text-gray-900 dark:text-white'}`}>{user.name}</h4>
                      <p className="text-[11px] text-gray-500 truncate mt-0.5 font-medium">{user.department} {user.position ? `• ${user.position}` : ''}</p>
                    </div>
                  </button>
                ))}
              </div>

              <div className="p-4 border-t border-border flex justify-end space-x-2 bg-white dark:bg-popover shadow-[0_-10px_20px_rgba(0,0,0,0.02)]">
                <button onClick={() => { setIsModalOpen(false); setSelectedUsers([]); setGroupName(""); }} className="px-5 py-2.5 rounded-xl text-sm font-bold text-gray-600 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-white/10 transition-colors">
                  Cancel
                </button>
                <button 
                  onClick={modalType === 'create' ? handleCreateChat : handleAddMembersToThread}
                  disabled={selectedUsers.length === 0}
                  className="px-5 py-2.5 rounded-xl text-sm font-bold bg-primary text-white hover:bg-blue-700 transition-colors disabled:opacity-50 disabled:shadow-none shadow-lg shadow-blue-500/30 flex items-center"
                >
                  {modalType === 'create' ? 'Start Chat' : 'Add to Group'} {selectedUsers.length > 0 && <span className="ml-1.5 bg-white/20 px-2 py-0.5 rounded-md">{selectedUsers.length}</span>}
                </button>
              </div>

            </motion.div>
          )}
        </AnimatePresence>

      </div>
    </div>
  );
}

