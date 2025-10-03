import type { Decorator } from '@storybook/react';
import { QueryClientProvider } from '@tanstack/react-query';
import { createTestQueryClient } from './queryClient';

export const withQueryClient: Decorator = (Story) => {
  const queryClient = createTestQueryClient();
  return (
    <QueryClientProvider client={queryClient}>
      <Story />
    </QueryClientProvider>
  );
};
