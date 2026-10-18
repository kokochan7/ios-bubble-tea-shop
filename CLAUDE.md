# Bubble Tea Shop: notes for Claude

Unity 6.6 (URP, uGUI, Input System) top-down iOS idle/tycoon game. Kenny edits on Windows and builds iOS on a Mac; he is fairly new to Unity/C#, so explain any Unity menu steps plainly.

## Workflow
- No PRs. Run the compile check, then push straight to `main` (`git push origin HEAD:main`). Kenny clicks Pull origin in GitHub Desktop.
- Bump `ModelKit.BuildTag` (`Assets/_Game/Scripts/Extras/ModelKit.cs`, e.g. `"17 Oct K"` -> `"17 Oct L"`, new date resets to `A`) on every push. It shows in the PETS tab so Kenny can confirm he has the latest build.
- After pushing, tell Kenny any Unity steps: usually **Bubble Tea > Build All Shops**, sometimes **Delete Save Data**.

## Compile check
```
tools/compile-check/check.sh
```
Installs mono and downloads Unity 2021 reference DLLs + Roslyn on first run (cached in `~/.cache/unitycheck`), then compiles Scripts and Editor. uGUI, EventSystems, InputSystem and URP come from hand-written stubs in `tools/compile-check/Stubs.cs`; add a member there when the game really uses one that's missing. Passing here does not prove Unity 6.6 compiles it (see gotchas).

## Code map (`Assets/_Game/Scripts`, namespaces `BubbleTea.<Folder>`)
- **Core**: events, clock, settings, stats, small helpers (PopIn, FloatingText).
- **Player**: joystick, movement, camera.
- **Economy**: wallet, cash piles/bills, offline earnings.
- **Building**: unlock pads, buildables, decor, doors, move mode.
- **Cooking**: items, stacks/carrying, stations, recipes, supply truck/dock, river dinghy.
- **Customers**: spawner, customers, counter, order board, prices, VIPs, tour bus, rain.
- **Staff**: baristas, cashier, cleaner, stocker, payroll, sofa breaks.
- **Upgrades**: upgrade definitions and manager.
- **Extras**: code-built UI kit and HUD tabs (TEAM, PETS, decor, mini-games), `ModelKit` (runtime models), pets, team skills, collections.
- **Feedback**: sounds, music, haptics, confetti, character looks.
- **Goals / Rating / Saving / Travel / UI**: daily goals and records, shop rating, JSON save, shop-to-shop travel, panels and popups.
- `Assets/_Game/Editor`: `PrototypeSceneBuilder` builds every shop scene from code; `BobaArtKit`/`ArtLibrary` build art; `IosSetup`, `XcodeSandboxFix` for iOS builds.

## Conventions
- New levels: add a `ShopPlan` to `PrototypeSceneBuilder.AllShops` so Build All Shops includes it.
- Runtime models: use `ModelKit.Part(...)`, never `GameObject.CreatePrimitive` (iOS strips unused colliders, giving magenta/broken objects).
- Art is built in code (BobaArtKit), not imported assets. No tutorials or guide arrows.

## Unity 6.6 gotchas the compile check misses
- Fully qualify our own types in files with `using UnityEngine.UI;` and avoid generic class names (`UnityEngine.UI.SafeArea` exists in 6.6).
- `Scene.handle` is not an int: compare `Scene` structs.
- `Object.GetInstanceID()` is an error: use object references in sets.
