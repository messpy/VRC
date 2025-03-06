import os
import shutil
import zipfile
import glob
import tarfile
import datetime

def log_message(message):
    """ログファイル（process_log.txt）へ日時付きのメッセージを出力する"""
    timestamp = datetime.datetime.now().strftime("%Y-%m-%d %H:%M:%S")
    full_message = f"{timestamp} - {message}"
    print(full_message)
    with open(log_file_path, "a", encoding="utf-8") as log_file:
        log_file.write(full_message + "\n")

# 作業ディレクトリ（このスクリプトを実行している場所＝Downloads直下）
working_dir = os.getcwd()
log_file_path = os.path.join(working_dir, "process_log.txt")
log_message(f"作業ディレクトリ: {working_dir}")

# 共通の VRC フォルダ（Downloads/VRC）を作成（存在しなければ）
vrc_common_dir = os.path.join(working_dir, "VRC")
os.makedirs(vrc_common_dir, exist_ok=True)
log_message(f"共通のVRCフォルダ: {vrc_common_dir} を作成/確認しました。")

# 作業ディレクトリ内の全てのZIPファイルを取得
zip_files = glob.glob(os.path.join(working_dir, "*.zip"))
log_message(f"検出されたZIPファイル: {zip_files}")

for zip_file in zip_files:
    log_message("-----")
    zip_filename = os.path.basename(zip_file)
    log_message(f"ZIPファイルの処理開始: {zip_filename}")

    # ZIPファイルと同名のフォルダを作成
    base_name = os.path.splitext(zip_filename)[0]
    target_dir = os.path.join(working_dir, base_name)
    os.makedirs(target_dir, exist_ok=True)
    log_message(f"{target_dir} を作成しました。")
    
    # ZIPファイルを対象フォルダへ移動
    new_zip_path = os.path.join(target_dir, zip_filename)
    try:
        shutil.move(zip_file, new_zip_path)
        log_message(f"{zip_file} を {new_zip_path} に移動しました。")
    except Exception as e:
        log_message(f"{zip_file} の移動に失敗しました: {e}")
        continue
    
    # ZIPファイルを解凍
    try:
        with zipfile.ZipFile(new_zip_path, 'r') as z:
            z.extractall(target_dir)
            log_message(f"{new_zip_path} の解凍に成功しました。")
    except Exception as e:
        log_message(f"{new_zip_path} の解凍時にエラーが発生しました: {e}")
        continue
    
    # target_dir 内を再帰的に探索して、unitypackage または fbx ファイルを探す
    target_files = []
    for root, dirs, files in os.walk(target_dir):
        for file in files:
            lower_file = file.lower()
            if lower_file.endswith(".unitypackage") or lower_file.endswith(".fbx"):
                target_files.append(os.path.join(root, file))
    
    if target_files:
        log_message(f"対象ファイルが見つかりました: {target_files}")
        for file_path in target_files:
            try:
                dest_path = os.path.join(vrc_common_dir, os.path.basename(file_path))
                # 同名ファイルがあれば、名前を変更して上書きを防止
                if os.path.exists(dest_path):
                    base, ext = os.path.splitext(os.path.basename(file_path))
                    dest_path = os.path.join(vrc_common_dir, f"{base}_new{ext}")
                shutil.move(file_path, dest_path)
                log_message(f"{file_path} を {dest_path} に移動しました。")
                
                # unitypackage の場合、内容一覧をテキストファイルとして出力
                if dest_path.lower().endswith(".unitypackage"):
                    try:
                        with tarfile.open(dest_path, "r:gz") as tar:
                            members = tar.getmembers()
                            txt_filename = os.path.basename(dest_path) + "_contents.txt"
                            txt_path = os.path.join(vrc_common_dir, txt_filename)
                            with open(txt_path, "w", encoding="utf-8") as f:
                                for member in members:
                                    f.write(member.name + "\n")
                            log_message(f"{txt_path} に unitypackage の内容を出力しました。")
                    except Exception as e:
                        log_message(f"unitypackage {dest_path} の内容出力に失敗しました: {e}")
            except Exception as e:
                log_message(f"{file_path} の移動に失敗しました: {e}")
    else:
        log_message("unitypackage または fbx ファイルは検出されませんでした。")
