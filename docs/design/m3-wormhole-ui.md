# M3 Design — Wormhole UI

> **Status:** Proposal
> **Milestone:** M3 (UI Polish)

## Concept

A symlink is a wormhole — two locations connected by a tunnel through filesystem space. The UI
visualises this metaphor directly:

- **Idle:** A dormant wormhole invites interaction
- **Source loaded:** An active, looping wormhole ready to punch through to a destination
- **Error:** An unstable/collapsed wormhole

## State Machine

The wormhole has its own visual state machine that runs parallel to the functional state machine
(`Idle → SourceLoaded → Error`). The visual states are:

| Visual State | Trigger | Description |
|---|---|---|
| `Dormant` | App launches or after `Clear` | Static or subtly breathing visual. Text: "Drop a file or folder to open the wormhole" |
| `Opening` | `AcceptDrop` succeeds | One-shot transition animation (PNG sequence). Duration: ~800ms |
| `Open` | `Opening` completes | Looping animation (PNG sequence). The wormhole is active and draggable |
| `Closing` | `Clear` while `Open` | One-shot transition animation (PNG sequence). Returns to `Dormant` |
| `Unstable` | `CreateLink` throws | Glitched/collapsed visual. Returns to `Dormant` on `Clear` |

## Animation Assets

All animations are **PNG sequences** — no video files, no third-party animation libraries. The
implementation uses a `DispatcherTimer`-based frame cycler (or WPF `Storyboard`) stepping through a
named folder of numbered PNGs.

### Required Asset Sets

```
Assets/
  wormhole/
    dormant.png              — static idle image
    opening/
      0001.png .. 0024.png   — opening transition (~24 frames @ 30fps)
    loop/
      0001.png .. 0060.png   — seamless looping animation
    closing/
      0001.png .. 0024.png   — closing transition
    unstable/
      0001.png .. 0012.png   — error state (can loop or hold on last frame)
```

### Frame Cycler

```csharp
// Pseudocode — actual implementation will be a reusable AnimationPlayer
class FrameSequence
{
    string[] Frames;      // ordered PNG paths
    int FrameRate;        // e.g. 30
    bool Loop;            // true for Open, false for Opening/Closing
    Action? OnComplete;   // fired when a one-shot finishes
}
```

WPF `Image.Source` is swapped each tick. The cycler lives in the View layer (code-behind or a
`Behavior`), not the ViewModel — it is pure visual state with no functional logic.

## Interaction Design

### Idle → Source Loaded

- **Drop:** User drags a file/folder onto the window. The drop zone is the full window.
  - `DragEnter`/`DragOver` show a brief highlight on the dormant wormhole
  - `Drop` triggers `AcceptDrop` → if valid, the `Opening` animation plays, then `Open` loops
- **Click:** User clicks anywhere. File browser opens. If a source is selected, same transition.

### Source Loaded → Destination

- **Drag out:** User clicks and drags the wormhole itself (or the window) out to an Explorer location.
  - The wormhole is the visual drag handle — the cursor leaves the window carrying the portal
  - On release outside + over Explorer: wormhole stays `Open`; link is created
- **Click:** User clicks. Save dialog opens. If confirmed, link is created; wormhole stays `Open`.

### Source Loaded → Idle

- A small "close wormhole" button (top-right corner, same position as current clear button)
- Triggers `Closing` animation, then `Dormant`

### Error

- `CreateLink` throws → visual snaps to `Unstable` state
- `Clear` → `Dormant`

## Window Layout

```
┌─────────────────────────────┐
│                             │
│      ┌─────────────┐        │
│      │  wormhole   │        │  ← Image control, centered
│      │   visual    │        │     (150×150 or scales to fit)
│      └─────────────┘        │
│                             │
│  [instructional text]       │  ← "Drop a file to open the wormhole"
│                             │     or "Drag the wormhole to link"
│                             │
│                        [×]  │  ← Close button (top-right, 24×24)
└─────────────────────────────┘
```

The wormhole visual is the **centerpiece** — not a border or a background. The rest of the window is
dark space that frames it. Text is minimal and sits below the visual.

## Cursor

- **Idle:** Default arrow
- **Drag out:** Custom `.cur` file (provided by designer) — a small portal or ring that
  reinforces the wormhole metaphor. Falls back to `Cursors.Hand` if the custom cursor is missing.
- **Over drop zone (drag-in):** `Cursors.ArrowCD` or the custom cursor

The custom cursor asset lives at `Assets/cursors/wormhole-drag.cur`. The app loads it via
`CursorInteropHelper.Create` or falls back gracefully.

## Color Palette (proposed)

```
Background:     #0a0a0f  (near-black, space void)
Wormhole ring:  #7c3aed  (violet, primary accent)
Wormhole core:  #c4b5fd  (light violet, glow center)
Text primary:   #e2e8f0  (cool white)
Text secondary: #94a3b8  (muted slate)
Error accent:   #ef4444  (red for unstable state)
Close button:   #64748b  (slate, hover: #e2e8f0)
```

## Implementation Notes

- **No new dependencies.** All animation is frame-based PNG cycling using built-in WPF (`Image`,
  `DispatcherTimer`, `Storyboard`). No SkiaSharp, no Lottie, no video players.
- **ViewModel remains testable.** The visual state machine is View-layer only; `MainViewModel`
  continues to expose `AppState` and the same commands. The View translates `SourceLoaded` →
  `Opening → Open`, etc.
- **Asset loading is lazy.** Sequences are loaded on first use and held in memory during the
  session. The app still ships as a single `.exe` — PNGs are embedded resources.
- **Placeholder assets.** Until final art is ready, the implementation uses simple geometric shapes
  (ellipses with gradient brushes) generated in XAML. Swapping to PNG sequences is a matter of
  replacing the `Image.Source` binding with the frame cycler.

## Acceptance Criteria

- [ ] Wormhole visual cycles through `Dormant → Opening → Open` on valid drop
- [ ] Wormhole loops while `Open` without frame drops at 30fps
- [ ] `Closing` animation plays on clear, then returns to `Dormant`
- [ ] `Unstable` visual appears on error; `Clear` returns to `Dormant`
- [ ] Custom drag cursor loads and falls back to `Hand` if missing
- [ ] All existing tests pass; no logic leaks from View into ViewModel
- [ ] Window remains ~300×300; wormhole scales to fit
