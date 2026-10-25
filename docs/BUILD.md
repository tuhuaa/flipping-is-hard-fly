# 从源码构建 / Building from source

## 前置

| 项 | 要求 |
| --- | --- |
| .NET SDK | 8.0+（目标框架 `net6.0`，SDK 8 会自动拉取 net6 targeting pack） |
| 游戏 | Steam 正版 *Flipping is Hard*，已成功用 BepInEx 6 启动过一次 |
| BepInEx | 6.0.0-be.755 或更新（IL2CPP，win-x64） |

## 关键前提：interop 程序集

IL2CPP 游戏没有可直接引用的托管 dll。必须先让 BepInEx 跑一次，它会把
`GameAssembly.dll` + `global-metadata.dat` 反编译成一组互操作程序集，落在：

```
<游戏目录>\BepInEx\interop\      （177 个 dll，约 123 MB）
<游戏目录>\BepInEx\unity-libs\   （Unity 引擎参考 dll）
```

本工程引用的是 `interop\` 里的 `Assembly-CSharp.dll`（含 `EHS.FlyCheat`）
以及 `UnityEngine.CoreModule` / `IMGUIModule` / `PhysicsModule` /
`TextRenderingModule` / `Unity.InputSystem`。

## 编译

1. 改 `src/FlippingFly.csproj` 里的 `<GameDir>`，指向你的游戏目录：

   ```xml
   <GameDir>D:\SteamLibrary\steamapps\common\Flipping is Hard</GameDir>
   ```

2. 构建：

   ```bash
   cd src
   dotnet build -c Release
   ```

3. 把 `src/bin/Release/net6.0/FlippingFly.dll` 复制到 `<游戏目录>\BepInEx\plugins\`。

所有引用都设了 `Private=false`，输出目录里只会生成 `FlippingFly.dll` 本身。

## 打包整合包

仓库不带任何游戏相关二进制。发版时的整合包按下面结构组装，然后作为 Release 附件上传：

```
winhttp.dll
doorstop_config.ini
.doorstop_version
changelog.txt            (BepInEx 自带)
dotnet\                  (BepInEx 6 IL2CPP 附带的 .NET 6 运行时)
BepInEx\core\            (BepInEx 6 IL2CPP 本体)
BepInEx\plugins\FlippingFly.dll
BepInEx\config\cyb.flippingishard.fly.cfg   (可选，预置默认值)
```

**不要**把 `BepInEx\interop\`、`BepInEx\unity-libs\` 提交进仓库 —— 那是从游戏本体反编译出来的
派生内容，既大（130 MB+）又有版权问题，让使用者本地生成即可。
（若确实想省掉使用者首次几分钟的等待，可以把它们放进 Release 附件，
但要在说明里写清：游戏版本更新后需删除这两个目录让 BepInEx 重新生成。）

## 开发笔记（踩过的坑）

1. 这个版本的 Il2CppInterop **没有** `[RegisterTypeInIl2Cpp]` 特性，
   只需 `ClassInjector.RegisterTypeInIl2Cpp<T>()`；注入的 MonoBehaviour 必须提供
   `public XxxBehaviour(IntPtr ptr) : base(ptr) { }` 构造函数。
2. interop 里的 `GUIStyle` 没有拷贝构造函数、也没有 `normal` 属性。
   画 HUD 要直接改 `GUI.skin.label` 的 `fontSize` / `font`，颜色用 `GUI.contentColor`。
3. Unity 6 里 `Rigidbody.velocity` 已废弃，用 `linearVelocity`。
4. `Font.CreateDynamicFontFromOSFont(new[]{"Microsoft YaHei"}, 18)` 可以正常拿到中文字体（IMGUI 动态字体按需生成字形）。
5. `-batchmode -nographics` 下 `Keyboard.current` 恒为 null，属于无输入设备的正常现象，
   不代表窗口模式有问题 —— 用无头方式验证时不要被这条日志误导。
6. 想快速查某个 interop 类型有哪些公开方法，可以用任意 .NET 元数据读取器（`System.Reflection.Metadata`），
   比在 130MB dll 里瞎猜快得多。
