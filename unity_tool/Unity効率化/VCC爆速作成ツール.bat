@echo off
:: ==========================================
:: VRChat 新規プロジェクト作成スクリプト
:: - ユーザーがプロジェクト名を入力
:: - 新規VRChatプロジェクトを作成
:: - 指定したVPMリポジトリを追加
:: - VPMパッケージをインストール
:: ==========================================

:: VPMの実行ファイルを指定
set "VPM_PATH=C:\Users\kenny\.dotnet\tools\vpm.exe"

:: VRChatプロジェクトの保存先
set "PROJECT_ROOT=C:\Users\kenny\AppData\Local\VRChatCreatorCompanion\Project Backups"

:: VPMリポジトリのURL
set "VPM_REPO_URL=https://vcc.vrcfury.com/"

:: 使用するテンプレート（world or avatar）
set "PROJECT_TEMPLATE=world"

:: ユーザーにプロジェクト名を入力させる
set /p PROJECT_NAME=新しいVRChatプロジェクト名を入力してください: 

:: フルパスを作成
set "PROJECT_PATH=%PROJECT_ROOT%\%PROJECT_NAME%"

:: 既に存在する場合はエラー
if exist "%PROJECT_PATH%" (
    echo [ERROR] 既に同じ名前のプロジェクトが存在します: %PROJECT_PATH%
    exit /b 1
)

echo ===========================================
echo VRChat 新規プロジェクトを作成
echo プロジェクト名: %PROJECT_NAME%
echo テンプレート: %PROJECT_TEMPLATE%
echo ===========================================

:: 新規プロジェクトを作成
"%VPM_PATH%" new "%PROJECT_NAME%" "%PROJECT_TEMPLATE%" --path "%PROJECT_ROOT%"

if %errorlevel% neq 0 (
    echo [ERROR] プロジェクトの作成に失敗しました！
    exit /b 1
)

:: プロジェクトのフルパスを確認
if not exist "%PROJECT_PATH%" (
    echo [ERROR] プロジェクトフォルダが見つかりません: %PROJECT_PATH%
    exit /b 1
)

:: VPMリポジトリを追加
echo ===========================================
echo VRChat プロジェクトにリポジトリを追加
echo プロジェクト: %PROJECT_PATH%
echo リポジトリ: %VPM_REPO_URL%
echo ===========================================

"%VPM_PATH%" add repo "%VPM_REPO_URL%" --project "%PROJECT_PATH%"

if %errorlevel% neq 0 (
    echo [ERROR] リポジトリの追加に失敗しました！
    exit /b 1
)

:: 追加されたリポジトリを確認
echo [INFO] 追加されたリポジトリを確認中...
"%VPM_PATH%" list repos --project "%PROJECT_PATH%"

:: VPMパッケージをインストール
echo ===========================================
echo VPMパッケージをインストール
echo ===========================================
"%VPM_PATH%" install --project "%PROJECT_PATH%"

if %errorlevel% neq 0 (
    echo [ERROR] パッケージのインストールに失敗しました！
    exit /b 1
)

echo ===========================================
echo [SUCCESS] 新規プロジェクトの作成とリポジトリの追加が完了しました！
echo ===========================================

exit /b 0
