# Level 1 Hostage Objective

## Player flow

1. Approach a hostage, aim at it, and press `C` when `C  ASK HOSTAGE TO FOLLOW` appears.
2. The selected hostage follows approximately 2.2 metres behind the player. Its movement speed, turning speed, follow distance, and catch-up distance are editable on `HostageEscort`.
3. Only one hostage can follow at a time. Trying to select the other hostage displays `ONLY ONE HOSTAGE CAN FOLLOW AT A TIME`.
4. Lead the hostage into the trigger volume on the tent. Contact automatically rescues and hides that hostage; no additional button press is needed.
5. Repeat for the second hostage. The objective HUD updates `HOSTAGES SAVED: 0 / 2`, `1 / 2`, and `2 / 2`.
6. Saving both hostages pauses Level 1 and displays the Victory Panel. `Next Level` loads `Cutscene_Level2`.

## Authored objects

- `Level 1 Objective Controller` in `Scene_Level1` owns the current escort, rescue count, warning, victory state, and serialized scene references.
- `Hostage A` and `Hostage B` contain `HostageEscort`, a trigger capsule, and a kinematic Rigidbody.
- `Tent` contains `TentRescueZone` and an editable trigger box fitted to its rendered bounds.
- `Level 1 Player HUD` contains `Objective Panel`, `Hostage Warning Prompt`, and `Victory Panel`.

The hostage trigger does not physically block the player. Following uses basic translation and rotation with no animation, as requested.

## Health and cursor regression fix

The player and enemy health bars are now non-interactable Unity `Slider` components. `CombatHealth` remains the authoritative value, and each presenter uses `SetValueWithoutNotify` so the Slider's fill rectangle actually changes width.

Level 1 explicitly hides and requests a locked cursor when it starts or restarts. Game Over and Victory unlock it for their buttons. Back to Menu intentionally leaves it visible because the main-menu buttons require a pointer.

## Inspector tuning

- Select either hostage prefab to edit follow distance, movement speed, turning speed, and maximum catch-up distance.
- Select `Level 1 Objective Controller` to edit the warning duration, UI references, hostage list, tent zone, and next cutscene name.
- Resize the tent's `Box Collider` if the rescue area should be smaller or larger than the tent's current rendered bounds.

## Manual test

1. Enter Level 1 and confirm the cursor is hidden and mouse look works.
2. Take damage and confirm both the number and green Slider width decrease. Shoot an enemy and confirm its red Slider width decreases.
3. Recruit Hostage A, then try to recruit Hostage B and confirm the warning appears.
4. Walk away and confirm Hostage A follows behind rather than entering the first-person view.
5. Lead Hostage A into the tent and confirm it disappears and the counter reaches `1 / 2`.
6. Rescue Hostage B and confirm the Victory Panel appears with `2 / 2`.
7. Press `Next Level` and confirm `Cutscene_Level2` opens.
8. Separately die and press Restart; confirm Level 1 reloads with the cursor hidden.
