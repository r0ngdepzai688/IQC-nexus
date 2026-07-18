"use client";

import React, { useEffect, useRef, useState } from "react";
import { CheckCircle2, Search, UserCircle2 } from "lucide-react";
import {
  getAllUsers,
  getUserById,
  UserData,
} from "@/lib/data/usersService";

export interface UserSearchInputProps {
  value: string;
  onChange: (empId: string) => void;
  placeholder?: string;
  className?: string;
}

export function UserSearchInput({
  value,
  onChange,
  placeholder = "Nhập tên hoặc mã nhân viên (@Tên)...",
  className = "",
}: UserSearchInputProps) {
  const [query, setQuery] = useState("");
  const [isEditing, setIsEditing] = useState(false);
  const [isOpen, setIsOpen] = useState(false);
  const [users] = useState<UserData[]>(getAllUsers);
  const containerRef = useRef<HTMLDivElement>(null);

  const selectedUser = value ? getUserById(value) : undefined;

  const displayedQuery = isEditing
    ? query
    : selectedUser?.name ?? "";

  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      const target = event.target;

      if (
        target instanceof Node &&
        containerRef.current &&
        !containerRef.current.contains(target)
      ) {
        setIsOpen(false);
        setIsEditing(false);
      }
    };

    document.addEventListener("mousedown", handleClickOutside);

    return () => {
      document.removeEventListener("mousedown", handleClickOutside);
    };
  }, []);

  const cleanQuery = displayedQuery.startsWith("@")
    ? displayedQuery.substring(1).toLowerCase()
    : displayedQuery.toLowerCase();

  const filteredUsers = users
    .filter(
      (user) =>
        user.name.toLowerCase().includes(cleanQuery) ||
        user.empId.toLowerCase().includes(cleanQuery) ||
        user.department.toLowerCase().includes(cleanQuery),
    )
    .slice(0, 8);

  const handleSelect = (user: UserData) => {
    setQuery(user.name);
    setIsEditing(false);
    setIsOpen(false);
    onChange(user.empId);
  };

  const handleInputChange = (
    event: React.ChangeEvent<HTMLInputElement>,
  ) => {
    const nextQuery = event.target.value;

    setQuery(nextQuery);
    setIsEditing(true);
    setIsOpen(true);

    if (value) {
      onChange("");
    }
  };

  const handleFocus = () => {
    if (!isEditing) {
      setQuery(selectedUser?.name ?? "");
      setIsEditing(true);
    }

    setIsOpen(true);
  };

  return (
    <div
      ref={containerRef}
      className={`relative ${className}`}
    >
      <div className="relative">
        <Search className="absolute left-3 top-2.5 h-4 w-4 text-muted-foreground" />

        <input
          type="text"
          value={displayedQuery}
          onChange={handleInputChange}
          onFocus={handleFocus}
          placeholder={placeholder}
          className="w-full rounded-lg border border-border bg-background py-2 pl-9 pr-3 text-sm outline-none transition-all focus:border-primary focus:ring-1 focus:ring-primary/20"
        />

        {value && (
          <CheckCircle2 className="absolute right-3 top-2.5 h-4 w-4 text-emerald-500" />
        )}
      </div>

      {isOpen && displayedQuery && (
        <div className="custom-scrollbar absolute z-50 mt-1 max-h-[300px] w-full overflow-y-auto rounded-xl border border-border bg-background shadow-xl">
          {filteredUsers.length > 0 ? (
            <div className="p-1">
              {filteredUsers.map((user) => (
                <div
                  key={user.empId}
                  onClick={() => handleSelect(user)}
                  className="group flex cursor-pointer items-center rounded-lg px-3 py-2 transition-colors hover:bg-muted/50"
                >
                  <div className="mr-3 flex h-8 w-8 flex-shrink-0 items-center justify-center rounded-full bg-indigo-100 text-xs font-bold text-indigo-700 dark:bg-indigo-900/50 dark:text-indigo-300">
                    {user.name.substring(0, 2).toUpperCase()}
                  </div>

                  <div className="flex flex-col overflow-hidden">
                    <span className="truncate text-sm font-semibold text-foreground transition-colors group-hover:text-primary">
                      {user.name}
                    </span>

                    <span className="truncate text-[10px] font-medium text-muted-foreground">
                      {user.department ||
                        user.organization ||
                        "No Department"}{" "}
                      • {user.empId}
                    </span>
                  </div>
                </div>
              ))}
            </div>
          ) : (
            <div className="flex flex-col items-center p-4 text-center text-sm text-muted-foreground">
              <UserCircle2 className="mb-2 h-8 w-8 text-muted/50" />
              Không tìm thấy nhân sự phù hợp
            </div>
          )}
        </div>
      )}
    </div>
  );
}
