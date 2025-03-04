import os
import shutil
import zipfile
import glob

# 作業用フォルダ（このスクリプトを実行するディレクトリ＝Downloads直下）
working_dir = os.getcwd()
print("作業ディレクトリ:", working_dir)

# 処理済みファイルを移動するフォルダを定義・作成
processed_dir = os.path.join(working_dir, "Processed")
os.makedirs(processed_dir, exist_ok=True)

# 作業用フォルダ内の全ての ZIP ファイルを取得
zip_files = glob.glob(os.path.join(working_dir, "*.zip"))
print("検出されたZIPファイル:", zip_files)

for zip_file in zip_files:
    # zip ファイルのファイル名（拡張子を除く）
    base_name = os.path.splitext(os.path.basename(zip_file))[0]
    # zip と同じ名前のフォルダを作成
    target_dir = os.path.join(working_dir, base_name)
    os.makedirs(target_dir, exist_ok=True)
    print(f"{target_dir} を作成しました。")
    
    # zip ファイルを作成したフォルダに移動
    new_zip_path = os.path.join(target_dir, os.path.basename(zip_file))
    try:
        shutil.move(zip_file, new_zip_path)
        print(f"{zip_file} を {new_zip_path} に移動しました。")
    except Exception as e:
        print(f"{zip_file} の移動に失敗しました: {e}")
        continue

    # zip ファイルを解凍
    try:
        with zipfile.ZipFile(new_zip_path, 'r') as z:
            z.extractall(target_dir)
            print(f"{new_zip_path} の解凍に成功しました。")
    except RuntimeError as e:
        # 暗号化されたファイルの場合、エラーメッセージに "encrypted" が含まれるのでスキップ
        if "encrypted" in str(e).lower():
            print(f"{os.path.basename(zip_file)} はパスワード保護されているためスキップします。")
        else:
            print(f"{new_zip_path} の解凍時にRuntimeErrorが発生しました: {e}")
    except Exception as e:
        print(f"{new_zip_path} の解凍時に予期せぬエラーが発生しました: {e}")

    # ZIPファイルの処理が完了したら、target_dir ごと processed_dir へ移動
    dest = os.path.join(processed_dir, os.path.basename(target_dir))
    try:
        # 同じ名前のフォルダがすでにあれば上書きしないように名前を変更する
        if os.path.exists(dest):
            dest += "_new"
        shutil.move(target_dir, dest)
        print(f"{target_dir} を {dest} に移動しました。")
    except Exception as e:
        print(f"{target_dir} の移動に失敗しました: {e}")
