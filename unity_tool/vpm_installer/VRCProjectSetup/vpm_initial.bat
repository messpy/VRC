@echo off
setlocal enabledelayedexpansion

echo ==============================
echo     初期セットアップ開始
echo ==============================

rem --- .NET ランタイムの確認 ---
echo Checking .NET runtimes...
dotnet --list-runtimes > runtimes.txt
findstr /C:"Microsoft.NETCore.App 8." runtimes.txt >nul
if errorlevel 1 (
    echo.
    echo [ERROR] .NET 8.x ランタイムが見つかりませんでした。
    echo https://dotnet.microsoft.com/ja-jp/download/dotnet/8.0 から .NET 8.x をインストールしてください。
    pause
    exit /b 1
) else (
    echo .NET 8.x ランタイムが見つかりました。
)
del runtimes.txt

rem --- vpm の確認 ---
echo.
echo Checking vpm...
vpm --version >nul 2>&1
if errorlevel 1 (
    echo.
    echo vpm がインストールされていません。
    set /p choice="自動でインストールしますか？ (y/n): "
    if /i "%choice%"=="y" (
        echo Installing vpm...
        dotnet tool install --global vrchat.vpm.cli
        if errorlevel 1 (
            echo [ERROR] vpm のインストールに失敗しました。
            pause
            exit /b 1
        ) else (
            echo vpm のインストールが完了しました。
        )
    ) else (
        echo vpm を手動でインストールしてください:
        echo   dotnet tool install --global vrchat.vpm.cli
        pause
        exit /b 1
    )
) else (
    echo vpm は既にインストールされています。
    set /p choice="vpm をアップデートしますか？ (y/n): "
    if /i "%choice%"=="y" (
        echo Updating vpm...
        dotnet tool update --global vrchat.vpm.cli
        if errorlevel 1 (
            echo [ERROR] vpm のアップデートに失敗しました。
            pause
            exit /b 1
        ) else (
            echo vpm のアップデートが完了しました。
        )
    )
)

echo.
echo ==============================
echo 初期セットアップが完了しました。
pause
