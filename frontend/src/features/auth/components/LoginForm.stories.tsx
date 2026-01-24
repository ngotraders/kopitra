import type { Meta, StoryObj } from "@storybook/react-vite";
import { LoginForm } from "./LoginForm";
import { AuthProvider } from "../../../providers/AuthProvider";
import { BrowserRouter } from "react-router-dom";

const meta = {
  title: "Features/Auth/LoginForm",
  component: LoginForm,
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
} satisfies Meta<typeof LoginForm>;

export default meta;
type Story = StoryObj<typeof meta>;

export const LoginTab: Story = {
  parameters: {
    docs: {
      description: {
        story:
          "ログインタブが表示されている状態です。メールアドレスとパスワードを入力してログインできます。",
      },
    },
  },
};

export const RegisterTab: Story = {
  play: async ({ canvasElement }) => {
    // タブをクリックして登録画面に切り替える
    const canvas = canvasElement;
    const registerTab = canvas.querySelector('[role="tab"]:last-child') as HTMLElement;
    if (registerTab) {
      registerTab.click();
    }
  },
  parameters: {
    docs: {
      description: {
        story:
          "新規登録タブが表示されている状態です。名前、メールアドレス、パスワードを入力して登録できます。",
      },
    },
  },
};

export const WithError: Story = {
  parameters: {
    docs: {
      description: {
        story: "エラーメッセージが表示されている状態です。",
      },
    },
  },
};

export const Loading: Story = {
  parameters: {
    docs: {
      description: {
        story: "ログイン処理中のローディング状態です。",
      },
    },
  },
};
