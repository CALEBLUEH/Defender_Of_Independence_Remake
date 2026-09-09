# Level 3 Main Typing Gameplay

`LevelThreeTypingGameplay` begins when the final tutorial phrase is completed. It reuses the user's authored `Tutorial Panel`, `Incident Tutorial Panel`, and their existing text positions; setup does not rebuild or reposition either panel.

## Gameplay flow

- The speech follows the supplied assignment document order: 14 speech passages followed by seven `MERDEKA` entries.
- Every speech entry has its own editable duration. The saved defaults range from 4 seconds for short entries to 16 seconds for the longest passage.
- Grey `[TYPE:...]` text must be typed before each speech timer reaches zero. Correct characters turn green.
- Unlike the tutorial, every gameplay speech entry advances at zero even when incomplete. A miss removes one of three shared health points.
- Incidents are scheduled independently after the assignment's specified speech entries. Each waits a random 1–2 seconds before appearing.
- Gameplay incidents use the existing incident panel but choose a random position that keeps the full panel inside its parent canvas. The prompt and grey repair command have one empty line between them.
- Left Tab changes the selected panel. Leaving a partially typed panel clears that panel's current progress.
- Completing an incident repair command deactivates that incident immediately. Letting its own timer expire removes one health point and closes it.
- Two medium incidents are selected without replacement from the configured medium incident pool during one run.
- At zero health, gameplay stops and `Level 3 Failure Overlay` appears. Pressing `R` fades to black, resets the run without reloading the scene, returns to the final `I'm ready!` tutorial check, and fades back in. The ready-check timer remains paused until the screen is visible.
- After the final `MERDEKA`, `On Gameplay Completed` is invoked. This event is exposed for the later credits or final-result flow.

## Inspector tuning

Select `Level 3 Gameplay UI`:

- `Level Three Typing Gameplay` exposes the ordered main lines, a labelled `Per-Line Speech Timing` section, incident prompts and repair words, the fallback main duration, incident timer, 1–2 second spawn-delay range, safe canvas margin, retry fade duration, colours, and completion event.
- Each timing field is labelled with its line number and a preview of the matching speech, so balancing does not require cross-referencing raw array indices.
- `Level Three Health Display` exposes maximum health (currently 3), show duration, Slider, and numeric value text.
- `Ceremony Health Panel` is a top-right authored Canvas using the same navy/gold presentation as the tutorial UI.

## Third-party UI asset

The health icon is `Heart inside` by Lorc from Game-icons.net, licensed under Creative Commons Attribution 3.0. Its source and required credit are recorded in `Assets/Level3/UI/Health/SOURCE.md`.

## Manual check

Finish the tutorial, then confirm speech entries use their individual Inspector durations and keep advancing at zero whether completed or missed. At the first incident, confirm it appears after a short independent delay, stays fully within the canvas, contains a blank line before the repair command, and disappears immediately when `raise volume` is typed. Miss any combination of three entries, press `R`, and confirm the black transition returns to `I'm ready!` without replaying the opening cameras. Complete the ready check and confirm health resets to `3 / 3` and gameplay starts again.
