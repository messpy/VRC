import os
import shutil
import tarfile
import sys
import datetime
import tkinter as tk
from tkinter import filedialog

def log_message(message):
    """日時付きのログ出力"""
    timestamp = datetime.datetime.now().strftime("%Y-%m-%d %H:%M:%S")
    full_message = f"{timestamp} - {message}"
    print(full_message)

def extract_unitypackage(unitypackage_path):
    """
    指定された unitypackage ファイルを展開し、
    各アセットを元のパス構造に再配置する関数。
    """
    if not unitypackage_path.lower().endswith(".unitypackage"):
        log_message(f"{unitypackage_path} はunitypackageファイルではありません。")
        return

    base_dir = os.path.dirname(unitypackage_path)
    file_name = os.path.splitext(os.path.basename(unitypackage_path))[0]
    # 一旦、全内容を一時フォルダに展開
    temp_extract_dir = os.path.join(base_dir, file_name + "_temp_extracted")
    os.makedirs(temp_extract_dir, exist_ok=True)

    try:
        with tarfile.open(unitypackage_path, "r:gz") as tar:
            tar.extractall(path=temp_extract_dir)
        log_message(f"{unitypackage_path} の内容を一時フォルダ {temp_extract_dir} に展開しました。")
    except Exception as e:
        log_message(f"{unitypackage_path} の展開に失敗しました: {e}")
        return

    # 再配置先のディレクトリ（元のアセット構造を復元するためのフォルダ）
    output_dir = os.path.join(base_dir, file_name + "_reassembled")
    os.makedirs(output_dir, exist_ok=True)

    # temp_extract_dir 内の各フォルダを処理
    for entry in os.listdir(temp_extract_dir):
        entry_path = os.path.join(temp_extract_dir, entry)
        if os.path.isdir(entry_path):
            # 各アセットフォルダ内の「pathname」ファイルを読み込む
            pathname_file = os.path.join(entry_path, "pathname")
            asset_file = os.path.join(entry_path, "asset")
            if os.path.exists(pathname_file) and os.path.exists(asset_file):
                with open(pathname_file, "r", encoding="utf-8") as f:
                    original_path = f.read().strip()
                # original_path が "Assets/SomeFolder/SomeAsset.ext" などになっている場合
                dest_path = os.path.join(output_dir, original_path)
                dest_dir = os.path.dirname(dest_path)
                os.makedirs(dest_dir, exist_ok=True)
                try:
                    shutil.copy2(asset_file, dest_path)
                    log_message(f"アセットを {dest_path} に再配置しました。")
                except Exception as e:
                    log_message(f"{asset_file} の再配置に失敗しました: {e}")
            else:
                log_message(f"{entry_path} 内に pathname または asset ファイルが見つかりませんでした。")

    # 一時フォルダを削除する（必要に応じて）
    try:
        shutil.rmtree(temp_extract_dir)
        log_message(f"一時フォルダ {temp_extract_dir} を削除しました。")
    except Exception as e:
        log_message(f"一時フォルダ {temp_extract_dir} の削除に失敗しました: {e}")

def select_files_via_dialog():
    """
    ファイル選択ダイアログを表示し、ユーザーが選択した .unitypackage ファイルのリストを返す
    """
    root = tk.Tk()
    root.withdraw()  # メインウィンドウを非表示にする
    file_paths = filedialog.askopenfilenames(
        title="unitypackageファイルを選択してください",
        filetypes=[("Unity Package", "*.unitypackage")]
    )
    return list(file_paths)

if __name__ == "__main__":
    # コマンドライン引数が指定されていればそのファイル、なければダイアログで選択
    if len(sys.argv) < 2:
        log_message("コマンドライン引数が指定されなかったため、ファイル選択ダイアログを表示します。")
        unitypackage_files = select_files_via_dialog()
        if not unitypackage_files:
            log_message("ファイルが選択されませんでした。処理を終了します。")
            sys.exit(1)
    else:
        unitypackage_files = sys.argv[1:]

    # 選択または指定された各ファイルについて処理
    for file_path in unitypackage_files:
        log_message(f"処理開始: {file_path}")
        extract_unitypackage(file_path)
        log_message(f"処理終了: {file_path}")
