@echo off
setlocal enabledelayedexpansion

REM バッチファイルがあるディレクトリを親フォルダとする
set "project_path=%~dp0"
if "%project_path:~-1%"=="\" set "project_path=%project_pauth:~0,-1%"

echo ----------------------Project path: %project_path%
REM ユーザーにプロジェクト名を入力させる（フォルダ名と同じになる）
set /p project_name=プロジェクト名を入力してください:
echo ----------------------Project name: %project_name%
REM プロジェクトフォルダ（親フォルダ内にプロジェクト名と同じ名前のフォルダを作成）
set "project_dir=%project_path%\%project_name%"

REM Modular Avatar 用変数設定
set "MA_REPO=https://modular-avatar.nadena.dev/vpm.json"
set "MA_PACKAGE=nadena.dev.modular-avatar"

REM liltoon 用変数設定
set "LILTOON_REPO=https://lilxyzw.github.io/vpm.json"
set "LILTOON_PACKAGE=jp.lilxyzw.liltoon"

REM VRCFury 用変数設定
set "VRCFURY_REPO=https://vcc.vrcfury.com/vpm.json"
set "VRCFURY_PACKAGE=com.vrcfury.vrcfury"

REM Gesture Manager のパッケージID
set "GESTURE_PACKAGE=vrchat.blackstartx.gesture-manager"

echo -----------------------new project: %project_name%
REM プロジェクトフォルダが存在しなければ、新規作成
if not exist "%project_dir%" (
    echo Creating VRC Avatar project: %project_name% in %project_path%
    REM -p に親フォルダを指定すると、vpm new は親フォルダ内にプロジェクト名と同じフォルダを作成する
    vpm new "%project_name%" Avatar -p "%project_path%"
) else (
    echo Project %project_name% already exists.
)
echo ------------------Project directory: %project_dir%
REM プロジェクトフォルダへ移動
cd /d "%project_dir%" || exit /b 1

REM 各パッケージのリポジトリ登録
echo Registering Modular Avatar repository...
vpm add repo %MA_REPO%

echo Registering liltoon repository...
vpm add repo %LILTOON_REPO%

echo Registering VRCFury repository...
vpm add repo %VRCFURY_REPO%

e
echo ----------------------Modular Avatar: %MA_REPO%
REM Modular Avatar のインストール
if not exist "Packages\%MA_PACKAGE%" (
    echo Modular Avatar not found. Installing Modular Avatar...
    vpm add package %MA_PACKAGE% -p "%project_dir%"
) else (
    echo Modular Avatar is already installed.
)

echo l----------------------iltoon: %LILTOON_REPO%
REM liltoon のインストール
if not exist "Packages\%LILTOON_PACKAGE%" (
    echo liltoon not found. Installing liltoon...
    vpm add package %LILTOON_PACKAGE% -p "%project_dir%"
) else (
    echo liltoon is already installed.
)
echo ----------------------VRCFury: %VRCFURY_REPO%

REM VRCFury のインストール
if not exist "Packages\%VRCFURY_PACKAGE%" (
    echo VRCFury not found. Installing VRCFury...
    vpm add package %VRCFURY_PACKAGE% -p "%project_dir%"
) else (
    echo VRCFury is already installed.
)
echo Gesture Manager: %GESTURE_PACKAGE%

REM Gesture Manager のインストール
if not exist "Packages\%GESTURE_PACKAGE%" (
    echo Gesture Manager not found. Installing Gesture Manager...
    vpm add package %GESTURE_PACKAGE% -p "%project_dir%"
) else (
    echo Gesture Manager is already installed.
)

echo.
echo Avatar project "%project_name%" created and updated successfully with:
echo  - Modular Avatar
echo  - liltoon
echo  - VRCFury
echo  - Gesture Manager
echo.
pause
