import type { Meta, StoryObj } from '@storybook/react';
import { expect, userEvent, within } from '@storybook/test';
import { DashboardActivity } from './DashboardActivity';

const meta: Meta<typeof DashboardActivity> = {
  component: DashboardActivity,
  title: 'Dashboard/DashboardActivity',
};

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const errorFilter = canvas.getByRole('button', { name: /errors/i });
    await userEvent.click(errorFilter);
    expect(errorFilter).toHaveClass('dashboard__filter--active');
  },
};
