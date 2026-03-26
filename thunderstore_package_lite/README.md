# MikuBongFix

[![Thunderstore](https://img.shields.io/badge/Thunderstore-Download-blue)](https://thunderstore.io/c/peak/p/xiaofe12/MikuBongFix/)
[![GitHub](https://img.shields.io/badge/GitHub-Repository-black)](https://github.com/xiaofe12/MikuBongFix)

Replaces BingBong with Miku plushie model in PEAK game.

## Features

- **Model Replacement**: Replaces BingBong item with cute Miku plushie model
- **Custom Icon**: Custom Miku icon for inventory display
- **Custom Audio**: Custom audio responses when interacting
- **Full Compatibility**: Works in both single-player and multiplayer modes

## Requirements

- [BepInExPack PEAK](https://thunderstore.io/c/peak/p/BepInEx/BepInExPack_PEAK/) v5.4.75301 or later

## Installation

### Via Thunderstore Mod Manager
1. Install Thunderstore Mod Manager
2. Search for "MikuBongFix"
3. Click Install

### Manual Installation
1. Install [BepInExPack PEAK](https://thunderstore.io/c/peak/p/BepInEx/BepInExPack_PEAK/)
2. Download the latest release from [Thunderstore](https://thunderstore.io/c/peak/p/xiaofe12/MikuBongFix/) or [GitHub Releases](https://github.com/xiaofe12/MikuBongFix/releases)
3. Extract the contents to `PEAK/BepInEx/plugins/MikuBongFix/`
4. Launch the game

## File Structure

```
BepInEx/plugins/MikuBongFix/
├── plugins/
│   ├── MikuBongFix.dll    # Main plugin
│   ├── mikupeak           # Miku model asset bundle
│   ├── response_0.wav     # Audio response
│   ├── response_1.wav     # Audio response
│   ├── response_2.wav     # Audio response
│   └── response_3.wav     # Audio response
├── manifest.json
├── README.md
└── icon.png
```

## Configuration

No configuration required. The mod works automatically after installation.

## Known Issues

- None at this time. Please report any issues on [GitHub Issues](https://github.com/xiaofe12/MikuBongFix/issues).

## Changelog

### 1.0.0
- Initial stable release
- Fixed model visibility issues (material transparency fix)
- Added MikuDeformGuard to prevent model deformation
- Improved renderer handling and material application
- Updated dependencies to BepInExPack_PEAK

### 0.1.1
- Bug fixes and improvements

### 0.1.0
- Initial beta release

## Credits

- Miku model: [MikuFumo](https://github.com/FelineEntity/MikuFumo)
- Original mod concept: [FelineEntity](https://github.com/FelineEntity)

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Support

If you encounter any issues or have suggestions:
- Open an issue on [GitHub](https://github.com/xiaofe12/MikuBongFix/issues)
- Leave a comment on [Thunderstore](https://thunderstore.io/c/peak/p/xiaofe12/MikuBongFix/)
