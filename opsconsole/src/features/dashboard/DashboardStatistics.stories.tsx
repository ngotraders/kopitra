import type { Meta, StoryObj } from '@storybook/react';
import { expect, within } from '@storybook/test';
import { DashboardStatistics } from './DashboardStatistics';

const meta: Meta<typeof DashboardStatistics> = {
  component: DashboardStatistics,
  title: 'Dashboard/DashboardStatistics',
};

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    expect(canvas.getByRole('table')).toBeInTheDocument();
  },
};
