import { useQuery } from "@tanstack/react-query";
import { useAuth } from "../features/auth";
import { useApiClient } from "./useApiClient";

/**
 * ユーザー一覧を取得するカスタムフック
 * 認証状態を確認した上でクエリを実行します
 */
export const useUsersQuery = () => {
  const { isAuthenticated } = useAuth();
  const apiClient = useApiClient();

  return useQuery({
    queryKey: ["users"],
    queryFn: () => apiClient.get("/users"),
    enabled: isAuthenticated,
  });
};
