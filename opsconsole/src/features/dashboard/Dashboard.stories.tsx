import type { Meta, StoryObj } from '@storybook/react';
import { expect, userEvent, within } from '@storybook/test';
import { Dashboard } from './Dashboard';

const meta: Meta<typeof Dashboard> = {
  component: Dashboard,
  title: 'Pages/Dashboard',
};

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const warningFilter = canvas.getByRole('button', { name: /warnings/i });
    await userEvent.click(warningFilter);
    const rows = canvas.getAllByRole('row');
    expect(rows.length).toBe(2);
  },
};
