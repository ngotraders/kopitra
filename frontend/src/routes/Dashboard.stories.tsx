import type { Meta, StoryObj } from "@storybook/react-vite";
import { Dashboard } from "./Dashboard";
import { AuthProvider } from "../providers/AuthProvider";
import { BrowserRouter } from "react-router-dom";

const meta = {
  title: "Routes/Dashboard",
  component: Dashboard,
  parameters: {
    layout: "fullscreen",
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
} satisfies Meta<typeof Dashboard>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {
  parameters: {
    docs: {
      description: {
        story: "ダッシュボードページです。ユーザー情報が表示されます。",
      },
    },
  },
};

export const AdminUser: Story = {
  parameters: {
    docs: {
      description: {
        story: "管理者権限を持つユーザーのダッシュボードです。",
      },
    },
  },
};

export const RegularUser: Story = {
  parameters: {
    docs: {
      description: {
        story: "一般ユーザーのダッシュボードです。",
      },
    },
  },
};
