import { apiClient } from "./client";

// ユーザー管理用型定義
export interface AdminUserCreateRequest {
  email: string;
  name: string;
  role: "Admin" | "User";
  initialPassword?: string;
}

export interface AdminUserResponse {
  id: string;
  email: string;
  name: string;
  role: "Admin" | "User";
  createdAt: string;
  updatedAt: string;
}

export interface AdminUserListResponse {
  users: AdminUserResponse[];
  total: number;
  page: number;
  pageSize: number;
}

export interface AdminUserUpdateRequest {
  name?: string;
  role?: "Admin" | "User";
  email?: string;
}

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
  async createUser(data: AdminUserCreateRequest): Promise<AdminUserResponse> {
    return apiClient.post<AdminUserResponse>("/api/users", data);
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
    return apiClient.get<AdminUserListResponse>(`/api/users?${params.toString()}`);
  },

  async getUser(userId: string): Promise<AdminUserResponse> {
    return apiClient.get<AdminUserResponse>(`/api/users/${userId}`);
  },

  async updateUser(userId: string, data: AdminUserUpdateRequest): Promise<AdminUserResponse> {
    return apiClient.put<AdminUserResponse>(`/api/users/${userId}`, data);
  },

  async deleteUser(userId: string): Promise<void> {
    return apiClient.delete<void>(`/api/users/${userId}`);
  },

  async resetPassword(userId: string): Promise<{ temporaryPassword: string }> {
    return apiClient.post<{ temporaryPassword: string }>(
      `/api/users/${userId}/reset-password`,
      {}
    );
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
