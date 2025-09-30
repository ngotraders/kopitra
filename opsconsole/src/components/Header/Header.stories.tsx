import type { Meta, StoryObj } from '@storybook/react';
import { expect, fn, userEvent, within } from '@storybook/test';
import { Header } from './Header';

const meta: Meta<typeof Header> = {
  component: Header,
  title: 'Layout/Header',
  args: {
    environment: 'Production',
    userName: 'Alex Morgan',
    onSignOut: fn(),
  },
};

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {
  play: async ({ canvasElement, args }) => {
    const canvas = within(canvasElement);
    await userEvent.click(canvas.getByRole('button', { name: /sign out/i }));
    expect(args.onSignOut).toHaveBeenCalledTimes(1);
  },
};

export const Sandbox: Story = {
  args: {
    environment: 'Sandbox',
  },
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const badge = canvas.getByText(/sandbox/i);
    expect(badge).toBeInTheDocument();
  },
};
