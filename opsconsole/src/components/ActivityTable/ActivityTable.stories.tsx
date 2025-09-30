import type { Meta, StoryObj } from '@storybook/react';
import { expect, within } from '@storybook/test';
import { activities } from '../../data/dashboard.ts';
import { ActivityTable } from './ActivityTable';

const meta: Meta<typeof ActivityTable> = {
  component: ActivityTable,
  title: 'Dashboard/ActivityTable',
  args: {
    activities,
  },
};

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const rows = canvas.getAllByRole('row');
    expect(rows.length).toBeGreaterThan(1);
    expect(canvas.getByText(/recent activity/i)).toBeVisible();
  },
};

export const Filtered: Story = {
  args: {
    activities: activities.filter((activity) => activity.status === 'error'),
  },
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    expect(canvas.getAllByText(/error/i)).toHaveLength(1);
  },
};
