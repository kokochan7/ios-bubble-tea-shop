# Bubble Tea Shop

A 3D top-down iOS idle/arcade game made in Unity 6.6. You walk around your shop, scoop up cash from the floor, and spend it right away to unlock new stations and serve more customers.

**Current goal:** a playable gray-box prototype of the core loop: walk, collect cash, unlock stations, make drinks, sell drinks.

## Roadmap

| Step | What you can do when it's done |
|------|-------------------------------|
| 1. Project skeleton | Open the project in Unity and see the folder layout (this step) |
| 2. Player and cash | Walk with a joystick or mouse, pick up cash piles, watch the money counter go up |
| 3. Unlock zones | Stand on a floor pad to pay for a station and watch it appear |
| 4. Make and sell drinks | Stations build drinks step by step, customers buy them and drop cash |

How the code is organized is explained in [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md).

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
2. Press **Play**.
3. Click and drag anywhere in the Game view to use the joystick (or use WASD / arrow keys). Walk up to the green cash stacks and watch the bills fly to you while the counter at the top goes up.

The piles refill every few seconds so there's always something to collect. You can tweak speeds and values by selecting objects in the Hierarchy and editing their fields in the Inspector. Rebuilding the scene from the menu resets those changes.

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
    UI/         HUD, money counter, joystick visuals
```

Everything we make lives under `Assets/_Game`, so it never gets mixed up with Unity's template files or store assets you import later.

## Working with Git

- `.gitignore` keeps Unity's generated folders out of the repo.
- Every file in `Assets` has a matching `.meta` file. Always commit them together, or Unity loses track of references.
- Each roadmap step arrives as a pull request. To try one, switch to its branch in GitHub Desktop, let Unity reimport, and press Play.
