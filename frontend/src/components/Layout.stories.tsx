import type { Meta, StoryObj } from "@storybook/react-vite";
import { Layout } from "./Layout";
import { AuthProvider } from "../providers/AuthProvider";
import { BrowserRouter } from "react-router-dom";

const meta = {
  title: "Components/Layout",
  component: Layout,
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
} satisfies Meta<typeof Layout>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {
  args: {
    children: (
      <div style={{ padding: "20px" }}>
        <h1>サンプルコンテンツ</h1>
        <p>これはレイアウトコンポーネントのサンプルです。</p>
      </div>
    ),
  },
};

export const WithLongContent: Story = {
  args: {
    children: (
      <div style={{ padding: "20px" }}>
        <h1>長いコンテンツのテスト</h1>
        {Array.from({ length: 20 }, (_, i) => (
          <p key={i}>
            これはダミーテキストです。コンテンツがスクロール可能かどうかを確認するためのものです。
            段落番号: {i + 1}
          </p>
        ))}
      </div>
    ),
  },
};
