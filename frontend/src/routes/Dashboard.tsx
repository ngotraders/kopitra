import React from "react";
import { Typography, Container, Paper, Box } from "@mui/material";
import { useAuth } from "../features/auth";

export const Dashboard: React.FC = () => {
  const { user } = useAuth();

  return (
    <Container maxWidth="lg">
      <Box sx={{ mt: 4 }}>
        <Typography variant="h4" gutterBottom>
          ダッシュボード
        </Typography>
        <Paper sx={{ p: 3, mt: 3 }}>
          <Typography variant="h6" gutterBottom>
            ようこそ、{user?.displayName}さん
          </Typography>
          <Typography variant="body1" color="text.secondary">
            メールアドレス: {user?.email}
          </Typography>
          <Typography variant="body1" color="text.secondary">
            提供権限: {user?.canProvide ? "あり" : "なし"}
          </Typography>
          <Typography variant="body1" color="text.secondary">
            購読権限: {user?.canSubscribe ? "あり" : "なし"}
          </Typography>
          <Typography variant="body1" color="text.secondary">
            ステータス: {user?.isActive ? "有効" : "無効"}
          </Typography>
          <Typography variant="body1" color="text.secondary">
            登録日時: {user?.registeredAt}
          </Typography>
          {user?.lastLoginAt && (
            <Typography variant="body1" color="text.secondary">
              最終ログイン: {user.lastLoginAt}
            </Typography>
          )}
        </Paper>
      </Box>
    </Container>
  );
};
