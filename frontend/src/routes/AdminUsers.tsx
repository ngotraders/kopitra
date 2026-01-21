import React from "react";
import { useNavigate } from "react-router-dom";
import { Container, Box, Fade, Typography, Button } from "@mui/material";
import { AdminUserManagementPage } from "../features/admin";

export interface AdminUsersProps {
  onCreateNew?: () => void;
}

export const AdminUsers: React.FC<AdminUsersProps> = ({ onCreateNew }) => {
  const navigate = useNavigate();

  const handleViewDetail = (userId: string) => {
    navigate(`/admin/users/${userId}`);
  };

  return (
    <Fade in={true} timeout={500}>
      <Container maxWidth="lg" sx={{ mt: 4, mb: 6 }}>
        <Box sx={{ mb: 4, display: "flex", justifyContent: "space-between", alignItems: "center" }}>
          <Box>
            <Typography variant="h4" sx={{ fontWeight: 600, mb: 1 }}>
              ユーザー管理
            </Typography>
            <Typography variant="body2" color="text.secondary">
              システムユーザーを登録・管理できます
            </Typography>
          </Box>
          <Button variant="contained" color="primary" onClick={onCreateNew}>
            + 新規作成
          </Button>
        </Box>

        <Box sx={{ mt: 4 }}>
          <AdminUserManagementPage onViewDetail={handleViewDetail} />
        </Box>
      </Container>
    </Fade>
  );
};
