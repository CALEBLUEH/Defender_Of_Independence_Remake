# Level 3 Tutorial Gameplay UI

`Level 3 Gameplay UI` is authored directly in `Scene_Level3` and remains hidden until Camera 3 completes. The opening sequence's serialized `On Sequence Finished` event calls `LevelThreeTutorialPanel.ShowTutorial()`.

## Tutorial behaviour

- The main semi-transparent panel presents the Level 3 tutorial one page at a time.
- Its two non-interactable `Slider` components count down together. The left slider contracts toward the centre from the left rail and the right slider contracts toward the centre from the right rail.
- Required text is marked in the Inspector as `[TYPE:required text]`. At runtime the marker is hidden: untyped letters appear grey and correctly typed letters turn green.
- A required page repeats its timer until the full phrase is entered. Finishing early never skips the remaining time; the next page begins only when the current timer reaches zero.
- At the panel-switch lesson, the incident panel waits for an independently randomized 1–2 second delay, then fades in above the other gameplay UI with its own two-sided Slider timer. Its authored tutorial position is preserved.
- Left Tab switches the active typing panel. The selected panel uses the cyan glow. Switching away from a partially typed panel resets that panel's progress.
- The incident timer repeats until `left tab` is entered. Entering the full phrase deactivates the incident immediately, even if time remains. The main tutorial cannot advance past this lesson until the incident resolves.
- The three-point health panel remains hidden until the health tutorial page, where it fades into the top-right corner.
- Completing `I'm ready!` hands control to `LevelThreeTypingGameplay` through the tutorial's serialized completion event.
- Before the final page, a bottom-centre prompt pulses between partially transparent and opaque. Holding Space for three seconds fills its progress rail and jumps directly to `I'm ready!`; releasing Space early resets the hold. The prompt deactivates on the final page.
- A failed ceremony returns to the same final `I'm ready!` check rather than replaying the opening cameras or the full tutorial.

## Inspector tuning

Select `Level 3 Gameplay UI` in the scene. `Level Three Tutorial Panel` exposes the tutorial lines, incident line and trigger page, incident delay range, health-introduction page, skip-prompt references, three-second hold duration, pulse speed, all timer Slider references, fade timings, and normal/selected/typed/pending colours.

To add another required phrase, wrap only the phrase in `[TYPE:...]`. The current tutorial supports one required phrase per page.

## Manual check

Play `Scene_Level3`. Confirm the two main rails visibly drain, `this word.` blocks and repeats until typed, and the page waits for the timer after correct input. Release Space before three seconds and confirm the skip rail resets, then hold it fully and confirm the tutorial jumps to `I'm ready!` and hides the prompt. On a full tutorial run, confirm the incident delay, Tab reset, immediate incident completion, and health reveal still work.
