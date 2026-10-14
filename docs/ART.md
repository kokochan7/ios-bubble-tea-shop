# Adding real art

The game is built so 3D models can replace the gray boxes without touching code. Every station, character and cup has an **art slot**. Fill a slot with a model and rebuild the scene; leave it empty and that thing stays a gray box. Collisions and gameplay don't change, because the invisible boxes stay underneath.

## The quick way: Boba Art Kit

Click **Bubble Tea > Art > Build Boba Art Kit**, then **Bubble Tea > Build Prototype Scene**. It builds a matching set of pastel bubble tea shop models in code (tea urns, topping tubs, a cup sealer, chibi baristas, boba cups, plants, a menu board and a giant boba cup statue), puts them in every slot, and recolors the floor and walls. The models land in `Assets/_Game/Art/BobaKit`.

To upgrade single pieces with more detailed AI-made models, see [MESHY.md](MESHY.md). The Kenney steps below still work for any slot too.

## 1. Download the free packs

All of these are by [Kenney](https://kenney.nl). They're free and public domain (CC0), so you can ship them in a paid App Store game without credit, though a "Thanks Kenney" in your credits is nice. On each page, click **Download** (no account needed), then unzip.

| Pack | Page | Used for |
|------|------|----------|
| Furniture Kit | kenney.nl/assets/furniture-kit | Counter, tea brewer (coffee machine), topping bar (blender), taro station (microwave), fruit bar (fridge), upgrade desk, potted plants |
| Food Kit | kenney.nl/assets/food-kit | Drink cups |
| Mini Characters | kenney.nl/assets/mini-characters | You, customers and staff |
| Car Kit | kenney.nl/assets/car-kit | Drive-through cars |

## 2. Copy them into the project

1. In each unzipped pack, find the folder **Models > FBX format** (some packs call it `Models/FBX`).
2. Copy that whole folder, including any `Textures` folder next to the models, into the project at `Assets/_Game/Art/Kenney/<Pack name>`, for example `Assets/_Game/Art/Kenney/FurnitureKit`.
3. Switch back to Unity and wait for it to import.

If models show up **pink**, their materials use the old built-in shader. Select them and use **Edit > Rendering > Materials > Convert Selected Built-in Materials to URP**.

## 3. Fill the art slots

1. Click **Bubble Tea > Art > Auto-Fill From Art Folder**. It searches `Assets/_Game/Art` for models with matching names, such as `kitchenCoffeeMachine` for the tea brewer or `sedan` for cars, and fills every empty slot it can. The Console lists what it picked.
2. It then selects the **ArtSet** asset (`Assets/_Game/Data/ArtSet.asset`). Look through the slots in the Inspector and drag in a different model for any you don't like, or for any it couldn't fill.
3. Click **Bubble Tea > Build Prototype Scene** and press Play.

## 4. Fix models that sit wrong

Each slot has three small fixes:

- **Rotation Y:** stations should face the spot where you stand (the +Z side). If one faces backwards, type `180`. For sideways, try `90` or `-90`.
- **Size Multiplier:** models are scaled to match the gray box automatically. Use `1.2` to make one 20% bigger.
- **Offset:** nudge the model in meters, for example `0, 0.1, 0` to lift it.

Rebuild the scene after changing them.

## Notes

- Drinks keep their color: with a cup model, each drink shows a colored lid (with dark pearls for boba), so you can still tell orders apart.
- Characters get a little hop-and-rock waddle while walking, standing in for real walk animations. Real animations can come later.
- Commit the `Assets/_Game/Art` folder with its `.meta` files so the Art Set keeps its references.
- Any other models work too, from the Unity Asset Store, Quaternius, Poly Pizza and so on. Drop them in the Art folder and drag them into the slots by hand.
