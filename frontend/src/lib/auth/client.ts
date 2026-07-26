export const platformPermissions = [
  "dashboard.view",
  "import.view",
  "import.create",
  "import.review",
  "import.commit",
  "import.admin",
  "download.view",
  "download.manage",
  "audit.view",
  "user.manage",
  "role.manage",
] as const;

export type PlatformPermission = (typeof platformPermissions)[number];

export interface AuthUserDto {
  id: number;
  username: string;
  fullName: string;
  employeeId?: string;
  department?: string;
  knoxId?: string;
  position?: string;
  scope?: string;
  systemRole?: string;
  accountStatus?: string;
  organization?: string;
  part?: string;
  email?: string;
  roleProfile?: string;
  avatar?: string;
  roles?: string[];
  permissions?: string[];
}

export interface AuthUser {
  id: number;
  username: string;
  fullName: string;
  name: string;
  employeeId: string;
  department: string;
  knoxId: string;
  position: string;
  scope: string;
  systemRole: string;
  accountStatus: string;
  organization: string;
  part: string;
  email: string;
  roleProfile: string;
  avatar: string;
  roles: string[];
  permissions: string[];
}

export function normalizeAuthUser(dto: Partial<AuthUserDto> | null | undefined): AuthUser {
  if (!dto) {
    return {
      id: 0,
      username: "",
      fullName: "",
      name: "",
      employeeId: "",
      department: "",
      knoxId: "",
      position: "",
      scope: "",
      systemRole: "User",
      accountStatus: "Inactive",
      organization: "",
      part: "",
      email: "",
      roleProfile: "",
      avatar: "",
      roles: [],
      permissions: [],
    };
  }

  const permissions = Array.isArray(dto.permissions) ? dto.permissions : [];
  const roles = Array.isArray(dto.roles) && dto.roles.length > 0
    ? dto.roles
    : (dto.systemRole ? [dto.systemRole] : []);

  const fullName = dto.fullName ?? dto.username ?? "";

  return {
    id: dto.id ?? 0,
    username: dto.username ?? "",
    fullName,
    name: fullName,
    employeeId: dto.employeeId ?? dto.username ?? "",
    department: dto.department ?? "",
    knoxId: dto.knoxId ?? "",
    position: dto.position ?? "",
    scope: dto.scope ?? "",
    systemRole: dto.systemRole ?? "User",
    accountStatus: dto.accountStatus ?? "Inactive",
    organization: dto.organization ?? "",
    part: dto.part ?? "",
    email: dto.email ?? "",
    roleProfile: dto.roleProfile ?? "",
    avatar: dto.avatar ?? "",
    roles,
    permissions,
  };
}

interface LoginResponse {
  token: string;
  user: AuthUserDto;
}

const API_BASE = process.env.NEXT_PUBLIC_API_URL || "http://localhost:5000/api";
const tokenKey = "token";

async function request<T>(path: string, init: RequestInit = {}): Promise<T> {
  const token = typeof window === "undefined" ? null : localStorage.getItem(tokenKey);
  const response = await fetch(`${API_BASE}${path}`, {
    ...init,
    headers: {
      "Content-Type": "application/json",
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
      ...init.headers,
    },
  });

  if (!response.ok) {
    let message = "Authentication request failed";
    try {
      const errJson = await response.json();
      if (errJson && typeof errJson.message === "string") {
        message = errJson.message;
      }
    } catch {
      // Ignore non-JSON errors
    }
    throw new AuthApiError(response.status, message);
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return response.json() as Promise<T>;
}

export class AuthApiError extends Error {
  constructor(public readonly status: number, message: string = "Authentication request failed") {
    super(message);
    this.name = "AuthApiError";
  }
}

export const authClient = {
  async login(username: string, password: string): Promise<AuthUser> {
    const response = await request<LoginResponse>("/auth/login", {
      method: "POST",
      body: JSON.stringify({ username, password }),
    });
    const normalized = normalizeAuthUser(response.user);
    if (typeof window !== "undefined") {
      localStorage.setItem(tokenKey, response.token);
      localStorage.setItem("user", JSON.stringify(normalized));
    }
    return normalized;
  },

  async currentUser(): Promise<AuthUser> {
    const raw = await request<AuthUserDto>("/auth/me");
    const normalized = normalizeAuthUser(raw);
    if (typeof window !== "undefined") {
      localStorage.setItem("user", JSON.stringify(normalized));
    }
    return normalized;
  },

  async logout(): Promise<void> {
    try {
      await request<void>("/auth/logout", { method: "POST" });
    } finally {
      if (typeof window !== "undefined") {
        localStorage.removeItem(tokenKey);
        localStorage.removeItem("user");
      }
    }
  },

  getStoredUser(): AuthUser | null {
    if (typeof window === "undefined") return null;
    const raw = localStorage.getItem("user");
    if (!raw) return null;
    try {
      return normalizeAuthUser(JSON.parse(raw));
    } catch {
      return null;
    }
  },

  hasToken: () => typeof window !== "undefined" && Boolean(localStorage.getItem(tokenKey)),
};