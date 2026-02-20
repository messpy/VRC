#!/usr/bin/env python3
import sys
import subprocess
from pathlib import Path

def main():
    if len(sys.argv) != 2:
        print("Usage: python convert_unity_video.py <input_video>")
        sys.exit(1)

    input_path = Path(sys.argv[1]).resolve()

    # ---- if 確認 ----
    if not input_path.exists():
        print(f"[ERROR] File not found: {input_path}")
        sys.exit(1)

    if input_path.suffix.lower() not in [".mp4", ".mov", ".mkv"]:
        print("[ERROR] Unsupported file type.")
        sys.exit(1)

    output_path = input_path.with_name(input_path.stem + "_unity.mp4")

    if output_path.exists():
        print(f"[ERROR] Output already exists: {output_path}")
        sys.exit(1)

    # ---- ffmpeg コマンド ----
    cmd = [
        "ffmpeg",
        "-hide_banner",
        "-y",
        "-i", str(input_path),
        "-map", "0:v:0",
        "-map", "0:a?",
        "-vf", "fps=30,scale=in_range=pc:out_range=tv,format=yuv420p",
        "-c:v", "libx264",
        "-profile:v", "high",
        "-level", "4.1",
        "-crf", "20",
        "-preset", "medium",
        "-c:a", "aac",
        "-b:a", "192k",
        "-movflags", "+faststart",
        str(output_path)
    ]

    print("Running ffmpeg...")
    print(" ".join(cmd))

    try:
        subprocess.run(cmd, check=True)
    except subprocess.CalledProcessError:
        print("[ERROR] ffmpeg failed.")
        sys.exit(1)

    print(f"\n[SUCCESS] Created: {output_path}")

if __name__ == "__main__":
    main()
