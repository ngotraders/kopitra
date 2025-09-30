# Ops Console Web

This package contains the Vite + React + TypeScript front-end for the TradeAgentEA operations console. It provides a management dashboard that surfaces key replication metrics, recent activity, and navigation for the broader platform.

## Getting started

```bash
npm install
npm run dev
```

Visit http://localhost:5173 to view the console locally.

## Storybook

Storybook is configured with interaction tests for every UI component. Run the catalogue with:

```bash
npm run storybook
```

Stories live next to their components and each story defines a `play` function that validates expected behaviour using Testing Library helpers.

## Testing

Vitest powers the unit tests for hooks and supporting libraries.

```bash
npm test
```

The suite includes coverage for the `useActivitiesFilter` hook that drives dashboard filtering state.
