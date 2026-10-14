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
| Staff | `Scripts/Staff` | Hired helpers that move items between stations and the counter | Milestone 4 |
| Feedback | `Scripts/Feedback` | Sounds, vibration, confetti, sale pop-ups | Milestone 6 |
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

The **ServingCounter** (Customers module) holds the menu. Each **Customer** picks an order from the menu when they arrive (only drinks some station in the shop can make), shows it in a bubble, queues, takes exactly that drink, pays its price into the counter's cash pile, and walks out. The counter only accepts a drink from the player while someone in line still needs one.

Because stations only care about "do my inputs match a step I know", new drinks and new station types are added entirely in the Inspector. Upgrades later (faster brewing, more output slots, higher prices) become extra fields on the same assets.

## Saving (milestone 1)

`SaveManager` is the only script that knows about the save file. Gameplay scripts just expose their state and a way to put it back (`Wallet.SetMoney`, `UnlockPad.Restore`), and raise the events they already had; the save manager listens and decides when to write. Each pad is found in the file by its **Save Id**, so renaming GameObjects never breaks a save. The file has a `version` number so later milestones can add fields and upgrade old saves instead of wiping them.

## Upgrades (milestone 2)

Upgrades never reach into other modules. `UpgradeManager` writes numbers into `Core/Stats` (walking speed multiplier, extra carry slots, brew speed multiplier, price multiplier), and the code that cares reads them: `PlayerMovement`, `ItemCarrier`, `Station` and `ServingCounter`. A new kind of upgrade means a new `StatId` plus one line where it's used; a new level or price is just an edit to the asset.

## Staff (milestone 4)

A `Barista` is just another `ItemCarrier`, so stations and the counter hand items to it through the same floor zones they use with the player. Two properties on `ItemCarrier` keep workers tidy: `GiveOnlyTo` (only drop items at this station or counter) and `TakeFilter` (only pick up this item). The counter tells workers what the line needs (`NextNeeded`) and keeps reservations so two baristas don't fetch the same order. With nothing to carry, a barista stands at the busiest station and speeds it up. Baristas, the cashier and the cleaner are hired with ordinary unlock pads whose buildable prefab is the worker, so saving and loading them needed no new code.

## Shop expansion (milestone 5)

Unlock pads can show and hide scene objects when they're bought (`showWhenUnlocked`, `hideWhenUnlocked`), which is how Expand Shop opens the wall and reveals the new floor; because it happens in the same code path as loading, a saved expansion comes back open. Each `ServingCounter` has a `lane` name and a price multiplier, and each `CustomerSpawner` sends customers to the counter with its lane, so the drive-through is just a second counter prefab plus a car-shaped customer prefab.

## Game feel (milestone 6)

Gameplay scripts announce moments through `Core/GameEvents` (cash collected, pad paid, unlocked, drink sold, upgrade bought) and never play sounds or effects themselves. The Feedback module listens and decides what each moment looks, sounds and feels like, so polish can change without touching gameplay. Player preferences live in `Core/GameSettings` (PlayerPrefs), separate from the save file.

## Art (milestone 7)

Art is applied only by the scene builder, never at runtime. `ArtSet` (an editor-only asset) maps slot names (`TeaBrewer`, `Customer`, `Cup`...) to models, and `ArtLibrary.Attach` puts a model inside a prefab, scaled to the gray box's footprint or height and standing on the floor. The gray-box parts stay as invisible colliders, so swapping art can never change gameplay. `Core/WalkBob` gives characters a procedural waddle until real animations arrive. `Editor/BobaArtKit` generates the default pastel models as prefabs from primitives and a tapered-cylinder mesh, with one shared material per palette color. Cup models may name parts `Drink` and `Pearls`; the scene builder recolors those per drink.

## iPhone (milestone 8)

`Core/AppSetup` runs before the first scene loads and sets 60 fps and no screen sleep. All HUD panels except the joystick live under a `SafeArea` object (`UI/SafeArea`) that shrinks to `Screen.safeArea`. `Editor/IosSetup` writes the iOS Player Settings; the Mac build steps are in [IOS_BUILD.md](IOS_BUILD.md).

## Offline earnings (milestone 9)

`Economy/OfflineEarnings` listens to `GameEvents.DrinkSold` and keeps a smoothed dollars-per-minute rate, measured one minute of play at a time. The save file (version 3) stores that rate and the time it was written; on load, `SaveManager` hands the time away to `OfflineEarnings.Grant`, which also handles the app returning from the background. The reward waits in `Pending` until `UI/WelcomeBackPopup`'s Collect button adds it to the wallet, and it is saved as part of the money so quitting before collecting loses nothing.

## Baristas (milestone 11)

`Staff/Barista` scores each `Station` by the drinks waiting customers ordered (`ServingCounter.QueuedOrders`): 2 per drink it makes, 1 per drink made from its output. They stand behind the best unclaimed station and call `Station.AddBarista`, which speeds it up by `baristaSpeedBonus` per barista.

## Seating (milestone 11)

`Customers/SeatingTable` holds seats and a mess object per seat. After buying, a `Customer` may reserve a clean seat (`TryReserve`), sit, then `Leave`, which pays into the area's `CashPile` and shows the mess. Anyone in the table's `HandoffZone` cleans one mess at a time. `GameEvents.MessChanged` lets `ShopRating` take stars off for messy seats.

## Customer moods (milestone 11)

`Customer` tracks how long it has waited against `patienceSeconds` and shows it on a mood light with a MaterialPropertyBlock. `ServingCounter.TrySell` takes a tip share from the customer and raises `GameEvents.Tipped`; a customer out of patience raises `GameEvents.CustomerLeftUpset` and leaves. `PickOrder` gives a new customer any drink left on the counter by someone who walked out.

## Daily goals (milestone 11)

`Goals/DailyGoals` listens to `GameEvents` (`DrinkServed`, `DrinkSold`, `Tipped`, `UpgradeBought`) and advances today's `GoalState`s. Goals are rolled from a seed made from the date and sized by how many drinks the shop can make. `SaveManager` saves them (save version 4) and calls `EnsureToday` after loading. `UI/GoalsPanel` shows them.

## Shop rating (milestone 11)

`Rating/ShopRating` blends a service score (moved by `GameEvents.CustomerServed` moods and `CustomerLeftUpset`) with the share of rating points bought on unlock pads, and writes `StatId.CustomerRate` and `StatId.ExtraCustomers`, which `CustomerSpawner` reads. The service score is saved (version 5). `UI/RatingView` draws the stars from a sprite generated in code. Decorations are ordinary buildables whose prefabs are solid gray boxes with art slots (`Planter`, `MenuBoard`, `Statue`).

## Second shop (milestone 11)

Each shop is its own scene, built by the same `PrototypeSceneBuilder` from a `ShopPlan` (scene path, save file, floor and wall materials). `ShopContentBuilder.Build(billPrefab, shop)` swaps in that shop's menu and puts its data and prefabs in a `Shop2` subfolder with costs scaled up, so the shops never overwrite each other. `Travel/ShopTravel` saves the current shop, clears `Stats` (the next shop applies its own upgrades and rating) and loads the other scene; `SaveManager.fileName` keeps each shop's progress in its own file. The first shop must stay scene 0 in the build.

## Kenny's playtest changes (milestone 10)

- The tutorial and the guide arrow were removed; players find the next pad themselves.
- `Staff/Cashier` stands by a counter and sends bills from its `ServingCounter.CashPile` straight to the player's `Wallet`.
- `ServingCounter` takes any menu drink (wanted ones first) and keeps extras for later customers; `PickOrder` already makes new customers order a waiting drink.
- Baristas, the cashier and the cleaner read `StatId.MoveSpeed`, and baristas also `StatId.CarryBonus` (several of the same item per trip).

## Conventions

- Namespace: `BubbleTea.<Module>`, for example `BubbleTea.Economy`.
- Serialized fields are `[SerializeField] private` with a short tooltip, so the Inspector explains itself.
- One public class per file, file named after the class.
- Gray-box first: primitives and flat colors until the loop is fun.
