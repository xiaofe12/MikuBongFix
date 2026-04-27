# Changelog

## 1.1.0
- Changed backpack hiding behavior to match the implementation used by Miku_three-physic.
- The replacement model now follows the original renderer hide rule instead of forcing local backpack visuals off.

## 1.0.5
- Added a master enable/disable switch for the mod.
- Removed `Main / Keep Miku Visible` and `Main / Safe Pickup And Backpack Materials` from config UI; both safeguards are now always enabled internally.
- Fixed the pickup-time _Tint material error by restoring Item.mainRenderer and Item.addtlRenderers to the original hidden BingBong renderers by default.
- Patched BackpackOnBackVisuals and ItemCooking so Miku replacement renderers are excluded from `_Tint` access during pickup, backpack, and cooking flows.
- Kept the replacement visibility guard so the Miku model does not disappear again after the renderer reference fix.
- Added Mod Settings compatible config entries for replacement enable toggle and scale multipliers.

## 1.0.2
- Initial stable release.
- Fixed model visibility issues related to replacement material transparency.
- Updated the package metadata for the current BepInEx PEAK pack.
