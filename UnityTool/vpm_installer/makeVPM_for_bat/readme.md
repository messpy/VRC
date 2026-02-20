初期セットアップスクリプト コマンド説明

このドキュメントでは、initial_setup.py 内で実行される各コマンドの目的と動作について説明します。

1. dotnet --list-runtimes

目的インストールされている .NET ランタイムの一覧を表示します。

使用例

dotnet --list-runtimes

詳細このコマンドを実行し、出力内に Microsoft.NETCore.App 8.x の行があるかを確認することで、vpm の実行に必要な .NET 8.x ランタイムがインストールされているかどうかを判定します。

2. vpm --version

目的vpm (VRChat Package Manager CLI) のバージョン情報を表示し、ツールが既にインストールされているかを確認します。

使用例

vpm --version

詳細インストールされていない場合は、エラーメッセージが出力されるため、その結果をもとに自動インストールの処理を行います。

3. dotnet tool install --global vrchat.vpm.cli

目的vpm CLI をグローバルツールとしてインストールします。

使用例

dotnet tool install --global vrchat.vpm.cli

詳細vpm がシステムに存在しない場合、このコマンドで自動的にインストールを行います。インストール完了後は、PATH にツールのパスが追加され、どこからでも vpm コマンドが利用可能になります。

4. dotnet tool update --global vrchat.vpm.cli

目的既にインストールされている vpm CLI を最新バージョンにアップデートします。

使用例

dotnet tool update --global vrchat.vpm.cli

詳細ユーザーにアップデートの意思確認後、実行されます。すでに最新の状態であれば、特に変更は加えられません。