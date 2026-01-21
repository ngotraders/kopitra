import React, { useEffect, useState } from "react";
import {
  Alert,
  Box,
  Card,
  CardContent,
  Chip,
  CircularProgress,
  IconButton,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TablePagination,
  TableRow,
  TextField,
  Tooltip,
  Typography,
} from "@mui/material";
import EditIcon from "@mui/icons-material/Edit";
import RefreshIcon from "@mui/icons-material/Refresh";
import { type AdminUser, userManagementApi } from "../../../api/admin";
import { AdminUserEditDialog } from "./AdminUserEditDialog";

export interface AdminUserManagementPageProps {
  onViewDetail?: (userId: string) => void;
}

const formatDateTime = (value?: string | null) => {
  if (!value) return "-";
  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? "-" : date.toLocaleString();
};

export const AdminUserManagementPage: React.FC<AdminUserManagementPageProps> = ({
  onViewDetail,
}) => {
  const [users, setUsers] = useState<AdminUser[]>([]);
  const [total, setTotal] = useState(0);
  const [page, setPage] = useState(0); // zero-based for TablePagination
  const [pageSize, setPageSize] = useState(10);
  const [search, setSearch] = useState("");
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [editDialogOpen, setEditDialogOpen] = useState(false);
  const [selectedUser, setSelectedUser] = useState<AdminUser | null>(null);

  const loadUsers = async () => {
    setIsLoading(true);
    setError(null);
    try {
      const res = await userManagementApi.listUsers(page + 1, pageSize, search.trim() || undefined);
      setUsers(res.users);
      setTotal(res.total);
    } catch (err: any) {
      setError(err?.message || "ユーザー取得に失敗しました");
      setUsers([]);
      setTotal(0);
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    loadUsers();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [page, pageSize, search]);

  const handleSearchChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    setSearch(e.target.value);
    setPage(0);
  };

  const handleChangePage = (_: unknown, newPage: number) => {
    setPage(newPage);
  };

  const handleChangeRowsPerPage = (event: React.ChangeEvent<HTMLInputElement>) => {
    setPageSize(parseInt(event.target.value, 10));
    setPage(0);
  };

  const handleEditUser = (user: AdminUser) => {
    setSelectedUser(user);
    setEditDialogOpen(true);
  };

  const handleCloseEditDialog = () => {
    setEditDialogOpen(false);
    setSelectedUser(null);
  };

  const handleEditSuccess = () => {
    loadUsers();
  };

  const hasData = users.length > 0;

  return (
    <Box sx={{ mt: 4 }} data-testid="admin-user-management">
      <Card
        sx={{
          boxShadow: "0 2px 8px rgba(0,0,0,0.1)",
          transition: "boxShadow 0.2s ease-in-out",
          "&:hover": {
            boxShadow: "0 4px 12px rgba(0,0,0,0.15)",
          },
        }}
      >
        <CardContent sx={{ p: 3 }}>
          <Box sx={{ display: "flex", justifyContent: "flex-end", mb: 2 }}>
            <Tooltip title="更新">
              <span>
                <IconButton
                  onClick={loadUsers}
                  disabled={isLoading}
                  aria-label="refresh"
                  sx={{
                    transition: "all 0.2s ease-in-out",
                    "&:hover": {
                      backgroundColor: "rgba(25, 118, 210, 0.08)",
                    },
                  }}
                >
                  <RefreshIcon />
                </IconButton>
              </span>
            </Tooltip>
          </Box>

          <TextField
            fullWidth
            size="small"
            placeholder="メールアドレスまたは表示名で検索"
            value={search}
            onChange={handleSearchChange}
            sx={{
              mb: 2,
              "& .MuiOutlinedInput-root": {
                transition: "all 0.2s ease-in-out",
                "&:hover": {
                  backgroundColor: "rgba(25, 118, 210, 0.02)",
                },
              },
            }}
            inputProps={{ "data-testid": "user-search" }}
          />

          {error && (
            <Alert
              severity="error"
              sx={{ mb: 2 }}
              data-testid="user-error"
              onClose={() => setError(null)}
            >
              {error}
            </Alert>
          )}

          <Box sx={{ position: "relative", overflowX: "auto" }}>
            <Table size="small">
              <TableHead>
                <TableRow sx={{ backgroundColor: "#f5f5f5" }}>
                  <TableCell sx={{ fontWeight: 600 }}>表示名</TableCell>
                  <TableCell sx={{ fontWeight: 600 }}>メール</TableCell>
                  <TableCell sx={{ fontWeight: 600 }}>提供</TableCell>
                  <TableCell sx={{ fontWeight: 600 }}>購読</TableCell>
                  <TableCell sx={{ fontWeight: 600 }}>ステータス</TableCell>
                  <TableCell sx={{ fontWeight: 600 }}>登録</TableCell>
                  <TableCell sx={{ fontWeight: 600 }}>最終ログイン</TableCell>
                  <TableCell sx={{ fontWeight: 600 }}>操作</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {hasData ? (
                  users.map((user) => (
                    <TableRow
                      key={user.id}
                      hover
                      onClick={() => onViewDetail?.(user.id)}
                      sx={{
                        cursor: "pointer",
                        transition: "background-color 0.2s",
                        "&:hover": {
                          backgroundColor: "rgba(25, 118, 210, 0.05)",
                        },
                      }}
                    >
                      <TableCell>{user.name || "-"}</TableCell>
                      <TableCell>{user.email}</TableCell>
                      <TableCell>
                        <Chip
                          label={user.canProvide ? "可" : "不可"}
                          color={user.canProvide ? "success" : "default"}
                          size="small"
                          variant="filled"
                        />
                      </TableCell>
                      <TableCell>
                        <Chip
                          label={user.canSubscribe ? "可" : "不可"}
                          color={user.canSubscribe ? "success" : "default"}
                          size="small"
                          variant="filled"
                        />
                      </TableCell>
                      <TableCell>
                        <Chip
                          label={user.isActive ? "有効" : "無効"}
                          color={user.isActive ? "success" : "default"}
                          size="small"
                          variant="filled"
                        />
                      </TableCell>
                      <TableCell>{formatDateTime(user.registeredAt)}</TableCell>
                      <TableCell>{formatDateTime(user.lastLoginAt)}</TableCell>
                      <TableCell>
                        <Tooltip title="編集">
                          <IconButton
                            size="small"
                            onClick={(e) => {
                              e.stopPropagation();
                              handleEditUser(user);
                            }}
                            sx={{
                              color: "primary.main",
                              transition: "all 0.2s ease-in-out",
                              "&:hover": {
                                backgroundColor: "rgba(25, 118, 210, 0.1)",
                                transform: "translateY(-2px)",
                              },
                            }}
                          >
                            <EditIcon fontSize="small" />
                          </IconButton>
                        </Tooltip>
                      </TableCell>
                    </TableRow>
                  ))
                ) : (
                  <TableRow>
                    <TableCell colSpan={8} align="center" sx={{ py: 3 }}>
                      <Typography variant="body2" color="text.secondary">
                        {isLoading ? "読み込み中..." : "データがありません"}
                      </Typography>
                    </TableCell>
                  </TableRow>
                )}
              </TableBody>
            </Table>

            {isLoading && (
              <Box
                sx={{
                  position: "absolute",
                  inset: 0,
                  display: "flex",
                  alignItems: "center",
                  justifyContent: "center",
                  bgcolor: "rgba(255,255,255,0.6)",
                }}
                data-testid="user-loading"
              >
                <CircularProgress size={32} />
              </Box>
            )}
          </Box>

          <TablePagination
            component="div"
            count={total}
            page={page}
            onPageChange={handleChangePage}
            rowsPerPage={pageSize}
            onRowsPerPageChange={handleChangeRowsPerPage}
            labelDisplayedRows={({ from, to, count }) => `${from}-${to} / ${count}`}
            rowsPerPageOptions={[5, 10, 20]}
          />
        </CardContent>
      </Card>

      <AdminUserEditDialog
        open={editDialogOpen}
        user={selectedUser}
        onClose={handleCloseEditDialog}
        onSuccess={handleEditSuccess}
      />
    </Box>
  );
};
