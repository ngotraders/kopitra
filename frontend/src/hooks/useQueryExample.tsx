/**
 * React Query の使用例とベストプラクティス
 *
 * 以下のパターンで useQuery を使用してください。
 * AuthProvider 経由で token が自動的に注入されます。
 */

import { useQuery } from "@tanstack/react-query";
import { useApiClient } from "./useApiClient";

// 例0: ユーザー一覧取得（useUsersQuery 参照）
export { useUsersQuery } from "./useUsersQuery";

// 例1: 基本的な使用方法
export const useSignals = () => {
  const apiClient = useApiClient();

  return useQuery({
    queryKey: ["signals"],
    queryFn: () => apiClient.get("/signals"),
    enabled: true, // token がある場合のみ実行したい場合は useAuth() から token を取得して enabled={!!token} に
  });
};

// 例2: パラメータ付きクエリ
export const useUserDetail = (userId: string | null) => {
  const apiClient = useApiClient();

  return useQuery({
    queryKey: ["users", userId],
    queryFn: () => apiClient.get(`/users/${userId}`),
    enabled: !!userId, // userId がある場合のみ実行
  });
};

// 例3: カスタムオプション
export const useAccounts = () => {
  const apiClient = useApiClient();

  return useQuery({
    queryKey: ["accounts"],
    queryFn: () => apiClient.get("/accounts"),
    staleTime: 10 * 60 * 1000, // 10分有効
    gcTime: 15 * 60 * 1000, // 15分キャッシュ保持
    retry: 2, // エラー時に2回リトライ
  });
};
