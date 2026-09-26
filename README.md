# Isombe: A Kitchen Puzzle Room

A first-person, single-room puzzle game made in **Unity 6 (6000.5, URP)**.

Dinner guests are waiting in the dining room. You are locked in a Rwandan kitchen with six minutes to cook **isombe**, a dish of pounded cassava leaves stewed with eggplant and onion. Read the kitchen, solve five cooking steps in order, and the dining room door opens. Take too long and the isombe burns.

> 🎥 **Gameplay video:** _add link here_

---

## The kitchen

A furnished, closed kitchen built from the **Free Kitchen** pack, with a window, and a door to a dining room where the guests' table is already laid. Each puzzle has its own station:

| Station | Where | Used in |
|---|---|---|
| Fridge | right wall, with a "shopping list" on the door | Gather (two ingredients hide inside) |
| Spice shelf, kitchen table | above the sink; centre of the room | Gather (oil; eggplant and the banana decoy) |
| Prep counter | back wall, beside the stove | Gather (four spots) and Cook (four prep bowls) |
| Mortar | kitchen table | Pound |
| Stove | back wall: gas knob, red-ringed igniter, pot on the burner | Stove and Cook |
| Serving table | by the dining room door | Serve |

---

## How to play

| Action | Input |
|---|---|
| Move | `W` `A` `S` `D` |
| Look | Mouse |
| Interact / pick up / place / drop | `E` or **Left click** |
| Restart after a win or loss | **Restart** button |

The game opens on an intro card with the story and controls. Press **E** to start the clock.

Aim the centre crosshair at an object. It grows and turns gold when the object is usable, the object glows, and a short prompt tells you the action (for example *Pick up* or *Turn knob*). Prompts never tell you what to do next.

---

## The puzzle chain (5 layers)

Each layer only responds when it is the **current** layer, so the puzzle has to be solved in order. `PuzzleManager` tracks every layer. The HUD shows **`Puzzle Progress: X / 5`**, a row of progress dots, and a one-line goal for the current step (for example *"Fire up the stove"*). The goal says what the step is about, never how to solve it.

**Where to go next** is shown without text: the spotlight above the current station brightens and slowly pulses, while the others dim.

**Every step feeds the next.** The four prep bowls start empty and grey. Each one fills with its ingredient's colour when that ingredient reaches the counter. The cassava-leaf bowl only fills once the leaves have been pounded. Those filled bowls are what you cook with.

| # | Layer | What you do | Clue (non-text) | Getting it wrong |
|---|---|---|---|---|
| 1 | **Gather** | Search the kitchen (two ingredients are in the fridge) and carry the four real ones to the prep counter. Each fills the bowl behind its spot | "Shopping list" on the fridge door shows pictures of the four ingredients; the banana is not one of them. Coloured rings mark the four counter spots | The decoy is rejected with a buzz |
| 2 | **Pound** | The gathered leaves appear in the mortar. Pound them *on the beat* six times | A drum beat plays only during this layer. The leaves darken with each good hit | Missing the beat resets the count |
| 3 | **Light the stove** | Turn the gas knob until the burner shows the right colour, then press the knob ringed in red (the igniter) | Blue flame sign under the hood. The burner flame and its light change colour with every turn | Igniting on the wrong colour costs **15 s** |
| 4 | **Cook** | Carry the four filled bowls to the pot, one at a time, in the right order | Strip on the tiles above the bowls: four coloured dots joined by arrows, in the bowls' colours | Smoke, every bowl goes back to its place, and it costs **20 s** |
| 5 | **Serve** | The finished dish appears beside the stove; carry it to the tray on the serving table | The serving table's light comes on and the goal line changes | — |

Solving layer 5 opens the dining room door with a cheer. Walking through the doorway wins the game.

### Clue system
All clues are diegetic, meaning they exist in the room itself: pictures, colours, a light that changes colour, a rhythm, and object placement. No clue uses text to give away a solution. Each clue board is tied to its layer by the `LayerClue` component. A board stays faint before its layer, gently pulses while its layer is active, and fades once that layer is solved. The room itself shows the player where to look next.

---

## Win and lose

- **Win:** all five layers are completed, the door unlocks and swings open, and the player walks out into the dining room. A *"Dinner is served!"* panel fades in with a cheer.
- **Lose:** the six-minute timer reaches zero and *"The isombe burned!"*. Wrong answers in layers 3 and 4 take time off the clock, which shakes and flashes red. In the last minute the clock turns red and pulses.
- Both end screens pause the game, free the cursor and offer **Restart**, which reloads the room from scratch.

---

## Technical overview

All gameplay code is in `Assets/Scripts`, written for this project:

| Folder | Script | Responsibility |
|---|---|---|
| Core | `PuzzleManager` | Layer order, progress UI, win/lose, restart. Raises a `LayerCompleted` event |
| Core | `GameTimer` | Countdown lose condition, time penalties, warning pulse |
| Core | `Door`, `ExitTrigger` | Door animation; **trigger collider** that wins the game |
| Core | `OpenOnUse` | Reusable one-shot "open this door" interactable |
| Core | `DebugSkip` | Editor-only test keys (compiled out of release builds) |
| Interaction | `Interactable` | Abstract base for everything the player can use |
| Interaction | `Pickup`, `PlacementZone`, `IDropTarget` | Carry, place and return objects; **trigger zones** that accept only the right items |
| Interaction | `HoverHighlight` | Glow on the object under the crosshair |
| Player | `PlayerController`, `PlayerInteractor` | First-person movement; raycast interaction, prompts, crosshair feedback |
| Puzzles | `MortarPuzzle` | Rhythm puzzle (timing window, hit counter, reset on a miss) |
| Puzzles | `StoveKnob`, `Igniter` | Colour selection and ignition with a penalty |
| Puzzles | `SequencePuzzle` | The pot: ordered drop target with reset and penalty |
| Puzzles | `PrepBowl` | Bowl that fills when its ingredient is gathered or pounded (listens to events) |
| Puzzles | `RevealOnLayer` | Keeps the finished dish hidden until the cooking layer is solved |
| UI | `LayerClue`, `ProgressPips`, `StationLight` | Layer-aware clue animation; HUD progress dots; station spotlights that point the way |
| Editor | `KitchenLayout`, `KitchenSetup` | One-click scene build and wiring (see below) |

Where the brief's technical requirements show up in the code:
- **Colliders and triggers:** `ExitTrigger` (`OnTriggerEnter`), `PlacementZone` trigger volumes, and a raycast against interactable colliders.
- **Conditional logic:** `bool` gates such as `IsActive` and `OnCorrectColour`, counters (`hits`, `step`, `Completed`) and resets.
- **Event-based interaction:** each puzzle exposes a `UnityEvent onSolved`. `PuzzleManager` raises a C# `LayerCompleted` event and `PlacementZone` raises `ItemPlaced`. The prep bowls listen to both.

### Scene setup (run once)
Open `Assets/Scenes/Kitchen.unity`, then choose **Tools ▸ Isombe Kitchen ▸ Apply Scene Setup**. The tool is safe to run again. It:
1. makes *Kitchen* the build scene,
2. builds the room from the Free Kitchen pack (with collision), closes it with Kenney walls, a window and a door, and adds the dining room,
3. places every station on real surfaces (raycast), using the stove's own knobs for the knob and igniter,
4. turns the bowls into prep bowls, the pot into a drop zone and the fridge door into an openable door,
5. gives interactables clear action prompts,
6. hooks up the crosshair, progress dots, goal line and intro card,
7. makes the station lights guide the player,
8. builds the clue boards (fridge door, stove, above the bowls).

---

## Project structure

```
Assets/
├── Art/UI/          Clue and HUD sprites (flame, dot, ring, arrow, card)
├── Kitchen/         Free Kitchen asset pack (Boxx-Games), URP materials
├── Audio/           Sound effects for every puzzle event
├── Materials/       Project materials (bowls, flame, walls, floor, outline)
├── Prefabs/         Ingredient pickups (onion, oil, eggplant, leaves, banana)
├── Scenes/          Kitchen.unity (the game)
├── Scripts/         Core · Interaction · Player · Puzzles · UI · Editor
└── ThirdParty/      Imported asset packs, unchanged, one folder per source
```

---

## Asset sources

| Asset | Source | License |
|---|---|---|
| Free Kitchen (room, cabinets, stove, hood, fridge, tables, chairs, pot, tableware, lamps) | [Boxx-Games on the Unity Asset Store](https://assetstore.unity.com/publishers/60462) | Unity Asset Store EULA |
| Food Kit (ingredients, prep bowls, mortar, stew pan, tray, clue pictures) | [Kenney – Food Kit](https://kenney.nl/assets/food-kit) | CC0 |
| Building Kit (window wall, doorway, door, dining room wall) | [Kenney – Building Kit](https://kenney.nl/assets/building-kit) | CC0 |
| Sound effects (`Assets/Audio`) | _TODO: add link(s)_ | _TODO_ |
| UI clue sprites (`Assets/Art/UI`) | Made for this project | — |
