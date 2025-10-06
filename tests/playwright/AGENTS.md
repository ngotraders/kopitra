# Playwright End-to-End Tests Guide

These instructions apply to the entire `tests/playwright` directory.

## Required Checks

Before submitting changes under `tests/playwright/`, run:

- `npx prettier@3.4.2 --check "**/*.{ts,tsx,js,json,md}"`
- `npx tsc --noEmit`
- `npx playwright test`
