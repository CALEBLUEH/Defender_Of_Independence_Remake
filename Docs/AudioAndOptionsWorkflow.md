# Audio and Options Workflow

## Runtime structure

- `Assets/Audio/Settings/GameAudioLibrary.asset` is the Inspector-editable catalogue for all music and effects.
- `Assets/Audio/Prefabs/Game Audio System.prefab` is present once in every build scene. Its service persists across scene loads and discards duplicate scene instances.
- Music and sound-effect volumes are saved separately in `PlayerPrefs` and applied immediately. They persist across scene changes and application restarts.
- Every authored UI `Button` receives the universal click effect automatically, including dynamically enabled buttons.

## Scene music

- Main Menu: `TerrariaTitleScreen`
- Level 1: `DragonCastle`
- Level 2: `CelesteOriginalSoundtrackMadelineandTheo`
- Level 3: `DeathByGlamour`, starting when the tutorial panel appears rather than when the scene opens
- Gallery: `KyrieEleison`
- Cutscenes and Credits have no automatic background music.

Background music loops. Opening the Escape menu pauses the music source; Resume continues from the same playback position. Loss music replaces the scene track and loops until the scene is restarted or left.

## Sound effects

- Fungus cutscene Writers and Level 2 conversation lines play the next-dialogue effect.
- Level 1, Level 2, and Gallery movement use the walking loop. Level 1 requires grounded movement, so jumping is silent.
- Player and enemy Level 1 shots use the supplied gunshot effect.
- Level 3 has one stable `AudioListener`. It plays cheering once when Camera 1 begins, keyboard taps for typed characters, and the Merdeka effect whenever a main gameplay line containing `Merdeka` appears. Scene changes stop remaining one-shot effects so cheering cannot carry into Credits.
- Level 1 and Level 3 losses now fade through black before revealing their loss UI; Level 2 retains its existing black result transition. Level 3 stops gameplay music immediately on the third miss, waits one second in silence, and then starts the looping loss track.

## Options UI

The Main Menu and every Escape menu contain one authored `Audio Options Panel`. `SOUND EFFECTS` and `MUSIC` sliders range from 0–100%, show the current percentage, and save changes immediately. The X button returns to the menu underneath it.

## Inspector assignments and tuning

- Replace or rebalance a clip in `GameAudioLibrary.asset`; gameplay scripts do not contain asset paths or hardcoded clip names.
- `Game Audio System.prefab` exposes the library and its two AudioSources.
- Walking thresholds and volume are serialized on each player object's `PlayerMovementLoopAudio` component.
- Level 1 and Level 3 loss-fade durations remain serialized on their existing gameplay controllers.

The source MP3s imported directly in Unity 6000.5.0f1. No conversion was needed, and the supplied source folder was not modified. Long music clips use streaming/background loading; short effects use compressed-in-memory import settings.

## Focused manual checks

1. Adjust both sliders, change scenes, return to the Main Menu, and confirm their values remain distinct.
2. Pause during a recognizable point in a music track and confirm Resume continues at that point.
3. In Level 1, walk, jump, fire, receive lethal damage, and confirm the walk loop stops in the air and the loss transition/music appear.
4. Confirm Level 3 is silent during its opening, cheering plays once, and gameplay music begins with the tutorial panel.
5. Check final loudness on the intended speakers/headphones; clip balance is subjective even when routing is correct.
