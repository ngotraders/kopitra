import React from "react";
import { Dialog, DialogTitle, DialogContent, DialogActions, Button, Slide } from "@mui/material";
import type { TransitionProps } from "@mui/material/transitions";
import { AdminUserRegistrationForm } from "./AdminUserRegistrationForm";

const Transition = React.forwardRef(function Transition(
  props: TransitionProps & {
    children: React.ReactElement;
  },
  ref: React.Ref<unknown>
) {
  return <Slide direction="up" ref={ref} {...props} />;
});

interface AdminUserNewDialogProps {
  open: boolean;
  onClose: () => void;
  onSuccess?: () => void;
}

export const AdminUserNewDialog: React.FC<AdminUserNewDialogProps> = ({
  open,
  onClose,
  onSuccess,
}) => {
  const formRef = React.useRef<{
    submit: () => Promise<void>;
  }>(null);
  const [isSubmitting, setIsSubmitting] = React.useState(false);

  const handleSuccess = () => {
    if (onSuccess) {
      onSuccess();
    }
    onClose();
  };

  const handleSubmitClick = async () => {
    setIsSubmitting(true);
    try {
      if (formRef.current) {
        await formRef.current.submit();
        handleSuccess();
      }
    } catch (err) {
      console.error("Submit error:", err);
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <Dialog
      open={open}
      TransitionComponent={Transition}
      onClose={onClose}
      maxWidth="sm"
      fullWidth
      PaperProps={{
        sx: {
          borderRadius: 2,
          boxShadow: "0 20px 60px rgba(0,0,0,0.3)",
        },
      }}
    >
      <DialogTitle sx={{ pb: 1, fontWeight: 600 }}>ユーザー新規作成</DialogTitle>
      <DialogContent dividers sx={{ py: 3 }}>
        <AdminUserRegistrationForm ref={formRef} onSuccess={handleSuccess} />
      </DialogContent>
      <DialogActions sx={{ p: 2 }}>
        <Button onClick={onClose} disabled={isSubmitting}>
          キャンセル
        </Button>
        <Button
          onClick={handleSubmitClick}
          variant="contained"
          color="primary"
          disabled={isSubmitting}
        >
          {isSubmitting ? "作成中..." : "作成"}
        </Button>
      </DialogActions>
    </Dialog>
  );
};
