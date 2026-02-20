"""
main.py - フレーム抽出 → (顔ぼかし) → パースペクティブ変換のメインスクリプト

使用方法:
    python main.py extract <video_file>
    python main.py blur <output_folder>
    python main.py convert <output_folder>
    python main.py pipeline <video_file> [--blur] [--dense]
"""

import argparse
import sys
from pathlib import Path

# config と各機能をインポート
import config
from video_frame_extractor import extract_frames
from face_blur import process_folder as blur_faces_folder
from batch_equirect2persp_ffmpeg import process_frames as convert_equirect


def cmd_extract(args):
    """
    フレーム抽出コマンド

    data/input/ 配下の動画ファイルを output/ に展開
    """
    print(f"\n{'='*60}")
    print(f"🎬 フレーム抽出を開始します")
    print(f"{'='*60}")

    video_path = config.DATA_INPUT_DIR / args.video_file

    if not video_path.exists():
        print(f"❌ エラー: '{video_path}' が見つかりません")
        return False

    try:
        extract_frames(
            str(video_path),
            str(config.OUTPUT_DIR),
            prefix=config.FRAME_PREFIX,
            extension=config.FRAME_EXTENSION
        )
        print(f"✅ フレーム抽出が完了しました\n")
        return True
    except Exception as e:
        print(f"❌ エラー: {e}\n")
        return False


def cmd_blur(args):
    """
    顔ぼかしコマンド

    output/<output_folder>/temp/frames/ 配下の画像に顔ぼかしを適用
    結果を output/<output_folder>/face_blurred_frames/ に保存
    """
    print(f"\n{'='*60}")
    print(f"😊 顔ぼかし処理を開始します")
    print(f"{'='*60}")

    folder_name = args.output_folder
    # temp/frames フォルダを確認
    frame_folder = config.OUTPUT_DIR / folder_name / "temp" / "frames"

    if not frame_folder.exists():
        print(f"❌ エラー: '{frame_folder}' が見つかりません")
        print(f"   先に extract コマンドでフレーム抽出を実行してください")
        return False

    try:
        blur_faces_folder(str(frame_folder), config.BLUR_STRENGTH)
        print(f"✅ 顔ぼかしが完了しました\n")
        return True
    except Exception as e:
        print(f"❌ エラー: {e}\n")
        return False


def cmd_convert(args):
    """
    Equirect→パースペクティブ変換コマンド

    output/<output_folder>/temp/frames/ の画像を複数方向に変換
    """
    print(f"\n{'='*60}")
    print(f"🔄 Equirect→パースペクティブ変換を開始します")
    print(f"{'='*60}")

    folder_name = args.output_folder

    try:
        convert_equirect(
            folder_name,
            use_dense_ring=args.dense
        )
        print(f"✅ 変換が完了しました\n")
        return True
    except Exception as e:
        print(f"❌ エラー: {e}\n")
        return False


def cmd_pipeline(args):
    """
    フルパイプライン実行
    extract → (blur) → convert を一気に実行
    """
    print(f"\n{'='*60}")
    print(f"🚀 フルパイプラインを開始します")
    print(f"{'='*60}\n")

    # Step 1: フレーム抽出
    class ExtractArgs:
        def __init__(self, video_file):
            self.video_file = video_file

    extract_ok = cmd_extract(ExtractArgs(args.video_file))
    if not extract_ok:
        return False

    # フレーム抽出で生成されたフォルダ名を特定
    import os
    output_folders = sorted([
        d for d in os.listdir(config.OUTPUT_DIR)
        if (config.OUTPUT_DIR / d).is_dir()
    ], reverse=True)

    if not output_folders:
        print("❌ エラー: 出力フォルダが見つかりません")
        return False

    latest_folder = output_folders[0]
    print(f"📁 生成されたフォルダ: {latest_folder}\n")

    # Step 2: 顔ぼかし（オプション）
    if args.blur:
        class BlurArgs:
            def __init__(self, folder):
                self.output_folder = folder

        blur_ok = cmd_blur(BlurArgs(latest_folder))
        if not blur_ok:
            print("⚠️  顔ぼかしがスキップされました\n")

    # Step 3: パースペクティブ変換
    class ConvertArgs:
        def __init__(self, folder, dense):
            self.output_folder = folder
            self.dense = dense

    convert_ok = cmd_convert(ConvertArgs(latest_folder, args.dense))
    if not convert_ok:
        return False

    print(f"{'='*60}")
    print(f"🎉 パイプライン完了！")
    print(f"{'='*60}\n")

    return True


def main():
    parser = argparse.ArgumentParser(
        description="Equirect 360動画 → パースペクティブ画像 変換ツール",
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog="""
例:
  # フレーム抽出のみ
  python main.py extract video.mp4

  # 顔ぼかしのみ
  python main.py blur test_145frames_0min4sec_20260122_220022

  # パースペクティブ変換のみ
  python main.py convert test_145frames_0min4sec_20260122_220022

  # 全処理を実行（顔ぼかしなし）
  python main.py pipeline video.mp4

  # 全処理を実行（顔ぼかしあり、密集度高い変換）
  python main.py pipeline video.mp4 --blur --dense
        """
    )

    subparsers = parser.add_subparsers(dest='command', help='実行するコマンド')

    # === extract サブコマンド ===
    extract_parser = subparsers.add_parser('extract', help='動画からフレームを抽出')
    extract_parser.add_argument('video_file', help='data/input/ 内の動画ファイル名 (例: video.mp4)')

    # === blur サブコマンド ===
    blur_parser = subparsers.add_parser('blur', help='抽出されたフレームの顔をぼかす')
    blur_parser.add_argument('output_folder', help='output/ 内のフォルダ名')

    # === convert サブコマンド ===
    convert_parser = subparsers.add_parser('convert', help='Equirect画像をパースペクティブ変換')
    convert_parser.add_argument('output_folder', help='output/ 内のフォルダ名')
    convert_parser.add_argument('--dense', action='store_true', help='密集度高い変換を使用')

    # === pipeline サブコマンド ===
    pipeline_parser = subparsers.add_parser('pipeline', help='フレーム抽出→変換を一気に実行')
    pipeline_parser.add_argument('video_file', help='data/input/ 内の動画ファイル名 (例: video.mp4)')
    pipeline_parser.add_argument('--blur', action='store_true', help='顔ぼかしを有効にする')
    pipeline_parser.add_argument('--dense', action='store_true', help='密集度高い変換を使用')

    # パースしてコマンド実行
    args = parser.parse_args()

    if not args.command:
        parser.print_help()
        return 1

    # 各コマンドに対応した処理
    if args.command == 'extract':
        success = cmd_extract(args)
    elif args.command == 'blur':
        success = cmd_blur(args)
    elif args.command == 'convert':
        success = cmd_convert(args)
    elif args.command == 'pipeline':
        success = cmd_pipeline(args)
    else:
        parser.print_help()
        return 1

    return 0 if success else 1


if __name__ == '__main__':
    sys.exit(main())
