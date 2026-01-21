import { apiClient } from "./client";

export interface LoginRequest {
  email: string;
  password: string;
}

export interface LoginResponse {
  accessToken: string;
  refreshToken: string;
  expiresIn: number;
}

export interface RegisterRequest {
  email: string;
  password: string;
  displayName: string;
}

export interface UserProfile {
  id: string;
  email: string;
  displayName: string;
  canProvide: boolean;
  canSubscribe: boolean;
  isActive: boolean;
  registeredAt: string;
  lastLoginAt?: string | null;
}

type UserApiResponse = {
  userId?: string;
  email?: string;
  displayName?: string;
  canProvide: boolean;
  canSubscribe: boolean;
  isActive: boolean;
  registeredAt: string;
  lastLoginAt?: string | null;
};

export const authApi = {
  async login(credentials: LoginRequest): Promise<LoginResponse> {
    const response = await apiClient.post<LoginResponse>("/auth/login", credentials);
    if (response.accessToken) {
      apiClient.setToken(response.accessToken);
    }
    return response;
  },

  async register(data: RegisterRequest): Promise<LoginResponse> {
    const response = await apiClient.post<LoginResponse>("/auth/register", data);
    if (response.accessToken) {
      apiClient.setToken(response.accessToken);
    }
    return response;
  },

  async getCurrentUser(): Promise<UserProfile> {
    const res = await apiClient.get<UserApiResponse>("/users/me");
    return {
      id: res.userId ?? "",
      email: res.email ?? "",
      displayName: res.displayName ?? "",
      canProvide: res.canProvide,
      canSubscribe: res.canSubscribe,
      isActive: res.isActive,
      registeredAt: res.registeredAt,
      lastLoginAt: res.lastLoginAt ?? null,
    };
  },

  logout(): void {
    apiClient.setToken(null);
  },
};
