import React, { useState } from "react";
import { TextField, FormControl, InputLabel, Select, MenuItem, Alert, Stack } from "@mui/material";
import { userManagementApi, type AdminUserCreateRequest } from "../../../api";

interface UserRegistrationFormProps {
  onSuccess?: (userId: string) => void;
  onError?: (error: string) => void;
}

export const AdminUserRegistrationForm = React.forwardRef<
  { submit: () => Promise<void> },
  UserRegistrationFormProps
>(({ onSuccess, onError }, ref) => {
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

  const handleSubmit = async () => {
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

  React.useImperativeHandle(ref, () => ({
    submit: () => handleSubmit(),
  }));

  return (
    <Stack spacing={2}>
      {error && (
        <Alert severity="error" onClose={() => setError("")}>
          {error}
        </Alert>
      )}

      {success && (
        <Alert severity="success" onClose={() => setSuccess("")}>
          ユーザーを作成しました
          {generatedPassword && (
            <>
              <br />
              <br />
              <strong>仮パスワード: </strong>
              <code
                style={{
                  display: "block",
                  marginTop: "8px",
                  padding: "8px",
                  backgroundColor: "rgba(0,0,0,0.05)",
                  borderRadius: "4px",
                  fontFamily: "monospace",
                  wordBreak: "break-all",
                }}
              >
                {generatedPassword}
              </code>
              <span style={{ display: "block", marginTop: "8px", fontSize: "0.875rem" }}>
                ユーザーに仮パスワードをお知らせください
              </span>
            </>
          )}
        </Alert>
      )}

      <TextField
        autoFocus
        fullWidth
        label="メールアドレス"
        type="email"
        value={formData.email}
        onChange={(e) => handleInputChange("email", e.target.value)}
        placeholder="user@example.com"
        disabled={isLoading}
        size="small"
        helperText="ユーザーのメールアドレスを入力"
      />

      <TextField
        fullWidth
        label="表示名"
        value={formData.name}
        onChange={(e) => handleInputChange("name", e.target.value)}
        placeholder="山田太郎"
        disabled={isLoading}
        size="small"
        helperText="ユーザーの表示名を入力"
      />

      <FormControl fullWidth disabled={isLoading} size="small">
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
    </Stack>
  );
});
