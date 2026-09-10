# Credits, Main Menu, and Gallery Content

## Credits flow

Completing the final Level 3 typing line invokes `LevelThreeCreditTransition.BeginCreditsFromLevelCompletion()` through the saved `On Gameplay Completed` event. The existing `Level 3 Opening Fade` takes two seconds to cover the screen, then loads `Scene_Credits`.

`Scene_Credits` contains an Inspector-authored full-screen `VideoPlayer`, camera, 2D `AudioSource`, completion-message panel, black fade, and bottom skip prompt. The supplied 1920 × 1080, 60-second `CreditVideo.mp4` is assigned directly as a `VideoClip`; its single audio track is routed to the AudioSource.

- Level completion requests the completion-message route. Its body copy is editable on `Credit Playback Camera > Credit Video Controller > Completion Message`.
- Entering the credits scene without a request behaves as direct Gallery playback and omits the completion message.
- A future Gallery interaction can call `CreditPlaybackSession.RequestFromGallery()` immediately before loading `Scene_Credits`.
- Holding Space for three seconds fills the bottom progress rail and uses the same fade-to-Gallery path as natural video completion.
- The video uses `Fit Inside` aspect handling. At non-16:9 resolutions the entire frame remains visible with black bars instead of being cropped.

## Imported content

The extracted source files are organized under `Assets/MainMenu/Art` and `Assets/Gallery/Art`; duplicate download ZIPs were not copied.

- `Main Menu Sky Dome.prefab` is placed under `Main Menu Environment` in `Scene_MainMenu`.
- `Tugu Negara Monument.prefab` is placed beside it for the later orbit-camera sequence.
- `Hintze Hall.prefab` is placed under `Gallery Environment` in `Scene_Gallery`.
- `Picture Frame.prefab` remains prefab-only and has a simple placement `BoxCollider`.

Scene instance names include `Position Here` so their root transforms remain easy to find and adjust. The Gallery hall is normalized to an 80-unit horizontal footprint and deliberately has no generated collision mesh yet; add deliberate floor/wall colliders after the player route and spawn point are chosen.

The hall's seven 8K source textures import with streaming mipmaps, compressed runtime data, and a 4096 maximum size. Its FBX material slots are explicitly mapped to URP/Lit materials, including separate UV1/UV2 wall sections and transparent gallery glass.

## Manual check

Finish Level 3 and confirm the two-second fade reaches black before the completion message, the video appears edge-to-edge at 16:9 with sound, and natural completion loads `Scene_Gallery`. Repeat and hold Space for three seconds; confirm the progress rail resets if released early and a completed hold fades to the same Gallery scene. Finally inspect the main-menu and Gallery scenes and adjust the named model-instance root transforms for the intended framing.
