# Level 2 Day System

## Player flow

Level 2 begins on Day 1. The player can approach `NextDayClock` in the main lobby and press **C** to end the current day. During the transition:

1. Player movement and camera look are temporarily disabled.
2. A full-screen black day card fades in.
3. The day increases by one.
4. The player returns to `PlayerInitialSpawnPoint`.
5. Scheduled character models update before the card fades out.

The clock stops at Day 6. It does not release the mouse pointer; pointer control remains reserved for future dialogue choices.

## Daily appointments

| Day | Theme | Recommended destination | Visible character placement |
| --- | --- | --- | --- |
| 1 | Preparation | Planning Room | Tunku Abdul Rahman at `TunkuAbdulRahman(Day1)` |
| 2 | Internal Security | British Office | Alan Lennox-Boyd at `AlanLennox-Boyd(Day2)` |
| 3 | Finance and Development | British Office | Alan Lennox-Boyd at `AlanLennox-Boyd(Day3)` |
| 4 | The Voice of the People | Radio Station Room | No character model; the radio event will be added later |
| 5 | Defense and Final Position | Planning Room | Tunku Abdul Rahman at `TunkuAbdulRahman(Day5)` |
| 6 | The Constitutional Conference | British Office | Both models at their Day 6 markers |

Each marker contains a scheduled wrapper and a prefab instance. Adjust the wrapper Transform under the marker to fine-tune the character's position, rotation, or scale without changing the shared character prefab.

## Inspector tuning

`Level 2 Gameplay/Level 2 Day System` contains the authoritative `LevelTwoDayController`. Its six briefing titles, appointment text, starting day, fade duration, and day-card hold duration are editable in the Inspector.

The character prefabs are stored in `Assets/Level2/Prefabs/Characters`. Source and licence details are recorded in `Assets/Level2/Art/THIRD_PARTY_SOURCES.md`.

To rebuild only the day system, model prefabs, HUD, and scheduled character wrappers, use **Project Tools > Level 2 > Build Day System and Characters**. The environment, room furniture, door markers, and player prefab are preserved.
