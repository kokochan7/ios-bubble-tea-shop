# Bubble Tea Shop

A 3D top-down iOS idle/arcade game made in Unity 6.6. You walk around your shop, scoop up cash from the floor, and spend it right away to unlock new stations and serve more customers.

**Current goal:** turn the finished gray-box prototype into the full game, one milestone at a time. See [docs/ROADMAP.md](docs/ROADMAP.md).

## Prototype steps (done)

| Step | What you can do when it's done |
|------|-------------------------------|
| 1. Project skeleton | Open the project in Unity and see the folder layout |
| 2. Player and cash | Walk with a joystick or mouse, pick up cash piles, watch the money counter go up |
| 3. Unlock zones | Stand on a floor pad to pay for a station and watch it appear |
| 4. Make and sell drinks | Stations build drinks step by step, customers buy them and drop cash |

The milestones toward the full game are in [docs/ROADMAP.md](docs/ROADMAP.md). How the code is organized is explained in [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md).

## First-time setup (Windows)

You only do this once. It turns this repo into a full Unity project on your PC.

### 1. Get the repo onto your PC

1. Install [GitHub Desktop](https://desktop.github.com/) and sign in.
2. **File > Clone repository**, pick `kokochan7/ios-bubble-tea-shop`, and clone it to a short path such as `C:\Dev\ios-bubble-tea-shop`.

### 2. Create the Unity project files

Unity Hub won't create a project inside a folder that already has files, so you create a throwaway project and copy three folders across.

1. Open **Unity Hub > New project**.
2. Pick editor **6.6** and the **Universal 3D** template (the one that uses URP).
3. Name it `BubbleTeaTemp`, set the location to `C:\Dev`, and click **Create project**.
4. When the editor finishes loading, close it.
5. In File Explorer, copy the **Assets**, **Packages** and **ProjectSettings** folders from `C:\Dev\BubbleTeaTemp` into `C:\Dev\ios-bubble-tea-shop`. If Windows asks about merging the `Assets` folder, say yes. Our `Assets\_Game` folder stays as it is.
6. You can delete `C:\Dev\BubbleTeaTemp` afterwards.

### 3. Open it

1. **Unity Hub > Add > Add project from disk**, and choose `C:\Dev\ios-bubble-tea-shop`.
2. Open it with Unity 6.6. The first import takes a few minutes.
3. In the Project window you should see `Assets/_Game` with the folders listed below.

### 4. Use Cursor as your code editor

1. In Unity: **Edit > Preferences > External Tools > External Script Editor > Browse**, and pick `Cursor.exe` (usually in `C:\Users\<you>\AppData\Local\Programs\cursor`).
2. Click **Regenerate project files**.
3. In Cursor, install the **Unity** extension (it pulls in C# support). Double-clicking a script in Unity now opens it in Cursor.

### 5. Save the project files to GitHub

In GitHub Desktop you'll see a lot of new files (everything under `Packages` and `ProjectSettings`, plus the template's sample scene). Commit them with a message like "Add Unity project files" and click **Push origin**. Folders such as `Library` and `Temp` are ignored on purpose; Unity rebuilds them.

### Optional: preview at phone size

In the **Game** view, use the resolution dropdown to pick an iPhone size (or add a 1170x2532 portrait entry). Building to a real iPhone needs your Mac and Xcode; that comes after the prototype.

## Try the prototype

1. In Unity's menu bar, click **Bubble Tea > Build Prototype Scene**. It creates `Assets/_Game/Scenes/Prototype.unity` with everything wired up and opens it.
2. Press **Play**. A new game starts with a short tutorial banner near the bottom that walks you through your first sale.
3. Click and drag anywhere in the Game view to use the joystick (or use WASD / arrow keys). Walk up to the green cash stacks and watch the bills fly to you while the counter at the top goes up.
4. Stand on the white **Counter** pad. Your money drains into it, and when it's paid off the counter pops up and the next pad (Tea Brewer) appears. That one unlocks the Topping Bar.
5. Once the Counter is built, customers walk in and line up behind it. Stand in front of the **Tea Brewer** to pick up the milk teas it makes, then stand in front of the **Counter** to drop them off. Each customer takes a drink and pays into the cash pile beside the counter, which you collect like any other cash.
6. For a pricier drink, drop milk teas at the **Topping Bar** first. It turns them into boba milk tea, which sells for more.
7. Each customer shows the drink they want on a bubble above their head and waits for exactly that one. The Topping Bar leads to the **Taro Station** and then the **Fruit Bar**, which add taro, taro boba and mango drinks to the orders.
8. The Topping Bar also reveals a **Hire Runner** pad. A runner keeps the counter stocked for you, so you can focus on collecting cash. A second runner pad appears after the first.
9. After the Fruit Bar, **Expand Shop** opens the east wall onto a new area with a **Drive Thru** window. Cars queue there and pay 1.5x.
10. A yellow arrow always points to the cheapest pad you can buy next. The **MENU** button (top right) turns sound and vibration on or off and can reset your progress.
11. Buying the Counter also reveals the purple **Upgrades** desk. Pay for it, then stand in front of it to buy faster walking, bigger carry stacks, faster brewing and higher drink prices.
12. After the Drive Thru, **Open Matcha House** builds a bus stop to a second shop. Click **Bubble Tea > Build Second Shop Scene** once so it has somewhere to go.

Your progress (money and unlocked stations) is saved as you play, so pressing Play again carries on where you stopped. To start over, stop Play and click **Bubble Tea > Delete Save Data** (it deletes both shops' saves). If you rebuild the scene, delete the save too so the two match.

Your shop keeps earning for up to 2 hours while you're away. Come back after a minute or more and a **Welcome back!** pop-up pays you. To see it without waiting, sell drinks for a couple of minutes, then click **Bubble Tea > Test Away Pop-Up (1 hour)** during Play.

The piles refill every few seconds so there's always something to collect. You can tweak speeds and values by selecting objects in the Hierarchy and editing their fields in the Inspector. Rebuilding the scene from the menu resets changes made in the scene, but keeps the prices, recipes and timings you edited in `Assets/_Game/Data`. The gray-box prefabs are rebuilt each time.

For the bubble tea shop look, click **Bubble Tea > Art > Build Boba Art Kit** and rebuild the scene. Other models (free packs, Meshy) are covered in [docs/ART.md](docs/ART.md) and [docs/MESHY.md](docs/MESHY.md).

To play on your iPhone (needs your Mac), follow [docs/IOS_BUILD.md](docs/IOS_BUILD.md).

To add new content without code, right-click in the Project window and use the **Create > Bubble Tea** menu:
- **Item**: something that can be carried, like a cup of taro tea.
- **Recipe Step**: what a station turns into what, and how long it takes. Leave the input empty for a station that makes things from nothing.
- **Drink**: an item customers will buy, and its price. Add it to the counter's menu.
- **Buildable**: a station's name, unlock price and prefab, for use on an unlock pad.
- **Upgrade**: a stat it changes, its value at each level, and the price of each level. Add it to the UpgradeManager's list in the scene (and rebuild the scene to get a row for it in the panel).

## Folder layout

```
Assets/_Game/
  Editor/       Editor-only tools, such as the scene builder menu
  Art/          Models, textures, icons
  Data/         ScriptableObject assets (station costs, recipes, prices)
  Materials/    Materials, including gray-box colors
  Prefabs/      Player, cash pile, stations, customers
  Scenes/       Game scenes
  Scripts/
    Core/       Shared helpers and game-wide events
    Player/     Movement, joystick, carrying items
    Economy/    Wallet, cash piles, pickups
    Building/   Unlock pads and buildable stations
    Cooking/    Ingredients, recipes, drink-making stations
    Customers/  Customer spawning, queueing, paying
    Saving/     Saving and loading progress
    Upgrades/   Upgrade levels and the upgrade desk
    Staff/      Hired helpers (runners)
    Feedback/   Sounds, vibration, confetti, pop-ups, guide arrow
    UI/         HUD, money counter, joystick visuals
```

Everything we make lives under `Assets/_Game`, so it never gets mixed up with Unity's template files or store assets you import later.

## Working with Git

- `.gitignore` keeps Unity's generated folders out of the repo.
- Every file in `Assets` has a matching `.meta` file. Always commit them together, or Unity loses track of references.
- Each roadmap step arrives as a pull request. To try one, switch to its branch in GitHub Desktop, let Unity reimport, and press Play.
