import React, { useState } from "react";
import {
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  TextField,
  Button,
  Stack,
  FormControlLabel,
  Checkbox,
  Alert,
  Slide,
} from "@mui/material";
import { type AdminUser, type AdminUserUpdateRequest, userManagementApi } from "../../../api";

const Transition = React.forwardRef(function Transition(props: any, ref: React.Ref<any>) {
  return <Slide direction="up" ref={ref} {...props} />;
});

export interface AdminUserEditDialogProps {
  open: boolean;
  user: AdminUser | null;
  onClose: () => void;
  onSuccess: () => void;
}

export const AdminUserEditDialog: React.FC<AdminUserEditDialogProps> = ({
  open,
  user,
  onClose,
  onSuccess,
}) => {
  const [name, setname] = useState(user?.name || "");
  const [email, setEmail] = useState(user?.email || "");
  const [canProvide, setCanProvide] = useState(user?.canProvide || false);
  const [canSubscribe, setCanSubscribe] = useState(user?.canSubscribe || false);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // Reset form when user changes
  React.useEffect(() => {
    if (user) {
      setname(user.name || "");
      setEmail(user.email || "");
      setCanProvide(user.canProvide || false);
      setCanSubscribe(user.canSubscribe || false);
      setError(null);
    }
  }, [user, open]);

  const handleClose = () => {
    setError(null);
    onClose();
  };

  const handleSubmit = async () => {
    if (!user?.id) return;

    // Validation
    if (!name.trim()) {
      setError("表示名を入力してください");
      return;
    }
    if (!email.trim()) {
      setError("メールアドレスを入力してください");
      return;
    }

    setIsLoading(true);
    setError(null);

    try {
      const updateData: AdminUserUpdateRequest = {
        name: name.trim(),
        email: email.trim(),
      };

      await userManagementApi.updateUser(user.id, updateData);
      onSuccess();
      handleClose();
    } catch (err: any) {
      setError(err?.message || "ユーザーの更新に失敗しました");
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <Dialog
      open={open}
      onClose={handleClose}
      maxWidth="sm"
      fullWidth
      TransitionComponent={Transition}
      PaperProps={{
        sx: {
          borderRadius: 2,
          boxShadow: "0 20px 60px rgba(0,0,0,0.3)",
        },
      }}
    >
      <DialogTitle sx={{ pb: 1, fontWeight: 600 }}>ユーザー編集</DialogTitle>
      <DialogContent dividers sx={{ py: 3 }}>
        <Stack spacing={2}>
          {error && (
            <Alert severity="error" onClose={() => setError(null)}>
              {error}
            </Alert>
          )}

          <TextField
            autoFocus
            fullWidth
            label="表示名"
            value={name}
            onChange={(e) => setname(e.target.value)}
            disabled={isLoading}
            size="small"
            helperText="ユーザーの表示名を入力"
          />

          <TextField
            fullWidth
            label="メールアドレス"
            type="email"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            disabled={isLoading}
            size="small"
            helperText="メールアドレスを入力"
          />

          <FormControlLabel
            control={
              <Checkbox
                checked={canProvide}
                onChange={(e) => setCanProvide(e.target.checked)}
                disabled={isLoading}
              />
            }
            label="シグナル提供者になれる"
          />

          <FormControlLabel
            control={
              <Checkbox
                checked={canSubscribe}
                onChange={(e) => setCanSubscribe(e.target.checked)}
                disabled={isLoading}
              />
            }
            label="シグナル購読者になれる"
          />
        </Stack>
      </DialogContent>
      <DialogActions sx={{ p: 2 }}>
        <Button onClick={handleClose} disabled={isLoading}>
          キャンセル
        </Button>
        <Button onClick={handleSubmit} variant="contained" color="primary" disabled={isLoading}>
          {isLoading ? "保存中..." : "保存"}
        </Button>
      </DialogActions>
    </Dialog>
  );
};
