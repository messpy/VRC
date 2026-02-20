"""
画像内の顔を検出してぼかすスクリプト
顔検出と Gaussian ぼかし処理を実装
"""
import cv2
import os
from pathlib import Path
from datetime import datetime
import config


def blur_faces(image_path, output_path, blur_strength=51):
    """
    画像内の顔を検出してぼかす

    Args:
        image_path (str): 入力画像のパス
        output_path (str): 出力画像のパス
        blur_strength (int): ぼかしの強さ（奇数、大きいほど強い）

    Returns:
        int: 検出された顔の数
    """
    # 画像を読み込む
    image = cv2.imread(image_path)
    if image is None:
        print(f"エラー: 画像 '{image_path}' を読み込めませんでした")
        return 0

    # グレースケールに変換
    gray = cv2.cvtColor(image, cv2.COLOR_BGR2GRAY)

    # 顔検出器を読み込む（OpenCVのHaar Cascade分類器）
    face_cascade = cv2.CascadeClassifier(
        cv2.data.haarcascades + 'haarcascade_frontalface_default.xml'
    )

    # 顔を検出
    faces = face_cascade.detectMultiScale(
        gray,
        scaleFactor=1.1,
        minNeighbors=5,
        minSize=(30, 30)
    )

    # ぼかし強度を奇数に調整
    if blur_strength % 2 == 0:
        blur_strength += 1

    # 検出された各顔にぼかしを適用
    for (x, y, w, h) in faces:
        # 顔領域を取得
        face_region = image[y:y+h, x:x+w]

        # Gaussian ぼかしを適用
        blurred_face = cv2.GaussianBlur(face_region, (blur_strength, blur_strength), 0)

        # ぼかした顔を元の画像に戻す
        image[y:y+h, x:x+w] = blurred_face

    # 結果を保存
    cv2.imwrite(output_path, image)

    return len(faces)


def process_folder(input_folder, blur_strength=51):
    """
    フォルダ内の全画像の顔をぼかす
    frames フォルダ内の画像を上書きして保存

    Args:
        input_folder (str): 入力フォルダのパス（frames フォルダ）
        blur_strength (int): ぼかしの強さ
    """
    input_path = Path(input_folder)

    if not input_path.exists():
        print(f"エラー: フォルダ '{input_folder}' が見つかりません")
        return

    # サポートする画像拡張子
    image_extensions = {'.jpg', '.jpeg', '.png', '.bmp', '.tiff', '.webp'}

    # フォルダ内の画像ファイルを取得
    image_files = [
        f for f in input_path.iterdir()
        if f.suffix.lower() in image_extensions
    ]

    if not image_files:
        print(f"エラー: '{input_folder}' に画像ファイルが見つかりません")
        return

    # ファイルをソート（順序を保証）
    image_files.sort()

    print(f"処理対象: {len(image_files)} 枚の画像")
    print(f"出力先: {input_path} (上書き)")
    print(f"ぼかし強度: {blur_strength}")
    print("\n処理を開始します...\n")

    total_faces = 0
    processed_count = 0

    for i, image_file in enumerate(image_files, 1):
        input_image_path = str(image_file)
        # 同じファイルに上書き保存
        output_image_path = str(image_file)

        # 顔検出とぼかし処理
        face_count = blur_faces(input_image_path, output_image_path, blur_strength)

        total_faces += face_count
        processed_count += 1

        print(f"[{i}/{len(image_files)}] {image_file.name}: {face_count}個の顔を検出")

    print(f"\n完了!")
    print(f"  - 処理した画像: {processed_count} 枚")
    print(f"  - 検出した顔の総数: {total_faces} 個")
    print(f"  - 保存先: {input_path}")


def main():
    """
    スタンドアロン実行用のメイン関数
    """
    import argparse
    import sys

    parser = argparse.ArgumentParser(description='画像内の顔を検出してぼかす')
    parser.add_argument('folder', help='output/ 内のフォルダ名（例: test_145frames_0min4sec_20260122_211204）')
    parser.add_argument('-b', '--blur', type=int, default=51,
                        help='ぼかしの強さ（奇数、デフォルト: 51）')

    args = parser.parse_args()

    # 入力パスから"output/"プレフィックスを除去
    folder_name = args.folder.replace('output/', '').replace('output\\', '')

    # config から OUTPUT_DIR を使用
    frame_folder = config.OUTPUT_DIR / folder_name / 'temp' / 'frames'

    if not frame_folder.exists():
        print(f"エラー: '{frame_folder}' が見つかりません")
        print(f"正しいフォルダ名を指定してください")
        return 1

    # 顔ぼかし処理を実行
    try:
        process_folder(str(frame_folder), args.blur)
        return 0
    except Exception as e:
        print(f"エラー: {e}")
        return 1


if __name__ == "__main__":
    import sys
    sys.exit(main())
