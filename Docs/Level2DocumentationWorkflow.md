# Level 2 Documentation Workflow

## Player interaction

- Aim at one of the four authored documentation colliders and press `C`.
- The Main Lobby instruction page is free, reusable, and never leaves the pool.
- Historical documents cost 1 Energy. The player starts each day with 4 Energy, restored by advancing the day at `NextDayClock`.
- An opened page disables movement and mouse look while releasing the pointer for the scrollbar and top-right `X`.
- Click `X`, press `C`, or press `Escape` to close a page.
- The British Office, Planning Room, and Radio Station each own three historical pages. A page is randomly selected without replacement and cannot reappear during the current playthrough.
- An exhausted location displays `NO DOCUMENTS REMAIN HERE` and consumes no Energy.

## Scene wiring

The `LevelTwoDoorInteractor` remains the single owner of the shared `C` interaction input. It raycasts from the player camera to the documentation colliders and continues to handle the existing nearby door and clock interactions.

The following existing scene objects carry `LevelTwoDocumentLocation`:

- `MainLobbyDocumentation(Tutorial)`
- `BritishOfficeNegotiateDocumentation`
- `PlanningRoomDocumentation`
- `RadioStationDocumentation`

Each location keeps its room type, collider, reuse policy, Energy cost, and document pages editable in the Inspector. The generated `Level 2 Document Panel` is under `Day HUD`, with a scroll view, left-aligned body text, permanent vertical scrollbar, and top-right close button.

The Main Lobby contains one gameplay guide rather than the former conference-schedule page. It also shows the active day's title and appointment. The other nine historical pages reproduce the documentation text from `Dummy Word File.docx`.

## Tuning and later work

- `LevelTwoDayController.maximumEnergy` controls the daily Energy allowance.
- `LevelTwoDoorInteractor.interactionRange` controls document, door, and clock interaction range.
- Documentation bonuses are intentionally not applied yet. The existing page data and Energy API are ready for the later negotiation-meter system.

## Rebuild and validation

- Rebuild scene wiring: `Project Tools > Level 2 > Build Documentation System`
- Run the automated Play Mode check: `Project Tools > Level 2 > Validate Documentation in Play Mode`
