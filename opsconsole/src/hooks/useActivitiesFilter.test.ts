import { act, renderHook } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { activities } from '../data/console.ts';
import { useActivitiesFilter } from './useActivitiesFilter.ts';

describe('useActivitiesFilter', () => {
  it('returns all activities by default ordered by recency', () => {
    const { result } = renderHook(() => useActivitiesFilter(activities));

    expect(result.current.statusFilter).toBe('all');
    const ids = result.current.filteredActivities.map((item) => item.id);
    expect(ids).toEqual(['act-1', 'act-2', 'act-3', 'act-4', 'act-5']);
  });

  it('filters activities by status', () => {
    const { result } = renderHook(() => useActivitiesFilter(activities));

    act(() => {
      result.current.setStatusFilter('warning');
    });

    expect(result.current.statusFilter).toBe('warning');
    expect(result.current.filteredActivities).toHaveLength(1);
    expect(result.current.filteredActivities[0].id).toBe('act-2');
  });

  it('computes totals for each status', () => {
    const { result } = renderHook(() => useActivitiesFilter(activities));

    expect(result.current.statusTotals).toEqual({
      success: 3,
      warning: 1,
      error: 1,
    });
  });
});
