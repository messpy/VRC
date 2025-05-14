import os
import sys
import zipfile
import json
import time
from datetime import datetime
import tarfile
import shutil
from utils import log_message, sanitize_folder_name

# キャッシュ生成を無効化
sys.dont_write_bytecode = True

# JSONファイル名
JSON_FILE_NAME = "downloadHistory_AE.json"
IGNORED_DIRECTORIES = ["__pycache__", "logs"]  # 無視するディレクトリ

def extract_unitypackage(unitypackage_path, output_dir):
    """unitypackageファイルを解凍して再配置"""
    try:
        with tarfile.open(unitypackage_path, "r:gz") as tar:
            temp_extract_dir = os.path.join(output_dir, "temp")
            os.makedirs(temp_extract_dir, exist_ok=True)
            tar.extractall(temp_extract_dir)
            log_message(f"{unitypackage_path} を一時フォルダ {temp_extract_dir} に展開しました。")

            for entry in os.listdir(temp_extract_dir):
                entry_path = os.path.join(temp_extract_dir, entry)
                if os.path.isdir(entry_path):
                    pathname_file = os.path.join(entry_path, "pathname")
                    asset_file = os.path.join(entry_path, "asset")
                    if os.path.exists(pathname_file) and os.path.exists(asset_file):
                        with open(pathname_file, "r", encoding="utf-8") as f:
                            original_path = sanitize_folder_name(f.read().strip())
                        dest_path = os.path.join(output_dir, original_path)
                        os.makedirs(os.path.dirname(dest_path), exist_ok=True)
                        shutil.copy2(asset_file, dest_path)
                        log_message(f"アセットを {dest_path} に再配置しました。")
            shutil.rmtree(temp_extract_dir)
            log_message(f"一時フォルダ {temp_extract_dir} を削除しました。")
    except Exception as e:
        log_message(f"{unitypackage_path} の解凍に失敗しました: {e}")

def load_json_data(json_file_path):
    """JSONファイルを読み込む"""
    if not os.path.exists(json_file_path):
        log_message(f"JSONファイルが見つかりません: {json_file_path}")
        return []
    try:
        with open(json_file_path, "r", encoding="utf-8") as f:
            data = json.load(f)
            log_message(f"JSONファイルを読み込みました: {json_file_path}")
            return data
    except Exception as e:
        log_message(f"JSONファイルの読み込み中にエラーが発生しました: {e}")
        return []

def get_title_from_json(zip_file_name, json_data):
    """JSONデータからZIPファイル名に対応するタイトルを取得"""
    for entry in json_data:
        if "files" in entry and zip_file_name in entry["files"]:
            title = entry.get("title", "Untitled")
            sanitized_title = sanitize_folder_name(title)
            return sanitized_title, entry
    return None, None

def rename_folder(original_folder, new_name):
    """フォルダ名を変更する"""
    parent_dir = os.path.dirname(original_folder)
    new_folder_path = os.path.join(parent_dir, new_name)
    try:
        os.rename(original_folder, new_folder_path)
        log_message(f"フォルダ名を変更しました: {original_folder} -> {new_folder_path}")
        return new_folder_path
    except Exception as e:
        log_message(f"フォルダ名の変更中にエラーが発生しました: {e}")
        return original_folder

def create_info_file(folder_path, zip_file_name, json_entry):
    """infoファイルを作成する"""
    info_file_path = os.path.join(folder_path, "info.txt")
    try:
        with open(info_file_path, "w", encoding="utf-8") as f:
            f.write(f"ZIPファイル名: {zip_file_name}\n")
            f.write(f"処理日時: {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}\n\n")
            f.write("JSON情報:\n")
            f.write(json.dumps(json_entry, ensure_ascii=False, indent=4))
        log_message(f"infoファイルを作成しました: {info_file_path}")
    except Exception as e:
        log_message(f"infoファイルの作成中にエラーが発生しました: {e}")

def handle_unitypackage_file(unitypackage_path, working_dir):
    """unitypackageファイルを解凍"""
    log_message(f"unitypackageファイル検出: {unitypackage_path}")
    output_dir = os.path.join(working_dir, os.path.splitext(os.path.basename(unitypackage_path))[0])
    os.makedirs(output_dir, exist_ok=True)
    extract_unitypackage(unitypackage_path, output_dir)  # extract_unitypackageを呼び出し

def handle_zip_file(zip_file_path, working_dir, json_data):
    """ZIPファイルを処理"""
    log_message(f"ZIPファイル検出: {zip_file_path}")
    output_dir = os.path.join(working_dir, os.path.splitext(os.path.basename(zip_file_path))[0])
    os.makedirs(output_dir, exist_ok=True)
    try:
        with zipfile.ZipFile(zip_file_path, 'r') as zip_ref:
            zip_ref.extractall(output_dir)
            log_message(f"ZIPファイルを解凍しました: {output_dir}")
            log_message(f"解凍されたファイル一覧: {os.listdir(output_dir)}")
    except Exception as e:
        log_message(f"ZIPファイルの解凍中にエラーが発生しました: {e}")
        return

    # JSONデータを使用してフォルダ名を修正
    zip_file_name = os.path.basename(zip_file_path)
    new_name, json_entry = get_title_from_json(zip_file_name, json_data)
    if new_name:
        renamed_folder = rename_folder(output_dir, new_name)
        create_info_file(renamed_folder, zip_file_name, json_entry)
    else:
        log_message(f"JSONデータに対応するタイトルが見つかりませんでした: {zip_file_name}")

def process_all_zip_files(working_dir, json_data):
    """作業ディレクトリ内のすべてのZIPファイルを処理"""
    zip_files = [f for f in os.listdir(working_dir) if f.lower().endswith(".zip") and f not in IGNORED_DIRECTORIES]
    if not zip_files:
        log_message("ZIPファイルが見つかりませんでした。")
        return

    for zip_file in zip_files:
        zip_file_path = os.path.join(working_dir, zip_file)
        handle_zip_file(zip_file_path, working_dir, json_data)

def remove_ignored_directories(working_dir):
    """無視するディレクトリを削除"""
    for dir_name in IGNORED_DIRECTORIES:
        dir_path = os.path.join(working_dir, dir_name)
        if os.path.exists(dir_path) and os.path.isdir(dir_path):
            try:
                os.rmdir(dir_path)
                log_message(f"不要なディレクトリを削除しました: {dir_path}")
            except Exception as e:
                log_message(f"ディレクトリの削除中にエラーが発生しました: {e}")

if __name__ == "__main__":
    try:
        # スクリプトの場所を作業ディレクトリに設定
        script_dir = os.path.dirname(os.path.abspath(sys.argv[0]))
        os.chdir(script_dir)  # 作業ディレクトリを変更
        working_dir = os.getcwd()
        print(f"作業ディレクトリ: {working_dir}")
        log_message(f"作業ディレクトリをスクリプトの場所に設定: {working_dir}")

        # 自分自身の更新日時を現在の日時に変更
        if getattr(sys, 'frozen', False):
            exe_path = sys.executable  # 実行中のexeファイルのパス
            current_time = time.time()
            os.utime(exe_path, (current_time, current_time))
        else:
            current_time = time.time()
            os.utime(sys.argv[0], (current_time, current_time))
            log_message(f"自身の更新日時を現在の日時に変更しました: {sys.argv[0]}")

        # 不要なディレクトリを削除
        remove_ignored_directories(working_dir)

        # JSONファイルを読み込む
        json_file_path = os.path.join(working_dir, JSON_FILE_NAME)
        json_data = load_json_data(json_file_path)

        # コマンドライン引数でファイルを指定
        if len(sys.argv) > 1:
            input_file = sys.argv[1]
            if input_file.lower().endswith(".unitypackage"):
                print(f"Unitypackage: {input_file}")
                handle_unitypackage_file(input_file, working_dir)
            elif zipfile.is_zipfile(input_file):
                print(f"Zip解凍: {input_file}")
                handle_zip_file(input_file, working_dir, json_data)
            else:
                print(f"エラー: 対応していないファイル形式です: {input_file}")
        else:
            # 引数がない場合は作業ディレクトリ内のZIPファイルを処理
            process_all_zip_files(working_dir, json_data)

    except Exception as e:
        log_message(f"スクリプト全体でエラーが発生しました: {e}")
