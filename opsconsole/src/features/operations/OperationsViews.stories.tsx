import type { Meta, StoryObj } from '@storybook/react';
import { expect, within } from '@storybook/test';
import {
  OperationsCommands,
  OperationsHistory,
  OperationsOverview,
  OperationsPerformance,
} from './OperationsViews';
import { withQueryClient } from '../../test/withQueryClient';

const overviewMeta: Meta<typeof OperationsOverview> = {
  component: OperationsOverview,
  title: 'Operations/Overview',
  decorators: [withQueryClient],
};

export default overviewMeta;

type OverviewStory = StoryObj<typeof overviewMeta>;

export const Overview: OverviewStory = {
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    expect(canvas.getAllByRole('heading', { level: 3 }).length).toBeGreaterThan(0);
  },
};

export const Commands: StoryObj<typeof overviewMeta> = {
  render: () => <OperationsCommands />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    expect(canvas.getByText(/command queue/i)).toBeVisible();
  },
};

export const History: StoryObj<typeof overviewMeta> = {
  render: () => <OperationsHistory />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    expect(canvas.getByRole('table')).toBeInTheDocument();
  },
};

export const Performance: StoryObj<typeof overviewMeta> = {
  render: () => <OperationsPerformance />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    expect(canvas.getByText(/copy group performance/i)).toBeVisible();
  },
};
