# Ops Console Navigation Plan

## Goals

- Provide dedicated views for user administration, EA and session lifecycle, copy trading groups, and EA state/command operations.
- Surface copy trading performance so operators can trace notification fan-out, execution conversions, and EA profitability.
- Support role-based access with clear separation between configuration tasks (users, EAs, copy groups) and live operations (EA state + commands).
- Ensure URLs are predictable, REST-like, and compatible with React Router nested routes for future implementation.

## High-level Information Architecture

```
/
├── /users
│   ├── /users (list)
│   ├── /users/new
│   └── /users/:userId
│        ├── overview
│        ├── permissions
│        └── activity
├── /eas
│   ├── /eas (EA catalogue)
│   ├── /eas/:eaId
│   │    ├── overview
│   │    ├── sessions
│   │    └── commands
│   └── /eas/:eaId/sessions/:sessionId
│         ├── details
│         └── logs
├── /copy-groups
│   ├── /copy-groups (list)
│   ├── /copy-groups/new
│   └── /copy-groups/:groupId
│         ├── overview
│         ├── membership
│         ├── routing
│         └── performance
└── /operations
     ├── /operations/overview
     ├── /operations/commands
     ├── /operations/history
     └── /operations/performance
```

### Navigation tiers

1. **Primary sidebar** — exposes the four verticals: Users, EAs, Copy Groups, Operations.
2. **Secondary tabs** — nested under each section to drill into detail (e.g., EA details vs sessions).
3. **Contextual drawers** — for quick actions (issue command, add user to group) without losing navigation state.

## URL Path Strategy

| Area | Path | Purpose |
| --- | --- | --- |
| Users | `/users` | List, filter, and bulk manage users. |
| Users | `/users/new` | Create a user with role + EA access configuration. |
| Users | `/users/:userId` | Summary card of the selected user, defaulting to the `overview` tab. |
| Users | `/users/:userId/permissions` | Manage roles, copy group membership, and EA entitlements. |
| Users | `/users/:userId/activity` | Show recent actions, login events, and queued commands. |
| EAs | `/eas` | Global EA catalogue with status, owner, and active session counts. |
| EAs | `/eas/:eaId` | EA detail landing, surfacing health status, current release, and assigned copy groups. |
| EAs | `/eas/:eaId/sessions` | Drilldown to live and historical sessions for the EA. |
| EAs | `/eas/:eaId/sessions/:sessionId` | Session detail (broker account, lifecycle events, heartbeat). |
| EAs | `/eas/:eaId/commands` | Command queue specific to an EA (start, stop, reload config). |
| Copy Groups | `/copy-groups` | List of groups with membership counts and routing status. |
| Copy Groups | `/copy-groups/new` | Wizard to define routing rules and membership. |
| Copy Groups | `/copy-groups/:groupId` | Group overview, assigned EAs, and environment. |
| Copy Groups | `/copy-groups/:groupId/membership` | Manage trader + EA membership, including bulk import. |
| Copy Groups | `/copy-groups/:groupId/routing` | Configure destinations (brokers, sub-accounts) and weights. |
| Copy Groups | `/copy-groups/:groupId/performance` | Summaries of copy trade notifications vs. fills plus per-EA profit metrics. |
| Operations | `/operations/overview` | Snapshot of EA statuses, command backlog, alert feed. |
| Operations | `/operations/commands` | Real-time command issuance dashboard with presets + audit trail. |
| Operations | `/operations/history` | Queryable log of executed commands and EA state transitions. |
| Operations | `/operations/performance` | Cross-group dashboards with conversion funnels and per-EA leaderboards scoped by filters. |

> `/operations` redirects to `/operations/overview` to keep a consistent landing view.

## Navigation Flow

1. **Landing** – `/operations/overview` surfaces real-time health, recommended for on-call teams.
2. **User admin** – `/users` for listing, with modal/drawer for quick edits; `new` path launches full-screen wizard.
3. **EA session workflows** – from `/eas`, operators can open an EA detail page and dive into session-specific URLs.
4. **Copy groups** – `/copy-groups` surfaces groups; selecting one transitions to `/copy-groups/:groupId` while preserving filters via query params (e.g., `?environment=sandbox`). The `performance` tab pivots the detail view to highlight notification counts, execution conversions, and EA performance against the selected timeframe.
5. **Command dispatch** – `/operations/commands` supports multi-select of EAs or copy groups; uses query params such as `?target=ea:123&preset=restart` to prefill commands.
6. **Performance analytics** – `/operations/performance` rolls up copy trade KPIs across groups. Query params such as `?range=7d&group=asia-ltf` scope the aggregates without breaking navigation state.

## State Management Considerations

- Use React Router v6 with nested routes to keep layout components persistent (header, sidebar) while swapping content.
- Persist filters and table pagination in query string parameters so that navigation actions maintain state across refreshes. Performance tabs share the same strategy for timeframe, instrument, and EA cohort filters.
- Guard routes via role permissions (e.g., `/users` accessible only to tenant admins); unauthorized access redirects to `/operations/overview` with a toast notification.
- Lazy-load detail routes to keep initial bundle small; operations overview remains highest priority.

## Next Steps

1. Implement a router shell that mounts shared layout and integrates sidebar selection with the URL.
2. Scaffold placeholder pages for each path to validate navigation, breadcrumbs, and redirects.
3. Define TypeScript route constants to avoid typos and drive breadcrumb generation.
4. Align API clients for users, EAs, sessions, copy groups, performance analytics, and operations commands with these paths.
5. Contract API responses for copy trade metrics (notification counts, fills, P&L) to support the new performance tabs.
