import type { Meta, StoryObj } from '@storybook/react';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { expect, within } from '@storybook/test';
import {
  CopyGroupDetailLayout,
  CopyGroupMembership,
  CopyGroupOverview,
  CopyGroupPerformance,
  CopyGroupRouting,
  CopyGroupsList,
} from './CopyGroups';
import { withQueryClient } from '../../test/withQueryClient';

const meta: Meta<typeof CopyGroupsList> = {
  component: CopyGroupsList,
  title: 'CopyGroups/Catalogue',
  decorators: [withQueryClient],
};

export default meta;

type Story = StoryObj<typeof meta>;

export const Catalogue: Story = {
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    expect(canvas.getByText(/copy groups/i)).toBeVisible();
  },
};

function renderDetail(path: string) {
  return (
    <MemoryRouter initialEntries={[path]}>
      <Routes>
        <Route path="/copy-groups/:groupId" element={<CopyGroupDetailLayout />}>
          <Route path="overview" element={<CopyGroupOverview />} />
          <Route path="membership" element={<CopyGroupMembership />} />
          <Route path="routing" element={<CopyGroupRouting />} />
          <Route path="performance" element={<CopyGroupPerformance />} />
        </Route>
      </Routes>
    </MemoryRouter>
  );
}

export const DetailOverview: StoryObj<typeof meta> = {
  render: () => renderDetail('/copy-groups/asia-momentum/overview?environment=production'),
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    expect(canvas.getByText(/operational summary/i)).toBeVisible();
  },
};

export const DetailMembership: StoryObj<typeof meta> = {
  render: () => renderDetail('/copy-groups/asia-momentum/membership?environment=production'),
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    expect(canvas.getByRole('table')).toBeInTheDocument();
  },
};

export const DetailRouting: StoryObj<typeof meta> = {
  render: () => renderDetail('/copy-groups/asia-momentum/routing?environment=production'),
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    expect(canvas.getByText(/destination/i)).toBeInTheDocument();
  },
};

export const DetailPerformance: StoryObj<typeof meta> = {
  render: () => renderDetail('/copy-groups/asia-momentum/performance?environment=production'),
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    expect(canvas.getByText(/trade agent/i)).toBeVisible();
  },
};
