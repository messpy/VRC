@echo off
:: ==========================================
:: VRChat �V�K�v���W�F�N�g�쐬�X�N���v�g
:: - ���[�U�[���v���W�F�N�g�������
:: - �V�KVRChat�v���W�F�N�g���쐬
:: - �w�肵��VPM���|�W�g����ǉ�
:: - VPM�p�b�P�[�W���C���X�g�[��
:: ==========================================

:: VPM�̎��s�t�@�C�����w��
set "VPM_PATH=C:\Users\kenny\.dotnet\tools\vpm.exe"

:: VRChat�v���W�F�N�g�̕ۑ���
set "PROJECT_ROOT=C:\Users\kenny\AppData\Local\VRChatCreatorCompanion\Project Backups"

:: VPM���|�W�g����URL
set "VPM_REPO_URL=https://vcc.vrcfury.com/"

:: �g�p����e���v���[�g�iworld or avatar�j
set "PROJECT_TEMPLATE=world"

:: ���[�U�[�Ƀv���W�F�N�g������͂�����
set /p PROJECT_NAME=�V����VRChat�v���W�F�N�g������͂��Ă�������: 

:: �t���p�X���쐬
set "PROJECT_PATH=%PROJECT_ROOT%\%PROJECT_NAME%"

:: ���ɑ��݂���ꍇ�̓G���[
if exist "%PROJECT_PATH%" (
    echo [ERROR] ���ɓ������O�̃v���W�F�N�g�����݂��܂�: %PROJECT_PATH%
    exit /b 1
)

echo ===========================================
echo VRChat �V�K�v���W�F�N�g���쐬
echo �v���W�F�N�g��: %PROJECT_NAME%
echo �e���v���[�g: %PROJECT_TEMPLATE%
echo ===========================================

:: �V�K�v���W�F�N�g���쐬
"%VPM_PATH%" new "%PROJECT_NAME%" "%PROJECT_TEMPLATE%" --path "%PROJECT_ROOT%"

if %errorlevel% neq 0 (
    echo [ERROR] �v���W�F�N�g�̍쐬�Ɏ��s���܂����I
    exit /b 1
)

:: �v���W�F�N�g�̃t���p�X���m�F
if not exist "%PROJECT_PATH%" (
    echo [ERROR] �v���W�F�N�g�t�H���_��������܂���: %PROJECT_PATH%
    exit /b 1
)

:: VPM���|�W�g����ǉ�
echo ===========================================
echo VRChat �v���W�F�N�g�Ƀ��|�W�g����ǉ�
echo �v���W�F�N�g: %PROJECT_PATH%
echo ���|�W�g��: %VPM_REPO_URL%
echo ===========================================

"%VPM_PATH%" add repo "%VPM_REPO_URL%" --project "%PROJECT_PATH%"

if %errorlevel% neq 0 (
    echo [ERROR] ���|�W�g���̒ǉ��Ɏ��s���܂����I
    exit /b 1
)

:: �ǉ����ꂽ���|�W�g�����m�F
echo [INFO] �ǉ����ꂽ���|�W�g�����m�F��...
"%VPM_PATH%" list repos --project "%PROJECT_PATH%"

:: VPM�p�b�P�[�W���C���X�g�[��
echo ===========================================
echo VPM�p�b�P�[�W���C���X�g�[��
echo ===========================================
"%VPM_PATH%" install --project "%PROJECT_PATH%"

if %errorlevel% neq 0 (
    echo [ERROR] �p�b�P�[�W�̃C���X�g�[���Ɏ��s���܂����I
    exit /b 1
)

echo ===========================================
echo [SUCCESS] �V�K�v���W�F�N�g�̍쐬�ƃ��|�W�g���̒ǉ����������܂����I
echo ===========================================

exit /b 0
