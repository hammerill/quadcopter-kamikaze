# Quadcopter Kamikaze

A companion mod for [Quadcopter Redux](https://www.gta5-mods.com/scripts/quadcopter-redux) that adds a kamikaze bomb feature to your FPV drone in GTA5.

## Features

- **Bomb prop** visually attached to the drone (selectable: bomb, C4, thermite, sticky bomb)
- **Weight simulation** — configurable downward force makes the drone heavier and harder to fly
- **Impact detonation** — triggers when collision velocity delta exceeds a configurable threshold
- **Manual detonation** — keyboard key (`J`), gamepad button, or FPV controller axis via DirectInput
- **Explosion cutscene** — camera pulls back to show the blast, HUD hidden, controls locked
- **Auto-respawn** — after the cutscene the drone reappears 200m above the explosion, bomb re-attached
- **In-game menu** (`F11`) to arm/disarm the bomb and tweak all parameters live

## Prerequisites

Your GTA5 install must already have:
- [ScriptHookV](http://www.dev-c.com/gtav/scripthookv/) (`dinput8.dll` + `ScriptHookV.dll` in GTA5 root)
- [ScriptHookVDotNet](https://github.com/scripthookvdotnet/scripthookvdotnet) (`ScriptHookVDotNet.asi` + `ScriptHookVDotNet3.dll` in GTA5 root)
- [Quadcopter Redux](https://www.gta5-mods.com/scripts/quadcopter-redux) installed and working

## Building (WSL2)

### 1. Install .NET SDK

```bash
wget https://dot.net/v1/dotnet-install.sh -O /tmp/dotnet-install.sh
chmod +x /tmp/dotnet-install.sh
/tmp/dotnet-install.sh --channel 8.0 --install-dir $HOME/.dotnet
```

Add to your shell profile (`~/.bashrc` or `~/.zshrc`):

```bash
export PATH="$HOME/.dotnet:$PATH"
```

Then reload:

```bash
source ~/.bashrc  # or source ~/.zshrc
```

Verify:

```bash
dotnet --version
```

### 2. Build

```bash
cd src
dotnet build
```

The output goes to `scripts/QuadcopterKamikaze/`.

## Installing

Copy the `scripts/QuadcopterKamikaze/` folder into your GTA5 `scripts/` directory. Your GTA5 install should look like:

```
Grand Theft Auto V/
    scripts/
        Quadcopter/              <-- existing Quadcopter Redux
            GTA5-Quadcopter.dll
            ...
        QuadcopterKamikaze/      <-- this mod
            QuadcopterKamikaze.dll
            QuadcopterKamikaze.ini
            LemonUI.SHVDN3.dll
            SharpDX.dll
            SharpDX.DirectInput.dll
```

You do **not** need to copy `ScriptHookVDotNet3.dll` — it's already in your GTA5 root from Quadcopter Redux.

## Usage

1. Launch GTA5
2. Start the drone with Quadcopter Redux (default: `G`)
3. Press `F11` to open the Kamikaze menu
4. Check **Arm Bomb** — a bomb prop attaches to the drone
5. Fly into a target or press `J` to detonate manually
6. Watch the explosion cutscene, then continue flying from above

### Assigning the BOOM button to your FPV controller

1. Plug in your FPV controller (ELRS dongle, etc.)
2. Open the Kamikaze menu (`F11`)
3. Select **Assign BOOM Axis**
4. Flip the switch on your radio you want to use as the BOOM trigger
5. Press **Enter** to confirm — the axis and threshold are auto-detected and saved to the INI

## Configuration

Edit `scripts/QuadcopterKamikaze/QuadcopterKamikaze.ini`:

```ini
[Keys]
MenuKey=F11          # Key to open/close the menu
ExplodeKey=J         # Key to manually detonate

[Controller]
ExplodeButton=47     # GTA Control ID for manual detonation (-1 to disable)

[Bomb]
ImpactForceThreshold=10   # Velocity delta (m/s) to trigger on impact (1-20)
ExplosionDamageScale=5    # Explosion damage multiplier (1-20)
CutsceneDuration=5        # Cutscene camera seconds (1-15)
BombMassFactor=8          # Extra downward force (1-20)

[Camera]
CameraDistance=15          # Cutscene camera distance in meters (1-30)

[DirectInput]
JoystickIndex=0            # FPV controller device index (0 = first joystick)
BoomAxis=-1                # Axis index (-1 = not assigned, use in-game menu to auto-detect)
BoomAxisThreshold=0.8      # Axis value that triggers detonation (0.0-1.0)
```

All of these can also be changed live through the in-game menu.
