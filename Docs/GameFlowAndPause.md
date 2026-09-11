# Main Menu Progression and Pause Flow

## Persistent states

- Initial state: `START` loads `Cutscene_Level1`. `GALLERY` opens the independence challenge.
- A perfect 16/16 challenge unlocks post-game mode. A lower score leaves the game in its initial state.
- Completing or skipping Level 3 unlocks post-game mode before credits load.
- Post-game `START` opens the three-level chapter selector. Every chapter begins at its cutscene.
- Post-game `GALLERY` loads `Scene_Gallery` and requests `InitialSpawnPoint`.
- `RESET` only deletes the story/post-game progression key. It does not delete audio, option, or Fungus save settings.

## Escape menus

All three cutscenes, all three gameplay scenes, credits, and the Gallery contain an authored `Global Pause Menu` canvas.

- Every menu has Resume, Options, and Back to Main Menu.
- Cutscenes add Skip Cutscene and load their matching gameplay scene.
- Levels 1 and 2 add Skip Level and load the next cutscene.
- Level 3 uses the existing black-fade credit transition, so it also records post-game completion.
- Credits uses the same finish path as the hold-Space skip.
- Gallery has no skip action.

The Options button opens the authored audio panel. Its separate sound-effect and music sliders apply immediately, persist across scenes and application restarts, and can be closed with the panel's X button.

## Inspector tuning

- `Main Menu Progression` contains editable scene names, message text flow references, and fade duration.
- Each `Global Pause Menu` contains its scene kind, skip destination, cursor behavior, and explicit components disabled while paused.
- Pause menus suspend background music and Resume continues it from the same playback position.
- `Level Three Credit Transition` remains the single Level 3-to-credits route.

## Manual checks

1. Clear progress with RESET and confirm START enters the Level 1 cutscene.
2. Open GALLERY, deliberately miss a quiz answer, and confirm the Gallery remains locked.
3. Retake the quiz with 16 correct answers and confirm START shows all three chapters and GALLERY enters the museum.
4. Press Escape in each cutscene, gameplay scene, credits, and Gallery. Check Resume, Back to Main Menu, cursor behavior, and the scene-specific skip button.
5. Finish Level 3 normally, allow credits to end, return to Main Menu, and confirm post-game mode remains unlocked after restarting the application.
