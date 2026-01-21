import type { Meta, StoryObj } from "@storybook/react";
import { ProtectedRoute } from "./ProtectedRoute";
import { AuthProvider } from "../features/auth/hooks/useAuth";
import { BrowserRouter } from "react-router-dom";
import { Box, Typography } from "@mui/material";

const meta = {
  title: "Components/ProtectedRoute",
  component: ProtectedRoute,
  parameters: {
    layout: "fullscreen",
    test: { disable: true },
  },
  decorators: [
    (Story) => (
      <BrowserRouter>
        <AuthProvider>
          <Story />
        </AuthProvider>
      </BrowserRouter>
    ),
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
    docs: {
      description: {
        story: "認証状態を確認中の場合、ローディングスピナーが表示されます。",
      },
    },
  },
};
