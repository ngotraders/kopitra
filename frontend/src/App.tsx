import React from "react";
import { BrowserRouter, Routes, Route, Navigate } from "react-router-dom";
import { CssBaseline, ThemeProvider, createTheme } from "@mui/material";
import { AuthProvider } from "./providers/AuthProvider";
import { useAuth, LoginForm } from "./features/auth";
import { Layout, ProtectedRoute } from "./components";
import { Dashboard, AdminUsers, AdminUserDetailPage } from "./routes";
import { AdminUserNewDialog } from "./features/admin";

const theme = createTheme({
  palette: {
    primary: {
      main: "#1976d2",
    },
    secondary: {
      main: "#dc004e",
    },
  },
});

const AppRoutes: React.FC = () => {
  const { isAuthenticated } = useAuth();
  const [showUserNew, setShowUserNew] = React.useState(false);

  const handleShowNewUser = () => {
    setShowUserNew(true);
  };

  const handleNewUserSuccess = () => {
    setShowUserNew(false);
  };

  return (
    <Routes>
      <Route
        path="/login"
        element={isAuthenticated ? <Navigate to="/" replace /> : <LoginForm />}
      />
      <Route
        path="/"
        element={
          <ProtectedRoute>
            <Layout>
              <Dashboard />
            </Layout>
          </ProtectedRoute>
        }
      />
      <Route
        path="/admin/users"
        element={
          <ProtectedRoute>
            <Layout>
              <>
                <AdminUsers onCreateNew={handleShowNewUser} />
                <AdminUserNewDialog
                  open={showUserNew}
                  onClose={() => setShowUserNew(false)}
                  onSuccess={handleNewUserSuccess}
                />
              </>
            </Layout>
          </ProtectedRoute>
        }
      />
      <Route
        path="/admin/users/:userId"
        element={
          <ProtectedRoute>
            <Layout>
              <AdminUserDetailPage />
            </Layout>
          </ProtectedRoute>
        }
      />
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  );
};

function App() {
  return (
    <ThemeProvider theme={theme}>
      <CssBaseline />
      <BrowserRouter>
        <AuthProvider>
          <AppRoutes />
        </AuthProvider>
      </BrowserRouter>
    </ThemeProvider>
  );
}

export default App;
