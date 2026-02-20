import subprocess
import sys

DEBUG = True  # デバッグモード：True にすると実行前にコマンド内容を表示します

def run_command(command, capture_output=True, cwd=None):
    """指定されたコマンドを実行し、標準出力を返す（失敗時は None）"""
    if DEBUG:
        print("実行コマンド:", " ".join(command))
    try:
        result = subprocess.run(command, capture_output=capture_output, text=True, check=True, cwd=cwd)
        if capture_output:
            return result.stdout.strip() if result.stdout is not None else ""
        else:
            return ""
    except subprocess.CalledProcessError as e:
        print("コマンド実行エラー:", e)
        return None
    except FileNotFoundError:
        print("コマンドが見つかりません:", command[0])
        return None

def check_dotnet():
    print("【.NET ランタイム の確認】")
    # dotnet --list-runtimes でインストール済みランタイム一覧を取得
    runtimes = run_command(["dotnet", "--list-runtimes"])
    if runtimes is None:
        print("エラー: .NET SDK またはランタイムがインストールされていないか、PATH に含まれていません。")
        print("https://dotnet.microsoft.com/download から .NET SDK をインストールしてください。")
        input("Enter キーを押して終了します...")
        sys.exit(1)
    else:
        found = False
        for line in runtimes.splitlines():
            # Microsoft.NETCore.App のバージョンが 8.x かチェック
            if line.startswith("Microsoft.NETCore.App 8."):
                found = True
                break
        if not found:
            print("エラー: vpm は .NET 8.x が必要です。")
            print("https://dotnet.microsoft.com/ja-jp/download/dotnet/8.0 から .NET 8.x をインストールしてください。")
            input("Enter キーを押して終了します...")
            sys.exit(1)
        else:
            print("必要な .NET 8.x ランタイムが見つかりました。")

def check_vpm():
    print("\n【vpm の確認】")
    version = run_command(["vpm", "--version"])
    if version is None:
        print("vpm がインストールされていません。")
        return False
    else:
        print(f"vpm バージョン {version} が見つかりました。")
        return True

def install_vpm():
    print("\n【vpm のインストール】")
    run_command(["dotnet", "tool", "install", "--global", "vrchat.vpm.cli"], capture_output=False)
    print("vpm のインストールが完了しました。")

def update_vpm():
    print("\n【vpm のアップデート】")
    run_command(["dotnet", "tool", "update", "--global", "vrchat.vpm.cli"], capture_output=False)
    print("vpm のアップデートが完了しました。")

def main():
    print("===== 初期セットアップ開始 =====\n")
    # .NET ランタイムの確認（Microsoft.NETCore.App の 8.x が必要）
    check_dotnet()
    
    # vpm の確認
    vpm_installed = check_vpm()
    
    if not vpm_installed:
        choice = input("\nvpm が見つかりませんでした。自動でインストールしますか？ (y/n): ").strip().lower()
        if choice == "y":
            install_vpm()
        else:
            print("vpm を手動でインストールしてください:")
            print("dotnet tool install --global vrchat.vpm.cli")
            sys.exit(1)
    else:
        choice = input("\nvpm をアップデートしますか？ (y/n): ").strip().lower()
        if choice == "y":
            update_vpm()
    
    print("\n初期セットアップが完了しました。")
    input("Enter キーを押して終了します...")

if __name__ == '__main__':
    main()
