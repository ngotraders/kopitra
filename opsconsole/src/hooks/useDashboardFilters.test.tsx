import { act, renderHook, waitFor } from '@testing-library/react';
import type { ReactNode } from 'react';
import { MemoryRouter } from 'react-router-dom';
import { describe, expect, it } from 'vitest';
import { useDashboardFilters } from './useDashboardFilters.ts';

describe('useDashboardFilters', () => {
  it('provides default timeframe and environment', () => {
    const wrapper = ({ children }: { children: ReactNode }) => (
      <MemoryRouter initialEntries={['/dashboard/activity']}>{children}</MemoryRouter>
    );

    const { result } = renderHook(() => useDashboardFilters(), { wrapper });

    expect(result.current.timeframe).toBe('24h');
    expect(result.current.environment).toBe('production');
  });

  it('reads values from the query string when present', () => {
    const wrapper = ({ children }: { children: ReactNode }) => (
      <MemoryRouter initialEntries={['/dashboard/statistics?timeframe=7d&environment=all']}>
        {children}
      </MemoryRouter>
    );

    const { result } = renderHook(() => useDashboardFilters(), { wrapper });

    expect(result.current.timeframe).toBe('7d');
    expect(result.current.environment).toBe('all');
  });

  it('updates search parameters when setters are invoked', async () => {
    const wrapper = ({ children }: { children: ReactNode }) => (
      <MemoryRouter initialEntries={['/dashboard/activity']}>{children}</MemoryRouter>
    );

    const { result } = renderHook(() => useDashboardFilters(), { wrapper });

    act(() => {
      result.current.setTimeframe('30d');
      result.current.setEnvironment('sandbox');
    });

    await waitFor(() => {
      expect(result.current.timeframe).toBe('30d');
      expect(result.current.environment).toBe('sandbox');
    });
  });
});
