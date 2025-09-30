import { useMemo, useState } from 'react';
import type { Activity, ActivityStatus } from '../types/dashboard.ts';

export type ActivityFilter = ActivityStatus | 'all';

export interface UseActivitiesFilterResult {
  statusFilter: ActivityFilter;
  setStatusFilter: (status: ActivityFilter) => void;
  filteredActivities: Activity[];
  statusTotals: Record<ActivityStatus, number>;
}

const statusOrder: ActivityStatus[] = ['success', 'warning', 'error'];

export function useActivitiesFilter(activities: Activity[]): UseActivitiesFilterResult {
  const [statusFilter, setStatusFilter] = useState<ActivityFilter>('all');

  const statusTotals = useMemo(() => {
    return activities.reduce(
      (acc, activity) => {
        acc[activity.status] += 1;
        return acc;
      },
      { success: 0, warning: 0, error: 0 } as Record<ActivityStatus, number>,
    );
  }, [activities]);

  const filteredActivities = useMemo(() => {
    if (statusFilter === 'all') {
      return [...activities].sort((a, b) => (a.timestamp < b.timestamp ? 1 : -1));
    }

    return activities
      .filter((activity) => activity.status === statusFilter)
      .sort((a, b) => (a.timestamp < b.timestamp ? 1 : -1));
  }, [activities, statusFilter]);

  const sortedTotals = useMemo(() => {
    const entries = statusOrder.map((status) => [status, statusTotals[status]] as const);
    return Object.fromEntries(entries) as Record<ActivityStatus, number>;
  }, [statusTotals]);

  return {
    statusFilter,
    setStatusFilter,
    filteredActivities,
    statusTotals: sortedTotals,
  };
}
