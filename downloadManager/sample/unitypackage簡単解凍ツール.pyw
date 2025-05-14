import os
import shutil
import tarfile
import sys
import datetime
import tkinter as tk
from tkinter import filedialog

# ---------------------------
# ログ出力用のグローバル変数設定
# ---------------------------
LOG_OUTPUT_FILE = None
# sys.argv[0] はスクリプト名なので、ユーザー引数は sys.argv[1] 以降
if len(sys.argv) > 1 and sys.argv[1].lower().endswith(".txt"):
    log_candidate = sys.argv[1]
    # ファイルが存在するかチェック、存在しなければ作成する
    if os.path.exists(log_candidate):
        if os.path.getsize(log_candidate) == 0:
            LOG_OUTPUT_FILE = log_candidate
    else:
        open(log_candidate, "w", encoding="utf-8").close()
        LOG_OUTPUT_FILE = log_candidate
    # ログ用引数を削除して、残りを unitypackage の引数として扱う
    sys.argv.pop(1)

def log_message(message, to_file=False):
    """日時付きのログ出力。グローバル変数 LOG_OUTPUT_FILE が設定されていれば、そこに出力します。"""
    timestamp = datetime.datetime.now().strftime("%Y-%m-%d %H:%M:%S")
    full_message = f"{timestamp} - {message}"
    if LOG_OUTPUT_FILE:
        with open(LOG_OUTPUT_FILE, "a", encoding="utf-8") as f:
            f.write(full_message + "\n")
    else:
        print(full_message)

def safe_extract(tar, path=".", members=None, *, numeric_owner=False):
    """
    tarfile.extractall の前に各メンバーのパスが安全か確認する関数。
    パストラバーサル攻撃を防ぎます。
    """
    for member in tar.getmembers():
        member_path = os.path.join(path, member.name)
        abs_path = os.path.abspath(path)
        abs_member_path = os.path.abspath(member_path)
        if not abs_member_path.startswith(abs_path):
            raise Exception("Tarファイル内のパスに不正な値が含まれています: " + member.name)
    tar.extractall(path, members, numeric_owner=numeric_owner)

def sanitize_path(path):
    """
    original_path に対して基本的なサニタイズを実施します。
    先頭のパス区切り文字を除去し、'..' などの上位ディレクトリ指定を排除します。
    """
    sanitized = os.path.normpath(path).lstrip(os.sep)
    parts = []
    for part in sanitized.split(os.sep):
        if part in ('..', ''):
            continue
        parts.append(part)
    return os.path.join(*parts)

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
    
    # 一時フォルダ（展開用）と再配置先フォルダを別名にする
    temp_extract_dir = os.path.join(base_dir, "temp_HolidayPack_" + file_name)
    output_dir = os.path.join(base_dir, "unipack_" + file_name)
    os.makedirs(temp_extract_dir, exist_ok=True)
    os.makedirs(output_dir, exist_ok=True)

    try:
        with tarfile.open(unitypackage_path, "r:gz") as tar:
            safe_extract(tar, path=temp_extract_dir)
        log_message(f"{unitypackage_path} の内容を一時フォルダ {temp_extract_dir} に展開しました。")
    except Exception as e:
        log_message(f"{unitypackage_path} の展開に失敗しました: {e}", to_file=True)
        return

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
                # original_path のサニタイズ処理
                original_path = sanitize_path(original_path)
                dest_path = os.path.join(output_dir, original_path)
                dest_dir = os.path.dirname(dest_path)
                os.makedirs(dest_dir, exist_ok=True)
                try:
                    shutil.copy2(asset_file, dest_path)
                    log_message(f"アセットを {dest_path} に再配置しました。")
                except Exception as e:
                    log_message(f"{asset_file} の再配置に失敗しました: {e}", to_file=True)
            else:
                log_message(f"{entry_path} 内に pathname または asset ファイルが見つかりませんでした。", to_file=True)

    # 一時フォルダを削除する（必要に応じて）
    try:
        shutil.rmtree(temp_extract_dir)
        log_message(f"一時フォルダ {temp_extract_dir} を削除しました。")
    except Exception as e:
        log_message(f"一時フォルダ {temp_extract_dir} の削除に失敗しました: {e}", to_file=True)

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
    root.destroy()  # 必ず破棄する
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

    sys.exit(0)
