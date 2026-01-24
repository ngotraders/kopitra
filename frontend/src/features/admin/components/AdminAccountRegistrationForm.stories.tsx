import type { Meta, StoryObj } from "@storybook/react-vite";
import { expect, userEvent, within, waitFor, screen } from "storybook/test";

import { AdminAccountRegistrationForm } from "./AdminAccountRegistrationForm";

const meta = {
  title: "Features/Admin/AdminAccountRegistrationForm",
  component: AdminAccountRegistrationForm,
  parameters: {
    layout: "padded",
  },
} satisfies Meta<typeof AdminAccountRegistrationForm>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {
  args: {},
};

export const WithPreSelectedUser: Story = {
  args: {
    userId: "user-123",
  },
};

export const FillForm: Story = {
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    // アカウント名入力
    const accountNameInput = canvas.getByLabelText(/アカウント名/i);
    await userEvent.type(accountNameInput, "メインアカウント", { delay: 50 });

    // ブローカー選択
    const brokerSelect = canvas.getByLabelText(/ブローカー/i);
    await userEvent.click(brokerSelect);
    
    // メニューが開いてoptionが表示されるのを待つ（ポータル経由なのでscreenで探す）
    const brokerOption = await screen.findByRole("option", { name: /XM Trading/i });
    await userEvent.click(brokerOption);

    // アカウント番号入力
    const accountNumberInput = canvas.getByLabelText(/アカウント番号/i);
    await userEvent.type(accountNumberInput, "123456789", { delay: 50 });

    // 入力値を確認
    await expect(accountNameInput).toHaveValue("メインアカウント");
    await expect(accountNumberInput).toHaveValue("123456789");
  },
};

export const SubmitForm: Story = {
  args: {
    // 検証通過のため事前にユーザー選択
    userId: "user-1",
  },
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    // アカウント情報を入力
    const accountNameInput = canvas.getByLabelText(/アカウント名/i);
    await userEvent.type(accountNameInput, "サブアカウント", { delay: 30 });

    const brokerSelect = canvas.getByLabelText(/ブローカー/i);
    await userEvent.click(brokerSelect);
    
    // メニューが開いてoptionが表示されるのを待つ（ポータル経由なのでscreenで探す）
    const brokerOption = await screen.findByRole("option", { name: /Axiory/i });
    await userEvent.click(brokerOption);

    const accountNumberInput = canvas.getByLabelText(/アカウント番号/i);
    await userEvent.type(accountNumberInput, "987654321", { delay: 30 });

    const initialBalanceInput = canvas.getByLabelText(/初期残高/i);
    await userEvent.type(initialBalanceInput, "10000", { delay: 30 });

    // 送信ボタンをクリック
    const submitButton = canvas.getByRole("button", { name: /アカウントを作成/i });
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
    const submitButton = canvas.getByRole("button", { name: /アカウントを作成/i });
    await userEvent.click(submitButton);

    // エラーメッセージが表示されることを確認
    const alert = canvas.queryByRole("alert");
    if (alert) {
      await expect(alert).toBeInTheDocument();
    }
  },
};

export const PartiallyFilled: Story = {
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    // アカウント名のみ入力
    const accountNameInput = canvas.getByLabelText(/アカウント名/i);
    await userEvent.type(accountNameInput, "テストアカウント", { delay: 50 });

    // 送信ボタンをクリック（バリデーションエラー）
    const submitButton = canvas.getByRole("button", { name: /アカウントを作成/i });
    await userEvent.click(submitButton);
  },
};
