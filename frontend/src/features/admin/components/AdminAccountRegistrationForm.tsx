import React, { useState, useEffect } from "react";
import {
  Box,
  Card,
  CardContent,
  TextField,
  Button,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  Alert,
  CircularProgress,
  Grid,
  Typography,
  Autocomplete,
} from "@mui/material";
import { accountManagementApi, type AdminAccountCreateRequest, type AdminUser } from "../../../api";
import { userManagementApi } from "../../../api";

interface AccountRegistrationFormProps {
  userId?: string;
  onSuccess?: (accountId: string) => void;
  onError?: (error: string) => void;
}

export const AdminAccountRegistrationForm: React.FC<AccountRegistrationFormProps> = ({
  userId,
  onSuccess,
  onError,
}) => {
  const [formData, setFormData] = useState<AdminAccountCreateRequest>({
    userId: userId || "",
    accountName: "",
    broker: "",
    accountNumber: "",
    status: "Active",
  });

  const [users, setUsers] = useState<AdminUser[]>([]);
  const [error, setError] = useState("");
  const [success, setSuccess] = useState("");
  const [isLoading, setIsLoading] = useState(false);
  const [loadingUsers, setLoadingUsers] = useState(false);

  useEffect(() => {
    loadUsers();
  }, []);

  const loadUsers = async () => {
    setLoadingUsers(true);
    try {
      const response = await userManagementApi.listUsers(1, 100);
      setUsers(response.users);
    } catch (err) {
      console.error("Failed to load users:", err);
    } finally {
      setLoadingUsers(false);
    }
  };

  const handleInputChange = (field: keyof AdminAccountCreateRequest, value: string | number) => {
    setFormData((prev) => ({
      ...prev,
      [field]: value,
    }));
    setError("");
  };

  const handleUserSelect = (user: AdminUser | null) => {
    if (user) {
      handleInputChange("userId", user.id);
    }
  };

  const validateForm = (): boolean => {
    if (!formData.userId) {
      setError("ユーザーを選択してください");
      return false;
    }
    if (!formData.accountName.trim()) {
      setError("アカウント名を入力してください");
      return false;
    }
    if (!formData.broker.trim()) {
      setError("ブローカーを選択してください");
      return false;
    }
    if (!formData.accountNumber.trim()) {
      setError("アカウント番号を入力してください");
      return false;
    }
    return true;
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    if (!validateForm()) {
      return;
    }

    setIsLoading(true);
    setError("");
    setSuccess("");

    try {
      const response = await accountManagementApi.createAccount(formData);
      setSuccess(`アカウントを作成しました。アカウントID: ${response.id}`);
      setFormData({
        userId: "",
        accountName: "",
        broker: "",
        accountNumber: "",
        status: "Active",
      });

      if (onSuccess) {
        onSuccess(response.id);
      }
    } catch (err: unknown) {
      const errorMessage = err instanceof Error ? err.message : "アカウント作成に失敗しました";
      setError(errorMessage);
      if (onError) {
        onError(errorMessage);
      }
    } finally {
      setIsLoading(false);
    }
  };

  const selectedUser = users.find((u) => u.id === formData.userId);

  return (
    <Box sx={{ maxWidth: 600, mx: "auto", mt: 4 }}>
      <Card>
        <CardContent sx={{ p: 4 }}>
          <Typography variant="h5" gutterBottom>
            アカウント登録
          </Typography>
          <Typography variant="body2" color="text.secondary" sx={{ mb: 3 }}>
            ユーザーの取引口座を登録します
          </Typography>

          {error && (
            <Alert severity="error" sx={{ mb: 2 }}>
              {error}
            </Alert>
          )}

          {success && (
            <Alert severity="success" sx={{ mb: 2 }}>
              {success}
            </Alert>
          )}

          <form onSubmit={handleSubmit}>
            <Grid container spacing={2}>
              <Grid>
                <Autocomplete
                  options={users}
                  getOptionLabel={(option) => `${option.name} (${option.email})`}
                  value={selectedUser || null}
                  onChange={(_, value) => handleUserSelect(value)}
                  loading={loadingUsers}
                  disabled={isLoading || !!userId}
                  renderInput={(params) => (
                    <TextField {...params} label="ユーザー" placeholder="ユーザーを選択" required />
                  )}
                />
              </Grid>

              <Grid>
                <TextField
                  fullWidth
                  label="アカウント名"
                  value={formData.accountName}
                  onChange={(e) => handleInputChange("accountName", e.target.value)}
                  placeholder="メインアカウント"
                  disabled={isLoading}
                />
              </Grid>

              <Grid>
                <FormControl fullWidth disabled={isLoading}>
                  <InputLabel id="broker-label">ブローカー</InputLabel>
                  <Select
                    labelId="broker-label"
                    value={formData.broker}
                    label="ブローカー"
                    onChange={(e) => handleInputChange("broker", e.target.value)}
                  >
                    <MenuItem value="">選択してください</MenuItem>
                    <MenuItem value="XMTrading">XM Trading</MenuItem>
                    <MenuItem value="Axiory">Axiory</MenuItem>
                    <MenuItem value="FXDD">FXDD</MenuItem>
                    <MenuItem value="TitanFX">TitanFX</MenuItem>
                    <MenuItem value="HotForex">HotForex</MenuItem>
                  </Select>
                </FormControl>
              </Grid>

              <Grid>
                <TextField
                  fullWidth
                  label="アカウント番号"
                  value={formData.accountNumber}
                  onChange={(e) => handleInputChange("accountNumber", e.target.value)}
                  placeholder="123456789"
                  disabled={isLoading}
                />
              </Grid>

              <Grid>
                <FormControl fullWidth disabled={isLoading}>
                  <InputLabel id="status-label">ステータス</InputLabel>
                  <Select
                    labelId="status-label"
                    value={formData.status}
                    label="ステータス"
                    onChange={(e) => handleInputChange("status", e.target.value)}
                  >
                    <MenuItem value="Active">有効</MenuItem>
                    <MenuItem value="Inactive">無効</MenuItem>
                  </Select>
                </FormControl>
              </Grid>

              <Grid>
                <TextField
                  fullWidth
                  label="初期残高（オプション）"
                  type="number"
                  value={formData.initialBalance || ""}
                  onChange={(e) =>
                    handleInputChange(
                      "initialBalance",
                      e.target.value ? parseFloat(e.target.value) : 0
                    )
                  }
                  placeholder="0"
                  disabled={isLoading}
                  inputProps={{ step: "0.01", min: "0" }}
                />
              </Grid>

              <Grid>
                <Button
                  type="submit"
                  fullWidth
                  variant="contained"
                  size="large"
                  disabled={isLoading || loadingUsers}
                >
                  {isLoading ? <CircularProgress size={24} /> : "アカウントを作成"}
                </Button>
              </Grid>
            </Grid>
          </form>
        </CardContent>
      </Card>
    </Box>
  );
};
