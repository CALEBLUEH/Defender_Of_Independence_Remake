# Gallery Exploration and Exhibit Dialogue

## Player and spawn routing

`Scene_Gallery` contains `Gallery Gameplay/Gallery First Person Player`, a model-less CharacterController using WASD and mouse look. Jumping is intentionally unavailable.

- Normal Gallery entry, including the Main Menu Gallery button and completion of the full game, uses the user's `InitialSpawnPoint`.
- Watching the credits from `CreditVideoPicture` returns to the user's `VideoSpawnPoint`.
- The active spawn request is consumed once when the Gallery loads, then resets to the initial route.
- A short black entry fade keeps the spawn change hidden. The cursor remains locked and hidden during Gallery exploration and picture dialogue.

## Historical pictures

The eight manually placed picture objects retain their original transforms, renderers, materials, and colliders. Each owns an Inspector-editable `GalleryExhibit` component:

- `JapanPicture`
- `LondonPicture`
- `TunkuPicture`
- `MalaysiaPicture`
- `BritishPicture`
- `ReturnPicture`
- `IndependencePicture`
- `NegotiationPicture`

Aim at a picture within 3.5 units and press C. Player movement pauses, the camera moves to the visible side of the picture and faces its renderer bounds, and the bottom dialogue panel presents one sentence at a time. Press Space to continue; the final prompt returns the camera to its exact saved local pose and restores movement.

The dialogue adapts the memory-hall subjects from `Dummy Word File.docx`. Independence explains the 1956 agreement, constitutional preparation, 31 August 1957 and the responsibility of self-government. Negotiation explains delegation unity, British security and administrative concerns, public support, and peaceful compromise.

## Credit replay and completion message

Aim at `CreditVideoPicture` and press C to fade into `Scene_Credits`. This route never shows the game-completion message and returns to `VideoSpawnPoint` whether the video ends naturally or is skipped.

Interactions use a camera-centre ray rather than proximity triggers. MeshColliders do not need `Is Trigger`. The Inspector-editable interaction range is 30 units, and a 0.75-unit thin-occluder tolerance lets a picture remain selectable when its thick frame or the wall immediately behind it is the first physics hit. Solid geometry farther in front still blocks interaction. The bottom prompt appears only while the crosshair is over a configured exhibit.

`QuizTrigger` owns the Tugu Negara Monument quiz. It presents all 16 questions from the design document in order, with four answers in a two-by-two grid. Answer positions are shuffled independently for every question. The X button and Escape close the quiz at any time; the final page reports the score and offers a retake. Mouse input is enabled while the quiz is open and first-person controls resume when it closes.

`CreditPicture` opens the editable thank-you/credit panel directly inside the Gallery. It does not load the video scene. `CreditVideoPicture` remains the separate interaction for replaying the video.

The Level 3 completion route now plays the video first. Only after playback ends or the player completes the hold-Space skip does the screen fade black and show the editable thank-you message. It then loads the Gallery at `InitialSpawnPoint`.

## Inspector tuning

- Select `Gallery First Person Player` to edit movement speed, gravity, mouse sensitivity, interaction range, thin-occluder tolerance, credit scene names, and the two spawn references.
- Select `Gallery Gameplay` to edit all 16 quiz questions, answer choices, correct-answer indices, and the Gallery credit message.
- Select `Gallery Gameplay` to edit focus distance/duration, UI references, dialogue prompt pulse speed, and entry/exit fade durations.
- Select any named picture to edit its exhibit title and individual dialogue lines without changing code.

## Manual check

Enter the Gallery from the Main Menu and confirm the player starts at `InitialSpawnPoint`. Aim at every historical picture, press C, advance all sentences with Space, and confirm the camera returns to the same view. Watch the video from `CreditVideoPicture`, confirm there is no thank-you message, and verify the player returns at `VideoSpawnPoint`. Complete Level 3 separately and confirm its thank-you message appears after the video before returning at `InitialSpawnPoint`.
