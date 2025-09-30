import type { Meta, StoryObj } from '@storybook/react';
import { expect, within } from '@storybook/test';
import { StatCard } from './StatCard';

const meta: Meta<typeof StatCard> = {
  component: StatCard,
  title: 'Dashboard/StatCard',
  args: {
    id: 'copy-rate',
    label: 'Copy Success Rate',
    value: '98.6%',
    delta: 2.1,
    description: 'Successful downstream fills in the last 24h.',
  },
};

export default meta;
type Story = StoryObj<typeof meta>;

export const PositiveTrend: Story = {
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    expect(canvas.getByLabelText(/percent increase/i)).toBeInTheDocument();
  },
};

export const NegativeTrend: Story = {
  args: {
    delta: -3.4,
    value: '176 ms',
    label: 'Median Latency',
  },
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    expect(canvas.getByLabelText(/percent decrease/i)).toBeInTheDocument();
  },
};
