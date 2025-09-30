# Ops Console

The ops console module hosts the TradeAgentEA management interface. The Vite + React + TypeScript frontend now lives directly in this directory and exposes navigation, metrics, and activity views for operations teams.

## Getting started

```bash
npm install
npm run dev
```

Visit http://localhost:5173 to view the console locally.

## Storybook

Storybook documents each UI component with interaction-focused stories. Launch the catalogue with:

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

## Formatting

Prettier formats TypeScript, JavaScript, CSS, and Markdown sources. Run the formatter with:

```bash
npm run format
```

Use `npm run format:check` in CI environments to verify that files already conform to the formatting rules.
