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
- Runners carry one item at a time (more with Carry More since milestone 10), never drop things at stations they just walk past, and don't double up on the same order. An item nobody needs gets thrown away after a few seconds.
- Hired runners come back when you load your save, like stations do.
- A cashier came later (milestone 10) to collect the counter's cash for you.

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

## Milestone 11: More features

Kenny picked these, built one pull request at a time in this order:

1. **Customer moods and tips** (done): a light beside each order turns from green to yellow to red as the customer waits. Serve them in the green and they tip half the drink's price, in the yellow a fifth. If it runs out, they say "Too slow!" and leave without buying. Patience drains slower for people further back in line. All the numbers are at the top of `Scripts/Customers/Customer.cs` (the prefabs are rebuilt from those defaults).
2. **Daily goals** (done): a pink GOALS button under MENU opens three goals for today, like "Sell 20 Taro Milk Tea", "Get 8 tips" or "Earn $500 from sales". Each shows its progress and a CLAIM button with its cash reward, a "!" appears on GOALS when one is ready, and a banner pops up when you finish one. A claimed goal is replaced by a new one straight away (it used to reset at midnight), and they grow as you unlock more drinks.
3. **Shop rating** (done): five stars under your money, from 1 to 5. Half comes from service (happy customers raise it, walkouts lower it) and half from building the shop up, including three new decorations to buy: a Planter, a Menu Board and a Boba Statue (they appeared after the Topping Bar; their pads are left out for now). More stars bring customers up to twice as often and let two more line up. Tap the stars for a reminder of how it works.
4. **Barista staff** (done): two Hire Barista pads appear after the Taro Station. A barista walks to the station your customers need most, stands behind it, and makes it work twice as fast. Every few seconds they check whether another station is busier and move if so. Only one barista works at each station.
5. **Seating area** (done): a Seating Area pad ($500) appears after the Topping Bar and builds two café tables with four seats. After buying, many customers sit for a few seconds and leave a few extra dollars in the pile by the tables, plus an empty cup. A seat with a cup on it can't be used until you walk up to the table to clean it ("Clean!"), and every messy seat costs a quarter star until you do.
6. **Second shop** (done): the Boba Shop is level 1. Once every pad in it is built, an **Open Matcha House** pad ($2,500) builds a bus stop. Stand on it and the ring fills, then you travel to the Matcha House: a green-walled shop with its own money, pads and upgrades, selling matcha, brown sugar and strawberry drinks at about twice the price (and everything costs three times as much). A bus stop there takes you back. Both shops keep their own progress, and whichever one you open pays you for the time you were away. The main menu's CONTINUE reopens the shop you were last in.
7. **First-minute tutorial** (built, then removed after playtesting along with the guide arrow: Kenny prefers players to find their own way).

## Milestone 10: Fixes and polish

After Kenny's first playtest of features 1-7:

- The tutorial and the yellow guide arrow are gone.
- **Hire Cashier** ($150, appears after the Tea Brewer): stands by the counter and banks customers' payments for you, so you don't have to walk to the cash pile.
- The counter now takes any drink on the menu you carry to it and keeps up to 8 for later customers, who order a waiting drink first.
- Walk Speed now speeds up runners, baristas and the cashier too, and Carry More lets runners carry several of the same drink per trip. Brew Speed already sped up every station.

After the second playtest:

- Runners now keep the counter stocked whenever nobody is waiting, bringing the ready drink it has fewest of, instead of standing still between orders.
- The cashier walks to a spot behind the counter (the right half, next to the cash pile) and faces the customers. The drop-off spot moved to the left half.
- The decoration pads (Planter, Menu Board, Boba Statue) are gone for now; the Seating Area now appears after the Topping Bar.
- The Matcha House (level 2) pad appears only once everything in the Boba Shop is built.

After the third playtest:

- **GM Tools** in the MENU (Editor and Development builds only): +$10,000, Build Everything, Instant Build and Max Upgrades.
- Runners and baristas are now one role, the **Barista**: they keep the counter stocked, and when there's nothing to carry they stand at the busiest station and help it brew faster. Up to five can be hired ($300, $600, $900, $1300, $1800).
- The cashier banks payments much faster.
- The Seating Area has four tables, and **More Seating** ($1200, after Expand Shop) adds four more in the new area.
- **Hire Cleaner** ($400, after the Seating Area): wipes the tables customers leave messy and banks the tips left on them.
- There is one floor cash spot instead of three, and it stops refilling after handing out $500 (saved, so it doesn't start over).

After the fourth playtest:

- When the counter is full, a drink someone in line wants goes straight to that customer (from you or a barista), so a counter full of the wrong drinks no longer blocks the line.
- Drinks on the counter that nobody in line wants are thrown out one by one, every 5 seconds.
- A **Trash** bin by the counter: stand next to it to throw away what you carry, one item at a time. Baristas take unneeded items there too.
- The first **Hire Barista** pad costs $200 and is there from the start.
- Customers wait about 30 seconds instead of 60. Every customer who walks out costs a star (earned back a little with each customer served); at 0 stars the shop closes and the game starts over with fresh saves. Customers only start coming once a station can make a drink, so you can't lose before the Tea Brewer is built.

Polish pass:

- **Hire Drive-Thru Cashier** ($400, appears with the Drive Thru) works the drive-thru window like the main cashier.
- Tiled floors, wooden planks in the expansion, striped wallpaper, a tall back wall with windows, trim, wall lamps, bunting and the shop's name on a sign, a rug under the café tables, door mats, and a street with trees outside. All built in code (`Editor/ShopDressing.cs`).
- Sharper picture: full render resolution on phones (was 80%), 4x anti-aliasing, crisper soft shadows, warmer lighting.
- Background music for each shop, composed in code (`Editor/MusicMaker.cs`): a bouncy pop tune in the Boba Shop and a mellow lo-fi one in the Matcha House. MENU has a Music on/off button. **Bubble Tea > Audio > Rebuild Music** rewrites the tracks.

Real-café layout:

- The dining room is at the front: customers come in through the front door, line up at the counter, and sit at café tables beside the line.
- The counter runs across the middle with waist-high glass walls on either side; you walk between the rooms through the staff door to the right of the counter.
- The kitchen is behind the counter: the bars stand in a row facing it, the baristas' pads line the back wall, and the upgrade desk, trash can and starting cash are back there too.
- Expand Shop adds more kitchen and dining space to the east (More Seating is in the new dining space). The drive-thru window is in the east wall, and cars drive up a lane outside it, stop at the window and drive off the far end.

After the fifth playtest:

- **Main menu**: the game opens on a title screen with PLAY (CONTINUE once you have a save) and SETTINGS (sound, music, vibration). MENU in a shop has a **Main Menu** button that saves and goes back. Build it once with **Bubble Tea > Build Main Menu Scene**; it goes first in the build.
- **Shops grow with the level**: the Boba Shop is now a small 22 x 20 m café (the expansion and drive-thru lane are smaller too); the Matcha House keeps the full 30 x 30 m. Each shop's positions live in a `ShopLayout` in `Editor/PrototypeSceneBuilder.cs`.
- **Game clock**: top left, running 2 game minutes per real second from 7:00 on day 1. Breakfast (7-9), lunch (11:30-1:30) and dinner (5:30-7:30) rushes bring customers 2-2.5x as often with longer lines; late at night it's quiet. The time is saved.
- **Drink prices**: tap a counter (the main one or the drive-thru window) to raise or lower each drink's price from 50% to 300%. Each customer has their own limit (100-180% of the normal price); above it they say "Too pricey!", leave and cost a star. Prices are saved per shop.
- **Goals** no longer reset at midnight: claiming a goal replaces it with a new one straight away.
- Drinks nobody wants leave the counter every 2 seconds. Customers wait 10 seconds, and each one served wins back a tenth of a star.
- Baristas carry 1 drink at first, plus 1 per Carry More level.

After the sixth playtest:

- Smoother progression: the Counter reveals only the Tea Brewer; the Tea Brewer reveals the first Hire Barista, the Upgrades desk and the Topping Bar; the Topping Bar reveals the Taro Station, Seating Area and Hire Cashier.
- Customers wait 20 seconds.
- The Hire Cleaner pad sits right beside the café tables (it still appears once the Seating Area is built).
- Swing doors on the staff entrance open as you or staff walk through (`Scripts/Building/SwingDoor.cs`).
- You carry 1 drink at first, like baristas, plus 1 per Carry More level.

After the seventh playtest:

- Play in the Editor starts at the main menu, like the game on a phone (`Editor/PlayFromMenu.cs`; untick **Bubble Tea > Play From Main Menu** to start in the open scene).
- The rug and its pink border under the first seating are gone, and both seating areas sit in one even row.
- Customers and staff are men or women at random, with random shirt, hair and skin colors (`Scripts/Feedback/CharacterLook.cs`, on the Boba Art Kit characters).
- The order above a customer is a bigger drink with no white plate.
- Everything built from a pad shows its name above it ("Counter", "Tea Brewer", "Barista"...), and staff carry theirs around (`Scripts/Building/NameTag.cs`).

Cozy art pass (in the style of Capybarista):

- Every box in the Boba Art Kit has softly rounded edges (`RoundedBox` in `Editor/BobaArtKit.cs`).
- Counters and stations are honey-wood cabinets with carved door panels, a lighter worktop and a band in each station's color. Café tables are round and wooden, with bentwood chairs.
- Dark plank floors (honey oak in the Matcha House), wood-board walls with thick dark caps, warm cream kitchen tiles, wooden shelves of mugs and jars on the back wall, wood window frames, a cobblestone sidewalk and deeper green grass.
- Warm golden lighting plus a cozy color grade: soft glow, gentle contrast, warmer colors and a light vignette (`Editor/CozyLook.cs`, saved as `Assets/_Game/Settings/CozyLook.asset`).

Taiwanese tea house pass and playtest fixes:

- Red and gold theme: red paper lanterns along the back wall and by the front door, a red sign with gold lettering, a noren curtain over the door, lattice windows, potted bamboo, and shelves of red tea tins and celadon cups (`Editor/ShopDressing.cs`).
- Staff (baristas, cashiers, cleaners) wear a uniform: red shirt, cream apron with a gold badge and a red cap. The player wears the red shirt too.
- Drinks stand side by side on the bars and counter (`ItemStack` columns and rows) at 1.5x size; baristas and the player carry them on a red tray.
- Bars hold 3 finished drinks to start; the new **Bigger Bars** upgrade adds 1 per level (`StatId.StationCapacity`).
- The topping bar pad appears after hiring the first barista. Customers wait 30 seconds.
- Money: bigger bills ($5 at the counter, $10 on the floor), a quieter tick at most about 8 times a second, and a cash register "ka-ching" on every sale.
- Smaller HUD: money at the top center, clock and stars top-left, MENU and GOALS top-right.

Orders pass:

- Customers only order drinks the shop can make from start to finish (`Station.AnyCanMake` follows the whole recipe chain).
- Customers order 1 to 5 drinks. The cap is the number of drinks the shop can make, so orders grow as stations unlock; small orders stay most common. Each extra drink adds 40% patience. They take each drink as it lands and leave with the last.
- An order board over each counter shows what the customer at the front still wants; drinks vanish from it as they're handed over (`Customers/OrderBoard`).
- Drink prices are 15% higher (rounded up). Hire Barista costs $150, Hire Barista 2 $500.
- Bars and counters are 1.25x bigger, and drinks on them are 2x size.

Matcha House pass:

- The Matcha House is a Japanese-style matcha café with its own bars and menu: Whisk Bar (Matcha Latte), Hojicha Bar (Hojicha Latte), Foam Bar (Matcha / Hojicha Cream Top), Strawberry Bar (Strawberry Matcha, poured over strawberry milk) and a new fifth bar, the Yuzu Bar (Yuzu Matcha Fizz). White paper lanterns, a green noren, matcha tins, and staff (and the player) in green uniforms.
- Its upgrades go to Lv 5/5. The Drink Price upgrade is gone. 3 barista pads in the Boba Shop, 4 in the Matcha House.
- Too-pricey walkouts cost 0.1 star and show a 5-second banner suggesting lower prices; customers accept prices up to 1.4x to 2.6x the normal price.
- The Upgrades menu pauses the game; its X closes it until you step off the desk and back on.
- Art: boba pearls sit inside a clear cup; the Tea Brewer is one large stainless steel urn.

Tighter shops pass:

- Customers order up to 3 drinks in the Boba Shop and 4 in the Matcha House (`ServingCounter.maxDrinksPerCustomer`).
- Offline earnings: 12% of the normal earning speed, for at most 1.5 hours away.
- Every drink is the same size (2x) on bars, the counter, trays, order bubbles and the order board. Trays are bigger to fit; green in the Matcha House.
- Baristas reserve the drink they're working on (fetching it or feeding its ingredient), and drinks being made from dropped-off ingredients count too, so two never do the same job. They no longer stock the counter with unordered drinks, and anything no customer wants goes straight in the trash.
- Both shops are more compact: the Boba Shop is 18 x 15 m (was 22 x 20), the Matcha House 24 x 22 m (was 30 x 30), with bars, desk and pads closer together.
- New upbeat, catchy music for both shops (faster, with a repeating melody hook).

Back wall and hip hop pass:

- Both shops now share one 18 x 15 m floor plan (`PrototypeSceneBuilder.CompactLayout`). Every bar stands in a row against the back wall (the Matcha House's Yuzu Bar at the east end), with a row of staff pads (cashier, baristas) in front of them; no pads overlap.
- The staff door moved to the east end of the glass wall and the café tables sit east of the queue, so nothing blocks the door. The Expand Shop pad is in the dining room's south-east corner.
- The upgrade desk stands free in the kitchen's west corner, and its zone surrounds the whole desk, so you can use it from any side.
- Music: laid-back hip hop beats for both shops (boom-bap drums, hi-hat rolls, 808 bass, electric-piano chords, a plucked pentatonic hook). New file names, so the tracks regenerate.
- Brighter scenes (stronger sun and ambient light, lighter floors, walls and grass) and Chinese decor: a gate with a jade-tiled roof and red couplet banners over the front door, a red runner up the queue, a string of lanterns across the dining room, a potted plum tree, paper-cut flowers on the windows, and plum trees in blossom outside.

Tap to open pass:

- **Tappable objects:** tap the Counter (or drive-thru window) to set drink prices, and tap the Upgrade Desk to open the upgrade menu (`UI/PricePanel` and `UI/UpgradePanel` listen to `VirtualJoystick.Tapped`; the desk is found by its `UpgradeZone` marker). Standing next to the desk no longer opens it.
- The red carpet is gone; the To Matcha House bus stop now stands on the left of the dining room, like level 2's To Boba Shop stop.
- Cash Pile 1 (the starting floor money) sits in the kitchen just left of the counter, with a "$on the pile/$left to collect" label (like $30/$500) that disappears once all $500 is collected.
- A sale makes no sound of its own anymore (the ka-ching and the ding were both removed); collecting money still ticks.
