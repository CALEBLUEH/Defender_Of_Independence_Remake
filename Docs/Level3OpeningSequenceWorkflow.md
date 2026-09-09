# Level 3 Opening Sequence

`Scene_Level3` begins behind a black overlay, fades into Camera 1, and plays the three authored shots in order:

1. Camera 1 plays `CameraAnimation`.
2. The screen fades to black for one second. While fully black, it switches to Camera 2 and starts Camera 2 plus `TunkuAbdulRahmanAnimation`; both animations continue while the black overlay fades away.
3. The same two-second black transition switches to Camera 3 and starts `CameraAnimation3` underneath its fade-in. Camera 3 remains active on its final frame.

There is intentionally no fade after Camera 3. The `On Sequence Finished` event on `Level 3 Opening Sequence` now fades in the authored Level 3 tutorial gameplay UI.

Camera 1 is explicitly sampled just inside its last keyed frame before the first transition. Sampling at the exact clip length could wrap to frame zero and flash its starting transform. The fade now holds a fully black rendered frame, swaps and starts Camera 2/Tunku, holds black for another frame, and then fades in.

## Inspector controls

- `Scene Entry Fade In Duration`: black fade used when arriving from the Level 3 cutscene; default 2 seconds.
- `Transition Half Duration`: fade-out and fade-in duration; default 1 second each (about 2 seconds total per camera change).
- `Playback Speed`: leave at 1 for the intended timing. It can be increased temporarily while previewing.
- All cameras, Animators, clips, and the fade overlay are assigned explicitly in the scene.

Camera 2's clip was converted from Legacy to the same Mecanim format as Cameras 1 and 3. Its animation curves, keyframes, 60 fps sample rate, and nine-second length are unchanged; only Legacy and looping playback were disabled. All three cameras now use their assigned Animators, and the obsolete `Animation` components are removed so they cannot compete with the sequence.

## Manual check

Open `Assets/Scene/Scene_Level3.unity` and enter Play Mode. Confirm that only one shot camera is active at a time, Tunku moves only during Camera 2, both camera changes pass through black, and Camera 3 remains visible without a final fade.
