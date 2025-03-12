@echo off
setlocal enabledelayedexpansion

REM �o�b�`�t�@�C��������f�B���N�g����e�t�H���_�Ƃ���
set "project_path=%~dp0"
if "%project_path:~-1%"=="\" set "project_path=%project_path:~0,-1%"

echo ----------------------Project path: %project_path%
REM ���[�U�[�Ƀv���W�F�N�g������͂�����i�t�H���_���Ɠ����ɂȂ�j
set /p project_name=�v���W�F�N�g������͂��Ă�������:
echo ----------------------Project name: %project_name%
REM �e�t�H���_���Ƀv���W�F�N�g���Ɠ������O�̃t�H���_���쐬
set "project_dir=%project_path%\%project_name%"

REM Modular Avatar �̃p�b�P�[�WID�i���|�W�g���o�^�͕s�v�Ƃ���j
set "MA_PACKAGE=nadena.dev.modular-avatar"

REM liltoon �p�ϐ��ݒ�
set "LILTOON_REPO=https://lilxyzw.github.io/vpm.json"
set "LILTOON_PACKAGE=jp.lilxyzw.liltoon"

REM VRCFury �p�ϐ��ݒ�
set "VRCFURY_REPO=https://vcc.vrcfury.com/vpm.json"
set "VRCFURY_PACKAGE=com.vrcfury.vrcfury"

REM Gesture Manager �̃p�b�P�[�WID
set "GESTURE_PACKAGE=vrchat.blackstartx.gesture-manager"

echo -----------------------New project: %project_name%
REM �v���W�F�N�g�t�H���_�����݂��Ȃ���΁A�V�K�쐬
if not exist "%project_dir%" (
    echo Creating VRC Avatar project: %project_name% in %project_path%
    REM -p �ɐe�t�H���_���w�肷��ƁAvpm new �͂��̃t�H���_���Ƀv���W�F�N�g���Ɠ����̃t�H���_���쐬����
    vpm new "%project_name%" Avatar -p "%project_path%"
) else (
    echo Project %project_name% already exists.
)
echo ------------------Project directory: %project_dir%
REM �v���W�F�N�g�t�H���_�ֈړ�
cd /d "%project_dir%" || exit /b 1

echo ------------------Project directory: %project_dir%

REM liltoon ���|�W�g���o�^
echo Registering liltoon repository...
vpm add repo %LILTOON_REPO%

REM VRCFury ���|�W�g���o�^
echo Registering VRCFury repository...
vpm add repo %VRCFURY_REPO%

REM Modular Avatar �̃C���X�g�[��
if not exist "Packages\%MA_PACKAGE%" (
    echo Modular Avatar not found. Installing Modular Avatar...
    vpm add package %MA_PACKAGE% -p "%project_dir%"
) else (
    echo Modular Avatar is already installed.
)

REM liltoon �̃C���X�g�[��
if not exist "Packages\%LILTOON_PACKAGE%" (
    echo liltoon not found. Installing liltoon...
    vpm add package %LILTOON_PACKAGE% -p "%project_dir%"
) else (
    echo liltoon is already installed.
)

REM VRCFury �̃C���X�g�[��
if not exist "Packages\%VRCFURY_PACKAGE%" (
    echo VRCFury not found. Installing VRCFury...
    vpm add package %VRCFURY_PACKAGE% -p "%project_dir%"
) else (
    echo VRCFury is already installed.
)

REM Gesture Manager �̃C���X�g�[��
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
"C:\Program Files\Unity\Hub\Editor\2022.3.22f1\Editor\Unity.exe" -projectPath "%project_dir%"

echo.
pause
