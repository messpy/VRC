import os
import time
import subprocess
from pywinauto import Application
import pyautogui

# UnityPackageのあるフォルダ
categoly = input("importするキャラクターを入れてください")
unitypackage_dir = r"C:\Users\kenny\Documents\VRC\0_USE_Package" + "\\" +  categoly

# UnityのImportウィンドウのタイトル
window_title = "Import Unity Package"

# すべての .unitypackage を取得
unitypackages = [os.path.join(unitypackage_dir, f) for f in os.listdir(unitypackage_dir) if f.endswith(".unitypackage")]

if not unitypackages:
    print("インポートする .unitypackage がありません")
    exit()

# 各 .unitypackage を開く
for package in unitypackages:
    print(f"インポート開始: {package}")
    
    # UnityPackage を開く
    subprocess.Popen(["start", "", package], shell=True)
    time.sleep(5)  # ウィンドウが開くまで待つ

    try:
        # Unity の Import ウィンドウを探す
        app = Application().connect(title=window_title)
        window = app.window(title=window_title)

        # ウィンドウをアクティブにする（もしフォーカスが外れていた場合）
        if not window.is_active():
            print(f"{window_title} が非アクティブなのでアクティブにします")
            window.set_focus()
            time.sleep(1)

        # ウィンドウの座標取得 (left, top, right, bottom)
        rect = window.rectangle()
        print(f"ウィンドウ位置: {rect}")

        # 右下のボタンの位置を計算（ウィンドウの右下 - 少し余裕を持たせる）
        button_x = rect.right - 50
        button_y = rect.bottom - 30

        # Importボタンをクリック
        pyautogui.click(button_x, button_y)
        print("Importボタンをクリックしました！")

        # インポート完了を待つ（適宜調整）
        time.sleep(5)

    except Exception as e:
        print(f"ウィンドウが見つかりません: {e}")
