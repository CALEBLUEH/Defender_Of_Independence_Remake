# Level 2 Cutscene and Environment

## Cutscene

`Cutscene_Level2` uses the same Fungus dialogue layout and Space-key flow as Level 1. Its seven dialogue beats cover the Japanese surrender, British return, opposition to the Malayan Union, Tunku Abdul Rahman's peaceful constitutional approach, and the player's role in the 1956 London delegation. The final prompt changes to **PRESS SPACE TO START** and loads `Scene_Level2`.

Rebuild it with **Project Tools > Cutscenes > Rebuild Level 2 Cutscene**.

## Environment and props

`Scene_Level2` contains `Level 2 Environment/Interior House (Decorate Rooms Here)` at the origin. The house uses generated URP materials and mesh colliders.

Forty furniture prefabs are organized under:

- `Assets/Level2/Prefabs/Furniture/Set01`
- `Assets/Level2/Prefabs/Furniture/Set02`
- `Assets/Level2/Prefabs/Furniture/Set03`

Each set-piece model was split at its authored top-level object boundaries, recentered on the floor, clearly renamed, and given an editable Box Collider. Drag individual prefabs into the house and adjust their Transform normally.

The progression props are ready but are not placed in the scene:

- `Assets/Level2/Prefabs/Props/Radio Station.prefab`
- `Assets/Level2/Prefabs/Props/Grandfather Clock.prefab`

Rebuild the imports with **Project Tools > Level 2 > Import House and Furniture Prefabs**. This imports textures, regenerates URP materials and colliders, rebuilds all prefabs, and replaces only the `Level 2 Environment` root in `Scene_Level2`.

Third-party sources and required credits are recorded in `Assets/Level2/Art/THIRD_PARTY_SOURCES.md`.

## Player and room doors

`Scene_Level2` now contains an authored `Level 2 Gameplay` root with a model-less first-person player, interaction prompt, and black transition overlay. The player starts at `PlayerInitialSpawnPoint`, uses WASD for movement and the mouse for looking, and intentionally has no jump or weapon behavior.

Press **C** near one of the three configured doors to move between its Exit and Enter spawn markers. The screen fades to black, the player moves at full black, and the view faces away from the door after arrival. The current pairs are:

- `TunkuAbdulRahmanDoor`
- `AlanLennox-BoydDoor`
- `RadioStationDoor`

Door range, room label, both spawn references, fade duration, and optional use of marker rotation are editable in the Inspector. `LevelTwoFirstPersonController.ReturnToInitialSpawn()` is the prepared entry point for the future skip-day system.

Rebuild only this gameplay wiring with **Project Tools > Level 2 > Build Player and Door Transitions**. It replaces only its own `Level 2 Gameplay` root, preserves the decorated environment/furniture, and reuses the six named room spawn markers.
