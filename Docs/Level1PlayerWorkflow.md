# Level 1 First-Person Player

## Controls

- `W`, `A`, `S`, `D`: run at 5 m/s.
- Hold `Left Shift`: walk at 2.5 m/s.
- Mouse: aim and turn the player body.
- Left mouse button: fire. Click once in Manual mode; hold in Auto mode.
- Right mouse button: toggle Manual/Auto fire mode.
- `R`: reload the 12-round magazine in 0.5 seconds. Reserve ammunition is unlimited.
- `C`: interact with an object that implements `IPlayerInteractable` while it is in range.
- Jump is intentionally disabled because it is not part of the assignment controls.

## Authored assets

The reusable player is `Assets/Level1/Prefabs/Player/LevelOnePlayer.prefab`. The Level 1 scene contains an instance named `Level 1 Player` at the existing `PlayerSpawnPoint` marker. The old standalone scene camera is replaced by the player's first-person camera.

The pistol viewmodel, camera recoil, weapon pushback, movement bob, crosshair, ammunition display, fire-mode display, and interaction prompt are authored in the prefab and remain editable in the Inspector. Reloading deliberately has no animation.

The counter-terrorist model is rigged, but the supplied download contains only repeated `eye_test` and `tools_preview` clips. It has no idle, walk, run, aim, fire, or reload gameplay clips. Those unrelated clips were removed from the compact project copy. The body is attached beneath the player root, so it follows horizontal mouse aim, and all body renderers use Shadows Only so the first-person camera cannot see the body while the character can still cast a shadow.

## Inspector tuning

- `First Person Controller`: run speed is `Move Speed`; held-Shift walk speed is `Sprint Speed`.
- `First Person Weapon Controller`: magazine size, reload duration, fire interval, range, damage, recoil, pushback, return speed, and movement bob.
- `Level One Player Interactor`: interaction range and layer mask.

Future enemies can receive pistol damage by implementing `IDamageable`. Future hostages or mission objects can show the `C` prompt by implementing `IPlayerInteractable`.

## Rebuild command

Use `Project Tools > Level 1 > Rebuild First Person Player` after intentionally changing the source models or generated-player setup. It preserves `PlayerSpawnPoint`, rebuilds the prefab, and replaces only the `Level 1 Player` scene instance.

## Manual test

1. Open `Scene_Level1` and press Play.
2. Confirm the mouse turns the view and player body, WASD runs, and held Shift is slower.
3. Fire 12 shots, verify the gun/camera kick backward and upward, then press `R` and confirm the counter refills after 0.5 seconds without a reload animation.
4. Toggle Manual/Auto with the right mouse button and confirm click-versus-hold firing.
5. Confirm the crosshair, ammunition count, and fire mode remain visible and that the player's body is not visible in first person.

## Third-party source status

The counter-terrorist source and CC BY 4.0 attribution are recorded beside the asset in `Assets/Level1/Art/Player/CounterTerrorist/SOURCE.md`.

The pistol archive did not include a creator name, source URL, or licence. Its project copy is marked for local assignment use only in `Assets/Level1/Art/Player/Weapon/Deagle/SOURCE.md`. Add the original download URL and licence before publishing or distributing a build.
