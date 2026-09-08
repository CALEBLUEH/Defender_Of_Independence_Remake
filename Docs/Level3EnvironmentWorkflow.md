# Level 3 environment workflow

## Authored assets

- `Assets/Level3/Prefabs/Environment/Stadium.prefab`
- `Assets/Level3/Prefabs/Props/Malaysia Flag.prefab`
- `Assets/Level3/Prefabs/Props/Debate Podium.prefab`
- `Assets/Scene/Scene_Level3.unity`

The stadium is placed at the world origin under `Level 3 Environment` and normalized to approximately 150 metres across. The flag is normalized to 6 metres tall. The podium is normalized to 1.2 metres tall and has an editable root `BoxCollider`.

Only the stadium is placed in the scene at this stage. Drag the flag and podium prefabs into the scene when their positions are ready.

## Malaysia flag animation

The supplied glTF contains the `Take 001` skeletal animation. glTFast imports it as a looping Mecanim clip; the setup creates `Malaysia Flag.controller`, references that compact imported clip directly, and assigns the controller to the prefab's Animator. The Animator uses `Always Animate` so the flag continues moving during a camera cut even when it is briefly outside the camera view.

The original animated flag texture is retained as a source dependency, while the prefab's cloth renderer uses `Malaysia Flag.mat` with the supplied Malaysian flag texture. The material is two-sided so the cloth remains visible from either side.

## Rebuilding

Use `Project Tools > Level 3 > Import Stadium, Flag, and Podium` if the source models need to be reimported. The setup preserves an already-placed stadium instance so manual scene positioning is not lost.

Use `Project Tools > Level 3 > Validate Environment in Play Mode` to instantiate the flag temporarily and verify that its rig transforms move. The temporary test instance is destroyed when Play Mode ends and is not saved into the scene.

## Later cutscene setup

When the Level 3 sequence is authored:

1. Drag `Malaysia Flag.prefab` and `Debate Podium.prefab` into `Scene_Level3`.
2. Place a separate Tunku Abdul Rahman instance at the desired starting marker.
3. Animate only the Tunku root position from the start marker to the podium marker; the character may remain in the current T-pose.
4. Keep these transforms exposed in the scene so their exact placement and timing can be adjusted manually.
