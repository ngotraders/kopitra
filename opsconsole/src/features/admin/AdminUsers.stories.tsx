import type { Meta, StoryObj } from '@storybook/react';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { expect, within } from '@storybook/test';
import {
  AdminUserActivity,
  AdminUserDetailLayout,
  AdminUserOverview,
  AdminUserPermissions,
  AdminUsersList,
} from './AdminUsers';
import { withQueryClient } from '../../test/withQueryClient';

const meta: Meta<typeof AdminUsersList> = {
  component: AdminUsersList,
  title: 'Admin/Users',
  decorators: [withQueryClient],
};

export default meta;

type Story = StoryObj<typeof meta>;

export const Catalogue: Story = {
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    expect(canvas.getByText(/manage console access/i)).toBeVisible();
  },
};

function renderUserDetail(path: string) {
  return (
    <MemoryRouter initialEntries={[path]}>
      <Routes>
        <Route path="/admin/users/:userId" element={<AdminUserDetailLayout />}>
          <Route path="overview" element={<AdminUserOverview />} />
          <Route path="permissions" element={<AdminUserPermissions />} />
          <Route path="activity" element={<AdminUserActivity />} />
        </Route>
      </Routes>
    </MemoryRouter>
  );
}

export const DetailOverview: StoryObj<typeof meta> = {
  render: () => renderUserDetail('/admin/users/user-1/overview?environment=sandbox'),
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    expect(canvas.getByText(/least-privilege access/i)).toBeVisible();
  },
};

export const DetailPermissions: StoryObj<typeof meta> = {
  render: () => renderUserDetail('/admin/users/user-1/permissions?environment=sandbox'),
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    expect(canvas.getByText(/permission matrix/i)).toBeVisible();
  },
};

export const DetailActivity: StoryObj<typeof meta> = {
  render: () => renderUserDetail('/admin/users/user-1/activity?environment=sandbox'),
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    expect(canvas.getByRole('table')).toBeInTheDocument();
  },
};
