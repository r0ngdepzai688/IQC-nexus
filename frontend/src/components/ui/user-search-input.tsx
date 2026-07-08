"use client";

import React, { useState, useEffect, useRef } from 'react';
import { Search, UserCircle2, CheckCircle2 } from 'lucide-react';
import { getAllUsers, UserData, getUserById } from '@/lib/data/usersService';
import { useOnClickOutside } from '@/lib/hooks/useOnClickOutside'; // We'll need a simple custom hook for this or inline it

export interface UserSearchInputProps {
  value: string; // The empId
  onChange: (empId: string) => void;
  placeholder?: string;
  className?: string;
}

export function UserSearchInput({ value, onChange, placeholder = "Nhập tên hoặc mã nhân viên (@Tên)...", className = "" }: UserSearchInputProps) {
  const [query, setQuery] = useState("");
  const [isOpen, setIsOpen] = useState(false);
  const [users, setUsers] = useState<UserData[]>([]);
  const containerRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    // Load users once
    setUsers(getAllUsers());
  }, []);

  // When value changes from outside (e.g. form reset), update the query to show the name
  useEffect(() => {
    if (!value) {
      setQuery("");
    } else {
      const u = getUserById(value);
      if (u) {
        setQuery(u.name); // Show the name in the input if we have an ID selected
      }
    }
  }, [value]);

  // Click outside to close dropdown
  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (containerRef.current && !containerRef.current.contains(event.target as Node)) {
        setIsOpen(false);
        // If they click away and the query doesn't perfectly match the selected user, 
        // we revert the query to the selected user's name
        if (value) {
          const u = getUserById(value);
          if (u && query !== u.name) setQuery(u.name);
        } else {
          setQuery("");
        }
      }
    };
    document.addEventListener("mousedown", handleClickOutside);
    return () => document.removeEventListener("mousedown", handleClickOutside);
  }, [containerRef, value, query]);

  // Filter logic
  // Allow searching by "@name" or just "name" or "empId"
  const cleanQuery = query.startsWith('@') ? query.substring(1).toLowerCase() : query.toLowerCase();
  
  const filteredUsers = users.filter(u => 
    u.name.toLowerCase().includes(cleanQuery) || 
    u.empId.toLowerCase().includes(cleanQuery) ||
    u.department.toLowerCase().includes(cleanQuery)
  ).slice(0, 8); // Limit to 8 suggestions for performance

  const handleSelect = (user: UserData) => {
    setQuery(user.name);
    onChange(user.empId);
    setIsOpen(false);
  };

  const handleInputChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    setQuery(e.target.value);
    setIsOpen(true);
    if (value) onChange(""); // clear selection if they start typing again
  };

  return (
    <div className={`relative ${className}`} ref={containerRef}>
      <div className="relative">
        <Search className="w-4 h-4 absolute left-3 top-2.5 text-muted-foreground" />
        <input
          type="text"
          value={query}
          onChange={handleInputChange}
          onFocus={() => setIsOpen(true)}
          placeholder={placeholder}
          className="w-full text-sm rounded-lg border border-border bg-background pl-9 pr-3 py-2 outline-none focus:border-primary focus:ring-1 focus:ring-primary/20 transition-all"
        />
        {value && (
          <CheckCircle2 className="w-4 h-4 absolute right-3 top-2.5 text-emerald-500" />
        )}
      </div>

      {isOpen && query && (
        <div className="absolute z-50 w-full mt-1 bg-background border border-border rounded-xl shadow-xl overflow-hidden max-h-[300px] overflow-y-auto custom-scrollbar">
          {filteredUsers.length > 0 ? (
            <div className="p-1">
              {filteredUsers.map(user => (
                <div 
                  key={user.empId} 
                  onClick={() => handleSelect(user)}
                  className="flex items-center px-3 py-2 hover:bg-muted/50 cursor-pointer rounded-lg transition-colors group"
                >
                  <div className="w-8 h-8 rounded-full bg-indigo-100 dark:bg-indigo-900/50 flex items-center justify-center text-indigo-700 dark:text-indigo-300 font-bold text-xs mr-3 flex-shrink-0">
                    {user.name.substring(0, 2).toUpperCase()}
                  </div>
                  <div className="flex flex-col overflow-hidden">
                    <span className="text-sm font-semibold text-foreground truncate group-hover:text-primary transition-colors">
                      {user.name}
                    </span>
                    <span className="text-[10px] font-medium text-muted-foreground truncate">
                      {user.department || user.organization || "No Department"} • {user.empId}
                    </span>
                  </div>
                </div>
              ))}
            </div>
          ) : (
            <div className="p-4 text-center text-sm text-muted-foreground flex flex-col items-center">
              <UserCircle2 className="w-8 h-8 text-muted/50 mb-2" />
              Không tìm thấy nhân sự phù hợp
            </div>
          )}
        </div>
      )}
    </div>
  );
}
