# Level 1 Instruction Start Gate

`Scene_Level1` begins paused with the mission instruction panel visible. The player cannot move, look, interact, or shoot, and the enemy director does not spawn its initial group until **START MISSION** is selected.

The panel contains the complete keyboard/mouse controls, the two-hostage rescue objective, mission notes, and a permanent vertical scrollbar. The Start button restores normal time, locks and hides the pointer, enables player controls, and starts the editable enemy spawn schedule.

While the instruction panel is open, both Unity's cursor and the Starter Assets cursor settings remain unlocked with look input disabled. This prevents the Starter Assets focus callback from recapturing the pointer before **START MISSION** can be clicked.

To rebuild the authored panel, use **Project Tools > Level 1 > Build Instruction Panel**. The layout remains editable under `Level 1 Player HUD/Instruction Panel`, while the wiring component is the scene root `Level 1 Instruction Controller`.

Enemy counts and reinforcement timing remain editable on `Level One Enemy Director`. The start gate calls `BeginGameplay()` once, so repeated button events cannot duplicate the first wave.
