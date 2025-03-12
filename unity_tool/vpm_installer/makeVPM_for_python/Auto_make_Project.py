import json
import os
import subprocess
import sys

def run_command(command, cwd=None):
    """指定されたコマンドを実行し、終了コードを返す"""
    result = subprocess.run(command, cwd=cwd)
    return result.returncode

def main():
    # スクリプトがあるディレクトリを親フォルダとする
    project_path = os.path.dirname(os.path.abspath(__file__))
    print("----------------------Project path:", project_path)
    
    # ユーザーにプロジェクト名を入力させる
    project_name = input("プロジェクト名を入力してください: ").strip()
    print("----------------------Project name:", project_name)
    
    # 親フォルダ内にプロジェクト名と同じ名前のフォルダを作成
    project_dir = os.path.join(project_path, project_name)
    
    # プロジェクトフォルダが存在しなければ、新規作成
    if not os.path.exists(project_dir):
        print(f"Creating VRC Avatar project: {project_name} in {project_path}")
        ret = run_command(["vpm", "new", project_name, "Avatar", "-p", project_path])
        if ret != 0:
            print("Error: プロジェクト作成に失敗しました。")
            sys.exit(1)
    else:
        print(f"Project {project_name} already exists.")
    
    print("------------------Project directory:", project_dir)
    os.chdir(project_dir)
    
    # Packages ディレクトリをチェック（存在しなければ作成）
    packages_dir = os.path.join(project_dir, "Packages")
    if not os.path.exists(packages_dir):
        os.makedirs(packages_dir, exist_ok=True)
    
    # JSONファイルからパッケージ情報を読み込み
    packages_json_path = os.path.join(project_path, "packages.json")
    if not os.path.exists(packages_json_path):
        print(f"Error: packages.json が見つかりません。場所: {packages_json_path}")
        sys.exit(1)
    
    with open(packages_json_path, "r", encoding="utf-8") as f:
        config = json.load(f)
    
    packages = config.get("packages", [])
    if not packages:
        print("パッケージ情報がありません。")
        sys.exit(1)
    
    # JSONに記載された各パッケージについて、enabled が True ならインストール
    for pkg in packages:
        name = pkg.get("name", "Unnamed Package")
        package_id = pkg.get("package_id", "")
        enabled = pkg.get("enabled", True)
        if not enabled:
            print(f"{name} (ID: {package_id}) は無効です。スキップします。")
            continue
        
        pkg_dir = os.path.join("Packages", package_id)
        if not os.path.exists(pkg_dir):
            print(f"{name} (ID: {package_id}) が見つかりません。インストールします...")
            ret = run_command(["vpm", "add", "package", package_id, "-p", project_dir])
            if ret != 0:
                print(f"Error: {name} のインストールに失敗しました。")
        else:
            print(f"{name} (ID: {package_id}) は既にインストールされています。")
    
    print()
    print(f"Avatar project '{project_name}' created and updated successfully!")
    input("Press Enter to exit...")

if __name__ == '__main__':
    main()
