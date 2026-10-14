# Architecture

This is the plan for how the prototype's code fits together. It's written so a newcomer to Unity can follow it, and it will be updated as each step lands.

## Guiding ideas

1. **Data in ScriptableObjects, behavior in MonoBehaviours.** Prices, unlock costs, recipes and timings live in assets under `Assets/_Game/Data`. Adding a new drink or station should mean making a new asset in the Inspector, not writing new code.
2. **Small components that each do one job.** A station is a GameObject with a few components snapped together (for example "accepts items", "processes for N seconds", "outputs an item"), rather than one big class per station type.
3. **Modules talk through simple events.** The wallet doesn't know about the UI; it raises "money changed" and the money counter listens. That keeps modules swappable and easy to test in isolation.
4. **Mobile first.** Object pooling for things spawned often (cash, cups, customers), no per-frame allocations in hot paths, and touch input that also works with a mouse in the editor.

## Modules

| Module | Folder | Responsibility | Arrives in |
|--------|--------|----------------|-----------|
| Core | `Scripts/Core` | Small shared helpers such as the pop-in animation (pooling lives inside each system for now) | Step 3 |
| Player | `Scripts/Player` | Joystick input, movement, camera | Step 2 |
| Economy | `Scripts/Economy` | Wallet (current money), cash piles, cash flying to the player | Step 2 |
| UI | `Scripts/UI` | Money counter, joystick visuals, unlock pad labels | Steps 2-3 |
| Building | `Scripts/Building` | Unlock pads that drain money and reveal stations | Step 3 |
| Cooking | `Scripts/Cooking` | Items, recipe steps, drinks, item stacks (including the one the player carries), stations | Step 4 |
| Customers | `Scripts/Customers` | Spawning, queueing at the counter, buying drinks, dropping cash | Step 4 |
| Upgrades | `Scripts/Upgrades` | Upgrade levels, buying them, and the zone at the upgrade desk | Milestone 2 |
| Saving | `Scripts/Saving` | Writing progress to a JSON file and putting it back on start | Milestone 1 |

Dependencies only point one way: Player, Economy, Building, Cooking and Customers may use Core, Upgrades uses Economy (to pay), UI and Saving may read from any of them, but Core never depends on gameplay code.

## The core loop as data flow

```
Customer pays  ->  Cash pile on floor  ->  Player walks over it  ->  Wallet goes up
       ^                                                                  |
       |                                                                  v
Drink handed over  <-  Stations build the drink  <-  Unlock pad spends money on a station
```

## Cooking and crafting model (step 4)

The cooking system is built from three kinds of data and one kind of station.

- **ItemDefinition** (ScriptableObject): anything that can be carried or placed, such as a cup, black tea, milk tea, or tapioca pearls. Holds a name, icon and the prefab used to show it.
- **RecipeStep** (ScriptableObject): "take these inputs, wait this long, produce this output". Example: *Cup + Black Tea -> Tea Cup, 1.5 s*.
- **DrinkRecipe** (ScriptableObject): the finished drink a customer orders, its sale price, and the item that represents it. Example: *Brown Sugar Boba, $12*.
- **Station** (MonoBehaviour): has an input slot, an output slot and a list of RecipeSteps it can perform. When its inputs match a step, it runs the timer and produces the output. A tea brewer, milk dispenser and topping bar are all the same component with different RecipeSteps assigned.

Items move between **ItemStacks** (the player's hands, a station's input and output, the counter). A station's **HandoffZone** is the square in front of it: while the player stands there, the station takes any item it can use and hands back finished ones.

The **ServingCounter** (Customers module) accepts any drink on its menu. **Customers** queue behind it, take a drink, pay its price into the counter's cash pile, and walk out.

Because stations only care about "do my inputs match a step I know", new drinks and new station types are added entirely in the Inspector. Upgrades later (faster brewing, more output slots, higher prices) become extra fields on the same assets.

## Saving (milestone 1)

`SaveManager` is the only script that knows about the save file. Gameplay scripts just expose their state and a way to put it back (`Wallet.SetMoney`, `UnlockPad.Restore`), and raise the events they already had; the save manager listens and decides when to write. Each pad is found in the file by its **Save Id**, so renaming GameObjects never breaks a save. The file has a `version` number so later milestones can add fields and upgrade old saves instead of wiping them.

## Upgrades (milestone 2)

Upgrades never reach into other modules. `UpgradeManager` writes numbers into `Core/Stats` (walking speed multiplier, extra carry slots, brew speed multiplier, price multiplier), and the code that cares reads them: `PlayerMovement`, `ItemCarrier`, `Station` and `ServingCounter`. A new kind of upgrade means a new `StatId` plus one line where it's used; a new level or price is just an edit to the asset.

## Conventions

- Namespace: `BubbleTea.<Module>`, for example `BubbleTea.Economy`.
- Serialized fields are `[SerializeField] private` with a short tooltip, so the Inspector explains itself.
- One public class per file, file named after the class.
- Gray-box first: primitives and flat colors until the loop is fun.
