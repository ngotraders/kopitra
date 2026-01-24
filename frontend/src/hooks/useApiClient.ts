import { useRef, useEffect, useState } from "react";
import { ApiClient } from "../api";
import { useAuth } from "../features/auth";
import { authApi } from "../api/auth";

export const useApiClient = (): ApiClient => {
  const { token } = useAuth();

  // クライアントインスタンスを状態として保持
  const [client] = useState(() => {
    // 初回レンダリング時にのみ実行される初期化関数
    // tokenを返すクロージャを渡すことで、常に最新のtokenにアクセス可能
    const tokenGetter = () => token;
    return new ApiClient(process.env.VITE_API_URL || "/api", tokenGetter);
  });

  // tokenの最新値を追跡するためのref
  const tokenRef = useRef(token);

  useEffect(() => {
    tokenRef.current = token;
  }, [token]);

  // トークンリフレッシュコールバックを設定
  useEffect(() => {
    client.setTokenRefreshCallback(async () => {
      return await authApi.refreshToken();
    });
  }, [client]);

  return client;
};
