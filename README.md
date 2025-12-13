# Repo Player Map

A BepInEx plugin for **R.E.P.O.** that displays all players on the map as
colored circles with player initials.

Distributed via **Thunderstore Mod Manager**.

---

## Features (WIP)

- Shows all connected players on a map overlay
- Player dots are color-coded
- Displays player initials
- Works in multiplayer lobbies
- Lightweight IMGUI overlay (no UI assets required)

---

## Installation (Players)

1. Install **Thunderstore Mod Manager**
2. Select **R.E.P.O.**
3. Install **Repo Player Map**
4. Launch the game via Thunderstore

---

## Installation (Developers)

### Requirements
- Windows
- .NET SDK 8+
- R.E.P.O. installed via Steam
- BepInExPack for R.E.P.O. installed (via Thunderstore)

### Environment Variables (recommended)

Set these once on your machine:

```powershell
setx REPO_DIR "D:\SteamLibrary\steamapps\common\REPO"
setx TMM_PLUGINS_DIR "C:\Users\YOUR_NAME\AppData\Roaming\Thunderstore Mod Manager\DataFolder\REPO\profiles\YOUR_PROFILE\BepInEx\plugins"
```

Restart your terminal after running setx.

### Build

```
dotnet build -c Release
```

The DLL will automatically be copied to:
- your Thunderstore profile (if TMM_PLUGINS_DIR is set)
- the local Thunderstore packaging folder

### Development Notes

- Built against Unity 2022.3.x
- Uses BepInEx 5.4.x
- Uses IMGUI (OnGUI) for the overlay
- Multiplayer detection via Photon (WIP)


### Roadmap


-[ ] Detect real player positions

-[ ] Map-only toggle

-[ ] Configurable scale & opacity

-[ ] Icon shapes (circle / arrow)

-[ ] Optional minimap integration