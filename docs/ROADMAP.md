# Roadmap to the full game

The prototype (steps 1-4) proved the core loop: walk, grab cash, unlock stations, make drinks, sell them. These milestones turn it into a game you can put on the App Store. Each one arrives as one or more pull requests, gray-box first, with steps to try it in Unity.

The order is a suggestion: anything can be moved, merged or dropped.

| # | Milestone | What you can do when it's done |
|---|-----------|--------------------------------|
| 1 | **Save and load** | Quit and reopen the game and your money and unlocked stations are still there |
| 2 | **Upgrades** | Spend money to walk faster, carry more cups, brew faster and sell drinks for more |
| 3 | **More drinks and stations** | Customers order specific drinks (taro, fruit tea, boba) shown in a bubble; new stations make them |
| 4 | **Staff** | Hire a cashier and a runner who serve customers and carry drinks for you |
| 5 | **Shop expansion** | Unlock a second room or a drive-through window, with more customers and bigger prices |
| 6 | **Guidance and game feel** | An arrow points to the next thing to do, plus sounds, haptics, particles and a settings menu |
| 7 | **Real art** | Low-poly models, characters with walk animations, and a cute UI instead of gray boxes |
| 8 | **iPhone build** | The game runs smoothly on your iPhone through Xcode and TestFlight |
| 9 | **Offline earnings** | Come back to money earned while away |
| 10 | **Fixes and polish** | Everything found while playtesting on PC and iPhone is fixed |
| 11 | **More features** | New things to do (picked by Kenny, see the list below) |
| 12 | **Rewarded ads** | Optionally watch an ad to double offline money |
| 13 | **App Store launch** | Icon, screenshots, privacy details, and submission |

### Feature ideas for milestone 11

Pick any, in any order, or add your own:

- **Customer moods and tips:** customers lose patience in a long queue; fast service earns a tip and a happy face.
- **Daily goals:** three small tasks a day ("Sell 20 taro teas") that pay a bonus.
- **Shop rating:** stars that go up as you serve and decorate, bringing more customers.
- **More staff:** a barista who runs a station for you, and a cleaner for tables.
- **Seating area:** tables where customers sit and leave mess to clean up.
- **Second shop:** once the first is finished, open a new location with new drinks.
- **Seasonal drinks:** limited-time recipes that pay extra.
- **First-minute tutorial:** step-by-step hints for brand-new players.
- **Big-number money:** show $1.2K and $3.4M instead of long numbers.

## Milestone 1: Save and load

- A `SaveManager` in the scene writes your money and each unlock pad's progress to a small JSON file.
- It saves right away when a station unlocks, every few seconds while your money changes, and when the app goes to the background (on iPhone that can be the last moment before the app is closed).
- Loading puts unlocked stations straight back (no flying money) and keeps half-paid pads half paid.
- **Bubble Tea > Delete Save Data** starts you over; **Bubble Tea > Show Save File** opens the folder it lives in.

Not saved on purpose: cash lying on the floor, drinks on stations, and customers in line. They refill within seconds of starting.

## Milestone 2: Upgrades

- Buying the Counter reveals a second pad: the purple **Upgrades** desk ($40).
- Stand in front of the desk and a panel opens with four upgrades, each with 4 levels: **Walk Speed**, **Carry More**, **Brew Speed** and **Drink Price**. Buttons grey out when you can't afford them.
- Upgrades are data: each is an asset in `Assets/_Game/Data/Upgrades` with a list of values and prices you can edit, and new ones are made with **Create > Bubble Tea > Upgrade**.
- Upgrade levels are saved with the rest of your progress.

## Milestone 3: More drinks and orders

- Each customer now wants one specific drink, shown as a little cup on a white bubble above their head. They only take that drink.
- The counter only accepts drinks someone in line is waiting for, so it never clogs up with drinks nobody buys.
- Customers only order drinks your shop can make, so new drinks show up in orders as you unlock their stations.
- New drinks: **Taro Milk Tea** ($12), **Taro Boba** ($20, taro tea through the Topping Bar) and **Mango Fruit Tea** ($14).
- New stations on the pad chain after the Topping Bar: **Taro Station** ($250), then **Fruit Bar** ($400).

## Milestone 4: Staff

- Buying the Topping Bar reveals a **Hire Runner** pad ($300) on the left, and that one reveals **Hire Runner 2** ($600).
- A runner (green) keeps the counter stocked: they fetch the drink the line needs from the station where it's ready. If it isn't ready anywhere, they carry the ingredient to the station that makes it, like plain milk tea to the Topping Bar for a boba order.
- Runners carry one item at a time, never drop things at stations they just walk past, and don't double up on the same order. An item nobody needs gets thrown away after a few seconds.
- Hired runners come back when you load your save, like stations do.
- There's no separate cashier: customers already pay at the counter on their own, so the only manual job left is collecting the cash.

## Milestone 5: Shop expansion

- The shop now has low walls, so nobody can walk off the edge.
- After the Fruit Bar, an **Expand Shop** pad ($1,000) appears by the east wall. Paying it removes the wall and reveals a new 16 x 30 m area.
- In the new area, a **Drive Thru** pad ($600) builds a blue drive-through window. Cars line up, order like customers, and pay **1.5x** the menu price.
- Each counter has a **lane** (Main, DriveThru) and its own customer spawner, so more counters can be added later without code. Runners serve whichever counter has someone waiting.

## Milestone 6: Guidance and game feel

- **Guide arrow:** a bouncing yellow marker hovers over the cheapest pad you haven't bought yet, and a yellow arrow at your feet points toward it.
- **Sounds:** a tick for every bill (rising in pitch as you grab a stack), ticks while paying a pad, a chime on unlock, a ding on each sale, and a chime for upgrades. They're simple generated tones for now; real sound files can be dropped into the **SoundEffects** component later.
- **Confetti** bursts when a station unlocks, and a green **+$15** floats up from the counter on every sale.
- **Vibration** on unlocks and upgrades (iPhone only).
- **Settings:** a MENU button in the top-right corner with Sound on/off, Vibration on/off, and Reset Progress (tap twice).

## Milestone 7: Real art

- Every station, character, car and cup has an **art slot** in `Assets/_Game/Data/ArtSet.asset`. Filled slots replace the gray box when the scene is rebuilt, and empty ones keep it.
- Models are sized to fit automatically, and each slot can be turned, scaled or nudged.
- **Bubble Tea > Art > Auto-Fill From Art Folder** finds matching models by name. It's tuned for Kenney's free kits (Furniture, Food, Mini Characters, Car).
- Characters waddle when they walk, and plants decorate the corners if you add some.
- Step-by-step download guide: [ART.md](ART.md).
- **Boba Art Kit** (added later): **Bubble Tea > Art > Build Boba Art Kit** fills every slot with code-built pastel bubble tea shop models and recolors the floor and walls. Hero pieces can be upgraded with Meshy models: [MESHY.md](MESHY.md).

## Milestone 8: iPhone build

- **Bubble Tea > Apply iPhone Settings** sets the app name, bundle ID, portrait only, iOS 15+ and automatic signing in one click.
- The money counter, MENU and upgrade buttons stay inside the iPhone's safe area, clear of the notch and home bar.
- The game runs at 60 frames per second and the screen doesn't sleep while you play.
- Step-by-step Mac and Xcode guide: [IOS_BUILD.md](IOS_BUILD.md). TestFlight waits for the paid Apple Developer account at milestone 10.

## Milestone 9: Offline earnings

- While you play, the game measures how much your shop earns per minute from drink sales (runners and upgrades count too).
- When you come back, a **Welcome back!** pop-up pays half that speed for the time you were gone, up to 2 hours. Tap **COLLECT** to add it to your money. It shows on a fresh launch and when the app returns from the background on iPhone.
- The share (50%), the 2-hour cap and the 1-minute minimum are fields on the **OfflineEarnings** object.
- **Bubble Tea > Test Away Pop-Up (1 hour)** shows the pop-up during Play without waiting.
- Still to come: an optional rewarded ad to double the amount. It needs an ad network account (such as Unity LevelPlay) first, so it is a separate step.
