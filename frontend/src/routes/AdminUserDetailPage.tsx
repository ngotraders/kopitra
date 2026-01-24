import React, { useEffect, useState } from "react";
import { useParams, useNavigate } from "react-router-dom";
import {
  Box,
  Card,
  CardHeader,
  CardContent,
  CardActions,
  Button,
  Stack,
  Typography,
  Chip,
  Divider,
  Alert,
  CircularProgress,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableRow,
  Tooltip,
  Container,
  Fade,
} from "@mui/material";
import EditIcon from "@mui/icons-material/Edit";
import DeleteIcon from "@mui/icons-material/Delete";
import AddIcon from "@mui/icons-material/Add";
import { type AdminUser, userManagementApi } from "../api";

export interface AdminUserDetailPageProps {
  userId?: string;
  onBack?: () => void;
}

export const AdminUserDetailPage: React.FC<AdminUserDetailPageProps> = ({ userId, onBack }) => {
  const params = useParams<{ userId: string }>();
  const navigate = useNavigate();
  const resolvedUserId = userId ?? params.userId;
  const handleBack = onBack ?? (() => navigate("/admin/users"));
  const [user, setUser] = useState<AdminUser | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [showDeleteConfirm, setShowDeleteConfirm] = useState(false);

  useEffect(() => {
    const loadUser = async () => {
      setIsLoading(true);
      setError(null);
      try {
        if (!resolvedUserId) {
          throw new Error("ユーザーIDが指定されていません");
        }
        const userData = await userManagementApi.getUser(resolvedUserId);
        setUser(userData);
      } catch (err: unknown) {
        const message = err instanceof Error ? err.message : "ユーザー情報の取得に失敗しました";
        setError(message);
      } finally {
        setIsLoading(false);
      }
    };

    loadUser();
  }, [resolvedUserId]);

  const handleDeleteUser = async () => {
    try {
      if (!resolvedUserId) return;
      await userManagementApi.deleteUser(resolvedUserId);
      handleBack();
    } catch (err: unknown) {
      const message = err instanceof Error ? err.message : "ユーザーの削除に失敗しました";
      setError(message);
    } finally {
      setShowDeleteConfirm(false);
    }
  };

  if (!resolvedUserId) {
    return (
      <Container maxWidth="lg" sx={{ mt: 4, mb: 6 }}>
        <Typography color="error" sx={{ mb: 2 }}>
          ユーザーIDが指定されていません
        </Typography>
        <Button onClick={handleBack} variant="outlined">
          ← 一覧に戻る
        </Button>
      </Container>
    );
  }

  if (isLoading) {
    return (
      <Box sx={{ display: "flex", justifyContent: "center", alignItems: "center", minHeight: 400 }}>
        <CircularProgress />
      </Box>
    );
  }

  if (!user) {
    return (
      <Box sx={{ textAlign: "center", py: 4 }}>
        <Typography color="error">ユーザーが見つかりません</Typography>
        <Button onClick={handleBack} sx={{ mt: 2 }}>
          一覧に戻る
        </Button>
      </Box>
    );
  }

  return (
    <Fade in={true} timeout={500}>
      <Container maxWidth="lg" sx={{ mt: 4, mb: 6 }}>
        <Box sx={{ mb: 4 }}>
          <Button onClick={handleBack} variant="outlined" sx={{ mb: 2 }}>
            ← 一覧に戻る
          </Button>
          <Typography variant="h4" sx={{ fontWeight: 600, mb: 1 }}>
            ユーザー詳細
          </Typography>
          <Typography variant="body2" color="text.secondary">
            {user.name} ({user.email})
          </Typography>
        </Box>

        {error && (
          <Alert severity="error" onClose={() => setError(null)} sx={{ mb: 3 }}>
            {error}
          </Alert>
        )}

        <Stack spacing={3}>
          {/* セクション1: 基本情報 */}
          <Card
            sx={{
              boxShadow: "0 2px 8px rgba(0,0,0,0.1)",
              transition: "boxShadow 0.2s ease-in-out",
            }}
          >
            <CardHeader title="基本情報" sx={{ pb: 1 }} />
            <Divider />
            <CardContent sx={{ p: 3 }}>
              <Stack spacing={2}>
                <Box>
                  <Typography variant="caption" color="text.secondary">
                    表示名
                  </Typography>
                  <Typography variant="body1" sx={{ fontWeight: 500 }}>
                    {user.name || "-"}
                  </Typography>
                </Box>
                <Box>
                  <Typography variant="caption" color="text.secondary">
                    メールアドレス
                  </Typography>
                  <Typography variant="body1" sx={{ fontWeight: 500 }}>
                    {user.email}
                  </Typography>
                </Box>
                <Box>
                  <Typography variant="caption" color="text.secondary">
                    ステータス
                  </Typography>
                  <Box sx={{ mt: 1 }}>
                    <Chip
                      label={user.isActive ? "有効" : "無効"}
                      color={user.isActive ? "success" : "default"}
                      variant="filled"
                      size="small"
                    />
                  </Box>
                </Box>
                <Box>
                  <Typography variant="caption" color="text.secondary">
                    登録日時
                  </Typography>
                  <Typography variant="body1" sx={{ fontWeight: 500 }}>
                    {user.registeredAt ? new Date(user.registeredAt).toLocaleString() : "-"}
                  </Typography>
                </Box>
                <Box>
                  <Typography variant="caption" color="text.secondary">
                    最終ログイン
                  </Typography>
                  <Typography variant="body1" sx={{ fontWeight: 500 }}>
                    {user.lastLoginAt
                      ? new Date(user.lastLoginAt).toLocaleString()
                      : "ログイン履歴なし"}
                  </Typography>
                </Box>
              </Stack>
            </CardContent>
            <Divider />
            <CardActions sx={{ p: 2 }}>
              <Button color="primary" startIcon={<EditIcon />}>
                編集
              </Button>
              <Button
                color="error"
                startIcon={<DeleteIcon />}
                onClick={() => setShowDeleteConfirm(true)}
              >
                削除
              </Button>
            </CardActions>
          </Card>

          {/* セクション2: 権限設定 */}
          <Card
            sx={{
              boxShadow: "0 2px 8px rgba(0,0,0,0.1)",
              transition: "boxShadow 0.2s ease-in-out",
            }}
          >
            <CardHeader title="権限設定" sx={{ pb: 1 }} />
            <Divider />
            <CardContent sx={{ p: 3 }}>
              <Stack spacing={2}>
                <Box
                  sx={{ display: "flex", alignItems: "center", justifyContent: "space-between" }}
                >
                  <Box>
                    <Typography variant="body2" sx={{ fontWeight: 500 }}>
                      シグナル提供者
                    </Typography>
                    <Typography variant="caption" color="text.secondary">
                      トレーディングシグナルを提供できる
                    </Typography>
                  </Box>
                  <Chip
                    label={user.canProvide ? "許可" : "禁止"}
                    color={user.canProvide ? "success" : "default"}
                    variant="filled"
                    size="small"
                  />
                </Box>

                <Divider />

                <Box
                  sx={{ display: "flex", alignItems: "center", justifyContent: "space-between" }}
                >
                  <Box>
                    <Typography variant="body2" sx={{ fontWeight: 500 }}>
                      シグナル購読者
                    </Typography>
                    <Typography variant="caption" color="text.secondary">
                      他のユーザーのシグナルを購読できる
                    </Typography>
                  </Box>
                  <Chip
                    label={user.canSubscribe ? "許可" : "禁止"}
                    color={user.canSubscribe ? "success" : "default"}
                    variant="filled"
                    size="small"
                  />
                </Box>
              </Stack>
            </CardContent>
          </Card>

          {/* セクション3: アカウント管理 */}
          <Card
            sx={{
              boxShadow: "0 2px 8px rgba(0,0,0,0.1)",
              transition: "boxShadow 0.2s ease-in-out",
            }}
          >
            <CardHeader
              title="取引アカウント管理"
              action={
                <Tooltip title="新規アカウント追加">
                  <Button variant="contained" color="primary" size="small" startIcon={<AddIcon />}>
                    アカウント追加
                  </Button>
                </Tooltip>
              }
              sx={{ pb: 1 }}
            />
            <Divider />
            <CardContent sx={{ p: 0 }}>
              <Table>
                <TableHead>
                  <TableRow sx={{ backgroundColor: "#f5f5f5" }}>
                    <TableCell sx={{ fontWeight: 600 }}>口座名</TableCell>
                    <TableCell sx={{ fontWeight: 600 }}>ブローカー</TableCell>
                    <TableCell sx={{ fontWeight: 600 }}>口座番号</TableCell>
                    <TableCell sx={{ fontWeight: 600 }}>ステータス</TableCell>
                    <TableCell sx={{ fontWeight: 600 }} align="right">
                      操作
                    </TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  <TableRow>
                    <TableCell colSpan={5} align="center" sx={{ py: 3 }}>
                      <Typography variant="body2" color="text.secondary">
                        取引アカウントがまだ登録されていません
                      </Typography>
                    </TableCell>
                  </TableRow>
                </TableBody>
              </Table>
            </CardContent>
          </Card>

          {/* セクション4: アクティビティログ */}
          <Card
            sx={{
              boxShadow: "0 2px 8px rgba(0,0,0,0.1)",
              transition: "boxShadow 0.2s ease-in-out",
            }}
          >
            <CardHeader title="アクティビティログ" sx={{ pb: 1 }} />
            <Divider />
            <CardContent sx={{ p: 3 }}>
              <Typography variant="body2" color="text.secondary">
                アクティビティログはまだ実装されていません
              </Typography>
            </CardContent>
          </Card>
        </Stack>

        {/* 削除確認ダイアログ */}
        <Dialog open={showDeleteConfirm} onClose={() => setShowDeleteConfirm(false)}>
          <DialogTitle>ユーザー削除の確認</DialogTitle>
          <DialogContent>
            <Typography>ユーザー「{user.name}」を削除してもよろしいですか？</Typography>
            <Typography variant="caption" color="error" sx={{ display: "block", mt: 2 }}>
              この操作は取り消せません
            </Typography>
          </DialogContent>
          <DialogActions>
            <Button onClick={() => setShowDeleteConfirm(false)}>キャンセル</Button>
            <Button onClick={handleDeleteUser} color="error" variant="contained">
              削除
            </Button>
          </DialogActions>
        </Dialog>
      </Container>
    </Fade>
  );
};
