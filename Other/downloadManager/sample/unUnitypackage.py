import os
import shutil
import tarfile
from datetime import datetime

def log_message(message):
    """日時付きのログ出力"""
    timestamp = datetime.now().strftime("%Y-%m-%d %H:%M:%S")
    print(f"{timestamp} - {message}")

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
    temp_extract_dir = os.path.join(base_dir, "temp_" + file_name)
    output_dir = os.path.join(base_dir, "unipack_" + file_name)
    os.makedirs(temp_extract_dir, exist_ok=True)
    os.makedirs(output_dir, exist_ok=True)

    try:
        with tarfile.open(unitypackage_path, "r:gz") as tar:
            for member in tar.getmembers():
                member_path = os.path.join(temp_extract_dir, member.name)
                abs_path = os.path.abspath(temp_extract_dir)
                abs_member_path = os.path.abspath(member_path)
                if not abs_member_path.startswith(abs_path):
                    raise Exception("Tarファイル内のパスに不正な値が含まれています: " + member.name)
            tar.extractall(temp_extract_dir)
        log_message(f"{unitypackage_path} の内容を一時フォルダ {temp_extract_dir} に展開しました。")
    except Exception as e:
        log_message(f"{unitypackage_path} の展開に失敗しました: {e}")
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
                    log_message(f"{asset_file} の再配置に失敗しました: {e}")
            else:
                log_message(f"{entry_path} 内に pathname または asset ファイルが見つかりませんでした。")

    # 一時フォルダを削除する（必要に応じて）
    try:
        shutil.rmtree(temp_extract_dir)
        log_message(f"一時フォルダ {temp_extract_dir} を削除しました。")
    except Exception as e:
        log_message(f"一時フォルダ {temp_extract_dir} の削除に失敗しました: {e}")

# 使用例
if __name__ == "__main__":
    #TEST用
    unitypackage_path = input("解凍する .unitypackage ファイルのパスを入力してください: ").strip()
    if os.path.exists(unitypackage_path):
        extract_unitypackage(unitypackage_path)
    else:
        log_message(f"指定されたファイルが存在しません: {unitypackage_path}")
