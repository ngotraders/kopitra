# Ops Console Contribution Guidelines

These instructions apply to the entire `opsconsole` directory, including nested content.

## Storybook Coverage

- Every React component (files ending in `.tsx` that render UI) must have a co-located Storybook story file using Component Story Format (CSF).
- Each story file must export at least one story with a `play` function that exercises a meaningful interaction or assertion using Testing Library utilities (e.g., `within`, `expect`).
- When adding new stories, ensure they are discoverable under logical titles that match the component's folder structure.

## Testing

- Custom hooks and non-component library functions must include Vitest unit tests.
- Prefer colocating tests next to the implementation using the `*.test.ts` or `*.test.tsx` naming convention.

## Documentation & Comments

- Author comments and documentation in English.

## Tooling

- Keep Storybook and Vitest configurations in sync with the component and hook requirements above.

## Required Checks

Before submitting changes under `opsconsole/`, run:

- `npm run format:check`
- `npm run lint`
- `npm run build`
- `npm run test`
