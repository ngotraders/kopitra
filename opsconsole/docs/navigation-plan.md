# Ops Console Navigation Plan

## Goals

- Provide dedicated views for user administration, EA and session lifecycle, copy trading groups, and EA state/command operations.
- Surface copy trading performance so operators can trace notification fan-out, execution conversions, and EA profitability.
- Support role-based access with clear separation between configuration tasks (users, EAs, copy groups) and live operations (EA state + commands).
- Ensure URLs are predictable, REST-like, and compatible with React Router nested routes for future implementation.

## High-level Information Architecture

The console prioritizes day-to-day operations while keeping low-frequency administration tasks discoverable but discreet. The sidebar only contains workstreams that operators open repeatedly during a shift. Low-frequency flows (user onboarding, copy group creation) live behind action buttons or secondary menus within their parent areas.

```
/
├── /operations (default landing)
│   ├── /operations/overview
│   ├── /operations/commands
│   ├── /operations/history
│   └── /operations/performance
├── /copy-groups
│   ├── /copy-groups (list)
│   └── /copy-groups/:groupId
│         ├── overview
│         ├── membership
│         ├── routing
│         └── performance
├── /eas
│   ├── /eas (EA catalogue)
│   ├── /eas/:eaId
│   │    ├── overview
│   │    ├── sessions
│   │    └── commands
│   └── /eas/:eaId/sessions/:sessionId
│         ├── details
│         └── logs
└── /admin
     └── /admin/users
          ├── /admin/users (list)
          ├── /admin/users/:userId
          │     ├── overview
          │     ├── permissions
          │     └── activity
          └── (drawer) create-user
```

### Navigation tiers

1. **Primary sidebar** — exposes Operations, Copy Groups, EAs, and the compact Admin section.
2. **Secondary tabs** — nested under each section to drill into detail (e.g., EA details vs. sessions, copy group performance).
3. **Contextual drawers** — for quick actions (issue command, add user to group, invite user) without losing navigation state.

## URL Path Strategy

| Area | Path | Purpose |
| --- | --- | --- |
| Admin › Users | `/admin/users` | List, filter, and bulk manage users. Quick filters for inactive users and role.
| Admin › Users | `/admin/users/:userId` | Summary card of the selected user, defaulting to the `overview` tab.
| Admin › Users | `/admin/users/:userId/permissions` | Manage roles, copy group membership, and EA entitlements.
| Admin › Users | `/admin/users/:userId/activity` | Show recent actions, login events, and queued commands.
| EAs | `/eas` | Global EA catalogue with status, owner, and active session counts. |
| EAs | `/eas/:eaId` | EA detail landing, surfacing health status, current release, and assigned copy groups. |
| EAs | `/eas/:eaId/sessions` | Drilldown to live and historical sessions for the EA. |
| EAs | `/eas/:eaId/sessions/:sessionId` | Session detail (broker account, lifecycle events, heartbeat). |
| EAs | `/eas/:eaId/commands` | Command queue specific to an EA (start, stop, reload config). |
| Copy Groups | `/copy-groups` | List of groups with membership counts and routing status. |
| Copy Groups | `drawer:/copy-groups/new` | Wizard to define routing rules and membership, launched from "Create" button not sidebar. |
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

1. **Landing** – `/operations/overview` surfaces real-time health and serves as the default redirect for signed-in operators.
2. **On-call interventions** – `/operations/commands` exposes presets and bulk actions with audit trails; `/operations/history` validates outcomes.
3. **EA session workflows** – from `/eas`, operators can open an EA detail page and dive into session-specific URLs for heartbeat checks and log inspection.
4. **Copy group tuning** – `/copy-groups` surfaces groups; selecting one transitions to `/copy-groups/:groupId` while preserving filters via query params (e.g., `?environment=sandbox`). The `performance` tab highlights notification counts, execution conversions, and EA profit against timeframe and instrument filters.
5. **Performance analytics** – `/operations/performance` rolls up copy trade KPIs across groups. Query params such as `?range=7d&group=asia-ltf` scope the aggregates without breaking navigation state.
6. **Administrative upkeep** – `/admin/users` gives tenant admins a dedicated but secondary area. User creation launches a drawer (`drawer:/admin/users/new`) to avoid occupying primary navigation.

## State Management Considerations

- Use React Router v6 with nested routes to keep layout components persistent (header, sidebar) while swapping content.
- Persist filters and table pagination in query string parameters so that navigation actions maintain state across refreshes. Performance tabs share the same strategy for timeframe, instrument, and EA cohort filters.
- Guard routes via role permissions (e.g., `/users` accessible only to tenant admins); unauthorized access redirects to `/operations/overview` with a toast notification.
- Lazy-load detail routes to keep initial bundle small; operations overview remains highest priority.

## Use-case Scenarios and Screen Specifications

### Operations Overview (`/operations/overview`)

- **Primary use case**: On-call operator reviewing live system health during trading hours.
- **Layout**: Three-column grid with (1) fleet status summary cards (active EAs, stalled sessions, broker outages), (2) incident timeline with acknowledgement controls, and (3) copy trade funnel widgets (notifications sent, fills, conversion rate for current trading session).
- **Key interactions**: Inline acknowledgement, drill-down links to affected EAs or copy groups, time range selector (auto-refresh every 15s by default).

### Operations Commands (`/operations/commands`)

- **Primary use case**: Rapidly pausing or restarting EAs when anomalies appear.
- **Layout**: Left rail with saved presets and targeting chips; central command composer with validation preview; right panel showing pending and recently executed commands with status badges.
- **Key interactions**: Multi-select of targets, preset application, inline confirmation drawer that logs who approved the command, and live result streaming.

### Operations Performance (`/operations/performance`)

- **Primary use case**: Quantifying copy trade effectiveness at the console level.
- **Layout**: Timeframe and cohort filter header, KPI tiles (notification volume, fill conversions, aggregate P&L, latency), stacked bar chart for notification vs. fill counts per copy group, leaderboard table listing EAs with per-trade P&L, win rate, and conversion delta.
- **Key interactions**: Export to CSV, compare two time ranges, apply environment filter that syncs with copy group detail pages via query params.

### Copy Group Detail (`/copy-groups/:groupId`)

- **Overview tab**: Cards summarizing member counts, default routing, recent incidents. Quick actions include "Add EA" drawer and "Duplicate group" wizard.
- **Membership tab**: Table with trader and EA entries, filterable by role, including a "Bulk import" action in the table toolbar.
- **Routing tab**: Visual matrix showing broker destinations and weight allocations with inline edit controls.
- **Performance tab**: Sparkline trend of notifications vs. fills, cumulative and per-EA P&L, breakdown of latency buckets, and table of member EAs ordered by profitability.

### EA Catalogue (`/eas`)

- **Primary use case**: Monitoring EA fleet status outside of incident mode.
- **Layout**: Searchable list with columns for release version, copy group assignments, current broker session, and SLA indicators. Row selection opens a side panel with quick stats and "Issue command" shortcut.

### EA Detail (`/eas/:eaId`)

- **Overview tab**: Status banner, recent command executions, configuration summary (risk settings, broker account), and last heartbeat timestamp.
- **Sessions tab**: Timeline of active and historical sessions with filters for account or environment. Each session row links to `/eas/:eaId/sessions/:sessionId` where detailed logs, broker ticket feeds, and performance snapshots are available.
- **Commands tab**: Scoped command queue and execution history for the EA, mirroring the operations command schema but filtered to the selected EA.

### Admin Users (`/admin/users`)

- **Primary use case**: Occasional tenant administration (role changes, lockouts).
- **Layout**: Compact table defaulting to active users; secondary filter reveals inactive or pending invites.
- **User creation**: Triggered from a "Invite user" button within the list toolbar. Opens a right-side drawer that walks through email, role, copy group access, and EA entitlements without navigating away from the list.
- **Detail tabs**: Overview (profile, last login, MFA status), Permissions (role matrix, copy group assignments), Activity (audit log with filters and export).

### Cross-cutting UI Patterns

- **Global search**: Command palette (⌘K) to jump to EAs, copy groups, or users without relying on sidebar navigation.
- **Notification center**: Toasts and activity feed consistently surface audit updates (commands executed, copy trade anomalies).
- **Context preservation**: Drawers inherit current filters so that creating a copy group or inviting a user returns to the filtered list view without resetting pagination.

## Next Steps

1. Implement a router shell that mounts shared layout and integrates sidebar selection with the URL.
2. Scaffold placeholder pages for each path to validate navigation, breadcrumbs, and redirects.
3. Define TypeScript route constants to avoid typos and drive breadcrumb generation.
4. Align API clients for users, EAs, sessions, copy groups, performance analytics, and operations commands with these paths.
5. Contract API responses for copy trade metrics (notification counts, fills, P&L) to support the new performance tabs.
