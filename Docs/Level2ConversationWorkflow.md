# Level 2 Dialogue and Room Energy Workflow

## Daily conversations

Aim at the scheduled character and press `C` to spend 1 Energy and begin that day's conversation. Day 4 uses the authored `RadioStationCollider`. The dialogue window releases the mouse pointer and presents the choices from `Dummy Word File.docx`. After selecting a choice, click `CONTINUE`; the Day 6 finale proceeds through all three questions before closing.

Each conversation can begin only once. Its collider remains targetable during the active day so a completed interaction reports `NO CONVERSATION REMAINS HERE TODAY`. Dialogue choices do not change British Confidence, Delegation Unity, or Public Support yet.

| Day | Trigger | Steps |
| --- | --- | --- |
| 1 | Tunku Abdul Rahman in the Planning Room | Delegation objective |
| 2 | Alan Lennox-Boyd in the British Office | Internal security |
| 3 | Alan Lennox-Boyd in the British Office | Finance and development |
| 4 | `RadioStationCollider` | Public radio broadcast |
| 5 | Tunku Abdul Rahman in the Planning Room | Defense cooperation |
| 6 | Alan Lennox-Boyd in the British Office | Constitution, independence date, final commitment |

Character wrappers contain an editable child named `Conversation Collider`. The radio event reuses the user's existing collider. All six triggers, dialogue steps, choice labels, and responses are Inspector-visible.

## Room entry Energy

- The first entry into each room on a given day costs 1 Energy.
- Exiting is free.
- After paying for a room, the player may leave and re-enter that room freely for the rest of the day.
- Advancing to the next day makes each room's first entry cost Energy again.
- On Day 6, the Planning Room and Radio Station doors are locked. Only the Alan Lennox-Boyd/British Office door remains available.

The paid-room state is stored by each `LevelTwoDoorTransition` and compared with the current day, so no separate reset wiring is required.

## Rebuild and validation

- Rebuild: `Project Tools > Level 2 > Build Dialogue and Door Energy System`
- Validate: `Project Tools > Level 2 > Validate Dialogue and Door Energy in Play Mode`
