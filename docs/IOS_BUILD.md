# Putting the game on your iPhone

You make the game on your Windows PC, but only a Mac can turn it into an iPhone app. Steps 1 to 6 are done once. After that, each new version is just "Every new version" at the bottom.

The project is already set up for iPhone: the app name (Bubble Tea Shop), the bundle ID (`com.kokochan7.bubbleteashop`), portrait only, iOS 15 or newer, automatic signing, and the scene list (Main Menu, Boba Shop, Matcha House). You don't need to change any Player Settings.

## What you need

- Your **Mac**, updated to the latest macOS it can run.
- **Xcode 26 or newer** (free in the Mac App Store). An iPhone 17 runs iOS 26, and older Xcode versions can't install apps on it. Xcode is big, so start the download first.
- Your **iPhone 17** and its USB-C cable.
- Your normal **Apple ID**. A free Apple ID is enough to run the game on your own phone. TestFlight and the App Store need the paid Apple Developer Program ($99 a year), which is for later.

## 1. On the PC: make sure everything is pushed

1. In Unity, click **Bubble Tea > Build Second Shop Scene**, then **Bubble Tea > Build Prototype Scene**.
2. In GitHub Desktop, commit all changed files and click **Push origin**. The Mac gets the game from GitHub, so anything not pushed won't be in the app.

## 2. On the Mac: install Unity and get the project

1. Install **Unity Hub**, then install **Unity 6.6** (the exact same version as on your PC). When it asks for modules, tick **iOS Build Support**. If 6.6 is already installed: Installs > the gear next to 6.6 > Add modules > iOS Build Support.
2. Install **GitHub Desktop**, sign in, and clone `kokochan7/ios-bubble-tea-shop`.
3. In Unity Hub: **Add > Add project from disk**, pick the cloned folder, and open it with Unity 6.6. The first open takes a while.

## 3. Switch the project to iPhone

1. **File > Build Profiles**, click **iOS** on the left, then **Switch Platform**. The first switch takes several minutes.
2. Check the scene list at the top: **MainMenu** first, then **Prototype** and **Shop2**. If anything else is there, click **Bubble Tea > Apply iPhone Settings** and it tidies the list.

## 4. Build the Xcode project

1. Still in Build Profiles, click **Build**.
2. Make a new folder **outside** the project, for example `Builds/BubbleTea` in your home folder, and choose it.
3. When Unity finishes, that folder holds an Xcode project.

## 5. Sign it in Xcode (once)

1. Open **Unity-iPhone.xcodeproj** from the build folder.
2. Go to **Xcode > Settings > Accounts**, click **+**, choose Apple ID and sign in.
3. In the left sidebar, click **Unity-iPhone** at the very top. Then click the **Unity-iPhone** target and open the **Signing & Capabilities** tab.
4. Tick **Automatically manage signing** and set **Team** to your name (Personal Team).
5. If Xcode says the bundle identifier isn't available, change it to something unique like `com.kenny.bubbleteashop`.

## 6. Get the iPhone ready (once)

1. Plug the iPhone into the Mac, unlock it, and tap **Trust This Computer**.
2. In Xcode, open **Window > Devices and Simulators** and wait until the iPhone shows up and finishes "Preparing" (this can take a few minutes the first time).
3. On the iPhone, go to **Settings > Privacy & Security > Developer Mode**, turn it on, and restart when it asks. After the restart, tap **Turn On**. The Developer Mode option only appears after the phone has been connected to Xcode.

## 7. Run it

1. In Xcode's top bar, pick your iPhone as the run destination.
2. Press the **Play button (▶)**. The first build takes a few minutes.
3. The first time, the iPhone won't open the app yet. On the phone, go to **Settings > General > VPN & Device Management**, tap your Apple ID under Developer App, and tap **Trust**. Then open **Bubble Tea Shop** from the home screen.

With a free Apple ID, the app stops opening after 7 days. Just press ▶ in Xcode again to renew it.

## Every new version

1. On the PC, commit and push.
2. On the Mac, open GitHub Desktop and click **Pull origin**, then open the project in Unity.
3. Build Profiles > **Build**, choose the same folder, and pick **Replace** when asked.
4. In Xcode, press ▶. Your signing settings are kept.

## What to check on the phone

- The money counter and buttons sit below the Dynamic Island, not under it.
- The joystick follows your thumb.
- It feels smooth and the screen doesn't dim while you play.
- Tapping the Counter opens prices, and tapping the Upgrade Desk opens upgrades.
- Close the app from the app switcher and reopen it: your money and stations are still there.

## When something goes wrong

- **Xcode build fails with "Sandbox: ... deny file-write-create" or "deny file-read-data":** Xcode's User Script Sandboxing is blocking Unity's IL2CPP step. The project turns it off by itself on every iOS build, so pull the latest version and build from Unity again (choose Replace). To fix an existing Xcode project by hand: click Unity-iPhone at the top of the left sidebar, open Build Settings, search "sandboxing", and set **User Script Sandboxing** to **No** for every target (Unity-iPhone, UnityFramework, GameAssembly). Then Product > Clean Build Folder and press ▶.
- **"iOS module not loaded" in Unity:** iOS Build Support isn't installed for 6.6 (see step 2).
- **The iPhone isn't in Xcode's device list, or "iOS 26 is not supported":** update Xcode, and make sure Developer Mode is on (step 6).
- **"No signing certificate" or "Failed to register bundle identifier":** pick your Team, or change the bundle ID (step 5).
- **"Untrusted Developer" on the phone:** trust your Apple ID (step 7.3).
- **App is very dark or pink:** a model's material isn't URP yet; see [ART.md](ART.md).
- Anything else: copy the first red error from Unity's Console or Xcode's Issue navigator into the project thread.
