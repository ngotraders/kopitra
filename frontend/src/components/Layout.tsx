import React from "react";
import { Link as RouterLink } from "react-router-dom";
import {
  AppBar,
  Toolbar,
  Typography,
  Box,
  IconButton,
  Menu,
  MenuItem,
  Button,
  Stack,
} from "@mui/material";
import { AccountCircle } from "@mui/icons-material";
import { useAuth } from "../features/auth";

export const Layout: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const { user, logout } = useAuth();
  const [anchorEl, setAnchorEl] = React.useState<null | HTMLElement>(null);

  const handleMenu = (event: React.MouseEvent<HTMLElement>) => {
    setAnchorEl(event.currentTarget);
  };

  const handleClose = () => {
    setAnchorEl(null);
  };

  const handleLogout = () => {
    handleClose();
    logout();
  };

  return (
    <Box sx={{ display: "flex", flexDirection: "column", minHeight: "100vh" }}>
      <AppBar position="static">
        <Toolbar>
          <Typography
            variant="h6"
            component={RouterLink}
            to="/"
            sx={{ flexGrow: 1, color: "inherit", textDecoration: "none" }}
          >
            Kopitra
          </Typography>
          <Stack direction="row" spacing={1} sx={{ mr: 2 }}>
            <Button color="inherit" component={RouterLink} to="/">
              ダッシュボード
            </Button>
            <Button color="inherit" component={RouterLink} to="/admin/users">
              ユーザー管理
            </Button>
          </Stack>
          {user && (
            <Box sx={{ display: "flex", alignItems: "center", gap: 2 }}>
              <Typography variant="body2">{user.name}</Typography>
              <IconButton size="large" onClick={handleMenu} color="inherit">
                <AccountCircle />
              </IconButton>
              <Menu
                anchorEl={anchorEl}
                anchorOrigin={{
                  vertical: "bottom",
                  horizontal: "right",
                }}
                keepMounted
                transformOrigin={{
                  vertical: "top",
                  horizontal: "right",
                }}
                open={Boolean(anchorEl)}
                onClose={handleClose}
              >
                <MenuItem onClick={handleLogout}>ログアウト</MenuItem>
              </Menu>
            </Box>
          )}
        </Toolbar>
      </AppBar>
      <Box component="main" sx={{ flexGrow: 1, p: 3 }}>
        {children}
      </Box>
    </Box>
  );
};
