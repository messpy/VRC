@echo off
setlocal enabledelayedexpansion

echo ==============================
echo     �����Z�b�g�A�b�v�J�n
echo ==============================

rem --- .NET �����^�C���̊m�F ---
echo Checking .NET runtimes...
dotnet --list-runtimes > runtimes.txt
findstr /C:"Microsoft.NETCore.App 8." runtimes.txt >nul
if errorlevel 1 (
    echo.
    echo [ERROR] .NET 8.x �����^�C����������܂���ł����B
    echo https://dotnet.microsoft.com/ja-jp/download/dotnet/8.0 ���� .NET 8.x ���C���X�g�[�����Ă��������B
    pause
    exit /b 1
) else (
    echo .NET 8.x �����^�C����������܂����B
)
del runtimes.txt

rem --- vpm �̊m�F ---
echo.
echo Checking vpm...
vpm --version >nul 2>&1
if errorlevel 1 (
    echo.
    echo vpm ���C���X�g�[������Ă��܂���B
    set /p choice="�����ŃC���X�g�[�����܂����H (y/n): "
    if /i "%choice%"=="y" (
        echo Installing vpm...
        dotnet tool install --global vrchat.vpm.cli
        if errorlevel 1 (
            echo [ERROR] vpm �̃C���X�g�[���Ɏ��s���܂����B
            pause
            exit /b 1
        ) else (
            echo vpm �̃C���X�g�[�����������܂����B
        )
    ) else (
        echo vpm ���蓮�ŃC���X�g�[�����Ă�������:
        echo   dotnet tool install --global vrchat.vpm.cli
        pause
        exit /b 1
    )
) else (
    echo vpm �͊��ɃC���X�g�[������Ă��܂��B
    set /p choice="vpm ���A�b�v�f�[�g���܂����H (y/n): "
    if /i "%choice%"=="y" (
        echo Updating vpm...
        dotnet tool update --global vrchat.vpm.cli
        if errorlevel 1 (
            echo [ERROR] vpm �̃A�b�v�f�[�g�Ɏ��s���܂����B
            pause
            exit /b 1
        ) else (
            echo vpm �̃A�b�v�f�[�g���������܂����B
        )
    )
)

echo.
echo ==============================
echo �����Z�b�g�A�b�v���������܂����B
pause
