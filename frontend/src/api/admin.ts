import { apiClient } from "./client";

// ユーザー管理用型定義
export interface AdminUserCreateRequest {
  email: string;
  name: string;
  role?: "Admin" | "User";
  initialPassword?: string;
}

export interface AdminUser {
  id: string;
  email: string;
  name: string;
  role?: "Admin" | "User";
  canProvide?: boolean;
  canSubscribe?: boolean;
  isActive?: boolean;
  registeredAt?: string;
  lastLoginAt?: string | null;
  createdAt?: string;
  updatedAt?: string;
}

export interface AdminUserListResponse {
  users: AdminUser[];
  total: number;
  page: number;
  pageSize: number;
}

export interface AdminUserUpdateRequest {
  name?: string;
  role?: "Admin" | "User";
  email?: string;
}

type AdminUserApiResponse = {
  userId?: string;
  id?: string;
  email?: string;
  name?: string;
  role?: "Admin" | "User";
  canProvide?: boolean;
  canSubscribe?: boolean;
  isActive?: boolean;
  registeredAt?: string;
  lastLoginAt?: string | null;
  createdAt?: string;
  updatedAt?: string;
};

const normalizeAdminUser = (u: AdminUserApiResponse): AdminUser => {
  return {
    id: u.userId ?? u.id ?? "",
    email: u.email ?? "",
    name: u.name ?? "",
    role: u.role,
    canProvide: u.canProvide,
    canSubscribe: u.canSubscribe,
    isActive: u.isActive,
    registeredAt: u.registeredAt,
    lastLoginAt: u.lastLoginAt ?? null,
    createdAt: u.createdAt,
    updatedAt: u.updatedAt,
  };
};

// アカウント（取引口座）管理用型定義
export interface AdminAccountCreateRequest {
  userId: string;
  accountName: string;
  broker: string;
  accountNumber: string;
  mt4Password?: string;
  status: "Active" | "Inactive";
  initialBalance?: number;
}

export interface AdminAccountResponse {
  id: string;
  userId: string;
  accountName: string;
  broker: string;
  accountNumber: string;
  status: "Active" | "Inactive";
  balance: number;
  createdAt: string;
  updatedAt: string;
}

export interface AdminAccountListResponse {
  accounts: AdminAccountResponse[];
  total: number;
  page: number;
  pageSize: number;
}

export interface AdminAccountUpdateRequest {
  accountName?: string;
  status?: "Active" | "Inactive";
  balance?: number;
}

// ユーザー管理API
export const userManagementApi = {
  async createUser(data: AdminUserCreateRequest): Promise<AdminUser> {
    const res = await apiClient.post<AdminUserApiResponse>("/users", data);
    return normalizeAdminUser(res);
  },

  async listUsers(
    page: number = 1,
    pageSize: number = 10,
    search?: string
  ): Promise<AdminUserListResponse> {
    const params = new URLSearchParams({
      page: page.toString(),
      pageSize: pageSize.toString(),
    });
    if (search) {
      params.append("search", search);
    }
    const res = await apiClient.get<AdminUserApiResponse[] | AdminUserListResponse>(
      `/users?${params.toString()}`
    );

    // Backend currently returns an array; normalize either shape
    if (Array.isArray(res)) {
      const users = res.map(normalizeAdminUser);
      const start = (page - 1) * pageSize;
      const paged = users.slice(start, start + pageSize);
      return {
        users: paged,
        total: users.length,
        page,
        pageSize,
      };
    }

    if (res && "users" in res) {
      return {
        users: res.users.map(normalizeAdminUser),
        total: res.total,
        page: res.page,
        pageSize: res.pageSize,
      };
    }

    return { users: [], total: 0, page, pageSize };
  },

  async getUser(userId: string): Promise<AdminUser> {
    const res = await apiClient.get<AdminUserApiResponse>(`/users/${userId}`);
    return normalizeAdminUser(res);
  },

  async updateUser(userId: string, data: AdminUserUpdateRequest): Promise<AdminUser> {
    const res = await apiClient.put<AdminUserApiResponse>(`/users/${userId}`, data);
    return normalizeAdminUser(res);
  },

  async deleteUser(userId: string): Promise<void> {
    return apiClient.delete<void>(`/users/${userId}`);
  },

  async resetPassword(userId: string): Promise<{ temporaryPassword: string }> {
    return apiClient.post<{ temporaryPassword: string }>(`/users/${userId}/reset-password`, {});
  },
};

// アカウント管理API
export const accountManagementApi = {
  async createAccount(data: AdminAccountCreateRequest): Promise<AdminAccountResponse> {
    return apiClient.post<AdminAccountResponse>("/admin/accounts", data);
  },

  async listAccounts(page: number = 1, pageSize: number = 10): Promise<AdminAccountListResponse> {
    return apiClient.get<AdminAccountListResponse>(
      `/admin/accounts?page=${page}&pageSize=${pageSize}`
    );
  },

  async getAccount(accountId: string): Promise<AdminAccountResponse> {
    return apiClient.get<AdminAccountResponse>(`/admin/accounts/${accountId}`);
  },

  async updateAccount(
    accountId: string,
    data: AdminAccountUpdateRequest
  ): Promise<AdminAccountResponse> {
    return apiClient.put<AdminAccountResponse>(`/admin/accounts/${accountId}`, data);
  },

  async deleteAccount(accountId: string): Promise<void> {
    return apiClient.delete<void>(`/admin/accounts/${accountId}`);
  },

  async listUserAccounts(userId: string): Promise<AdminAccountListResponse> {
    return apiClient.get<AdminAccountListResponse>(`/admin/users/${userId}/accounts`);
  },
};
