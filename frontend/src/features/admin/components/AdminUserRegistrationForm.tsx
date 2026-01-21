import React, { useState } from "react";
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
} from "@mui/material";
import { userManagementApi, type AdminUserCreateRequest } from "../../../api";

interface UserRegistrationFormProps {
  onSuccess?: (userId: string) => void;
  onError?: (error: string) => void;
}

export const AdminUserRegistrationForm: React.FC<UserRegistrationFormProps> = ({
  onSuccess,
  onError,
}) => {
  const [formData, setFormData] = useState<AdminUserCreateRequest>({
    email: "",
    name: "",
    role: "User",
  });
  const [error, setError] = useState("");
  const [success, setSuccess] = useState("");
  const [isLoading, setIsLoading] = useState(false);
  const [generatedPassword, setGeneratedPassword] = useState("");

  const handleInputChange = (field: keyof AdminUserCreateRequest, value: string) => {
    setFormData((prev) => ({
      ...prev,
      [field]: value,
    }));
    setError("");
  };

  const validateForm = (): boolean => {
    if (!formData.email) {
      setError("メールアドレスを入力してください");
      return false;
    }
    if (!formData.email.includes("@")) {
      setError("有効なメールアドレスを入力してください");
      return false;
    }
    if (!formData.name.trim()) {
      setError("名前を入力してください");
      return false;
    }
    return true;
  };

  const generatePassword = (): string => {
    const length = 12;
    const charset = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%";
    let password = "";
    for (let i = 0; i < length; i++) {
      password += charset.charAt(Math.floor(Math.random() * charset.length));
    }
    return password;
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
      const password = generatePassword();
      setGeneratedPassword(password);

      const dataWithPassword: AdminUserCreateRequest = {
        ...formData,
        initialPassword: password,
      };

      const response = await userManagementApi.createUser(dataWithPassword);
      setSuccess(`ユーザーを作成しました。ユーザーID: ${response.id}`);
      setFormData({
        email: "",
        name: "",
        role: "User",
      });

      if (onSuccess) {
        onSuccess(response.id);
      }
    } catch (err: any) {
      const errorMessage = err.message || "ユーザー作成に失敗しました";
      setError(errorMessage);
      if (onError) {
        onError(errorMessage);
      }
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <Box sx={{ maxWidth: 600, mx: "auto", mt: 4 }}>
      <Card>
        <CardContent sx={{ p: 4 }}>
          <Typography variant="h5" gutterBottom>
            ユーザー登録
          </Typography>
          <Typography variant="body2" color="text.secondary" sx={{ mb: 3 }}>
            新しいユーザーアカウントを作成します
          </Typography>

          {error && (
            <Alert severity="error" sx={{ mb: 2 }}>
              {error}
            </Alert>
          )}

          {success && (
            <Alert severity="success" sx={{ mb: 2 }}>
              {success}
              {generatedPassword && (
                <Box sx={{ mt: 1, p: 1, bgcolor: "#f0f0f0", borderRadius: 1 }}>
                  <Typography variant="body2">
                    <strong>仮パスワード:</strong>
                  </Typography>
                  <Typography
                    variant="body2"
                    sx={{
                      fontFamily: "monospace",
                      wordBreak: "break-all",
                      p: 1,
                      bgcolor: "#fff",
                      borderRadius: 0.5,
                      mt: 1,
                    }}
                  >
                    {generatedPassword}
                  </Typography>
                  <Typography variant="caption" sx={{ mt: 1, display: "block" }}>
                    ※ ユーザーに仮パスワードをお知らせください
                  </Typography>
                </Box>
              )}
            </Alert>
          )}

          <form onSubmit={handleSubmit}>
            <Grid container spacing={2}>
              <Grid>
                <TextField
                  fullWidth
                  label="メールアドレス"
                  type="email"
                  value={formData.email}
                  onChange={(e) => handleInputChange("email", e.target.value)}
                  placeholder="user@example.com"
                  disabled={isLoading}
                />
              </Grid>

              <Grid>
                <TextField
                  fullWidth
                  label="名前"
                  value={formData.name}
                  onChange={(e) => handleInputChange("name", e.target.value)}
                  placeholder="山田太郎"
                  disabled={isLoading}
                />
              </Grid>

              <Grid>
                <FormControl fullWidth disabled={isLoading}>
                  <InputLabel>権限</InputLabel>
                  <Select
                    value={formData.role}
                    label="権限"
                    onChange={(e) => handleInputChange("role", e.target.value)}
                  >
                    <MenuItem value="User">ユーザー</MenuItem>
                    <MenuItem value="Admin">管理者</MenuItem>
                  </Select>
                </FormControl>
              </Grid>

              <Grid>
                <Button
                  type="submit"
                  fullWidth
                  variant="contained"
                  size="large"
                  disabled={isLoading}
                >
                  {isLoading ? <CircularProgress size={24} /> : "ユーザーを作成"}
                </Button>
              </Grid>
            </Grid>
          </form>
        </CardContent>
      </Card>
    </Box>
  );
};
