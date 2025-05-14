import os
import tarfile
import shutil
from utils import log_message, sanitize_folder_name

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

def handle_unitypackage_file(unitypackage_path, working_dir):
    """unitypackageファイルを解凍"""
    log_message(f"unitypackageファイル検出: {unitypackage_path}")
    output_dir = os.path.join(working_dir, os.path.splitext(os.path.basename(unitypackage_path))[0])
    os.makedirs(output_dir, exist_ok=True)
    extract_unitypackage(unitypackage_path, output_dir)
