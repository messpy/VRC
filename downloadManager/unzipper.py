import os
import json
import shutil

# 設定
JSON_FILE_NAME = "downloadHistory_AE.json"

def process_files_from_json(json_file_path, working_dir):
    """JSONファイルを基にファイルを処理し、フォルダを作成"""
    if not os.path.exists(json_file_path):
        print(f"JSONファイル {json_file_path} が存在しません。")
        return

    try:
        # JSONファイルを読み込み
        with open(json_file_path, "r", encoding="utf-8") as json_file:
            data = json.load(json_file)

        for item in data:
            title = item.get("title")
            files = item.get("files", [])
            if not title or not files:
                print(f"無効なデータ: {item}")
                continue

            # タイトルに基づいたフォルダ作成
            folder_path = os.path.join(working_dir, title)
            os.makedirs(folder_path, exist_ok=True)
            print(f"フォルダ作成: {folder_path}")

            # ファイルをフォルダに移動
            for file_name in files:
                source_file = os.path.join(working_dir, file_name)
                if os.path.exists(source_file):
                    shutil.move(source_file, folder_path)
                    print(f"ファイル移動: {source_file} -> {folder_path}")
                else:
                    print(f"ファイルが見つかりません: {source_file}")

        # JSONファイルを削除
        os.remove(json_file_path)
        print(f"JSONファイル {json_file_path} を削除しました。")

    except Exception as e:
        print(f"処理中にエラーが発生しました: {e}")

if __name__ == "__main__":
    working_dir = os.getcwd()  # 作業ディレクトリの取得
    json_file_path = os.path.join(working_dir, JSON_FILE_NAME)

    process_files_from_json(json_file_path, working_dir)
