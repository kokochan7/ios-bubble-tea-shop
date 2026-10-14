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
| 9 | **Offline earnings and rewards** | Come back to money earned while away; optional rewarded ads to double it |
| 10 | **App Store launch** | Icon, screenshots, privacy details, and submission |

## Milestone 1: Save and load

- A `SaveManager` in the scene writes your money and each unlock pad's progress to a small JSON file.
- It saves right away when a station unlocks, every few seconds while your money changes, and when the app goes to the background (on iPhone that can be the last moment before the app is closed).
- Loading puts unlocked stations straight back (no flying money) and keeps half-paid pads half paid.
- **Bubble Tea > Delete Save Data** starts you over; **Bubble Tea > Show Save File** opens the folder it lives in.

Not saved on purpose: cash lying on the floor, drinks on stations, and customers in line. They refill within seconds of starting.
