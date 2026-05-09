# FNAF Music Box - FFXIV Dalamud Plugin

A Dalamud overlay plugin that recreates the Five Nights at Freddy's 2 music box winding minigame experience within Final Fantasy XIV.

## Features

- **Interactive Overlay**: Draggable, resizable overlay window with a circular countdown meter and wind-up button
- **Dynamic Audio System**: 
  - Looping music while the meter has charge
  - Fail state audio when the meter depletes
  - Wind-up sound effect while holding the button
- **Visual Failstate**: Screen-filling overlay image that progressively fades in as the meter depletes (visible from 50% onwards)
- **Pause Control**: Pause/resume the meter and audio from the settings menu
- **Customizable Settings**: Adjust opacity, scale, wind speed, decay speed, and volume
- **Persistent Configuration**: All settings are saved and restored between sessions

## Installation

1. Download the latest release of `FNAFMusicBoxFFXIV.dll`
2. Place the DLL and accompanying assets (`musicbox.mp3`, `failstate.mp3`, `windup.mp3`, `windupmusicbutton.png`, `fail.png`) in your Dalamud plugins folder
3. Reload plugins or restart the game
4. Use `/musicbox` command to toggle the overlay

## Usage

### Commands
- `/musicbox` - Toggle overlay visibility
- `/musicbox show` - Show overlay
- `/musicbox hide` - Hide overlay
- `/musicbox lock` - Lock/unlock overlay position
- `/musicbox reset` - Reset meter to empty
- `/musicbox config` - Open settings window

### Controls
- **Wind Button**: Click and hold the wind-up button to charge the meter
- **Right-Click**: Right-click the button to open settings/pause menu

### Settings (Right-Click Menu)
- **Lock Position**: Prevent accidental movement of the overlay
- **Pause**: Pause the meter decay and all audio
- **Opacity**: Adjust overlay transparency
- **Wind Speed**: How quickly the meter charges (10-80)
- **Decay Speed**: How quickly the meter depletes (4-30)
- **Volume**: Master volume for all audio (0-100%)

## Gameplay Loop

1. Plugin starts with meter fully wound (100%)
2. The meter begins decaying as the fail state audio plays
3. At 50% depletion, a visual overlay begins fading in
4. Click and hold the wind button to charge the meter back up
5. Stop winding before reaching 100% to maintain meter charge
6. If meter reaches 0%, fail state is fully visible and audio plays
7. Use pause to temporarily stop everything

## Audio Assets

The plugin requires three audio files:
- `musicbox.mp3` - Looping music (plays while meter > 0%)
- `failstate.mp3` - Fail state sound (plays while meter at 0%)
- `windup.mp3` - Wind-up sound (plays while holding button)

And two image assets:
- `windupmusicbutton.png` - The wind-up button image
- `fail.png` - Full-screen failstate overlay image (scales to 16:9)

## Requirements

- FFXIV with Dalamud launcher
- XIVLauncher
- .NET 10.0 (or newer)

## Configuration

Settings are automatically saved to your Dalamud configuration folder and restored on startup.

Default values:
- Wind Speed: 38 units/second
- Decay Speed: 12 units/second
- Opacity: 100%
- Volume: 100%
- Scale: 1.0x

## Building from Source

```bash
dotnet build "FNAFMusicBoxFFXIV.csproj"
```

Output will be in `dist/FNAFMusicBoxFFXIV.dll`

## Author

Created for FFXIV Dalamud by ScSyn

## License

This project is provided as-is for personal use with FFXIV.

## Disclaimer

This plugin is not affiliated with Square Enix, Bandai Namco Entertainment, or Scott Cawthon. Five Nights at Freddy's is a trademark of Scott Cawthon. Use at your own risk.
