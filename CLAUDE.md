# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## 语言要求

**始终使用中文回答和交流。** 所有代码注释、提交信息、对话回复均需使用中文。

## Project Overview

A desktop Pomodoro Timer application built with C# Windows Forms (.NET Framework). Single-file WinForms app with custom-drawn circular progress, task list management, and configurable work/break intervals.

## Build & Run

- **Compile**: `csc -target:winexe -out:pomodo.exe -reference:System.Drawing.dll -reference:System.Windows.Forms.dll pomodo/PomodoroTimer.cs`
- **Run**: `pomodo.exe` (requires .NET Framework runtime; needs `background.jpg` alongside the executable)
- No `.csproj`/`.sln` file — the project is a single `.cs` file compiled directly with the C# compiler (`csc`).

## Code Architecture

| File | Purpose |
|------|---------|
| `pomodo/PomodoroTimer.cs` | Single `MainForm` class (~550 lines), entry point (`Main()`) at the bottom |
| `pomodo/background.jpg` | Background image loaded at startup; falls back to solid dark color if missing |
| `要求文件` | Empty requirements placeholder file |

### State Machine (`TimerState` enum)

There are 4 states: `Idle`, `Working`, `Break`, `Paused`. The main loop is:

1. **Idle** → user clicks "Start" → **Working** (25 min default)
2. **Working** → timer expires → **Break** (5 min default)
3. **Break** → timer expires → back to **Idle**
4. Any active state → user clicks "Pause" → **Paused** → user clicks "Resume" → resumes prior state

Transitions live in `BtnStartPause_Click()` — the state logic uses status label text heuristics to decide which state to resume into, which is fragile when adding new states.

### Key Controls

- **Circular progress** (`_circlePanel.Paint`) — custom drawn with `DrawArc`; progress angle calculated in `UpdateDisplay()` from remaining/total seconds
- **Settings panel** (`_settingsPanel`) — toggle visibility with the gear button; work/break duration NumericUpDowns; changes apply immediately when Idle, otherwise on next cycle
- **Task list** (`_taskList`) — simple `ListBox`-based; add via text box + Enter key or "添加" button; remove selected via "删除任务" button (falls back to last item)

### Visual Design

- Dark theme (`#2C2C2E` background) with accent colors (red `#FF6B6B` for work, cyan `#48DBFB` for break)
- Background image with semi-transparent overlay for readability
- Rounded buttons via `GraphicsPath` region clipping
- Anti-aliased circular progress ring

## Known Limitations

- No `.csproj`/`.sln` — needs one created if adding NuGet dependencies or multi-file structure
- Background image path is hardcoded to executable directory
- No data persistence (pomodoro count, tasks, settings reset on close)
- Paused-state resume uses string matching on `_lblStatus.Text` rather than storing the prior state
- Task list has no completion marking, only add/remove
