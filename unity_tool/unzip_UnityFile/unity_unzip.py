import os
import stat
import shutil
import sys
import tarfile
import zipfile

# ログファイルの設定
log_file = "extraction_log.txt"
zip_log_file = "extracted_zips.txt"

# ログ書き込み関数
def write_log(message):
    print(message)
    with open(log_file, "a", encoding="utf-8") as log:
        log.write(message + "\n")

# 解凍済みの `.zip` を記録するファイルの初期化
if not os.path.exists(zip_log_file):
    with open(zip_log_file, "w", encoding="utf-8") as f:
        f.write("")

# `.zip` 解凍履歴を取得
with open(zip_log_file, "r", encoding="utf-8") as f:
    extracted_zips = set(f.read().splitlines())

# スクリプトのあるディレクトリを基準にする
script_dir = os.path.dirname(os.path.abspath(__file__))
outputDir = os.path.join(script_dir, "extracted_assets")
workingDir = os.path.join(script_dir, ".working")

# 既存の出力ディレクトリを確認
if os.path.exists(outputDir):
    write_log(f"⚠ Output directory '{outputDir}' already exists. Overwriting...")
    shutil.rmtree(outputDir)

if os.path.exists(workingDir):
    shutil.rmtree(workingDir)

os.makedirs(outputDir, exist_ok=True)

# `.zip` を解凍する関数（無限ループ防止）
def extract_zip(zip_path, extract_to):
    if zip_path in extracted_zips:
        write_log(f" Already extracted: {zip_path}")
        return

    try:
        with zipfile.ZipFile(zip_path, 'r') as zip_ref:
            zip_ref.extractall(extract_to)
        write_log(f" Extracted ZIP: {zip_path} → {extract_to}")

        # 解凍済みリストに追加
        extracted_zips.add(zip_path)
        with open(zip_log_file, "a", encoding="utf-8") as f:
            f.write(zip_path + "\n")

    except Exception as e:
        write_log(f" Failed to extract ZIP: {e}")

# `.unitypackage` を解凍する関数
def extract_unitypackage(unitypackage, extract_to):
    unitypackage_name = os.path.splitext(os.path.basename(unitypackage))[0]
    unitypackage_output = os.path.join(extract_to, unitypackage_name)

    try:
        with tarfile.open(unitypackage, 'r') as tar:
            tar.extractall(unitypackage_output)
        write_log(f" Extracted UnityPackage: {unitypackage} → {unitypackage_output}")
    except Exception as e:
        write_log(f" Failed to extract .unitypackage: {e}")
        return

    # Unity のパス情報を解析
    mapping = {}
    for i in os.listdir(unitypackage_output):
        rootFile = os.path.join(unitypackage_output, i)
        asset = i

        if os.path.isdir(rootFile):
            realPath = ""
            hasAsset = False

            for j in os.listdir(rootFile):
                if j == "pathname":
                    with open(os.path.join(rootFile, j), encoding="utf8") as f:
                        realPath = f.readline().strip()
                elif j == "asset":
                    hasAsset = True

            if hasAsset:
                mapping[asset] = realPath

    # `.unity` ファイルを適切な場所に移動
    unity_dest = os.path.join(extract_to, "unity_files")
    os.makedirs(unity_dest, exist_ok=True)

    for asset, path in mapping.items():
        path, filename = os.path.split(path)
        destFile = os.path.join(unity_dest, filename)
        source = os.path.join(unitypackage_output, asset, "asset")

        shutil.move(source, destFile)

        # 権限を適切に設定（macOS / Linux）
        os.chmod(destFile, stat.S_IRUSR | stat.S_IWUSR | stat.S_IRGRP | stat.S_IROTH)

        write_log(f" Extracted Unity file: {filename} → {unity_dest}")

# スクリプトと同じディレクトリ内の `.zip` を探して解凍
for root, _, files in os.walk(script_dir):
    for file in files:
        if file.endswith(".zip"):
            zip_path = os.path.join(root, file)
            extract_zip(zip_path, outputDir)

# `.zip` の中に `.unitypackage` がある場合も解凍
for root, _, files in os.walk(outputDir):
    for file in files:
        if file.endswith(".unitypackage"):
            unitypackage_path = os.path.join(root, file)
            extract_unitypackage(unitypackage_path, outputDir)

# `.unitypackage` がそのまま置かれていた場合も解凍
for root, _, files in os.walk(script_dir):
    for file in files:
        if file.endswith(".unitypackage"):
            unitypackage_path = os.path.join(root, file)
            extract_unitypackage(unitypackage_path, outputDir)

# 解凍後のファイルを一覧表示
write_log("\n📂 解凍後のファイル一覧:")
for root, dirs, files in os.walk(outputDir):
    level = root.replace(outputDir, "").count(os.sep)
    indent = " " * (level * 4)
    write_log(f"{indent}📁 {os.path.basename(root)}/")
    subindent = " " * ((level + 1) * 4)
    for file in files:
        write_log(f"{subindent}{file}")

write_log("\nExtraction complete!")
