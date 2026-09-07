# Level 1 Enemy Combat

## Implemented behaviour

- Level 1 starts with 5 enemies at `EnemySpawnPoint` and adds 2 enemies every 60 seconds, up to 25 alive enemies.
- Each spawned enemy randomly begins in `Attack` or `Defend`.
- `Attack` enemies pursue the player and stop at their preferred combat distance.
- `Defend` enemies are assigned randomly to `Hostage1Location` or `Hostage2Location`, wander within a radius around that site, and stop to fire when they see the player.
- Every non-lethal hit has a 10% chance to switch an enemy to `Retreat` for 3 seconds. Afterwards it randomly returns to `Attack` or `Defend`.
- When an enemy dies, surviving enemies within the player-area radius switch to `Defend`; enemies outside it switch to `Attack`.
- Enemies have a 110-degree horizontal field of view. Sight distance is not capped; walls and other colliders still block line of sight.
- Pistol accuracy interpolates from 85% at 6 metres to 20% at 35 metres. These values and the miss angle are editable.
- Every successful player or enemy pistol hit rolls independently between 5 and 10 damage. Both limits remain editable on the respective weapon component.
- Enemy world-space health bars are active and publish their initialized value after subscribers are ready, so the fill stays synchronized with `CombatHealth`.

## Player damage and game over

- A hit briefly overlays the player's view with a transparent red flash, holds for 0.08 seconds, and fades over 0.65 seconds. Peak alpha and timing are editable on `PlayerDamageFeedback`.
- Lethal damage disables player control, pauses gameplay, unlocks the cursor, and opens the authored `Game Over Panel` under `Level 1 Player HUD`.
- `Restart` restores time and reloads the active Level 1 scene.
- `Back To Menu` restores time and loads `Scene_MainMenu`.
- `Level 1 Event System` uses the Input System UI module so both buttons work independently from the disabled player input component.

## Authored assets

- Enemy prefab: `Assets/Level1/Prefabs/Enemy/LevelOneEnemy.prefab`
- Terrorist model, textures, and URP materials: `Assets/Level1/Art/Enemy/Terrorist`
- AI Navigation surface: `Level 1 Navigation Surface` in `Scene_Level1`
- Navigation data: `Assets/Scene/Scene_Level1/NavMesh.asset`
- Scene controller: `Level 1 Enemy Director` in `Scene_Level1`

The prefab uses the same Deagle asset as the player. Its arm pose is procedural, so both hands remain on editable `Right Hand Grip` and `Left Hand Grip` markers without requiring a supplied animation clip. Leg animation is intentionally omitted.

## Inspector balancing

Select `Level 1 Enemy Director` to edit initial count, reinforcement count, interval, spawn radius, maximum alive enemies, and the player-area radius used after a death.

`Spawn Radius` is currently `0`, so all enemies initially use the exact `EnemySpawnPoint` location projected vertically onto the walkable NavMesh. Increase it later if a spread-out spawn wave is preferred.

The project defines `Player` and `Enemy` physics layers. Their mutual collision is disabled, while weapon raycasts and line-of-sight queries can still detect both layers.

Open `LevelOneEnemy.prefab` to edit:

- attack, defend, and retreat speeds;
- combat distance and navigation refresh rate;
- defence wander radius and interval;
- view angle and sight layers;
- fire interval, damage, weapon range, near/far accuracy distances, accuracy percentages, and maximum miss angle;
- retreat chance, duration, and distance;
- both hand-grip markers and `EnemyAimPose` weight;
- maximum health and the world health-bar layout.

The player's health bar is beside the ammunition display. Both player and enemy bars use non-interactable `Slider` components whose fill rectangles are driven by `CombatHealth`. Aiming the crosshair directly at a living enemy shows text such as `Enemy: 100%` beneath the crosshair.

## Hostage and tent props

- `Hostage.prefab` is placed as `Hostage A` and `Hostage B`, parented at zero local offset under `Hostage1Location` and `Hostage2Location`.
- `Tent.prefab` and its URP material are ready in `Assets/Level1/Prefabs/Props`.
- The saved scene did not contain a root object named `TentLocation`, so the setup deliberately did not guess a tent position. Add/save that marker and rerun `Project Tools > Level 1 > Apply Game Over, Health, and Props` to place the tent at exact zero local offset.
- Source and CC BY 4.0 attribution records are stored beside each imported art folder in `SOURCE.md`.

## Rebuild command

Use `Project Tools > Level 1 > Repair Enemy Scene Wiring` if the scene controller or NavMesh surface is lost after a scene overwrite. This preserves the existing player instance and scene composition, reconnects `EnemySpawnPoint`, `Hostage1Location`, and `Hostage2Location`, configures collision layers, and rebakes the surface.

Use `Project Tools > Level 1 > Rebuild Enemy Combat System` only after intentionally changing the source models or generated prefabs because that command also rebuilds the player and enemy prefabs.

## Manual test

1. Open `Scene_Level1`, press Play, and confirm exactly 5 enemies spawn around `EnemySpawnPoint`.
2. Watch Attack enemies pursue and Defend enemies patrol either hostage site. Stand behind a wall to verify it blocks their fire.
3. Aim at an enemy and confirm the crosshair prompt and world health bar update when the enemy is shot.
4. Let an enemy shoot the player and confirm the health bar near the ammunition counter decreases.
5. Temporarily set reinforcement interval to a small value and verify each wave adds the configured number without exceeding the maximum alive count.
6. Temporarily raise retreat chance to 100%, shoot an enemy once, and verify it runs away for the configured duration before selecting Attack or Defend.
7. Kill one enemy and observe nearby survivors defend while distant survivors attack.
8. Let the player take a non-lethal hit and confirm the red overlay appears, then fades and the numeric/fill health display agrees with the damage taken.
9. Let the player die and confirm gameplay pauses, `Restart` reloads Level 1, and `Back To Menu` loads `Scene_MainMenu`.

## Third-party source status

The supplied Terrorist archive contained an FBX and three textures, but no creator name, download URL, or licence. Its status is recorded in `Assets/Level1/Art/Enemy/Terrorist/SOURCE.md`. Keep it to local assignment use until the original Sketchfab URL and licence are supplied, then add the required attribution before publishing or distributing the game.
