# Putting the game on your iPhone

You make the game on your Windows PC, but Apple only lets a Mac turn it into an iPhone app. This guide is done once on the Mac; after that, each new version is steps 4 to 7 again.

## What you need

- Your Mac with **Xcode** installed (free from the Mac App Store; it's big, so start it early).
- **Unity Hub** on the Mac with **Unity 6.6** and the **iOS Build Support** module. In Unity Hub: Installs > the gear next to 6.6 > Add modules > tick iOS Build Support.
- Your iPhone and a USB cable.
- A free **Apple ID**. That's enough to run the game on your own phone. TestFlight (sharing with friends) and the App Store need the paid Apple Developer Program ($99 a year), which we'll set up at milestone 10.

## 1. Get the project onto the Mac

Install GitHub Desktop (or use `git` in Terminal) and clone `kokochan7/ios-bubble-tea-shop`. Open the folder in Unity Hub (Add > Add project from disk) with Unity 6.6.

## 2. Apply the iPhone settings (once)

In Unity, click **Bubble Tea > Apply iPhone Settings**. That sets the app name (Bubble Tea Shop), the bundle ID (`com.kokochan7.bubbleteashop`), portrait only, iOS 15 or newer, and automatic signing. Commit and push the changed `ProjectSettings` files so the PC has them too.

## 3. Switch to iOS

**File > Build Profiles**, pick **iOS**, click **Switch Platform** (this takes a few minutes the first time). Make sure `Prototype` is in the scene list; if not, open it and click **Add Open Scenes**.

## 4. Build the Xcode project

Still in Build Profiles, click **Build** and choose a new folder outside the project, for example `~/Builds/BubbleTea`. Unity writes an Xcode project there.

## 5. Sign it in Xcode

1. Open `Unity-iPhone.xcodeproj` from that folder.
2. Xcode > Settings > Accounts > **+** and sign in with your Apple ID.
3. Click **Unity-iPhone** at the top of the left sidebar, then the **Unity-iPhone** target, then **Signing & Capabilities**.
4. Tick **Automatically manage signing** and pick your name (Personal Team) as the **Team**.
5. If Xcode says the bundle ID is taken, change it to something unique like `com.yourname.bubbleteashop`.

## 6. Get the iPhone ready

1. Plug in the iPhone and tap **Trust** on the phone.
2. On the phone: Settings > Privacy & Security > **Developer Mode** > on, then restart when asked. (The option appears after the phone has been plugged into Xcode once.)

## 7. Run it

Pick your iPhone in the device menu at the top of Xcode and press the **Play** (▶) button. The first time, the phone may refuse to open the app: go to Settings > General > VPN & Device Management, tap your Apple ID, and **Trust** it. Then open Bubble Tea Shop.

With a free Apple ID the app stops opening after 7 days; just run it from Xcode again.

## What to check on the phone

- The money counter, MENU and upgrade buttons sit below the notch or Dynamic Island, not under it.
- The joystick follows your thumb anywhere on the lower screen.
- It feels smooth (the game asks for 60 frames per second) and the screen doesn't dim while you play.
- Vibration works on unlocks and upgrades, and Settings can turn it off.
- Close the app from the app switcher and reopen it: your money and stations are still there.

## When something goes wrong

- **"No signing certificate" or "Failed to register bundle identifier":** change the bundle ID in step 5.
- **Build fails in Unity with "iOS module not loaded":** iOS Build Support isn't installed for 6.6 (see What you need).
- **App is very dark or pink:** a model's material isn't URP yet; see [ART.md](ART.md).
- Anything else: copy the first red error from Unity's Console or Xcode's Issue navigator into the project thread.
