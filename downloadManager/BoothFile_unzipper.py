import os
import json
import zipfile
from datetime import datetime
import re  # 特殊文字を除去するためのライブラリ
import shutil

# 設定
JSON_FILE_NAME = "downloadHistory_AE.json"
LOG_FILE_NAME = "process.log"

def log_message(message):
    """ログメッセージをファイルに書き出す"""
    with open(LOG_FILE_NAME, "a", encoding="utf-8") as log_file:
        log_file.write(f"{datetime.now().strftime('%Y-%m-%d %H:%M:%S')} - {message}\n")

def sanitize_folder_name(name):
    """フォルダ名の特殊文字を除去"""
    return re.sub(r'[<>:"/\\|?*]', '_', name)

def process_download_folder(working_dir):
    """作業フォルダ内のZIPファイルをJSONデータに基づいて処理"""
    json_data = None
    json_file_path = os.path.join(working_dir, JSON_FILE_NAME)

    # ログの初期化
    with open(LOG_FILE_NAME, "w", encoding="utf-8") as log_file:
        log_file.write("=== 処理開始 ===\n")

    # JSONファイルの読み込み
    if os.path.exists(json_file_path):
        with open(json_file_path, "r", encoding="utf-8") as json_file:
            json_data = json.load(json_file)
        log_message(f"JSONファイル {JSON_FILE_NAME} を読み込みました。")
    else:
        log_message(f"JSONファイル {JSON_FILE_NAME} が存在しません。")

    # 作業フォルダ内のファイルを確認
    log_message(f"作業フォルダ: {working_dir}")
    log_message("フォルダ内ファイル一覧:")
    for file_name in os.listdir(working_dir):
        log_message(f" - {file_name}")

    # ZIPファイルを検索して処理
    for file_name in os.listdir(working_dir):
        file_path = os.path.join(working_dir, file_name)
        if zipfile.is_zipfile(file_path):  # ZIPファイル判定
            log_message(f"ZIPファイル検出: {file_name}")
            try:
                handle_zip_file(file_path, working_dir, json_data)
            except Exception as e:
                log_message(f"エラー発生: {file_name} の処理中にエラーが起きました: {e}")
        else:
            continue  # ZIP以外のファイルは完全に無視

def handle_zip_file(zip_file_path, working_dir, json_data):
    """ZIPファイルを解凍し、関連データを記録"""
    log_message(f"解凍開始: {zip_file_path}")
    matched_entry = None
    if json_data:
        for entry in json_data:
            if os.path.basename(zip_file_path) in entry.get("files", []):
                matched_entry = entry
                break

    folder_name = matched_entry["title"] if matched_entry else os.path.splitext(os.path.basename(zip_file_path))[0]
    sanitized_folder_name = sanitize_folder_name(folder_name)
    folder_path = os.path.join(working_dir, sanitized_folder_name)

    log_message(f"解凍先フォルダ名: {sanitized_folder_name}")

    try:
        os.makedirs(folder_path, exist_ok=True)
        with zipfile.ZipFile(zip_file_path, 'r') as zip_ref:
            log_message(f"ZIPファイル内容: {zip_ref.namelist()}")
            for file in zip_ref.namelist():
                try:
                    zip_ref.extract(file, folder_path)
                except Exception as e:
                    log_message(f"ファイル解凍エラー: {file} - {e}")
                    raise e
        log_message(f"解凍成功: {zip_file_path} -> {folder_path}")
        shutil.move(zip_file_path, folder_path)  # ZIPファイルを解凍先フォルダに移動
        log_message(f"ZIPファイル移動完了: {zip_file_path} -> {folder_path}")
    except Exception as e:
        log_message(f"解凍中にエラー: {e}")
        if os.path.exists(folder_path):
            shutil.rmtree(folder_path)  # フォルダを強制削除
        raise e

    record_info(folder_path, zip_file_path, matched_entry)

def record_info(folder_path, zip_file_path, matched_entry):
    """解凍結果やJSON情報を記録"""
    log_message(f"情報記録開始: {folder_path}")
    info_file_path = os.path.join(folder_path, "info.txt")
    with open(info_file_path, "w", encoding="utf-8") as info_file:
        info_file.write(f"ZIPファイル名: {os.path.basename(zip_file_path)}\n")
        info_file.write(f"解凍先フォルダ: {folder_path}\n")
        info_file.write(f"処理日時: {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}\n\n")

        # JSONデータがある場合、その内容を記録
        if matched_entry:
            info_file.write("JSON情報:\n")
            info_file.write(json.dumps(matched_entry, indent=4, ensure_ascii=False))
        else:
            info_file.write("JSON内に該当エントリがありませんでした。\n")

    log_message(f"情報記録完了: {folder_path}")

if __name__ == "__main__":
    try:
        working_dir = os.getcwd()  # 作業ディレクトリの取得
        log_message(f"現在の作業ディレクトリ: {working_dir}")
        process_download_folder(working_dir)
    except Exception as e:
        log_message(f"スクリプト全体でエラーが発生しました: {e}")
