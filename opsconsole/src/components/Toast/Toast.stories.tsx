import type { Meta, StoryObj } from '@storybook/react';
import { expect, fn, userEvent, within } from '@storybook/test';
import { Toast } from './Toast';

const meta: Meta<typeof Toast> = {
  component: Toast,
  title: 'Components/Toast',
  args: {
    title: 'Access denied',
    description: 'You do not have permission to view that area.',
    intent: 'error',
    onDismiss: fn(),
  },
};

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {
  play: async ({ canvasElement, args }) => {
    const canvas = within(canvasElement);
    const dismiss = canvas.getByRole('button', { name: /dismiss notification/i });
    await userEvent.click(dismiss);
    expect(args.onDismiss).toHaveBeenCalledTimes(1);
  },
};
