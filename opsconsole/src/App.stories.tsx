import type { Meta, StoryObj } from '@storybook/react';
import { expect, fn, userEvent, within } from '@storybook/test';
import App from './App';

const meta: Meta<typeof App> = {
  component: App,
  title: 'Pages/App',
  args: {
    onSignOut: fn(),
  },
};

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const operationsLink = canvas.getByRole('link', { name: /operations/i });
    await userEvent.click(operationsLink);
    expect(operationsLink).toHaveAttribute('aria-current', 'page');
  },
};

export const SignOutInteraction: Story = {
  play: async ({ canvasElement, args }) => {
    const canvas = within(canvasElement);
    await userEvent.click(canvas.getByRole('button', { name: /sign out/i }));
    expect(args.onSignOut).toHaveBeenCalledTimes(1);
  },
};
