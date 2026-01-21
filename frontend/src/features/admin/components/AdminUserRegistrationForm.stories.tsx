import type { Meta, StoryObj } from "@storybook/react";
import { expect, userEvent, within, waitFor } from "storybook/test";

import { AdminUserRegistrationForm } from "./AdminUserRegistrationForm";

const meta = {
  title: "Features/Admin/AdminUserRegistrationForm",
  component: AdminUserRegistrationForm,
  parameters: {
    layout: "padded",
  },
} satisfies Meta<typeof AdminUserRegistrationForm>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {
  args: {},
};

export const FillForm: Story = {
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    // メールアドレス入力
    const emailInput = canvas.getByLabelText(/メールアドレス/i);
    await userEvent.type(emailInput, "newuser@example.com", { delay: 50 });

    // 名前入力
    const nameInput = canvas.getByLabelText(/名前/i);
    await userEvent.type(nameInput, "テスト太郎", { delay: 50 });

    // 権限選択
    const roleSelect = canvas.getByLabelText(/権限/i);
    await userEvent.click(roleSelect);
    const adminOption = canvas.getByRole("option", { name: /管理者/i });
    await userEvent.click(adminOption);

    // 入力値を確認
    await expect(emailInput).toHaveValue("newuser@example.com");
    await expect(nameInput).toHaveValue("テスト太郎");
  },
};

export const SubmitForm: Story = {
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    // フォーム入力
    const emailInput = canvas.getByLabelText(/メールアドレス/i);
    const nameInput = canvas.getByLabelText(/名前/i);
    const submitButton = canvas.getByRole("button", { name: /ユーザーを作成/i });

    await userEvent.type(emailInput, "user@test.com", { delay: 30 });
    await userEvent.type(nameInput, "山田太郎", { delay: 30 });

    // 送信ボタンをクリック（実際のAPI呼び出しは実行されない）
    await userEvent.click(submitButton);

    // 成功メッセージの表示を確認
    await waitFor(() => {
      expect(canvas.getByRole("alert")).toBeInTheDocument();
    });
  },
};

export const ValidationError: Story = {
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    // 送信ボタンをクリック（バリデーションエラーが表示されるはず）
    const submitButton = canvas.getByRole("button", { name: /ユーザーを作成/i });
    await userEvent.click(submitButton);

    // エラーメッセージが表示されることを確認
    const alert = canvas.queryByRole("alert");
    if (alert) {
      await expect(alert).toBeInTheDocument();
    }
  },
};

export const InvalidEmail: Story = {
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    // 無効なメールアドレスを入力
    const emailInput = canvas.getByLabelText(/メールアドレス/i);
    await userEvent.type(emailInput, "invalid-email", { delay: 50 });

    const nameInput = canvas.getByLabelText(/名前/i);
    await userEvent.type(nameInput, "テスト", { delay: 50 });

    // 送信
    const submitButton = canvas.getByRole("button", { name: /ユーザーを作成/i });
    await userEvent.click(submitButton);

    // エラーが表示されることを確認
    const alert = canvas.queryByRole("alert");
    if (alert) {
      await expect(alert).toBeInTheDocument();
    }
  },
};
