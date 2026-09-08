# Cutscene workflow

## Current sequence

`Scene_MainMenu` Start button -> `Cutscene_Level1` -> `Scene_Level1` -> `Cutscene_Level2` -> `Scene_Level2` -> `Cutscene_Level3` -> `Scene_Level3`

The Start button uses `SceneTransitionButton` and fades the screen to black before loading the cutscene. The Level 1 cutscene uses a Fungus Flowchart named `Level 1 Prologue Flowchart`. Each sentence is a standard Fungus `Say` command, followed by the Fungus-compatible `Load Scene Direct` command. The direct command avoids the long unused-asset cleanup performed by Fungus's legacy loader in Unity 6.

Space is read explicitly through Unity's Input System and forwarded to the Fungus dialog input. While a line is waiting, the prompt fades in and out. The final line uses `Set Final Prompt` so the prompt reads `PRESS SPACE TO START`.

## Edit Level 1 dialogue

1. Open `Assets/Scene/Cutscene_Level1.unity`.
2. Select `Level 1 Prologue Flowchart`.
3. Open `Tools > Fungus > Flowchart Window` if the Flowchart window is not visible.
4. Select `Play Level 1 Prologue` and edit its `Say` commands.
5. Keep `Set Final Prompt` directly before the last `Say` command.
6. Keep `Load Scene Direct` as the final command.

## Add a future cutscene image

1. Import a PNG or JPG under a clearly named art folder.
2. Select the image and set Texture Type to `Sprite 2D and UI`, then apply it.
3. Open the cutscene scene and expand `Level 1 Cutscene > Cutscene Canvas`.
4. Select `Backdrop Image Assign Future Art Here`.
5. Drag the imported Sprite into the Image component's Source Image field.
6. Change the Image color alpha from 0 to 255.
7. Enable Preserve Aspect for letterboxing, or disable it if the art was authored at the target 16 by 9 resolution.

The backdrop is behind the title and dialogue panel, so no code changes are needed.

## Level 3 cutscene

`Cutscene_Level3` reuses the same Fungus presentation and Space input. Its nine dialogue beats bridge the February 1956 London agreement, Tunku Abdul Rahman's announcement in Malacca, and the transition to the independence ceremony on 31 August 1957. The final prompt reads `PRESS SPACE TO START`, then `Load Scene Direct` opens `Scene_Level3`.

Rebuild it with **Project Tools > Cutscenes > Rebuild Level 3 Cutscene**.

## Create later cutscenes

Use `Cutscene_Level1` as the reference. Each cutscene needs its own Flowchart dialogue, its final `Load Scene Direct` target, and an EventSystem. Do not duplicate scene names in Build Settings.

For future level-selection buttons, add `SceneTransitionButton` to the button and enter the corresponding cutscene scene name, such as `Cutscene_Level2`.

## Level 1 map and lighting

- The map instance is under `Level 1 Environment > Dust2 Map (Set Spawn Points Later)` in `Scene_Level1`.
- Change the map's position, rotation, or scale from that root Transform if the level needs to move later.
- Thirty-four generated URP materials are kept in `Assets/Level1/Art/Environment/Dust2Map/Materials` and remain editable. Their textures use repeat wrapping, trilinear filtering, mipmaps, and anisotropic filtering so the supplied UV tiling is preserved.
- The imported map has a generated Mesh Collider covering all 34 material submeshes. Review collision around steps and narrow doorways after adding the player controller.
- `Level 1 Lighting` contains the global daylight colour profile and a reflection probe. The existing Directional Light supplies realtime sunlight and shadows.
- When the final props and layout are in place, select `Reflection Probe - Bake When Layout Is Final` and use **Bake** in its Inspector. A final lightmap bake can wait until level composition is stable.
- Player, enemy, hostage, and objective spawn points have deliberately not been added.

The replacement environment is `de_dust2 - CS map` by vrchris from Sketchfab, supplied under CC BY 4.0. Full source and credit details are stored beside the asset in `Dust2Map/SOURCE.md`.
