# Game QA & Simplified Chinese LQA — Portfolio Sample

**Portfolio practice sample.** This page documents QA work I actually performed on a student prototype and separates it from test cases and localization checks proposed for demonstration. The sample test designs below are not historical bug reports, do not claim that these cases were executed, and are not commercial LQA experience.

## Project context

**Debt Runner** is a four-person Unity 6 / URP student prototype completed in a four-week production cycle and selected for the Academy of Art University Spring Show 2026.

My project work included iterative checks of tutorial flow, combat readability, UI clarity, round pacing, and reward feedback. I used playtest and instructor feedback to prioritize adjustments, validated repeated game-state transitions, and resolved compile/runtime blockers while integrating gameplay systems.

## QA work performed

- Reviewed tutorial and UI clarity, combat readability, round pacing, and reward feedback during iterative playtests.
- Used feedback to identify and prioritize adjustments for the playable showcase build.
- Rechecked repeated state transitions and resolved compile/runtime blockers during integration.

The project did not use a documented commercial QA process. This portfolio page does not claim a production bug database, formal test-cycle ownership, or commercial localization testing.

## Sample functional test designs

These are proposed test designs based on systems described in the project README. Confirm exact behavior against the current build and design documentation before treating any expected result as a product requirement.

| ID | Area | Setup and steps | Expected behavior to verify | Evidence to capture |
|---|---|---|---|---|
| QA-S01 | Game-state flow | Start a run; reach the end condition; use the available restart or return action; repeat once. | The game enters the intended states in order; controls respond in each state; no stale HUD, duplicate scene, or soft lock remains. | Build/version, video, state sequence, repro rate |
| QA-S02 | Projectile and enemy interaction | Start combat; observe a projectile travel toward an enemy; repeat with multiple enemies and after a target is removed. | Targeting and hit behavior match the design; projectiles do not persist, duplicate, or target invalid objects unexpectedly. | Build/version, short clip, target/enemy conditions |
| QA-S03 | XP and level-up flow | Earn XP until the level-up threshold; select a reward if prompted; continue combat. | XP and level state update once; the reward selection resolves; play resumes without losing input or applying the reward twice. | Before/after level and XP, selected reward, clip |
| QA-S04 | Settlement and shop loop | Complete a run and enter the settlement/shop flow; use the available continue/exit action. | The result and shop screens match the design; choices update the intended state; leaving the flow starts or returns to the correct state. | Build/version, actions, resulting state, screenshot |

**Execution status:** Design examples only; no execution result is claimed here. Record actual pass/fail, build, date, and evidence only after running the tests against a specific build.

## Sample Simplified Chinese LQA checklist

**Practice checklist for a future localized build.** Debt Runner is not represented here as having shipped Chinese localization.

- **Meaning and context:** Compare each string with its source and in-game context; flag omitted meaning, changed intent, or ambiguous references.
- **Terminology and consistency:** Check approved names for gameplay systems, items, UI actions, and recurring concepts across screens.
- **Naturalness:** Check whether Simplified Chinese reads naturally for the intended player and genre.
- **UI fit:** Check truncation, wrapping, line breaks, button width, overlapping text, and text over icons.
- **Glyphs and punctuation:** Check font coverage, missing glyphs, punctuation style, and mixed Chinese/Latin text.
- **Dynamic content:** Check variables, numbers, player names, plural-like constructions, and strings assembled at runtime.
- **Issue evidence:** Capture the build/device, screen or string ID, steps, expected meaning/layout, actual result, screenshot, severity, and retest status.

## Bug report template

Use this template only for an issue reproduced in a real build. Replace every bracketed field with observed information.

- **Title:** [area] concise description
- **Build / platform:** [version, OS/device, resolution]
- **Steps to reproduce:** [numbered, repeatable steps]
- **Expected:** [requirement or source-backed expected behavior]
- **Actual:** [observed behavior]
- **Repro rate:** [e.g., 3/3]
- **Severity:** [impact-based rating]
- **Evidence:** [screenshot/video and relevant string or state]
- **Retest:** [not retested / passed / still present, with build]

## Scope and honesty

My demonstrated experience is hands-on QA iteration within student game development and production coordination. The functional cases and Chinese LQA checklist on this page are portfolio practice materials. They are intended to show how I structure test coverage and evidence; they should not be mistaken for completed commercial test runs.
