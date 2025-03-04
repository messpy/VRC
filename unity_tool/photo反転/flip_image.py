import sys
from PIL import Image

def main():
    if len(sys.argv) < 2:
        print("Usage: python flip_image.py <image_file>")
        return

    image_path = sys.argv[1]

    try:
        # 画像を開く
        with Image.open(image_path) as img:
            # 左右反転（水平反転）する
            flipped_img = img.transpose(Image.FLIP_LEFT_RIGHT)

            # 出力ファイル名を生成 (元のファイル名に _flipped を追加)
            parts = image_path.rsplit(".", 1)
            if len(parts) == 2:
                output_path = parts[0] + "_flipped." + parts[1]
            else:
                output_path = image_path + "_flipped"

            # 反転画像を保存
            flipped_img.save(output_path)
            print(f"Flipped image saved as: {output_path}")

    except Exception as e:
        print("Error processing image:", e)

if __name__ == "__main__":
    main()
