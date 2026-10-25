using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FlippingFly
{
    /// <summary>
    /// 飞行模块：
    ///   F = 原生 FlyCheat 开关（联机同步走原生，推荐）
    ///   G = 手动穿墙飞行（单机兜底）
    ///   H = 显示/隐藏面板，[ ] = 实时调速度（会写回 cfg）
    /// 速度也可以在 BepInEx/config/cyb.flippingishard.fly.cfg 里改。
    /// </summary>
    public class FlyBehaviour : MonoBehaviour
    {
        public FlyBehaviour(IntPtr ptr) : base(ptr) { }

        // ---------- 状态 ----------
        private EHS.FlyCheat _flyCheat;
        private float _nextFindTime;
        private bool _nativeFlyOn;

        private bool _manualOn;
        private Rigidbody _rb;
        private Transform _tf;
        private bool _rbWasKinematic;
        private bool _rbWasGravity;
        private bool _rbWasCollisions;
        private Vector3 _lastManualVel;

        private bool _showOverlay = true;
        private string _toast = "";
        private Color _toastColor = Color.yellow;
        private float _toastUntil;
        private string _lastKey = "-";
        private float _lastKeyUntil;
        private int _frames;
        private bool _stylesReady;

        // 面板上显示的“当前生效速度”
        private float _nativeSpeedShown;
        private float _nativeSuperShown;

        // ================= Unity 回调 =================

        private void Awake()
        {
            try { _showOverlay = Plugin.CfgShowHud.Value; } catch { }
        }

        private void Update()
        {
            _frames++;
            try
            {
                PollHotkeys();
                EnsureFlyCheat();
                if (_manualOn) HandleManualFly();
            }
            catch (Exception e)
            {
                Plugin.Logger.LogError("[FlippingFly] Update error: " + e);
            }
        }

        private void OnDestroy()
        {
            try { if (_manualOn) ExitManual(); } catch { }
        }

        // ================= 热键 =================

        private void PollHotkeys()
        {
            var kb = Keyboard.current;
            if (kb == null)
            {
                if (_frames % 600 == 0) Plugin.Logger.LogWarning("[FlippingFly] Keyboard.current is null (InputSystem not ready)");
                return;
            }

            if (kb.fKey.wasPressedThisFrame) { NoteKey("F"); ToggleNative(); }
            if (kb.gKey.wasPressedThisFrame) { NoteKey("G"); ToggleManual(); }
            if (kb.hKey.wasPressedThisFrame)
            {
                NoteKey("H");
                _showOverlay = !_showOverlay;
                try { Plugin.CfgShowHud.Value = _showOverlay; Plugin.CfgFileRef.Save(); } catch { }
                ShowToast(_showOverlay ? "HUD ON" : "HUD OFF", Color.cyan);
            }
            if (kb.leftBracketKey.wasPressedThisFrame) { NoteKey("["); NudgeSpeed(-2f); }
            if (kb.rightBracketKey.wasPressedThisFrame) { NoteKey("]"); NudgeSpeed(+2f); }
        }

        private void NoteKey(string k)
        {
            _lastKey = k;
            _lastKeyUntil = Time.unscaledTime + 3f;
            Plugin.Logger.LogInfo("[FlippingFly] key captured: " + k);
        }

        /// <summary>[ ] 调速度：原生开着就调原生，否则调手动；结果写回 cfg。</summary>
        private void NudgeSpeed(float delta)
        {
            try
            {
                if (_nativeFlyOn)
                {
                    float cur = Plugin.CfgNativeSpeed.Value > 0f ? Plugin.CfgNativeSpeed.Value : _nativeSpeedShown;
                    if (cur <= 0f) cur = 18f;
                    cur = Mathf.Clamp(cur + delta, 2f, 200f);
                    Plugin.CfgNativeSpeed.Value = cur;
                    Plugin.CfgFileRef.Save();
                    ApplyNativeOverrides();
                    ShowToast("native speed " + cur.ToString("0"), Color.cyan);
                }
                else
                {
                    float cur = Mathf.Clamp(Plugin.CfgManualSpeed.Value + delta, 2f, 200f);
                    Plugin.CfgManualSpeed.Value = cur;
                    Plugin.CfgFileRef.Save();
                    ShowToast("manual speed " + cur.ToString("0"), Color.cyan);
                }
            }
            catch (Exception e)
            {
                Plugin.Logger.LogError("[FlippingFly] NudgeSpeed failed: " + e.Message);
            }
        }

        // ================= 原生 FlyCheat =================

        private void EnsureFlyCheat()
        {
            if (_flyCheat != null) return;
            if (Time.unscaledTime < _nextFindTime) return;
            _nextFindTime = Time.unscaledTime + 1.5f;
            try
            {
                _flyCheat = UnityEngine.Object.FindObjectOfType<EHS.FlyCheat>();
                if (_flyCheat != null)
                {
                    Plugin.Logger.LogInfo("[FlippingFly] EHS.FlyCheat found on: " + _flyCheat.gameObject.name);
                    ReadNativeSpeeds();
                }
            }
            catch (Exception e)
            {
                Plugin.Logger.LogError("[FlippingFly] FindObjectOfType<FlyCheat> failed: " + e);
            }
        }

        private void ReadNativeSpeeds()
        {
            if (_flyCheat == null) return;
            try
            {
                _nativeSpeedShown = _flyCheat.flySpeed;
                _nativeSuperShown = _flyCheat.superFlySpeed;
                Plugin.Logger.LogInfo("[FlippingFly] game default flySpeed=" + _nativeSpeedShown
                    + " superFlySpeed=" + _nativeSuperShown
                    + " multiplier=" + _flyCheat.flySpeedMultiplier
                    + " smoothing=" + _flyCheat.targetPositionSmoothingDuration);
            }
            catch (Exception e)
            {
                Plugin.Logger.LogWarning("[FlippingFly] ReadNativeSpeeds failed: " + e.Message);
            }
        }

        private void ApplyNativeOverrides()
        {
            if (_flyCheat == null) return;
            try
            {
                if (Plugin.CfgNativeSpeed.Value > 0f) _flyCheat.flySpeed = Plugin.CfgNativeSpeed.Value;
                if (Plugin.CfgNativeSuperSpeed.Value > 0f) _flyCheat.superFlySpeed = Plugin.CfgNativeSuperSpeed.Value;
                if (Plugin.CfgNativeMultiplier.Value > 0f) _flyCheat.flySpeedMultiplier = Plugin.CfgNativeMultiplier.Value;
                if (Plugin.CfgSmoothing.Value > 0f) _flyCheat.targetPositionSmoothingDuration = Plugin.CfgSmoothing.Value;

                _nativeSpeedShown = _flyCheat.flySpeed;
                _nativeSuperShown = _flyCheat.superFlySpeed;
            }
            catch (Exception e)
            {
                Plugin.Logger.LogWarning("[FlippingFly] ApplyNativeOverrides failed: " + e.Message);
            }
        }

        private void ToggleNative()
        {
            EnsureFlyCheat();
            if (_flyCheat == null)
            {
                Plugin.Logger.LogWarning("[FlippingFly] FlyCheat not in scene -> switch to manual fly");
                ShowToast("NO FlyCheat -> MANUAL", Color.yellow);
                ToggleManual();
                return;
            }

            try
            {
                _nativeFlyOn = !_nativeFlyOn;
                if (_nativeFlyOn) ApplyNativeOverrides();
                _flyCheat.SetExternalFlyEnabled(_nativeFlyOn);
                Plugin.Logger.LogInfo("[FlippingFly] native fly = " + _nativeFlyOn);
                ShowToast(_nativeFlyOn ? "NATIVE FLY ON" : "NATIVE FLY OFF", _nativeFlyOn ? Color.green : Color.gray);
            }
            catch (Exception e)
            {
                Plugin.Logger.LogError("[FlippingFly] SetExternalFlyEnabled failed: " + e);
                ShowToast("NATIVE FLY FAILED", Color.red);
            }
        }

        // ================= 手动穿墙飞行 =================

        private void ToggleManual()
        {
            if (_manualOn)
            {
                ExitManual();
                ShowToast("MANUAL FLY OFF", Color.gray);
                return;
            }
            if (!EnterManual()) return;
            ShowToast("MANUAL FLY ON", Color.green);
        }

        private bool EnterManual()
        {
            EnsureFlyCheat();
            Transform root = null;
            if (_flyCheat != null) root = _flyCheat.transform;

            if (root != null)
            {
                _rb = root.GetComponent<Rigidbody>();
                if (_rb == null) _rb = root.GetComponentInParent<Rigidbody>();
                if (_rb == null) _rb = root.GetComponentInChildren<Rigidbody>();
                _tf = _rb != null ? _rb.transform : root;
            }

            if (_tf == null)
            {
                Plugin.Logger.LogWarning("[FlippingFly] manual fly: player transform not found");
                ShowToast("PLAYER NOT FOUND", Color.red);
                return false;
            }

            if (_rb != null)
            {
                _rbWasKinematic = _rb.isKinematic;
                _rbWasGravity = _rb.useGravity;
                _rbWasCollisions = _rb.detectCollisions;
                _rb.isKinematic = true;
                _rb.useGravity = false;
                _rb.detectCollisions = false;
            }

            _lastManualVel = Vector3.zero;
            _manualOn = true;
            Plugin.Logger.LogInfo("[FlippingFly] manual fly ON, target=" + _tf.name + ", rb=" + (_rb != null));
            return true;
        }

        private void ExitManual()
        {
            _manualOn = false;
            if (_rb != null)
            {
                _rb.useGravity = _rbWasGravity;
                _rb.detectCollisions = _rbWasCollisions;
                _rb.isKinematic = _rbWasKinematic;
                if (!_rb.isKinematic)
                {
                    _rb.linearVelocity = _lastManualVel;
                    _rb.angularVelocity = Vector3.zero;
                }
            }
            Plugin.Logger.LogInfo("[FlippingFly] manual fly OFF");
        }

        private void HandleManualFly()
        {
            var kb = Keyboard.current;
            if (kb == null || _tf == null) return;

            var cam = Camera.main;
            if (cam == null) return;

            Vector3 fwd = cam.transform.forward; fwd.y = 0f;
            Vector3 right = cam.transform.right; right.y = 0f;
            if (fwd.sqrMagnitude > 0.0001f) fwd.Normalize();
            if (right.sqrMagnitude > 0.0001f) right.Normalize();

            float s = Plugin.CfgManualSpeed.Value * (kb.leftShiftKey.isPressed ? Plugin.CfgManualBoost.Value : 1f);
            Vector3 vel = Vector3.zero;
            if (kb.wKey.isPressed) vel += fwd * s;
            if (kb.sKey.isPressed) vel -= fwd * s;
            if (kb.dKey.isPressed) vel += right * s;
            if (kb.aKey.isPressed) vel -= right * s;
            if (kb.eKey.isPressed || kb.spaceKey.isPressed) vel.y += s;
            if (kb.qKey.isPressed || kb.leftCtrlKey.isPressed) vel.y -= s;

            _lastManualVel = vel;
            if (vel.sqrMagnitude < 0.0001f) return;

            Vector3 pos = _tf.position + vel * Time.deltaTime;
            if (_rb != null) _rb.position = pos;
            _tf.position = pos;
        }

        // ================= HUD =================

        private void ShowToast(string msg, Color c)
        {
            _toast = msg;
            _toastColor = c;
            _toastUntil = Time.unscaledTime + 2.5f;
        }

        private void SetupStyles()
        {
            try
            {
                Font f = null;
                try { f = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei", "SimHei", "Arial" }, 18); } catch { }

                var s = GUI.skin.label;
                s.fontSize = 16;
                s.fontStyle = FontStyle.Bold;
                if (f != null) s.font = f;
                _stylesReady = true;
                Plugin.Logger.LogInfo("[FlippingFly] HUD styles ready, cjkFont=" + (f != null));
            }
            catch (Exception e)
            {
                _stylesReady = true;
                Plugin.Logger.LogError("[FlippingFly] SetupStyles failed: " + e.Message);
            }
        }

        private static void Text(float x, float y, string txt, Color c, int size)
        {
            GUI.contentColor = new Color(0f, 0f, 0f, 0.9f);
            GUI.skin.label.fontSize = size;
            GUI.Label(new Rect(x + 2f, y + 2f, 700f, size + 10f), txt);
            GUI.contentColor = c;
            GUI.Label(new Rect(x, y, 700f, size + 10f), txt);
            GUI.contentColor = Color.white;
        }

        private void OnGUI()
        {
            if (!_showOverlay) return;
            try
            {
                if (!_stylesReady) SetupStyles();

                GUI.color = new Color(0f, 0f, 0f, 0.55f);
                GUI.DrawTexture(new Rect(10f, 10f, 470f, 150f), Texture2D.whiteTexture);
                GUI.color = Color.white;

                Text(20f, 14f, "FlippingFly v" + Plugin.Version + "   fly mod ready", Color.white, 16);

                string native = "F native fly : " + (_nativeFlyOn ? "ON" : "off")
                                + "  [" + (_flyCheat != null ? "FlyCheat OK" : "FlyCheat none") + "]"
                                + "  spd " + _nativeSpeedShown.ToString("0") + "/" + _nativeSuperShown.ToString("0");
                Text(20f, 40f, native, _nativeFlyOn ? Color.green : new Color(0.72f, 0.72f, 0.72f), 15);

                string manual = "G manual fly : " + (_manualOn ? "ON" : "off")
                                + "  spd " + Plugin.CfgManualSpeed.Value.ToString("0")
                                + " x" + Plugin.CfgManualBoost.Value.ToString("0.#");
                Text(20f, 62f, manual, _manualOn ? Color.green : new Color(0.72f, 0.72f, 0.72f), 15);

                Text(20f, 84f, "WASD move / E up / Q down / Shift boost", new Color(0.7f, 0.85f, 1f), 15);

                string keyLine = "last key: " + _lastKey + "      H hud / [ ] speed";
                Text(20f, 106f, keyLine, Time.unscaledTime < _lastKeyUntil ? Color.yellow : new Color(0.55f, 0.55f, 0.55f), 15);

                Text(20f, 128f, "cfg: BepInEx\\config\\cyb.flippingishard.fly.cfg", new Color(0.5f, 0.6f, 0.7f), 13);

                if (Time.unscaledTime < _toastUntil && !string.IsNullOrEmpty(_toast))
                {
                    float a = Mathf.Clamp01((_toastUntil - Time.unscaledTime) / 0.6f);
                    var c = _toastColor; c.a = a;
                    Text(Screen.width * 0.5f - 160f, 180f, _toast, c, 30);
                }
            }
            catch (Exception e)
            {
                if (_frames % 300 == 0) Plugin.Logger.LogError("[FlippingFly] OnGUI error: " + e.Message);
            }
        }
    }
}
