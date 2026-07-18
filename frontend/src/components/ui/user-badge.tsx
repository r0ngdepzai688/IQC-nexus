"use client";

import Image from "next/image";

import React, { useMemo } from 'react';
import { MessageSquare } from 'lucide-react';
import { getUserById } from '@/lib/data/usersService';

export interface UserBadgeProps {
  name: string; // This can now be the EmpId (e.g. "SYN-0001") or fallback name
  avatarUrl?: string;
  size?: 'sm' | 'md' | 'lg';
  className?: string;
  avatarOnly?: boolean;
}

export function UserBadge({ name, avatarUrl, size = 'sm', className = '', avatarOnly = false }: UserBadgeProps) {
  // Try to lookup the user. If found, use real name, otherwise fallback to whatever was passed.
  const realName = useMemo(() => {
    const user = getUserById(name);
    return user ? user.name : name;
  }, [name]);

  const getInitials = (n: string) => {
    const parts = n.split(' ');
    if (parts.length >= 2) {
      return (parts[0][0] + parts[parts.length - 1][0]).toUpperCase();
    }
    return n.substring(0, 2).toUpperCase();
  };

  const handleChatClick = (e: React.MouseEvent) => {
    e.stopPropagation(); // prevent triggering parent clicks like opening a modal
    // In a real app, this would open a chat widget or dispatch an event
    if (typeof window !== 'undefined') {
      alert(`Đang mở cửa sổ chat với ${realName}...`);
    }
  };

  const sizeClasses = {
    sm: { wrapper: 'h-6', avatar: 'w-6 h-6 text-[10px]', text: 'text-[11px]' },
    md: { wrapper: 'h-8', avatar: 'w-8 h-8 text-xs', text: 'text-sm' },
    lg: { wrapper: 'h-10', avatar: 'w-10 h-10 text-sm', text: 'text-base' },
  };

  return (
    <button 
      onClick={handleChatClick}
      className={`inline-flex items-center bg-gray-100 hover:bg-indigo-50 dark:bg-white/5 dark:hover:bg-indigo-900/20 rounded-full transition-colors duration-200 border border-transparent hover:border-indigo-200 dark:hover:border-indigo-500/30 group ${sizeClasses[size].wrapper} ${!avatarOnly ? 'px-1 py-1 pr-3 space-x-2' : ''} ${className}`}
      title={`Chat với ${realName}`}
    >
      <div className={`${sizeClasses[size].avatar} rounded-full bg-indigo-100 dark:bg-indigo-900/50 flex items-center justify-center font-bold text-indigo-700 dark:text-indigo-300 flex-shrink-0 relative overflow-hidden`}>
        {avatarUrl ? (
          <Image src={avatarUrl} alt={realName} width={40} height={40} className="w-full h-full rounded-full object-cover" />
        ) : (
          getInitials(realName)
        )}
        {avatarOnly && (
           <div className="absolute inset-0 bg-black/40 flex items-center justify-center opacity-0 group-hover:opacity-100 transition-opacity">
             <MessageSquare className="w-3 h-3 text-white" />
           </div>
        )}
      </div>
      {!avatarOnly && (
        <>
          <span className={`${sizeClasses[size].text} font-semibold text-gray-700 dark:text-gray-300 group-hover:text-indigo-700 dark:group-hover:text-indigo-300 truncate max-w-[120px]`}>
            {realName}
          </span>
          <MessageSquare className="w-3 h-3 text-indigo-400 opacity-0 group-hover:opacity-100 transition-opacity ml-1" />
        </>
      )}
    </button>
  );
}
