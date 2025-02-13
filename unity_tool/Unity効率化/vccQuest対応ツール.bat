@echo off
setlocal enabledelayedexpansion

:: プロジェクトの検索対象フォルダ（必要に応じて変更）
set "SEARCH_DIR=C:\Users\%USERNAME%\Documents\Unity Projects"

:: 検索対象のプロジェクト名（カレントディレクトリから取得）
set "PROJECT_NAME=MyUnityProject"
set "PROJECT_PATH="

:: VPMのパス（必要に応じて変更）
set "VPM_PATH=C:\Users\%USERNAME%\AppData\Local\VRChatCreatorCompanion\vrchat-cli\vpm.exe"

:: 既存のプロジェクトを検索
for /d %%d in ("%SEARCH_DIR%\*") do (
    if exist "%%d\Assets" (
        if exist "%%d\Packages" (
            if exist "%%d\ProjectSettings" (
                if /i "%%~nd"=="%PROJECT_NAME%" (
                    set "PROJECT_PATH=%%d"
                )
            )
        )
    )
)

:: プロジェクトが見つかった場合
if not "%PROJECT_PATH%"=="" (
    echo [INFO] プロジェクトが見つかりました: %PROJECT_PATH%
    echo [INFO] VPMパッケージを追加中...
    
    :: Quest用のパッケージを追加
    "%VPM_PATH%" add modular-avatar
    "%VPM_PATH%" add vrc-android-tools
    "%VPM_PATH%" update

    echo [INFO] VPMパッケージ追加完了！
    exit /b 0
)

:: プロジェクトが見つからなかった場合、新規作成
echo [INFO] プロジェクトが見つかりませんでした。新規作成します。

set "NEW_PROJECT_PATH=%SEARCH_DIR%\%PROJECT_NAME%-Quest"

:: コピー処理（必要なものをコピー）
mkdir "%NEW_PROJECT_PATH%"
robocopy "%SEARCH_DIR%\%PROJECT_NAME%" "%NEW_PROJECT_PATH%" /E

:: VPMでQuest用ツールをセットアップ
cd /d "%NEW_PROJECT_PATH%"
"%VPM_PATH%" add modular-avatar
"%VPM_PATH%" add vrc-android-tools
"%VPM_PATH%" update

echo [INFO] Quest用プロジェクトのセットアップが完了しました！
exit /b 0