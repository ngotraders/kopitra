import type { Meta, StoryObj } from '@storybook/react';
import { expect, fn, userEvent, within } from '@storybook/test';
import { DashboardFilterBar } from './DashboardFilterBar';

const meta: Meta<typeof DashboardFilterBar> = {
  component: DashboardFilterBar,
  title: 'Dashboard/DashboardFilterBar',
  args: {
    timeframe: '24h',
    environment: 'production',
    onTimeframeChange: fn(),
    onEnvironmentChange: fn(),
  },
};

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {
  play: async ({ canvasElement, args }) => {
    const canvas = within(canvasElement);
    const sevenDayButton = canvas.getByRole('button', { name: /7 days/i });
    await userEvent.click(sevenDayButton);
    expect(args.onTimeframeChange).toHaveBeenCalledWith('7d');
  },
};
