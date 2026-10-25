using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using Il2CppInterop.Runtime.Injection;

namespace FlippingFly
{
    [BepInPlugin(Guid, "FlippingFly", Version)]
    public class Plugin : BasePlugin
    {
        public const string Guid = "cyb.flippingishard.fly";
        public const string Version = "1.1.0";

        internal static ManualLogSource Logger;

        // ---- 可调节项（游戏内按 [ ] 也能改，改完自动写回 cfg） ----
        internal static ConfigEntry<bool> CfgShowHud;
        internal static ConfigEntry<float> CfgNativeSpeed;
        internal static ConfigEntry<float> CfgNativeSuperSpeed;
        internal static ConfigEntry<float> CfgNativeMultiplier;
        internal static ConfigEntry<float> CfgSmoothing;
        internal static ConfigEntry<float> CfgManualSpeed;
        internal static ConfigEntry<float> CfgManualBoost;

        internal static ConfigFile CfgFileRef;

        public override void Load()
        {
            Logger = Log;
            Logger.LogInfo("[FlippingFly] loading v" + Version);

            CfgFileRef = Config;

            CfgShowHud = Config.Bind("HUD", "ShowHud", true,
                "是否显示左上角状态面板 / show the on-screen status panel");

            CfgNativeSpeed = Config.Bind("Speed", "NativeFlySpeed", 0f,
                "原生飞行速度，0 = 不改游戏原值 / native fly speed, 0 = keep game default");
            CfgNativeSuperSpeed = Config.Bind("Speed", "NativeSuperFlySpeed", 0f,
                "原生飞行按住 Shift 的速度，0 = 不改 / native speed while holding Shift, 0 = keep");
            CfgNativeMultiplier = Config.Bind("Speed", "NativeSpeedMultiplier", 0f,
                "原生飞行速度额外倍数，0 = 不改 / extra multiplier, 0 = keep");
            CfgSmoothing = Config.Bind("Speed", "PositionSmoothing", 0f,
                "位置平滑时长（联机瞬移/抖动就调大，如 0.15），0 = 不改 / position smoothing, 0 = keep");
            CfgManualSpeed = Config.Bind("Speed", "ManualFlySpeed", 18f,
                "手动飞行速度（单机兜底模式）/ manual fly speed");
            CfgManualBoost = Config.Bind("Speed", "ManualBoostMultiplier", 3f,
                "手动飞行按住 Shift 的倍数 / manual fly boost multiplier");

            Config.Save();
            Logger.LogInfo("[FlippingFly] config: " + Config.ConfigFilePath);

            try
            {
                ClassInjector.RegisterTypeInIl2Cpp<FlyBehaviour>();
                Logger.LogInfo("[FlippingFly] FlyBehaviour injected into IL2CPP");
            }
            catch (System.Exception e)
            {
                Logger.LogError("[FlippingFly] inject failed: " + e);
            }

            try
            {
                AddComponent<FlyBehaviour>();
                Logger.LogInfo("[FlippingFly] FlyBehaviour added");
            }
            catch (System.Exception e)
            {
                Logger.LogError("[FlippingFly] AddComponent failed: " + e);
            }
        }
    }
}
