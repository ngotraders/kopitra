export interface ApiError {
  message: string;
  status: number;
}

export interface TokenRefreshCallback {
  (): Promise<string | null>;
}

export class ApiClient {
  private baseUrl: string;
  private getToken: () => string | null;
  private refreshToken: TokenRefreshCallback | null = null;
  private isRefreshing = false;
  private refreshPromise: Promise<string | null> | null = null;

  constructor(
    baseUrl: string = "/api",
    getToken: () => string | null = () => localStorage.getItem("authToken")
  ) {
    this.baseUrl = baseUrl;
    this.getToken = getToken;
  }

  setTokenRefreshCallback(callback: TokenRefreshCallback): void {
    this.refreshToken = callback;
  }

  private async request<T>(
    endpoint: string,
    options: RequestInit = {},
    isRetry: boolean = false
  ): Promise<T> {
    const token = this.getToken();
    const headers: HeadersInit = token
      ? {
          "Content-Type": "application/json",
          Authorization: `Bearer ${token}`,
          ...options.headers,
        }
      : {
          "Content-Type": "application/json",
          ...options.headers,
        };

    const response = await fetch(`${this.baseUrl}${endpoint}`, {
      ...options,
      headers,
    });

    // 401エラーの場合、トークンリフレッシュを試みる
    if (response.status === 401 && !isRetry && this.refreshToken) {
      // 既にリフレッシュ中の場合は、そのPromiseを待つ
      if (this.isRefreshing && this.refreshPromise) {
        await this.refreshPromise;
      } else {
        // トークンリフレッシュを開始
        this.isRefreshing = true;
        this.refreshPromise = this.refreshToken();

        try {
          const newToken = await this.refreshPromise;
          if (newToken) {
            // リフレッシュ成功、元のリクエストをリトライ
            return this.request<T>(endpoint, options, true);
          }
        } finally {
          this.isRefreshing = false;
          this.refreshPromise = null;
        }
      }
    }

    if (!response.ok) {
      const error: ApiError = {
        message: await response.text().catch(() => "Unknown error"),
        status: response.status,
      };
      throw error;
    }

    const contentType = response.headers.get("content-type");
    if (contentType && contentType.includes("application/json")) {
      return response.json();
    }

    return {} as T;
  }

  async get<T>(endpoint: string): Promise<T> {
    return this.request<T>(endpoint, { method: "GET" });
  }

  async post<T>(endpoint: string, data?: unknown): Promise<T> {
    return this.request<T>(endpoint, {
      method: "POST",
      body: data ? JSON.stringify(data) : undefined,
    });
  }

  async put<T>(endpoint: string, data?: unknown): Promise<T> {
    return this.request<T>(endpoint, {
      method: "PUT",
      body: data ? JSON.stringify(data) : undefined,
    });
  }

  async delete<T>(endpoint: string): Promise<T> {
    return this.request<T>(endpoint, { method: "DELETE" });
  }
}

export const apiClient = new ApiClient();
