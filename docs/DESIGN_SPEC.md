# OSRS PvP Helper — Avalonia Redesign · Design Specification

> **Status:** Draft v0.1 · Design only — implementation begins after the current .NET 10 upgrade PR is merged.
> **Scope:** Re-platform the existing Windows console app into a modern **.NET 10 + Avalonia** desktop application. Windows-only. Dark mode only. Deep‑space / purple theme. **Full feature parity** with the console app, dramatically easier to use.

---

## 1. Purpose & Goals

The current product is a Windows console application (`OSRS-AutoSwitcher & PvPHelper`) that automates OldSchool RuneScape combat actions — equipment switching, prayer switching, and special attacks — by capturing on‑screen coordinates and replaying mouse/keyboard input on global hotkeys. It works, but the console UX is the bottleneck: calibration by hovering + pressing `A`, typing space‑separated slot numbers, choosing prayers from a numbered list of 29, and rebinding hotkeys through nested menus.

This redesign keeps the automation engine and reimagines the **interface** as a proper GUI.

### Goals
- **Full feature parity** with the console app (see §8 parity matrix).
- **Radically easier setup**: visual pickers, click‑to‑anchor calibration, inline hotkey capture.
- **A single cohesive design system**: dark‑only, deep‑space nebula theme.
- **Safe & controllable at runtime**: master arm/disarm, panic key, tray + status HUD.
- **Reuse the proven input engine** (global low‑level hooks, mouse automation) behind a clean service layer.

### Non‑Goals (this phase)
- Cross‑platform support (Windows‑only by design — Win32 input APIs).
- Light theme (explicitly dark‑only).
- New automation *capabilities* beyond parity + the usability wins listed here (e.g. no new bots, no OCR). Future ideas are parked in §13.
- Any implementation work — this document is the spec only.

---

## 2. Platform & Tech Stack

| Concern | Choice | Notes |
|---|---|---|
| Runtime | **.NET 10** (`net10.0-windows`) | Matches the just-upgraded csproj. |
| UI framework | **Avalonia 11.x** (Fluent base theme, heavily re‑skinned) | XAML + MVVM, GPU-accelerated, styleable down to the pixel. |
| MVVM | **CommunityToolkit.Mvvm** | Source-generated `ObservableObject` / `RelayCommand`; no boilerplate. |
| DI / lifetime | **Microsoft.Extensions.Hosting + DI** | Services, config, logging, graceful shutdown. |
| Logging | **Microsoft.Extensions.Logging** (+ optional Serilog file sink) | In-app log viewer reads the same stream. |
| Settings serialization | **System.Text.Json** (new format) + **Newtonsoft.Json** kept only for **legacy import** | See §9 migration. |
| Global input hooks | **LowLevelInput.Net** (already adopted in the WIP branch) | Keyboard **and** mouse hook → supports Mouse4/Mouse5 as hotkeys. |
| Input replay | Win32 `SendInput` (preferred) / `SetCursorPos` + `mouse_event` | Migrate off `SendKeys` (see §10). |
| Tray icon | Avalonia `TrayIcon` | Menu + show/hide/arm/quit. |
| Overlays | Borderless transparent topmost Avalonia windows | Calibration overlay (interactive) + status HUD (click‑through). |
| Optional controls | **FluentAvalonia** | Only if we want its `NavigationView`; otherwise a custom sidebar for full theme control. **Recommendation: custom sidebar.** |
| Packaging | Self-contained single-file **win-x64**, ReadyToRun | Portable `.exe`. Optional **Velopack** for auto-update (§12). |

### Solution layout
```
OSRS.PvPHelper.sln
├─ src/
│  ├─ PvPHelper.Core/            # Domain + engine (no UI dependency)
│  │  ├─ Models/                 #   Settings, Profile, Binding, SlotGrid, Prayer…
│  │  ├─ Input/                  #   IHotkeyHook, IInputSender, HotkeyToken (VK + mouse)
│  │  ├─ Features/               #   AutoSwitchAction, PrayerAction, SpecAction
│  │  ├─ Calibration/            #   Grid generators (inventory, prayerbook, spec bar)
│  │  ├─ Automation/             #   ActionScheduler, anti-ban humanizer
│  │  └─ Persistence/            #   Profile store, legacy importer
│  ├─ PvPHelper.App/             # Avalonia UI (MVVM)
│  │  ├─ Views/  ViewModels/  Controls/  Themes/  Assets/
│  │  └─ Overlays/               #   CalibrationOverlay, StatusHud
│  └─ PvPHelper.Tests/           # Core engine unit tests (grid math, scheduling, import)
└─ docs/DESIGN_SPEC.md
```

**Architectural rule:** `PvPHelper.Core` has **no** Avalonia/WinForms/Drawing dependency in its public domain model. Screen coordinates use a plain `PixelPoint`‑style struct; hotkeys use a custom `HotkeyToken` (not `System.Windows.Forms.Keys`). This makes the engine unit‑testable and decouples it from the legacy types (which survive only inside the importer).

---

## 3. Architecture Overview

```
              ┌────────────────────────────────────────────┐
              │                 PvPHelper.App               │
              │  Views ⇄ ViewModels  (CommunityToolkit MVVM)│
              │        │                     ▲              │
              └────────┼─────────────────────┼──────────────┘
                       │ commands            │ observable state
              ┌────────▼─────────────────────┴──────────────┐
              │                PvPHelper.Core                │
              │                                              │
              │  ProfileStore ── Settings/Profiles (JSON)    │
              │                                              │
              │  HotkeyHook (LowLevelInput)                  │
              │      │ key/mouse up events                   │
              │      ▼                                       │
              │  ActionScheduler ── resolves bindings →      │
              │      ├─ PrayerAction                         │
              │      ├─ AutoSwitchAction    (ordered, w/     │
              │      └─ SpecAction           anti-ban delays)│
              │                    │                         │
              │              IInputSender (SendInput)        │
              └──────────────────────────────────────────────┘
```

### Threading model
- **UI thread**: Avalonia dispatcher only.
- **Hook thread**: LowLevelInput's global hook callback; must stay lightweight. It enqueues a token to the scheduler and returns immediately.
- **Action executor thread**: a single dedicated worker drains the scheduler queue and performs the timed mouse/keyboard sequence (blocking `Sleep`s live here, never on the UI thread). One action sequence at a time; re‑entrancy guarded so a second hotkey can't interleave clicks.
- **State bridge**: engine raises events (`Armed`, `ActionStarted/Completed`, `RunningTime`, `LastError`) marshaled to the UI thread for the dashboard/HUD.

---

## 4. Design Language — "Nebula"

Dark‑only. Built from the reference image: a near‑black indigo void bleeding into violet nebula clouds, a warm champagne starfield, and a single shooting‑star streak used as the brand motif.

### 4.1 Color tokens

**Backgrounds (dark → deep)**
| Token | Hex | Use |
|---|---|---|
| `bg.void` | `#07060E` | Deepest layer, window edges, behind scrims |
| `bg.base` | `#0C0A18` | App background |
| `bg.nebula` | `#150E28` | Ambient nebula haze (gradient stops, hero areas) |
| `surface` | `#17132C` | Cards, panels, list rows |
| `surface.raised` | `#1F1840` | Popovers, dialogs, hovered rows |
| `scrim` | `rgba(7,6,14,0.72)` | Overlay/modal backdrop |

**Lines**
| Token | Hex |
|---|---|
| `border.hairline` | `#2A2350` |
| `border.strong` | `#3A3168` |

**Accent — violet (primary)**
| Token | Hex |
|---|---|
| `accent` | `#8B5CF6` |
| `accent.hover` | `#9D74F8` |
| `accent.deep` | `#6D28D9` |
| `accent.press` | `#5B21B6` |
| `accent.gradient` | `linear-gradient(135°, #6D28D9 0%, #9D4EDD 55%, #C77DFF 100%)` |
| `accent.glow` | `rgba(124,58,237,0.45)` (outer glow / focus ring) |

**Starlight — warm secondary** (the champagne stars; used sparingly for the comet motif, "armed" sparkle, sparklines, premium dividers)
| Token | Hex |
|---|---|
| `starlight` | `#E9D8AE` |
| `starlight.dim` | `#D8C28A` |

**Text**
| Token | Hex | Use |
|---|---|---|
| `text.primary` | `#EDEBF7` | Headings, primary content |
| `text.secondary` | `#B6AFD4` | Labels, supporting copy |
| `text.muted` | `#7B739E` | Hints, placeholders, metadata |
| `text.disabled` | `#554E73` | Disabled controls |

**Semantic**
| Token | Hex | Meaning |
|---|---|---|
| `armed` | `#A78BFA` + `accent.glow` | Master ARMED (pulsing violet) |
| `ok` | `#3ED598` | Connected / valid calibration / success toast |
| `warn` | `#F2C14E` | Warnings (matches starlight warmth) |
| `danger` | `#FB6F92` | Stop / panic / destructive; deep `#E23E6B` |

> **Contrast:** all text/background pairs target **WCAG AA** (≥4.5:1 body, ≥3:1 large). `text.muted` on `surface` is the tightest pair — verify at implementation.

### 4.2 Typography
| Role | Font (intended) | Fallback stack | Notes |
|---|---|---|---|
| Display / headings | **Space Grotesk** | `Segoe UI, system-ui` | Slightly geometric, "spacey". Tight tracking on H1/H2. |
| UI / body | **Inter** | `system-ui, Segoe UI` | The workhorse. |
| Mono | **JetBrains Mono** | `ui-monospace, Cascadia Code, Consolas` | Hotkey chips, pixel coordinates, run timer, slot indices. |

Type scale (px / weight): H1 28/600 · H2 20/600 · H3 16/600 · Body 14/400 · Label 13/500 · Caption 12/500 · Mono‑chip 12/500.

### 4.3 Spacing, radius, elevation, motion
- **Spacing scale:** 2 · 4 · 8 · 12 · 16 · 20 · 24 · 32 · 40.
- **Radius:** `sm 6` (chips/inputs) · `md 10` (buttons) · `lg 14` (cards) · `xl 20` (dialogs/HUD) · `pill 999`.
- **Elevation:** `surface` fill + 1px `border.hairline` + soft outer shadow `0 8px 24px rgba(0,0,0,.45)` + 1px inner top highlight `rgba(255,255,255,.03)`. Accent/armed elements add `accent.glow`.
- **Motion:** 120ms hover · 200ms section/page transitions · 280ms ease‑out overlays. **Armed HUD** breathes a 2.4s violet glow pulse. **Comet motif**: a 6s diagonal starlight streak on the splash, empty states, and successful calibration.
- **Iconography:** **Lucide** line icons, 1.5px stroke, `text.secondary` default / `accent` when active.

### 4.4 Ambient background
Every page sits on `bg.base` with a very subtle static nebula: two low‑opacity radial gradients (violet bottom‑right, indigo top‑left) + a faint procedural starfield (tiny champagne dots, ≤3% opacity). Kept extremely subtle so content stays legible. No animation on content pages except the comet on hero/empty states.

---

## 5. Information Architecture & Navigation

**Left sidebar rail** (persistent, collapsible to icons) + content area. Rail top→bottom:

| Icon | Section | Purpose |
|---|---|---|
| ◈ | **Dashboard** | At‑a‑glance state; arm/disarm; all bindings; run timer. Home. |
| ⚔ | **Auto‑Switch** | Manage gear‑switch bindings (visual inventory picker). |
| ✦ | **Prayers** | Manage prayer bindings (visual prayer book). |
| ☄ | **Spec Helper** | Manage special‑attack bindings. |
| ⌨ | **Hotkeys** | Central view of every binding + exit/arm keys; conflict detection. |
| ⛃ | **Profiles** | Save / load / duplicate / import / export presets; quick‑swap. |
| ⚙ | **Settings** | Anti‑ban, client mode, calibration, tray/HUD, app prefs. |

**Sidebar footer:** current profile name + a **master ARM/DISARM** toggle (mirrors the Dashboard and tray). A small connection dot shows whether calibration is complete (`ok`) or incomplete (`warn`).

**Window chrome:** custom title bar (theme‑matched) with app name, current‑profile pill, minimize / minimize‑to‑tray / close. Window is resizable; min 1100×720; remembers size/position.

---

## 6. Screen Specifications

### 6.1 Dashboard (home)
The command center. Sections:

1. **Arm banner** (full‑width hero card): giant **ARM / DISARM** toggle. When armed, the card glows violet and shows the run timer (`Running for 00:14:22`, mono) + the master toggle hotkey and panic key. When disarmed, it's muted grey and lists any blockers ("No hotkeys bound", "Spec bar not calibrated") as tappable chips that deep‑link to the fix.
2. **Status tiles** (row of compact stat cards): `Profile`, `Bindings` (e.g. "6 · 3 switch / 2 prayer / 1 spec"), `Calibration` (Inventory ✓ / Prayers ✓ / Spec bar ✓), `Client mode` (Fixed/Resizable/Stretched + scale).
3. **Bindings overview** (grouped list): every active binding as a row — hotkey chip · type icon · friendly name · a per‑row **enable toggle** · overflow menu (Edit / Test / Duplicate / Delete). Grouped by Auto‑Switch / Prayers / Spec.
4. **Activity log** (collapsible, bottom): timestamped feed — "△ Armed", "⚔ Switched: Full Rune", "☄ Spec ×2 on target", "⛔ Disarmed (Esc)". Filterable; the same stream as the file log.

**Parity note:** replaces the console `StartupChecks()` printout + the "Been running for N seconds" line, but shows it live and always‑visible.

### 6.2 Auto‑Switch page
List of switch bindings on the left; an editor on the right (or a full‑screen editor dialog when adding).

**Switch editor:**
- **Name** (e.g. "Full Rune", "Mage gear") + optional icon/color tag.
- **Hotkey** — inline capture control ("Press a key…", accepts Mouse4/5).
- **Inventory picker** — a **rendered 4×7 inventory grid (28 slots)**. Click slots to toggle inclusion; selected slots highlight violet and show a **click‑order number** (1,2,3…). Order = click order = execution order. Drag to reorder; right‑click to clear a slot. This replaces typing `"3 7 11"` space‑separated.
- **Options:** "Return mouse to start" (inherits global default, per‑binding override), inventory tab F‑key (defaults to global).
- **Test** button: dry‑run highlights the slots it would click, in order, with a countdown — without sending clicks (or optionally live‑fires if the game is focused).

**Parity:** console menu **1 (Setup Autoswitcher)** — inventory F‑key, calibrate inventory, N switches, per‑switch slot list + hotkey. Here calibration is shared (see §6.8) and slot entry is visual.

### 6.3 Prayers page
- List of prayer bindings + editor.
- **Prayer picker** — a **rendered prayer book** (the 29 prayers laid out 5‑wide as in‑game) with recognizable icons/labels grouped by tier (Thick Skin → Augury). Click a prayer to select; assign a hotkey via inline capture. Search/filter box for quick find.
- Shows the computed screen point for the selected prayer (from calibration) with a "re‑anchor" shortcut.
- The full prayer enum is preserved: Thick Skin, Burst of Strength, Clarity of Thought, Sharp Eye, Mystic Will, Rock Skin, Superhuman Strength, Improved Reflexes, Rapid Restore, Rapid Heal, Protect Item, Hawk Eye, Mystic Lore, Steel Skin, Ultimate Strength, Incredible Reflexes, Protect from Magic, Protect from Missiles, Protect from Melee, Eagle Eye, Mystic Might, Retribution, Redemption, Smite, Preserve, Chivalry, Piety, Rigour, Augury.

**Parity:** console menu **2 (Setup AutoPrayer)** — prayer F‑key, calibrate prayer panel, loop pick‑prayer + hotkey. Replaces the numbered 0–29 list with the visual book.

### 6.4 Spec Helper page
Editor fields per spec binding:
- **Name**, **Hotkey** (inline capture).
- **Spec weapon slots** — same visual inventory grid as Auto‑Switch (which slot(s) to equip the spec weapon from).
- **Wait for spec bar** — toggle (maps to `WaitForSpecBar`; controls the 450ms vs 100ms delay).
- **Spec times** — stepper (how many times to click the spec bar).
- **Attack tab F‑key** (defaults to global).
- **Test** dry‑run.

**Parity:** console menu **3 (Setup SpecHelper)** — attack F‑key, calibrate spec bar, N spec helpers, per‑helper slots + wait‑for‑bar + spec‑times + hotkey.

### 6.5 Hotkeys page
Single table of **every** binding across all features + the app‑level keys:
- Columns: Type · Name · **Hotkey chip** (click to re‑capture inline) · Enabled toggle.
- App keys section: **Master Arm/Disarm hotkey**, **Panic / Stop key** (maps to the console `ExitKey`, default `Esc`).
- **Conflict detection:** duplicate assignments highlight in `warn` with an inline "used by X" note; capture rejects a key already bound (with override option).
- Bulk actions: clear all, print/export binding sheet.

**Parity:** console menu **7 (Setup hotkeys)** — rebind any autoswitch/prayer/special hotkey + exit key. Same capability, non‑nested, with conflict safety the console lacked.

### 6.6 Profiles page
- **Profile cards**: name, summary (counts), last modified, client mode. Actions: **Load**, Rename, Duplicate, Delete, **Export** (`.json`), and an **Import** button.
- **Active profile** highlighted; a quick‑swap dropdown also lives in the sidebar footer and tray.
- **Hot‑swap while armed** allowed (with a confirm) — re‑binds without restarting.
- Storage: `%AppData%\OSRS-PvPHelper\profiles\*.json` (see §9). **Legacy import**: detect old `%AppData%\OSRS-AutoSwitcher\*.json` on first run and offer one‑click import.

**Parity:** console menus **4 (Save)** & **5 (Load)** — named JSON presets with override/new. Upgraded to full CRUD + import/export + hot‑swap.

### 6.7 Settings page
Grouped:
- **Anti‑ban / humanization** — **Mouse speed** (slider; label clarifies "lower = faster", maps to `Speed` steps), **Return to old mouse position** (global default toggle), plus new humanizer options (§10): movement jitter radius, per‑step delay range, click delay range, optional curved paths. Console parity = the first two; the rest are additive and default to the console's current behavior.
- **Client mode** — Fixed / Resizable / **Stretched (RuneLite)** + **UI scale**. Drives grid spacing so the `Stretched` flag (currently stubbed in the model) becomes functional. Presets for common scales; "auto from calibration" when three anchors are captured.
- **Calibration** — re‑run any anchor (inventory / prayer book / spec bar), view current captured points, adjust grid spacing (X/Y) with live preview.
- **Runtime** — start minimized to tray, minimize‑to‑tray on close, show status HUD (on/off + position + opacity), master arm hotkey, panic key, "disarm on focus loss of game" (optional safety).
- **App** — launch on Windows start (optional), check for updates, open log folder, about/version.

### 6.8 Calibration overlay (the marquee UX win)
Triggered from any "Calibrate" affordance. Flow:
1. A **translucent full‑screen overlay** appears over the game (scrim + instructional header with the comet motif). The game shows through.
2. **Step 1 — Inventory anchor:** "Click your **first inventory slot** (top‑left)." User clicks the actual game. We capture the pixel point.
3. Immediately render the **generated 4×7 grid** (28 markers) back onto the overlay so the user sees whether it lines up with their real inventory. If off: **drag the whole grid** to reposition, or adjust **X/Y spacing** with two sliders (replaces the hardcoded 40px / 35px). Live preview.
4. **Step 2 — Prayer anchor:** same, generating the 5×6 prayer grid (29 cells, 40×40 default spacing).
5. **Step 3 — Spec bar:** single click to mark the special‑attack bar point.
6. **Confirm** → points saved to the active profile; a success comet sweeps; overlay closes. Each anchor shows a green `ok` check on the Dashboard calibration tile.

Escape cancels; nothing is saved until Confirm. Multi‑monitor aware (overlay spans the monitor the game is on; captured points are absolute screen coordinates).

**Parity:** replaces the console "hover the slot and press `A`" capture for inventory, prayers, and spec bar — but visual, verifiable, and adjustable, with proper support for non‑fixed client scales.

### 6.9 Status HUD (in‑game overlay)
A small, always‑on‑top, **click‑through** widget (doesn't steal game input):
- Compact pill showing **ARMED/DISARMED** (violet glow when armed), **run timer**, and the **last action** ("⚔ Full Rune", "☄ Spec ×2").
- Optional mini list of armed hotkeys.
- Draggable to any corner; opacity + show/hide in Settings; hidden by default when disarmed.
- Rendered as a borderless transparent topmost window with input‑transparency (WS_EX_TRANSPARENT‑equivalent) so clicks pass to the game.

### 6.10 System tray
Tray icon (nebula glyph; violet when armed). Right‑click menu: **Arm / Disarm**, **Profile ▸** (quick‑swap submenu), **Show window**, **Show/Hide HUD**, **Calibrate**, **Quit**. Left‑click restores the window. Balloon/toast on arm/disarm and on errors.

### 6.11 First‑run / empty states
On first launch with no profile: a friendly hero ("Let's get you set up") with a **3‑step guided path** — Calibrate → Add your first switch/prayer/spec → Set your arm key — reusing the same pages, not a separate wizard. If a legacy console config is found, offer **Import existing setup**. Empty lists everywhere use the comet motif + a single clear CTA.

---

## 7. Component Library (reusable controls)

| Component | Description |
|---|---|
| **HotkeyCaptureBox** | "Press a key…" state → captures next key/mouse button via the hook; renders a mono **key chip**; supports Mouse4/5; shows conflicts. |
| **InventoryGrid** | 4×7 (28) interactive slot grid; toggle + ordered numbering + drag reorder; used by Auto‑Switch, Spec, calibration preview. |
| **PrayerBook** | 29‑prayer visual grid with icons/labels + search; single/multi select. |
| **BindingRow** | Hotkey chip + type icon + name + enable toggle + overflow menu; used on Dashboard/Hotkeys. |
| **StatTile** | Compact metric card (label + value + optional status dot). |
| **ArmToggle** | The master arm switch with glow/pulse states; mirrored in header, sidebar footer, tray, HUD. |
| **CalibrationOverlay** | Full‑screen interactive capture surface. |
| **StatusHud** | Click‑through in‑game widget. |
| **Toast** | Transient bottom‑right notifications (`ok`/`warn`/`danger`). |
| **NebulaBackground** | Reusable ambient gradient + starfield + optional comet. |
| **SpeedSlider** | Anti‑ban speed with "lower = faster" affordance and a live "feel" preview dot. |

---

## 8. Feature Parity Matrix

| # | Console feature | New app location | Status |
|---|---|---|---|
| 0 | Antiban settings — mouse speed, return‑to‑old‑position | **Settings ▸ Anti‑ban** | Parity + extra humanizer options |
| 1 | Setup Autoswitcher — inv F‑key, calibrate inventory, N switches, slot list, hotkey | **Auto‑Switch** page + **Calibration overlay** | Parity, visual slot picker |
| 2 | Setup AutoPrayer — prayer F‑key, calibrate prayers, prayer + hotkey | **Prayers** page + overlay | Parity, visual prayer book |
| 3 | Setup SpecHelper — attack F‑key, calibrate spec bar, slots, wait‑for‑bar, spec‑times, hotkey | **Spec Helper** page + overlay | Parity |
| 4 | Save Settings (named / override) | **Profiles** page | Parity + export |
| 5 | Load Settings | **Profiles** page | Parity + import + hot‑swap |
| 6 | Start PvPHelper (+ "running for N s") | **Dashboard arm** + **HUD** + tray | Parity, live & always visible |
| 7 | Setup hotkeys (rebind any + exit key) | **Hotkeys** page | Parity + conflict detection |
| 8 | Exit | Window/tray **Quit** | Parity |
| — | `ExitKey` (default Esc) | **Panic / Stop key** (Hotkeys/Settings) | Parity |
| — | `Stretched` client flag (stubbed) | **Settings ▸ Client mode** | **Now functional** |
| — | Mouse4/5 hotkeys (WIP) | **HotkeyCaptureBox** | Delivered |
| — | Action ordering prayer→items→spec on one key | **ActionScheduler** | Preserved (§10) |

---

## 9. Data Model & Persistence

### New profile model (`PvPHelper.Core.Models`)
Conceptually identical to the console `Settings`, but UI‑agnostic and named per‑profile:

```
Profile
  Name, Version, ClientMode { Fixed|Resizable|Stretched }, UiScale
  AntiBan { Speed, ReturnToOldPos, JitterRadius, StepDelayRange, ClickDelayRange, CurvedPaths }
  Tabs   { InventoryFKey, PrayerFKey, AttackFKey }
  Calibration {
     InventoryAnchor: PixelPoint, InventorySpacing: (x,y),
     PrayerAnchor:    PixelPoint, PrayerSpacing:    (x,y),
     SpecBarPoint:    PixelPoint
  }
  AppKeys { ArmHotkey: HotkeyToken, PanicKey: HotkeyToken }
  Switches:  [ { Id, Name, Hotkey: HotkeyToken, SlotOrder: int[], ReturnOverride?, Enabled } ]
  Prayers:   [ { Id, Prayer: enum, Hotkey: HotkeyToken, Enabled } ]
  SpecHelpers:[ { Id, Name, Hotkey, WeaponSlots: int[], WaitForSpecBar, SpecTimes, Enabled } ]
```

- `HotkeyToken` encodes source (`Keyboard`/`Mouse`) + code, so Mouse4/5 serialize cleanly (the legacy `System.Windows.Forms.Keys` couldn't represent extra mouse buttons well).
- Grids are **regenerated** from `Anchor + Spacing` (not stored as 28/29 baked points) so re‑anchoring and scale changes just work. The console stored baked `InventorySlots`/`Prayers` point lists — the importer converts those to anchor+spacing.
- Storage: `%AppData%\OSRS-PvPHelper\profiles\<name>.json` via System.Text.Json. A small `app.json` stores non‑profile prefs (tray/HUD, last profile, window bounds).

### Legacy migration
On first run, scan `%AppData%\OSRS-AutoSwitcher\*.json`. For each, deserialize with Newtonsoft into the **old** `Settings` shape and map:
- `Speed`→`AntiBan.Speed`, `ReturnToOldPos`→`AntiBan.ReturnToOldPos`, `Stretched`→`ClientMode`.
- `InventFKey/PrayerFkey/AttackFkey`→`Tabs.*`.
- `InventorySlots[0]`→`InventoryAnchor` (+ infer spacing 40/35 or from points); `Prayers` dict → `PrayerAnchor` (+40/40) and per‑prayer bindings; `SpecialAttackBarPoint`→`SpecBarPoint`.
- `ActiveAutoSwitchHotkeys` (Keys→slot list) → `Switches[]`; `ActivePrayerHotkeys` (Prayer→Keys) → `Prayers[]`; `SpecialAttackHotkeys` (Keys→{InventoryId,SpecTimes}) → `SpecHelpers[]`; `ExitKey`→`AppKeys.PanicKey`.
- `Keys` → `HotkeyToken` via a conversion table.

Offer this as **"Import existing setup"** on first run and in Profiles. Never delete the old files.

---

## 10. Automation Engine

Preserve the console's **observable behavior** exactly, then layer safety/quality on top.

- **Hotkey source:** LowLevelInput global keyboard + mouse hook; fire on **key‑up** (matches `Program.InputManagerOnOnKeyboardEvent` which acts on `KeyState.Up`). Mouse4/5 become first‑class hotkeys.
- **Action ordering on a single key** (unchanged): **Prayer → Auto‑Switch → Spec**, inserting the console's ~50ms guard delays between combined actions when the same key triggers more than one. (`ActivePrayer` → `ActiveSwitches(swappedPrayers)` → `DoSpecialAttack(swappedItems, swappedPrayers)`.)
- **Auto‑switch action:** press inventory tab F‑key → for each slot: move/set cursor to slot → click → short delay; optional return‑to‑start smooth move. (Mirrors `AutoSwitch.ActiveSwitches`.)
- **Prayer action:** press prayer tab F‑key → smooth‑move to prayer point → click → optional return. (Mirrors `PrayerSwitch.ActivePrayer`.)
- **Spec action:** press inventory F‑key → move to weapon slot(s) (equip) → press attack F‑key → wait **450ms if WaitForSpecBar else 100ms** → move to target (start pos) → click spec bar **SpecTimes** times (~100ms apart) → return. (Mirrors `SpecialAttack.DoSpecialAttack`.)
- **Anti‑ban / humanizer:** keep `LinearSmoothMove` semantics — N steps (Speed), ~10ms per step, randomized ±5px end point, ~50ms pre‑click. Expose the magic numbers as configurable ranges (defaults = current values) and optionally upgrade straight‑line moves to slight Bézier curves. Purely additive; defaults reproduce console feel.
- **Input replay:** migrate `SendKeys` + `mouse_event` to **`SendInput`** for reliability/less flakiness (the console `SendKeys.SendWait("{F1}")` approach is brittle). `SetCursorPos` retained for absolute positioning. This is an internal quality change, not a behavior change.
- **Re‑entrancy:** the executor runs one sequence at a time; a hotkey pressed mid‑sequence is ignored or queued (configurable), preventing the console's "doesn't always finish before a second hook" issue noted in the changelog.

### Safety guardrails (new, on top of parity)
- **Master ARM/DISARM**: hotkeys do nothing until armed. Disarming is instant and always available (hotkey + tray + HUD + window).
- **Panic / Stop key** (`ExitKey` parity, default `Esc`): immediately disarms and aborts any in‑flight sequence.
- **Optional "disarm on game focus loss"**: stops input replay if the game window isn't foreground.
- **Conflict detection** at bind time (no silent double‑binds).

---

## 11. Accessibility & Quality Bar
- Dark‑only, but honor **AA contrast**; never rely on color alone (icons + text for armed/disarmed, ok/warn/danger).
- Full **keyboard navigation** of the UI; visible focus rings (`accent.glow`).
- Respect **reduced‑motion** OS setting (disable comet/pulse animations).
- All timings/spacings from tokens; no magic numbers in views.
- Core engine covered by **unit tests** (grid math, ordering/scheduling, legacy import round‑trip).

---

## 12. Packaging & Distribution
- **Self‑contained single‑file win‑x64**, ReadyToRun; portable `.exe`, no install required (matches how the console release ships).
- App icon: existing `icons8-intelligence-64.ico` (or a new nebula/comet mark — TBD).
- Settings live in `%AppData%` (unchanged location family).
- **Optional auto‑update** via **Velopack** (GitHub Releases feed) — nice‑to‑have, not required for v1.
- Note: Avalonia + trimming can be finicky; ship untrimmed first, evaluate trimming later.

---

## 13. Open Questions & Future Ideas
- **Icon assets:** source official‑looking prayer/inventory iconography vs. abstract placeholders (avoid shipping Jagex art). Decide before build.
- **Auto‑detect client** (the calibration option not chosen now) could return later as an assist on top of the overlay.
- **Per‑binding sound/ää cue**, tick‑timing helpers, multi‑target spec — parked.
- **Cloud/profile sync** — out of scope.
- App/product **rename**? ("Nebula PvP Helper"?) — cosmetic, decide later.

---

## 14. Implementation Phasing (post‑merge)
1. **Foundations** — solution scaffold, DI/host, theme resource dictionary (tokens/typography), `NebulaBackground`, sidebar shell, empty pages.
2. **Core engine port** — move Features/Mouse/hooks into `PvPHelper.Core` behind `IInputSender`/`IHotkeyHook`; unit tests for grid math + ordering.
3. **Profiles + legacy import** — new model, ProfileStore, importer, Profiles page.
4. **Calibration overlay** — the marquee flow (inventory/prayer/spec anchors + spacing).
5. **Feature pages** — Auto‑Switch, Prayers, Spec (visual pickers + HotkeyCaptureBox), Hotkeys page + conflict detection.
6. **Runtime** — ActionScheduler, arm/disarm, panic, tray, Status HUD, Dashboard live state + activity log.
7. **Settings** — anti‑ban, client mode, runtime prefs; humanizer options.
8. **Polish & package** — motion, empty states, reduced‑motion, single‑file packaging, optional Velopack.

---

*End of spec v0.1 — palette finalized from the provided deep‑space reference image; ready for review before implementation.*
