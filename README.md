# FlippingFly

**《Flipping is Hard》飞天 Mod / Fly mod for Flipping is Hard**

基于游戏自带的 `EHS.FlyCheat` 组件实现飞行，带位置平滑，**联机合作时其他玩家能看到你在飞**；
另有独立的「手动穿墙飞行」作为单机兜底。屏幕上常驻 HUD，按键状态一目了然。

> 游戏：Steam 正式版 *Flipping is Hard*（appid 3826670，Unity 6000.0.66f1 / IL2CPP / win-x64）
> 框架：BepInEx 6.0.0-be.791（IL2CPP）

---

## 功能

| 功能 | 说明 |
| --- | --- |
| 原生飞行 `F` | 调用 `EHS.FlyCheat.SetExternalFlyEnabled()`，走游戏自带的网络同步与位置平滑，联机可用 |
| 手动飞行 `G` | 直接改 `Rigidbody` 位置并关闭重力/碰撞的穿墙飞行，单机兜底，联机回弹 |
| 屏幕 HUD | 左上角常驻状态面板：模式状态、是否找到 `FlyCheat`、当前速度、最后按下的键 |
| 按键文字验证 | 每按一次功能键，`last key` 变黄 + 屏幕中间闪一行大字 |
| 速度实时可调 | 游戏内 `[` `]` 调整并自动写回配置 |
| 配置化 | `BepInEx/config/cyb.flippingishard.fly.cfg`，可覆盖原生 `flySpeed` / `superFlySpeed` / 倍率 / 位置平滑 |

## 安装

1. 完全退出游戏。
2. 从 [Releases](../../releases) 下载整合包，**解压到游戏根目录**（与 `Flipping is Hard.exe` 同级）：

   ```
   ...\steamapps\common\Flipping is Hard\
   ```

   解压后应看到新增的 `winhttp.dll`、`doorstop_config.ini`、`dotnet\`、`BepInEx\`。
3. 从 Steam 正常启动游戏。

> **首次启动会卡 1~5 分钟**（BepInEx 正在反编译游戏的 IL2CPP 元数据、生成互操作程序集），
> 窗口可能白屏/无响应，这是正常的，别关。成功后游戏目录会出现 `BepInEx\interop\`，之后都是秒开。

详细图文步骤与排错见 [`docs/INSTALL-zh.txt`](docs/INSTALL-zh.txt)。

## 使用

进关卡后左上角出现 HUD：

```
FlippingFly v1.1.0   fly mod ready
F native fly : off  [FlyCheat OK]  spd 18/54
G manual fly : off  spd 18 x3
WASD move / E up / Q down / Shift boost
last key: -      H hud / [ ] speed
cfg: BepInEx\config\cyb.flippingishard.fly.cfg
```

| 按键 | 作用 |
| --- | --- |
| `F` | 原生飞行 开 / 关（推荐，联机同步正常） |
| `G` | 手动飞行 开 / 关（穿墙，单人用） |
| `H` | 显示 / 隐藏 HUD |
| `[` `]` | 速度 −2 / +2（自动写回 cfg） |
| `W A S D` | 前后左右（按镜头朝向） |
| `E` / `Q` | 上升 / 下降 |
| `Shift` | 加速（默认 3×） |
| 鼠标 | 转视角 |

`FlyCheat OK` = 当前场景找到游戏原生飞行组件；`FlyCheat none` = 还没生成玩家对象，此时按 `F` 会自动切到手动模式。

## 配置

文件：`BepInEx\config\cyb.flippingishard.fly.cfg`（改完重启游戏生效；`[` `]` 改的不需要重启）

```ini
[HUD]
ShowHud = true

[Speed]
NativeFlySpeed = 0           # 原生飞行速度；0 = 保持游戏原值，想快就 30 / 50 / 80
NativeSuperFlySpeed = 0      # 按住 Shift 时的速度；0 = 不改
NativeSpeedMultiplier = 0    # 额外倍数；0 = 不改，填 2 = 再快一倍
PositionSmoothing = 0        # 位置平滑时长；联机瞬移/抖动就填 0.1~0.3，0 = 不改
ManualFlySpeed = 18          # 手动飞行速度
ManualBoostMultiplier = 3    # 手动飞行 Shift 倍率
```

> 所有 `0` 都表示「不覆盖游戏原值」，只有填正数才生效 —— 避免一上来就把游戏手感改坏。

## 从源码构建

需要 .NET SDK 8（`dotnet` 在 PATH 里），以及游戏目录中由 BepInEx 生成的 interop 程序集。

```bash
# 1. 先让游戏跑一次 BepInEx，生成 BepInEx/interop/*.dll
# 2. 修改 src/FlippingFly.csproj 里的 <GameDir> 指向你的游戏目录
cd src
dotnet build -c Release
# 产物：bin/Release/net6.0/FlippingFly.dll -> 丢进 BepInEx/plugins/
```

工程引用全部设为 `Private=false`，不会把游戏 dll 复制进输出目录。详见 [`docs/BUILD.md`](docs/BUILD.md)。

## 原理

1. 游戏正式版里 `CheatsToggle` 这个输入动作还在（绑定 `LeftShift+F`），但**消费它的代码被裁了**，所以原生热键按了没反应。
2. `EHS.FlyCheat` 组件本身仍在场景里，并暴露了公开 API：
   `SetExternalFlyEnabled(bool)`、`IsFlying`、`flySpeed`、`superFlySpeed`、`flySpeedMultiplier`、`targetPositionSmoothingDuration`。
3. 本插件通过 BepInEx 6（IL2CPP）+ Il2CppInterop 直接调用这些 API，绕开被裁掉的热键，
   因此飞行逻辑、网络同步、位置平滑全部复用游戏自己的实现。
4. 输入读取走游戏同款的 Input System（`Keyboard.current`），HUD 用 IMGUI 自绘，
   字体尝试加载系统的「Microsoft YaHei」以正确显示中文。

## 常见问题

**左上角没有 HUD**
看 `BepInEx\LogOutput.log` 里有没有 `Loading [FlippingFly 1.1.0]`。没有就是解压位置不对，
或者 `winhttp.dll` 没和 `Flipping is Hard.exe` 在同一层。

**按 F 有字但手机不动**
说明这个版本原生飞行的输入也被裁了，用 `G` 手动模式（联机不同步）。

**联机时瞬移 / 抖动**
把 `PositionSmoothing` 调到 `0.1 ~ 0.3`；仍抖动就用原生模式（`F`）而不是手动模式。

**游戏更新后启动报错**
删掉 `BepInEx\interop\` 和 `BepInEx\unity-libs\`，重新启动让它按新版本生成一次。

**彻底卸载**
删除 `winhttp.dll`、`doorstop_config.ini`、`.doorstop_version`、`dotnet\`、`BepInEx\`。
游戏本体文件一个没改；不放心可在 Steam 里验证文件完整性。

## 免责声明

- 纯客户端内存修改，不改游戏文件，不含破解、不绕过任何反作弊。
- 游戏没有 VAC / EAC，本项目不涉及封号检测；但**联机请只在自己或朋友的房间里使用**，
  不要去公开房影响他人游戏体验。
- 仅用于单人娱乐与学习 IL2CPP Mod 开发，使用后果自负。

## 致谢

- [BepInEx](https://github.com/BepInEx/BepInEx) 6 IL2CPP 与 Il2CppInterop
- [d1ll3x3/FlippingIsHard-Trainer-BepInEx](https://github.com/d1ll3x3/FlippingIsHard-Trainer-BepInEx)
  提供了「原生 FlyCheat 会被 Shift+F 抢占」这条关键线索

## License

MIT，见 [LICENSE](LICENSE)。

---

<details>
<summary>English</summary>

**FlippingFly** is a small BepInEx 6 (IL2CPP) plugin that enables flight in *Flipping is Hard*.

The game ships a runtime `EHS.FlyCheat` component with a public `SetExternalFlyEnabled(bool)` API and
`flySpeed` / `superFlySpeed` / `flySpeedMultiplier` properties, but the release build ships the
`CheatsToggle` input action (`LeftShift+F`) without any consumer. This plugin binds that API to `F`
and adds a second, transform-based noclip mode on `G`, plus an IMGUI HUD that reports state,
speed and the last key pressed.

Controls: `F` native fly, `G` manual fly, `H` HUD, `[`/`]` speed, `WASD` move, `E`/`Q` up/down, `Shift` boost.
All speed values are configurable in `BepInEx/config/cyb.flippingishard.fly.cfg` (`0` = keep game default).

Client-side memory only, no file patching, no anti-cheat bypass. Use it in your own lobbies.

</details>
