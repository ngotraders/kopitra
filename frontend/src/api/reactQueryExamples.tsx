/**
 * React Query 設定確認ファイル
 *
 * このファイルは実装が正しく統合されたことを確認するためのものです。
 * 削除しても問題ありません。
 */

/* eslint-disable react-refresh/only-export-components */

import { useQuery } from "@tanstack/react-query";
import { useAuth } from "../features/auth";
import { useApiClient } from "../hooks/useApiClient";

interface Signal {
  id: string;
  name: string;
}

/**
 * 使用例1: シグナル一覧取得（認証済みユーザー向け）
 */
export const useSignalsQuery = () => {
  const { isAuthenticated } = useAuth();
  const apiClient = useApiClient();

  return useQuery<Signal[]>({
    queryKey: ["signals"],
    queryFn: () => apiClient.get<Signal[]>("/signals"),
    enabled: isAuthenticated, // 認証後のみ実行
  });
};

/**
 * 使用例2: ユーザー詳細取得（パラメータ付き）
 */
export const useUserQuery = (userId: string | null) => {
  const apiClient = useApiClient();

  return useQuery({
    queryKey: ["users", userId],
    queryFn: () => (userId ? apiClient.get(`/users/${userId}`) : null),
    enabled: !!userId,
  });
};

/**
 * 使用例3: アカウント一覧（キャッシュ戦略カスタム）
 */
export const useAccountsQuery = () => {
  const apiClient = useApiClient();

  return useQuery({
    queryKey: ["accounts"],
    queryFn: () => apiClient.get("/accounts"),
    staleTime: 10 * 60 * 1000, // 10分
    gcTime: 20 * 60 * 1000, // 20分
  });
};

/**
 * コンポーネント使用例
 */
export const ExampleComponent = () => {
  const { isAuthenticated } = useAuth();

  // クエリ実行
  const signals = useSignalsQuery();

  // 状態に基づいた表示
  if (!isAuthenticated) {
    return <div>ログインしてください</div>;
  }

  if (signals.isLoading) {
    return <div>読み込み中...</div>;
  }

  if (signals.error) {
    return <div>エラー: {signals.error.message}</div>;
  }

  return (
    <div>
      <h1>シグナル</h1>
      {signals.data && (
        <ul>
          {/* TypeScript型推論により自動補完サポート */}
          {signals.data.map((signal) => (
            <li key={signal.id}>{signal.name}</li>
          ))}
        </ul>
      )}
      <button onClick={() => signals.refetch()}>更新</button>
    </div>
  );
};

// このファイルは例示のためのもので、実際のコンポーネントではありません
