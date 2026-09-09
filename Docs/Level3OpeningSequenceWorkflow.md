# Level 3 Opening Sequence

`Scene_Level3` begins behind a black overlay, fades into Camera 1, and plays the three authored shots in order:

1. Camera 1 plays `CameraAnimation`.
2. The screen fades to black for one second, switches cameras, then fades in for one second. Camera 2 and `TunkuAbdulRahmanAnimation` then play together.
3. The same two-second black transition switches to Camera 3, which plays `CameraAnimation3` and remains active on its final frame.

There is intentionally no fade after Camera 3. The `On Sequence Finished` event on `Level 3 Opening Sequence` is ready for the gameplay UI panel that will be connected later.

## Inspector controls

- `Scene Entry Fade In Duration`: black fade used when arriving from the Level 3 cutscene; default 2 seconds.
- `Transition Half Duration`: fade-out and fade-in duration; default 1 second each (about 2 seconds total per camera change).
- `Playback Speed`: leave at 1 for the intended timing. It can be increased temporarily while previewing.
- All cameras, Animators, clips, and the fade overlay are assigned explicitly in the scene.

Camera 2's clip was converted from Legacy to the same Mecanim format as Cameras 1 and 3. Its animation curves, keyframes, 60 fps sample rate, and nine-second length are unchanged; only Legacy and looping playback were disabled. All three cameras now use their assigned Animators, and the obsolete `Animation` components are removed so they cannot compete with the sequence.

## Manual check

Open `Assets/Scene/Scene_Level3.unity` and enter Play Mode. Confirm that only one shot camera is active at a time, Tunku moves only during Camera 2, both camera changes pass through black, and Camera 3 remains visible without a final fade.
