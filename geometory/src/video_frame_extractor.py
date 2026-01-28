import cv2
import os
from pathlib import Path
from datetime import datetime
import config


def extract_frames(video_path, output_base_folder, prefix="frame", extension="jpg"):
    """
    動画をフレームごとに分割して指定フォルダに保存する

    Args:
        video_path (str): 入力動画のパス
        output_base_folder (str): 出力先ベースフォルダのパス
        prefix (str): 出力ファイル名のプレフィックス (デフォルト: "frame")
        extension (str): 出力画像の拡張子 (デフォルト: "jpg")
    """
    # 動画ファイルを開く
    video = cv2.VideoCapture(video_path)

    if not video.isOpened():
        print(f"エラー: 動画ファイル '{video_path}' を開けませんでした")
        return

    # 動画情報を取得
    fps = video.get(cv2.CAP_PROP_FPS)
    total_frames = int(video.get(cv2.CAP_PROP_FRAME_COUNT))
    width = int(video.get(cv2.CAP_PROP_FRAME_WIDTH))
    height = int(video.get(cv2.CAP_PROP_FRAME_HEIGHT))
    duration_sec = total_frames / fps if fps > 0 else 0

    # 動画ファイル名（拡張子なし）を取得
    video_filename = Path(video_path).stem

    # 時間を分秒形式に変換
    minutes = int(duration_sec // 60)
    seconds = int(duration_sec % 60)

    # フォルダ名を生成
    # 形式: "動画名_フレーム数frames_時間min秒sec_タイムスタンプ"
    # 例: "video_3000frames_1min40sec_20260122_143025"
    timestamp = datetime.now().strftime("%Y%m%d_%H%M%S")
    folder_name = f"{video_filename}_{total_frames}frames_{minutes}min{seconds}sec_{timestamp}"

    # 出力フォルダのパスを作成（temp/frames/ 内に保存）
    output_folder = os.path.join(output_base_folder, folder_name, "temp", "frames")
    Path(output_folder).mkdir(parents=True, exist_ok=True)

    print(f"動画情報:")
    print(f"  - FPS: {fps}")
    print(f"  - 総フレーム数: {total_frames}")
    print(f"  - 解像度: {width}x{height}")
    print(f"  - 再生時間: {minutes}分{seconds}秒")
    print(f"  - 出力フォルダ: {folder_name}")
    print(f"\nフレーム抽出を開始します...")

    frame_count = 0
    saved_count = 0

    while True:
        # フレームを読み込む
        ret, frame = video.read()

        # フレームの読み込みに失敗したら終了
        if not ret:
            break

        # ファイル名を生成（ゼロパディング付き）
        filename = f"{prefix}_{frame_count:06d}.{extension}"
        output_path = os.path.join(output_folder, filename)

        # フレームを保存
        cv2.imwrite(output_path, frame)
        saved_count += 1

        # 進捗表示
        if (frame_count + 1) % 100 == 0:
            print(f"  処理中: {frame_count + 1}/{total_frames} フレーム")

        frame_count += 1

    # リソースを解放
    video.release()

    print(f"\n完了!")
    print(f"  - 保存されたフレーム数: {saved_count}")
    print(f"  - 保存先: {output_folder}")


def extract_frames_interval(video_path, output_base_folder, interval=1, prefix="frame", extension="jpg"):
    """
    動画から指定間隔でフレームを抽出して保存する

    注: 現在 main.py からは呼び出されていません
    スタンドアロン実行時のみ使用可能です

    Args:
        video_path (str): 入力動画のパス
        output_base_folder (str): 出力先ベースフォルダのパス
        interval (int): フレーム抽出間隔（1なら全フレーム、2なら1フレームおき）
        prefix (str): 出力ファイル名のプレフィックス
        extension (str): 出力画像の拡張子
    """
    video = cv2.VideoCapture(video_path)

    if not video.isOpened():
        print(f"エラー: 動画ファイル '{video_path}' を開けませんでした")
        return

    fps = video.get(cv2.CAP_PROP_FPS)
    total_frames = int(video.get(cv2.CAP_PROP_FRAME_COUNT))
    duration_sec = total_frames / fps if fps > 0 else 0

    # 動画ファイル名（拡張子なし）を取得
    video_filename = Path(video_path).stem

    # 時間を分秒形式に変換
    minutes = int(duration_sec // 60)
    seconds = int(duration_sec % 60)

    # 抽出予定フレーム数を計算
    expected_frames = (total_frames + interval - 1) // interval

    # フォルダ名を生成
    # 形式: "動画名_フレーム数frames_interval間隔_時間min秒sec_タイムスタンプ"
    # 例: "video_300frames_interval10_1min40sec_20260122_143025"
    timestamp = datetime.now().strftime("%Y%m%d_%H%M%S")
    folder_name = f"{video_filename}_{expected_frames}frames_interval{interval}_{minutes}min{seconds}sec_{timestamp}"

    # 出力フォルダのパスを作成（temp/frames/ 内に保存）
    output_folder = os.path.join(output_base_folder, folder_name, "temp", "frames")
    Path(output_folder).mkdir(parents=True, exist_ok=True)

    print(f"動画情報:")
    print(f"  - FPS: {fps}")
    print(f"  - 総フレーム数: {total_frames}")
    print(f"  - 再生時間: {minutes}分{seconds}秒")
    print(f"  - 抽出間隔: {interval}フレームごと")
    print(f"  - 出力フォルダ: {folder_name}")
    print(f"\nフレーム抽出を開始します...")

    frame_count = 0
    saved_count = 0

    while True:
        ret, frame = video.read()
        if not ret:
            break

        # 指定間隔でフレームを保存
        if frame_count % interval == 0:
            filename = f"{prefix}_{saved_count:06d}.{extension}"
            output_path = os.path.join(output_folder, filename)
            cv2.imwrite(output_path, frame)
            saved_count += 1

            if saved_count % 100 == 0:
                print(f"  処理中: {saved_count} フレーム保存済み")

        frame_count += 1

    video.release()

    print(f"\n完了!")
    print(f"  - 保存されたフレーム数: {saved_count}")
    print(f"  - 保存先: {output_folder}")


def main():
    """
    スタンドアロン実行用のメイン関数
    """
    import argparse
    import sys

    parser = argparse.ArgumentParser(description='動画をフレームごとに分割して保存')
    parser.add_argument('filename', help='data/input フォルダ内の動画ファイル名 (例: video.mp4)')
    parser.add_argument('interval', type=int, nargs='?', default=1,
                        help='フレーム抽出間隔 (デフォルト: 1=全フレーム)')

    args = parser.parse_args()

    # config から設定を取得
    video_path = config.DATA_INPUT_DIR / args.filename
    output_base_folder = config.OUTPUT_DIR
    prefix = config.FRAME_PREFIX
    extension = config.FRAME_EXTENSION

    # 動画ファイルの存在確認
    if not video_path.exists():
        print(f"エラー: '{video_path}' が見つかりません")
        return 1

    # フレーム抽出を実行
    try:
        if args.interval == 1:
            extract_frames(str(video_path), str(output_base_folder), prefix, extension)
        else:
            extract_frames_interval(str(video_path), str(output_base_folder), args.interval,
                                    prefix, extension)
        return 0
    except Exception as e:
        print(f"エラー: {e}")
        return 1


if __name__ == "__main__":
    import sys
    sys.exit(main())
