"""
ffmpeg を使って、Equirect 360 画像を複数の直線的パースペクティブ画像に変換するスクリプト
設定は config.py から読み込む
"""

import os
import glob
import shutil
import subprocess
import argparse
import re
from pathlib import Path

# config から設定を読み込む
import config

def ensure_ffmpeg():
    if shutil.which("ffmpeg") is None:
        raise RuntimeError("ffmpeg が見つかりません。PATH を通すか、インストールしてください。")

def parse_args():
    p = argparse.ArgumentParser(
        description="Equirect 360 images -> multiple rectilinear perspective images using ffmpeg v360."
    )
    p.add_argument(
        "subdir",
        help="output/<subdir> を生成先にする。例: test_145frames_0min4sec_20260122_211204"
    )
    return p.parse_args()

def validate_subdir(subdir: str) -> str:
    # ifの確認: パス注入対策（../ や \ などを拒否）
    if not subdir or subdir.strip() == "":
        raise RuntimeError("subdir が空です。処理を中断します。")
    if any(sep in subdir for sep in ("/", "\\", ":", "..")):
        raise RuntimeError("subdir に禁止文字が含まれています（/, \\, :, ..）。処理を中断します。")
    # 文字を絞りたい場合（安全寄り）
    if not re.fullmatch(r"[A-Za-z0-9._-]+", subdir):
        raise RuntimeError("subdir は英数字と ._- のみ許可です。処理を中断します。")
    return subdir

def build_transforms_14():
    transforms = [
        (  0,  90, 0),
        (  0, -90, 0),

        (-90,   0, 0),
        (  0,   0, 0),
        ( 90,   0, 0),
        (180,   0, 0),

        (-135, 45, 0),
        ( -45, 45, 0),
        (  45, 45, 0),
        ( 135, 45, 0),

        (-135,-45, 0),
        ( -45,-45, 0),
        (  45,-45, 0),
        ( 135,-45, 0),
    ]
    return transforms

def build_transforms_dense(ring_step=None):
    """密集度高い変換パターンを構築"""
    if ring_step is None:
        ring_step = config.RING_STEP_DEG

    def ring(yaw_step, pitch):
        yaws = list(range(0, 360, yaw_step))
        return [(yaw if yaw <= 180 else yaw-360, pitch, 0) for yaw in yaws]

    transforms = []
    transforms += [(0,  90, 0), (0, -90, 0)]
    transforms += ring(ring_step, 0)
    transforms += ring(ring_step, 45)
    transforms += ring(ring_step, -45)
    return transforms

def build_v360_options(yaw, pitch, roll):
    """v360フィルタオプションを構築"""
    opts = [
        "input=e",
        "output=rectilinear",
        f"h_fov={config.HORIZONTAL_FOV}",
        f"v_fov={config.VERTICAL_FOV}",
        f"w={config.PERSPECTIVE_WIDTH}",
        f"h={config.PERSPECTIVE_HEIGHT}",
        f"yaw={yaw}",
        f"pitch={pitch}",
        f"roll={roll}",
    ]
    return "v360=" + ":".join(opts)

def _convert_images(output_dir, input_dir, transforms, preset_name):
    """
    実際の変換処理を実行する共通関数
    """
    images = []
    images += glob.glob(str(input_dir / "*.jpg"))
    images += glob.glob(str(input_dir / "*.jpeg"))
    images += glob.glob(str(input_dir / "*.png"))
    images.sort()

    if not images:
        raise RuntimeError(f"入力画像が見つかりません: {input_dir}")

    print(f"[情報] 方向数: {len(transforms)}  プリセット: {preset_name}")

    for img_path in images:
        base = os.path.splitext(os.path.basename(img_path))[0]
        print(f"\n=== {base} ===")
        digits = len(str(len(transforms)))

        for idx, (yaw, pitch, roll) in enumerate(transforms):
            out_name = f"{base}_{preset_name}_{idx:0{digits}d}_yaw{yaw:+d}_pit{pitch:+d}_rol{roll:+d}.jpg"
            out_path = str(output_dir / out_name)

            if (not config.OVERWRITE) and os.path.exists(out_path):
                print(f"skip: {out_name}")
                continue

            v360 = build_v360_options(yaw, pitch, roll)
            cmd = [
                "ffmpeg",
                "-y" if config.OVERWRITE else "-n",
                "-loglevel", config.FFMPEG_LOGLEVEL,
                "-i", img_path,
                "-vf", v360,
                "-frames:v", "1",
                out_path
            ]

            try:
                subprocess.run(cmd, check=True)
                print(f" -> {out_name}")
            except subprocess.CalledProcessError as e:
                print(f" !! 失敗: {out_name}  ({e})")

    print("\n✅ 変換が完了しました。")


def main():
    args = parse_args()
    subdir = validate_subdir(args.subdir)

    # 基準を「このスクリプトの場所」に固定（実行場所に依存しない）
    output_dir = config.OUTPUT_DIR / subdir
    input_dir  = output_dir / "temp" / "frames"

    # フォルダ確認
    print("input_dir :", input_dir)
    print("output_dir:", output_dir)
    if not input_dir.is_dir():
        raise RuntimeError("input_dir が存在しません。処理を中断します。")

    ensure_ffmpeg()
    output_dir.mkdir(parents=True, exist_ok=True)

    if config.USE_DENSE_RING:
        transforms = build_transforms_dense()
        preset_name = f"dense{config.RING_STEP_DEG}"
    else:
        transforms = build_transforms_14()
        preset_name = "rc14"

    _convert_images(output_dir, input_dir, transforms, preset_name)

def process_frames(subdir: str, use_dense_ring: bool = False):
    """
    main.py から呼ばれる用の関数

    Args:
        subdir: output/ 配下のサブディレクトリ名
        use_dense_ring: True なら密集度高い変換、False なら標準14方向
    """
    # 一時的に config の設定を上書き
    original_dense = config.USE_DENSE_RING
    config.USE_DENSE_RING = use_dense_ring

    try:
        subdir = validate_subdir(subdir)

        output_dir = config.OUTPUT_DIR / subdir
        input_dir  = output_dir / "temp" / "frames"

        if not input_dir.is_dir():
            raise RuntimeError(f"入力フォルダが見つかりません: {input_dir}")

        ensure_ffmpeg()
        output_dir.mkdir(parents=True, exist_ok=True)

        if config.USE_DENSE_RING:
            transforms = build_transforms_dense()
            preset_name = f"dense{config.RING_STEP_DEG}"
        else:
            transforms = build_transforms_14()
            preset_name = "rc14"

        _convert_images(output_dir, input_dir, transforms, preset_name)

    finally:
        # 設定を戻す
        config.USE_DENSE_RING = original_dense


if __name__ == "__main__":
    main()
