import os
import re
from datetime import datetime
import sys

# 実行ファイルのディレクトリを取得
if getattr(sys, 'frozen', False):
    # PyInstallerでビルドされた場合
    BASE_DIR = sys._MEIPASS
else:
    # 通常のスクリプト実行時
    BASE_DIR = os.path.dirname(os.path.abspath(__file__))

LOG_FILE_NAME = os.path.join(BASE_DIR, "process.log")

def log_message(message):
    """ログメッセージをファイルに書き出す"""
    try:
        log_dir = os.path.dirname(LOG_FILE_NAME)
        if not os.path.exists(log_dir):
            os.makedirs(log_dir)

        with open(LOG_FILE_NAME, "a", encoding="utf-8") as log_file:
            log_file.write(f"{datetime.now().strftime('%Y-%m-%d %H:%M:%S')} - {message}\n")
    except Exception as e:
        print(f"ログの書き込み中にエラーが発生しました: {e}")

def sanitize_folder_name(name):
    """フォルダ名の特殊文字を除去"""
    return re.sub(r'[<>:"/\\|?*]', '_', name)
