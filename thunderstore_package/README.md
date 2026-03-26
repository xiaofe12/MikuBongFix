# MikuBongFix

将 PEAK 里的 BingBong 替换为 Miku 外观，并修复可见性/材质问题。

## 功能

- 将 `BingBong` 外观替换为 Miku 模型
- 保持场景内可见，减少拾取/状态切换时消失问题
- 使用时触发一次整模挤压效果（不会循环重复）
- 支持自定义语音文件 `response_0.wav` ~ `response_3.wav`

## 版本 1.0.2

- 移除调试日志输出（精简运行日志）
- 调整挤压表现为慢速单次播放
- 每次“使用开始”仅触发一次，不会按住反复播放

## 安装

1. 安装 `BepInExPack_PEAK`
2. 将本模组文件夹放入：
   `PEAK/BepInEx/plugins/MikuBongFix-1.0.2/`
3. 启动游戏

## 语音文件

将以下文件放在插件目录（与 `MikuBongFix.dll` 同级）：

- `response_0.wav`
- `response_1.wav`
- `response_2.wav`
- `response_3.wav`
