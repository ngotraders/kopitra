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
  name: string;
}

export interface UserProfile {
  id: string;
  email: string;
  name: string;
  canProvide: boolean;
  canSubscribe: boolean;
  isActive: boolean;
  registeredAt: string;
  lastLoginAt?: string | null;
}

type UserApiResponse = {
  userId?: string;
  email?: string;
  name?: string;
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
      localStorage.setItem("authToken", response.accessToken);
      if (response.refreshToken) {
        localStorage.setItem("refreshToken", response.refreshToken);
      }
    }
    return response;
  },

  async register(data: RegisterRequest): Promise<LoginResponse> {
    const response = await apiClient.post<LoginResponse>("/auth/register", data);
    if (response.accessToken) {
      localStorage.setItem("authToken", response.accessToken);
      if (response.refreshToken) {
        localStorage.setItem("refreshToken", response.refreshToken);
      }
    }
    return response;
  },

  async refreshToken(): Promise<string | null> {
    try {
      const refreshToken = localStorage.getItem("refreshToken");
      if (!refreshToken) {
        return null;
      }

      const response = await apiClient.post<LoginResponse>("/auth/refresh", {
        refreshToken,
      });

      if (response.accessToken) {
        localStorage.setItem("authToken", response.accessToken);
        if (response.refreshToken) {
          localStorage.setItem("refreshToken", response.refreshToken);
        }
        return response.accessToken;
      }

      return null;
    } catch (error) {
      console.error("Token refresh failed:", error);
      // リフレッシュ失敗時はトークンをクリア
      localStorage.removeItem("authToken");
      localStorage.removeItem("refreshToken");
      return null;
    }
  },

  async getCurrentUser(): Promise<UserProfile> {
    const res = await apiClient.get<UserApiResponse>("/users/me");
    return {
      id: res.userId ?? "",
      email: res.email ?? "",
      name: res.name ?? "",
      canProvide: res.canProvide,
      canSubscribe: res.canSubscribe,
      isActive: res.isActive,
      registeredAt: res.registeredAt,
      lastLoginAt: res.lastLoginAt ?? null,
    };
  },

  logout(): void {
    localStorage.removeItem("authToken");
    localStorage.removeItem("refreshToken");
  },
};
