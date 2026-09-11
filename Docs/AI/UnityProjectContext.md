# Unity Project Context

## Level 2 documentation system

- Level 2 has four aim-based document interactions on the user's existing colliders: Main Lobby, British Office, Planning Room, and Radio Station.
- `LevelTwoDoorInteractor` is the single `C`-input owner for doors, the next-day clock, and documents.
- The Main Lobby guide is free, reusable, and contains no conference-schedule page. The other three `LevelTwoDocumentLocation` components randomly serve three historical pages without replacement per playthrough.
- `LevelTwoDocumentViewer` presents a left-aligned scrollable page, pauses first-person controls, releases the mouse pointer for the scrollbar/X button, and closes with X, `C`, or `Escape`.
- `LevelTwoDayController` now owns 4 daily Energy, exposes `TrySpendEnergy`, updates the HUD, and restores Energy on a new day.
- British Office, Planning Room, and Radio Station documents grant stackable +5 bonuses to future positive British Confidence, Delegation Unity, and Public Support gains respectively.

## Level 2 dialogue and room access

- Six day-specific `LevelTwoConversationTrigger` components cover Tunku (Days 1 and 5), Alan Lennox-Boyd (Days 2, 3, and 6), and the user's `RadioStationCollider` (Day 4).
- `LevelTwoConversationViewer` releases the pointer for choices, presents the assignment dialogue, randomizes centered answer positions, applies the authored meter effects, and supports the three-step Day 6 finale.
- Each daily conversation costs 1 Energy and can start only once. Completed triggers retain an exhausted prompt for the remainder of their day.
- Each door charges 1 Energy only on its first entry that day. Exits and later same-day re-entry are free.
- Day 6 locks the Planning Room and Radio Station doors, leaving only the British Office/Alan Lennox-Boyd door available.
- Entering Day 6 requires all three meters to be above 50; failing this check shows the restart result panel without advancing from Day 5. The final conference still requires all three meters to be above 65.
- Energy and negotiation meters are hidden during day-card transitions and render behind document/conversation panels.

Last analyzed: 2026-09-11
Analyzed commit: `ea60326be2f89db5d0e861d731aa285d8bda5e44`

## Project summary

`Defender_Of_Independence_Remake` is the clean Unity project for rebuilding the educational desktop game. The current URP project contains the menu/gallery foundation, a playable Level 1 hostage-rescue loop, Fungus cutscenes for Levels 1 and 2, and a Level 2 house/environment kit. The museum migration is sourced from `GameDevelopment2Assignment`, but unrelated senior-project scenes and systems should not be copied.

## Confirmed environment

- Unity Editor: 6000.5.0f1 (`88b47c5e7076`)
- Render pipeline: Universal Render Pipeline 17.5.0
- Input: Input System 1.19.0; Active Input Handling is now set to Both so the copied legacy-input museum controller and Starter Assets can coexist
- Cinemachine: 3.1.7
- glTFast: 6.19.0, used for the counter-terrorist glTF asset and selected to match the project's installed Burst version
- AI Navigation: 2.0.14, used by the Level 1 `NavMeshSurface` and enemy `NavMeshAgent` movement
- Target: desktop game; no platform build target was verified during onboarding

Sources: `ProjectSettings/ProjectVersion.txt`, `ProjectSettings/GraphicsSettings.asset`, `ProjectSettings/ProjectSettings.asset`, `Packages/manifest.json`, `Packages/packages-lock.json`.

## Structure and assemblies

- `Assets/Scene`: authored scenes
- `Assets/Script`: first-party scripts
- `Assets/Starter Assets`: Unity Starter Assets samples and runtime code
- `Assets/Font`, `Assets/Material`, `Assets/Prefab`: existing authoring folders
- `Unity.StarterAssets`: main runtime assembly; references the Input System
- `Unity.StartAssets.Editor` and `URPWizard`: editor-only Starter Assets assemblies
- Other first-party scripts currently compile into Unity's default project assembly
- `Assets/Script/Level1/Player`: first-person input, weapon, HUD, interaction, and damage-extension components
- `Assets/Script/Level1/Combat`: shared player/enemy health and target-display components
- `Assets/Script/Level1/Enemy`: attack, defend, retreat, spawning, aiming-pose, and world-health-bar components
- `Assets/Level1/Prefabs/Player/LevelOnePlayer.prefab`: generated, Inspector-editable Level 1 player
- `Assets/Level1/Prefabs/Enemy/LevelOneEnemy.prefab`: generated, Inspector-editable Terrorist enemy using the shared pistol
- `Assets/Level2/Art`: imported house, furniture, radio, and clock FBX/texture sources plus generated URP materials
- `Assets/Level2/Prefabs`: the house, 40 individually placeable furniture pieces, and progression prop prefabs
- `Assets/Level3`: stadium, animated Malaysia flag, podium, generated URP materials/prefabs, and third-party attribution records

## Scenes and startup flow

- The current flow is `Scene_MainMenu` → `Cutscene_Level1` → `Scene_Level1` → `Cutscene_Level2` → `Scene_Level2` → `Cutscene_Level3` → `Scene_Level3` → `Scene_Credits` → `Scene_Gallery`.
- `Scene_Level1` opens on a paused, scrollable instruction panel. START MISSION enables player controls and begins the initial enemy wave.
- `Cutscene_Level2` contains seven Fungus dialogue beats and loads `Scene_Level2` after its final Space prompt.
- `Scene_Level2` contains the imported interior house at the origin under `Level 2 Environment`, the user's room decoration, and a model-less WASD/mouse first-person player under `Level 2 Gameplay`. Three C-interaction doors use paired Enter/Exit markers and a black fade transition. The player exposes `ReturnToInitialSpawn()` for the future day-skip flow.
- Level 2 now owns a six-day progression under `Level 2 Gameplay/Level 2 Day System`. `NextDayClock` advances with C, shows a FNAF-inspired day card, returns the player to the lobby spawn, refreshes the appointment HUD, swaps scheduled character visibility, and stops at Day 6. Gameplay transitions keep the pointer captured; only future dialogue choices should release it.
- The senior project's requested source scene was the misspelled `Assets/Scene/Meuseum.unity`; its working-tree version was the migration source.
- `Scene_Level3` contains a normalized stadium prefab under `Level 3 Environment`. The Malaysia flag and debate podium are prefab-only until the user chooses their final cutscene positions.

## Architecture and conventions

The remake is early-stage and has no established gameplay architecture beyond Starter Assets. Prefer feature folders, Inspector-assigned references, serialized tuning values, and small components. Do not import unrelated senior-project systems merely to satisfy a scene reference.

## Level 3 opening and tutorial UI

- `Scene_Level3` plays Camera 1, Camera 2 with Tunku Abdul Rahman, then Camera 3. Camera swaps occur only behind a fully opaque two-sided black fade; each outgoing camera is sampled and frozen on its final keyed frame before fading.
- Camera 2's original keyframes are preserved, but its clip is now non-Legacy and non-looping like the other camera clips so all three shots use Animator controllers consistently.
- Camera 3 completion invokes the serialized `On Sequence Finished` event, which fades in `Level 3 Gameplay UI`.
- The Level 3 tutorial uses paired non-interactable Sliders that visibly drain inward. `[TYPE:...]` phrases remain grey until correctly typed, highlight typed characters, repeat the page timer when incomplete, and advance only at timer zero.
- A higher-sorting incident tutorial panel has its own paired timer and a randomized 1–2 second arrival delay. Left Tab swaps the selected glowing border, switching away clears partial input, and finishing the incident phrase deactivates it immediately. The main page remains blocked until the incident resolves.
- The tutorial reveals a three-point top-right health HUD only on its health-introduction page, then `I'm ready!` starts `LevelThreeTypingGameplay` through a serialized completion event.
- Level 3 main gameplay contains the assignment's ordered speech and seven final `MERDEKA` entries. Main entries advance at timer zero even when missed; scheduled incidents run independently, spawn within the canvas at randomized positions, and separate their prompt and repair phrase with a blank line. Either kind of miss removes shared health, and the third miss opens the failure overlay. Gameplay completion exposes an Inspector event for the later credits flow.
- Main speech timing is balanced per line through a custom Inspector section with a labelled duration beside every speech preview. Short defaults are 4 seconds and the longest passage defaults to 16 seconds; the original global value remains a safe fallback.
- The tutorial offers a pulsing bottom-centre `Hold Space` prompt with a three-second progress rail. Releasing early resets it; completing the hold jumps to the final `I'm ready!` page and deactivates the prompt.
- Level 3 failure recovery no longer reloads the scene. `R` uses the existing top-sorting black overlay, resets health and gameplay behind black, resumes at the final tutorial ready-check with its timer paused during the fade, and restarts gameplay only after that phrase is completed.
- The Level 3 health art is Lorc's `Heart inside` icon from Game-icons.net under CC BY 3.0; attribution is stored beside the imported PNG in `Assets/Level3/UI/Health/SOURCE.md`.

## Testing and tooling

- Unity Test Framework is available transitively, but no first-party EditMode or PlayMode tests were found.
- Unity 6000.5.0f1 is installed locally.
- No Unity MCP capability was available in this task.
- Because the user's source and destination projects were open in Unity, the final migrated project was copied to an isolated validation directory and opened with Unity 6000.5.0f1 in batch mode. Compilation and the museum migration/validation utility completed successfully.
- A standalone player build has not yet been run. Focused automated Play Mode smoke checks exist for the Level 1 enemy spawn/navigation and damage/game-over paths; final hands-on input and visual checks remain appropriate in the Editor.
- The Level 1 player setup utility completed in an isolated project copy and validated the player at the authored `PlayerSpawnPoint`, including the camera, input actions, weapon controller, and five shadows-only character renderers.
- After the saved scene lost its enemy director, a repair utility reproduced the zero-enemy failure and restored only the missing wiring. The AI Navigation 2.0.14 `NavMeshSurface` baked 1,077 vertices; `EnemySpawnPoint` projected vertically from `(-4.88, 1.02, -13.89)` to `(-4.88, 0.92, -13.89)` with no horizontal displacement. A Play Mode smoke check confirmed exactly five living enemies spawned on the surface. Player and Enemy layers are configured not to collide with each other.
- The Level 1 damage/game-over pass compiled and ran in an isolated Unity project. A Play Mode smoke check applied 7.5 damage to both teams and observed matching `0.925` health-bar fills, a visible player damage overlay, and a lethal-hit game-over state with time paused, controls disabled, two buttons, and an EventSystem.
- The supplied prisoner-hostage FBX and tent FBX/textures import as URP prefabs. Two hostages and the tent are attached to their saved scene markers.
- The health presentation was migrated from sprite-less Filled Images to non-interactable Sliders after gameplay showed that the rectangles stayed visually full. Runtime validation observed both Slider values and fill-rectangle anchors at `0.5` after applying 50 damage.
- Level 1 now has a serialized `Level 1 Objective Controller`: one hostage may follow behind the player at a time, tent contact rescues it, the HUD counts two rescues, Victory pauses gameplay, and Next Level loads `Cutscene_Level2`. A focused Play Mode smoke check exercised the blocked second escort, movement toward the trailing position, both rescues, victory, and the scene transition.
- A restart smoke check confirmed the reloaded Level 1 hides the cursor. The batch Editor cannot retain `CursorLockMode.Locked` without a focused Game view, so final lock-state validation remains a hands-on Editor check; production code requests both lock and hide.
- The Level 1 instruction authoring pass validated a vertical ScrollRect, complete control/objective text, player input gating, and an idempotent enemy start gate. The initial five enemies now spawn only after START MISSION.
- The Level 2 cutscene authoring pass validated seven dialogue commands, the final `PRESS SPACE TO START` mode, and its `Scene_Level2` load target.
- The Level 2 model pass inspected all FBX hierarchies before splitting, generated 40 furniture prefabs plus house/radio/clock prefabs, and validated every prefab has renderers and colliders. The house is 11.05 × 3.41 × 12.81 Unity units with four mesh colliders. All generated materials resolve to supported URP/Lit shaders and their intended base/normal/occlusion textures.
- The Level 1 instruction controller now synchronizes its menu/gameplay cursor state with `StarterAssetsInputs`, preventing focus changes from locking the pointer before START MISSION.
- The Level 2 gameplay authoring pass compiled under Unity 6000.5.0f1 and persisted a model-less CharacterController player, initial-spawn reference, three explicit door/spawn pairs, an Inspector-editable C prompt, and an unscaled-time black fade overlay without rebuilding the user's environment or furniture roots.
- The Level 2 day-system Play Mode smoke test advanced from Day 1 through Day 6, returned the player to `PlayerInitialSpawnPoint` after every clock use, verified daily character visibility counts of 1/1/1/0/1/2, and confirmed the clock cannot advance past the final day. A separate persistence pass reloaded all clock, HUD, briefing, and marker references.
- The Level 3 environment import compiled and passed a persistence validation for all three prefabs plus the saved stadium scene instance. A focused Play Mode smoke test instantiated the Malaysia flag and observed its rig transform animation advance to normalized time 0.166. The exact visual framing and final prop placement remain manual Scene-view work.
- The Level 3 camera/UI PlayMode regression suite covers Camera 1 retaining its final pose, Camera 2 and Tunku moving beneath the black fade-in, all timer Sliders counting down, tutorial repetition, delayed/immediate incident resolution, health reveal, tutorial-to-gameplay handoff, randomized incident canvas containment, blank-line incident formatting, health Slider synchronization, and failure after three combined misses.
- The persistent audio system routes 13 supplied clips through separate music/effects channels. Authored Options panels exist in all nine build scenes; sliders persist independently, pause/resume preserves music playback position, and Level 3 owns one stable listener. Its non-looping cheer begins with Camera 1, gameplay music starts with the tutorial, the third miss gives one second of silence before looping loss music, and scene changes clear leftover one-shots. Focused authored-scene and Play Mode validators pass.

## Important constraints and risks

- The senior project uses HDRP 17.5.0, while the remake uses URP 17.5.0. The copied materials were converted to URP Lit, missing material slots were repaired, and two HDRP-only missing components were removed from the copied scene.
- The senior museum scripts use the legacy `UnityEngine.Input` API. Active Input Handling is set to Both; a future rewrite can migrate those scripts fully to the Input System.
- Preserve copied `.meta` files so scene, prefab, model, texture, material, font, and script GUID references remain intact after reorganization.
- Fungus/Amanita references both UGUI and the Input System assemblies. UGUI 2.5.0 is explicitly present in the remake package manifest, and the hierarchy-icon editor integration has a Unity 6000.5 compatibility branch.
- The museum scene is not yet a finished gallery: its current hierarchy contains menu, settings, radio, interaction, and placeholder environment objects.
- The supplied counter-terrorist is rigged but has no usable gameplay animations; its repeated preview/test clips were removed. The first-person body follows player yaw and renders as shadows only.
- The supplied pistol has no source URL or licence in its archive. Treat it as local assignment material until the user supplies the original source and licence.
- The supplied Terrorist model also has no source URL or licence in its archive. Unity reports two source meshes without normals when calculating tangents; the complete character still renders in the validated preview, but the source file should be repaired if normal-mapped materials are added later.
- The local hostage archive is the CC BY 4.0 “Prisoner Hostage Low Poly Character” by 00amza, not the separately supplied KinderKiev URL. The actual imported asset's attribution is recorded in `Assets/Level1/Art/Hostage/SOURCE.md`. The tent is CC BY 4.0 by Arkikon and is recorded in its own source file.
- Level 2 third-party sources and licences are recorded in `Assets/Level2/Art/THIRD_PARTY_SOURCES.md`. Furniture Set 03 is CC BY-NC-SA 4.0, so it must not be used for a commercial release without replacement or separate permission, and share-alike obligations need review before distribution.
- All three Level 3 downloads are CC BY 4.0. Required credits, including the Malaysia flag's underlying “Animated Flag” source, are recorded in `Assets/Level3/THIRD_PARTY_SOURCES.md` and `Assets/Level3/Licenses`.
- The stadium FBX has no source normals. Unity recalculates them during import; this is currently a non-blocking visual warning but should be considered if the stadium is later given normal-mapped materials.
- Level 3 completion now fades through the existing black overlay into a reusable full-screen credits scene. The Inspector-editable thank-you message appears after the video ends or is skipped, then returns to the Gallery's `InitialSpawnPoint`. Gallery replay omits the message and returns to `VideoSpawnPoint`.
- `Scene_Gallery` contains a no-jump WASD/mouse first-person player, an entry/exit fade, and camera-centre C interaction with an Inspector-editable 30-unit range. Eight historical exhibits use focus/restore and line dialogue. `QuizTrigger` opens the 16-question Tugu Negara quiz with shuffled four-choice positions and a score result; `CreditPicture` opens the thank-you panel locally, while `CreditVideoPicture` replays the video.
- Main-menu sky-dome and Tugu Negara prefabs are placed under an editable environment root. The Hintze Hall FBX is placed in the Gallery with explicit URP wall/decor/floor/glass materials; its large textures use streaming mipmaps and a 4096 import cap. The Picture Frame is prefab-only. Third-party model credits are recorded under each feature's `Art/THIRD_PARTY_SOURCES.md`.
- The supplied Hintze Hall archive contained no local licence file. Its Sketchfab listing identifies Creative Commons Attribution but the exact licence version must be reconfirmed before public release.

## Source files inspected

- `Defender_Of_Independence_Remake/ProjectSettings/ProjectVersion.txt`
- `Defender_Of_Independence_Remake/ProjectSettings/GraphicsSettings.asset`
- `Defender_Of_Independence_Remake/ProjectSettings/ProjectSettings.asset`
- `Defender_Of_Independence_Remake/ProjectSettings/EditorBuildSettings.asset`
- `Defender_Of_Independence_Remake/Packages/manifest.json`
- `Defender_Of_Independence_Remake/Packages/packages-lock.json`
- `Defender_Of_Independence_Remake/Assets/Starter Assets/Runtime/Unity.StarterAssets.asmdef`
- `GameDevelopment2Assignment/ProjectSettings/ProjectVersion.txt`
- `GameDevelopment2Assignment/Packages/manifest.json`
- `GameDevelopment2Assignment/ProjectSettings/EditorBuildSettings.asset`
- `GameDevelopment2Assignment/Assets/Scene/Meuseum.unity`
- Museum dependency assets and the three referenced first-party scripts
