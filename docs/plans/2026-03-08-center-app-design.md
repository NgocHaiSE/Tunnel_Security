# Center App Design — Tunnel Security Control Hub

**Date:** 2026-03-08
**Status:** Approved
**Reference:** `center.html` (dark-mode dashboard mockup)

---

## Overview

WinUI 3 desktop application for the Central Monitoring Server. Aggregates data from all Station machines, displays geographic distribution, live data feeds, alerts, and station management.

---

## Visual Design

### Color Palette (from center.html)

| Token | Hex | Usage |
|---|---|---|
| `background-dark` | `#111621` | App background |
| `panel-dark` | `#1c2230` | Sidebar, header, cards |
| `primary` | `#144bb8` | Active nav item, buttons, links |
| `accent-success` | `#0bda5e` | Online status, positive values |
| `accent-warning` | `#fa6238` | Warning status, alert badge |
| `text-muted` | `#94a3b8` (slate-400) | Secondary labels |
| `border` | `#1e293b` (slate-800) | Card/panel borders |

### Layout

```
┌─────────────────────────────────────────────────────┐
│  SIDEBAR (256px, panel-dark)  │  HEADER (64px)       │
│  ─────────────────────────    │  ──────────────────── │
│  [●] Tunnel Security Center   │  [icon] Page Title    │
│       v1.0.0                  │               [🔍][🔔][⚙] │
│                               ├────────────────────── │
│  ● Dashboard                  │                       │
│  ○ Stations                   │   FRAME (Page)        │
│  ○ Alerts          [badge]    │                       │
│  ○ Data Streams               │                       │
│  ○ System Logs                │                       │
│                               │                       │
│  ─────────────────────────    │                       │
│  [+ Add Station]              │                       │
└─────────────────────────────────────────────────────┘
```

---

## Architecture

### Approach: Custom Sidebar Shell

- `MainWindow.xaml` — full shell: custom sidebar `Border/StackPanel` + `Frame` for page navigation
- No `NavigationView` — full control over sidebar layout, selected state, and bottom button
- MVVM pattern with `CommunityToolkit.Mvvm` (already in project)

### File Structure

```
Center/
├── App.xaml / App.xaml.cs
├── MainWindow.xaml / .cs          ← Shell: Sidebar + Header + Frame
├── Styles/
│   ├── Colors.xaml                ← Brand colors ResourceDictionary
│   └── Styles.xaml                ← Shared styles (cards, badges, buttons)
├── Models/
│   ├── StationInfo.cs             ← StationCode, Status, Latency, LastHeartbeat
│   ├── AlertEntry.cs              ← Severity, StationCode, Message, Timestamp, IsAcknowledged
│   └── LogEntry.cs                ← Level, Source, Message, Timestamp
├── Services/
│   ├── CenterApiService.cs        ← HTTP calls to Central Backend
│   └── SignalRService.cs          ← Real-time feed (heartbeats, alerts, logs)
├── ViewModels/
│   ├── MainViewModel.cs           ← UnreadAlertCount (badge), current page title
│   ├── DashboardViewModel.cs      ← Stat cards, map stations, live log entries
│   ├── StationsViewModel.cs       ← Station list, search/filter, connect
│   ├── AlertsViewModel.cs         ← Alert list, filter by severity/station, acknowledge
│   ├── DataStreamsViewModel.cs     ← Full live log, filter by station, pause/resume
│   └── SystemLogsViewModel.cs     ← Historical logs table, export
└── Views/
    ├── DashboardPage.xaml / .cs
    ├── StationsPage.xaml / .cs
    ├── AlertsPage.xaml / .cs
    ├── DataStreamsPage.xaml / .cs
    └── SystemLogsPage.xaml / .cs
```

---

## Pages

### 1. Dashboard (`DashboardPage`)

Mirrors `center.html` layout exactly:

**Row 1 — 4 Stat Cards** (UniformGrid 4 columns)
- Total Stations (count + trend indicator)
- Active Streams (count + trend indicator)
- System Health (% + "Optimal"/"Degraded" label)
- Data Rate (throughput + "Peak"/"Normal" label)

**Row 2 — Map + Live Log** (2/3 + 1/3 split)
- **Map panel:** `WebView2` rendering Mapbox map. Station nodes rendered as JS markers with color by status (green=online, orange=warning, gray=offline). Hover tooltip shows station name + status.
- **Live Data Intake panel:** Terminal-style log with dark background (`#020617`), monospace font, color-coded timestamps (green=info, orange=warning, blue=system). Auto-scroll to bottom. "STREAMING" badge in header.

**Row 3 — Station Table Preview**
- Top 5 stations, same columns as center.html: Station ID / Status / Latency / Transfer Rate / Actions
- "View All →" button navigates to StationsPage

---

### 2. Stations (`StationsPage`)

Full station management table:

- **Toolbar:** Search box (`AutoSuggestBox`) + Status filter buttons (All / Online / Warning / Offline)
- **Table** (`ListView` with custom `DataTemplate`):
  - Station ID (bold) + internal ID (muted subtitle)
  - Status badge (colored pill: ONLINE / WARNING / OFFLINE)
  - Latency (ms, colored by threshold)
  - Last heartbeat (relative time: "2s ago")
  - Actions: **CONNECT** button (appears on hover row)
- **Add Station** dialog (triggered from sidebar button): form with StationCode, Name, Area, Route, CenterUrl, CenterApiKey

---

### 3. Alerts (`AlertsPage`)

Aggregated alerts from all stations:

- **Filter bar:** Severity (Critical / Warning / Info) + Station dropdown + time range
- **Alert list** (`ListView`):
  - Severity badge + Station ID + Message + Timestamp
  - **Acknowledge** button per row
  - Unacknowledged alerts highlighted with left border accent
- Badge on sidebar nav item shows unacknowledged count (driven by `MainViewModel.UnreadAlertCount`)

---

### 4. Data Streams (`DataStreamsPage`)

Full-page live terminal feed:

- **Header:** Station filter ComboBox + Pause/Resume toggle + Clear button
- **Terminal area:** Full-height `ScrollViewer` + `ItemsRepeater`, monospace font, color-coded by level
- Auto-scroll locked to bottom unless user scrolls up (pause auto-scroll on user scroll)
- SignalR push appends new entries in real-time

---

### 5. System Logs (`SystemLogsPage`)

Historical activity log:

- **Filter:** Level (Debug/Info/Warning/Error) + date range picker + keyword search
- **Table** (`ListView`): Timestamp / Level badge / Source / Message
- **Export** button: CSV export via `SaveFilePicker`

---

## Key Technical Decisions

| Decision | Choice | Reason |
|---|---|---|
| Navigation | Custom sidebar + `Frame.Navigate()` | Full layout control, matches HTML reference |
| Map | `WebView2` + Mapbox GL JS | Reuses same approach as Station app |
| Real-time | SignalR client → `CenterApiService` | Consistent with Station architecture |
| MVVM | `CommunityToolkit.Mvvm` | Already in project, `[ObservableProperty]` reduces boilerplate |
| Theme | Dark only (force dark) | Matches center.html design intent |
| Station table | `ListView` with `DataTemplate` | WinUI native, supports virtualization |

---

## Dependencies to Add (Center.csproj)

```xml
<PackageReference Include="Microsoft.AspNetCore.SignalR.Client" Version="8.*" />
<PackageReference Include="Microsoft.Web.WebView2" Version="1.0.*" />
<PackageReference Include="DotNetEnv" Version="3.1.1" />
```

---

## Data Flow

```
Central Backend (ASP.NET Core)
    ↓ SignalR push (heartbeats, alerts, logs)
SignalRService.cs
    ↓ ObservableCollection updates
ViewModels (DashboardViewModel, AlertsViewModel, DataStreamsViewModel)
    ↓ INotifyPropertyChanged
XAML Views (auto-update via bindings)
```

HTTP polling fallback for stations list and historical logs (SignalR for real-time only).
