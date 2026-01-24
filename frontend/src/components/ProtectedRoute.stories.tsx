import type { Meta, StoryObj } from "@storybook/react-vite";
import { ProtectedRoute } from "./ProtectedRoute";
import { AuthContext, type AuthContextType } from "../providers/AuthProvider";
import { BrowserRouter } from "react-router-dom";
import { Box, Typography } from "@mui/material";
import React from "react";

// モック用のAuthProvider
const MockAuthProvider: React.FC<{
  children: React.ReactNode;
  value: AuthContextType;
}> = ({ children, value }) => {
  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
};

const meta = {
  title: "Components/ProtectedRoute",
  component: ProtectedRoute,
  parameters: {
    layout: "fullscreen",
  },
  decorators: [
    (Story, context) => {
      const mockAuthValue: AuthContextType = {
        user: context.parameters.mockUser || null,
        token: context.parameters.mockToken || null,
        isAuthenticated: !!context.parameters.mockToken,
        isLoading: context.parameters.mockLoading || false,
        login: async () => {},
        register: async () => {},
        logout: () => {},
      };

      return (
        <BrowserRouter>
          <MockAuthProvider value={mockAuthValue}>
            <Story />
          </MockAuthProvider>
        </BrowserRouter>
      );
    },
  ],
} satisfies Meta<typeof ProtectedRoute>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Authenticated: Story = {
  args: {
    children: (
      <Box sx={{ p: 4 }}>
        <Typography variant="h4">保護されたコンテンツ</Typography>
        <Typography variant="body1" sx={{ mt: 2 }}>
          認証済みユーザーのみがこのコンテンツを見ることができます。
        </Typography>
      </Box>
    ),
  },
  parameters: {
    mockToken: "mock-token",
    mockUser: { id: "1", name: "Test User", email: "test@example.com" },
    docs: {
      description: {
        story: "認証済みユーザーの場合、子要素がレンダリングされます。",
      },
    },
  },
};

export const Loading: Story = {
  args: {
    children: (
      <Box sx={{ p: 4 }}>
        <Typography variant="h4">保護されたコンテンツ</Typography>
      </Box>
    ),
  },
  parameters: {
    mockLoading: true,
    docs: {
      description: {
        story: "認証状態を確認中の場合、ローディングスピナーが表示されます。",
      },
    },
  },
};
