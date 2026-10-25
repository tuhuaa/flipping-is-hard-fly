# Changelog

## v1.1.0 — 2026-10-25

- 新增 `BepInEx` 配置项，可覆盖原生飞行的 `flySpeed` / `superFlySpeed` / `flySpeedMultiplier` / 位置平滑
- 游戏内 `[` `]` 实时调速（±2），自动写回 cfg，无需重启
- HUD 增加生效速度显示（`spd 18/54`）与 cfg 路径提示
- 配置默认值采用「0 = 不覆盖游戏原值」策略，避免破坏手感
- 提供内置 BepInEx 与预生成 interop 的整合包（首次启动免等待）

## v1.0.0 — 2026-10-25

- 首个版本：`F` 切换游戏原生 `EHS.FlyCheat`（联机同步），`G` 手动穿墙飞行（单机兜底）
- IMGUI HUD：模式状态、FlyCheat 是否找到、最后按下的键
- 按键反馈 Toast，便于确认按键是否被捕获
