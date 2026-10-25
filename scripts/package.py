#!/usr/bin/env python3
r"""
打包 FlippingFly 整合包（thin edition）。

用法：
    python scripts/package.py --game "D:\\SteamLibrary\\steamapps\\common\\Flipping is Hard" \
                              --dll  "src/bin/Release/net6.0/FlippingFly.dll" \
                              --out  "release/FlippingFly-v1.1.0-BepInEx6-IL2CPP.zip"

说明：
- 从已装好 BepInEx 6 的游戏目录里取 winhttp.dll / doorstop_config.ini /
  dotnet\ / BepInEx\core\，再加上编译好的插件。
- 默认**不含** BepInEx\interop 与 BepInEx\unity-libs（游戏派生内容，130MB+，
  让使用者本地生成）。需要带上时加 --with-interop。
"""
import argparse
import os
import shutil
import tempfile
import zipfile

INCLUDE = ["winhttp.dll", "doorstop_config.ini", ".doorstop_version", "changelog.txt"]
INCLUDE_DIRS = [("dotnet", "dotnet"), (os.path.join("BepInEx", "core"), os.path.join("BepInEx", "core"))]
OPTIONAL_DIRS = [("BepInEx", "interop"), ("BepInEx", "unity-libs")]


def main() -> None:
    ap = argparse.ArgumentParser()
    ap.add_argument("--game", required=True, help="游戏根目录（已装好 BepInEx 6 IL2CPP）")
    ap.add_argument("--dll", required=True, help="编译产物 FlippingFly.dll 路径")
    ap.add_argument("--out", required=True, help="输出 zip 路径")
    ap.add_argument("--with-interop", action="store_true", help="把 interop / unity-libs 也打进去")
    args = ap.parse_args()

    game = args.game
    with tempfile.TemporaryDirectory() as tmp:
        stage = os.path.join(tmp, "pack")
        os.makedirs(stage, exist_ok=True)

        for name in INCLUDE:
            src = os.path.join(game, name)
            if os.path.exists(src):
                shutil.copy2(src, os.path.join(stage, name))
        for src_rel, dst_rel in INCLUDE_DIRS:
            src = os.path.join(game, src_rel)
            if os.path.isdir(src):
                shutil.copytree(src, os.path.join(stage, dst_rel))

        plugins = os.path.join(stage, "BepInEx", "plugins")
        os.makedirs(plugins, exist_ok=True)
        shutil.copy2(args.dll, os.path.join(plugins, "FlippingFly.dll"))

        cfg = os.path.join(game, "BepInEx", "config", "cyb.flippingishard.fly.cfg")
        if os.path.exists(cfg):
            os.makedirs(os.path.join(stage, "BepInEx", "config"), exist_ok=True)
            shutil.copy2(cfg, os.path.join(stage, "BepInEx", "config", "cyb.flippingishard.fly.cfg"))

        if args.with_interop:
            for rel in OPTIONAL_DIRS:
                src = os.path.join(game, *rel)
                if os.path.isdir(src):
                    shutil.copytree(src, os.path.join(stage, *rel))

        out = os.path.abspath(args.out)
        os.makedirs(os.path.dirname(out), exist_ok=True)
        if os.path.exists(out):
            os.remove(out)

        count = 0
        with zipfile.ZipFile(out, "w", zipfile.ZIP_DEFLATED, compresslevel=9) as z:
            for root, _, files in os.walk(stage):
                for f in files:
                    full = os.path.join(root, f)
                    z.write(full, os.path.relpath(full, stage))
                    count += 1

        print(f"{count} files -> {out} ({os.path.getsize(out) / 1048576:.1f} MB)")


if __name__ == "__main__":
    main()
