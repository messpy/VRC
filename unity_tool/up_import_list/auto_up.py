import os
import time
import subprocess
from pywinauto import Application
import pyautogui

import os

# UnityPackageのあるフォルダ
avator = input("Avatorの名前: ")
unitypackage_dir = os.path.join(r"C:\Users\kenny\Documents\VRC\0_USE_Package", avator)

window_title = "Import Unity Package"

# すべての .unitypackage を取得
unitypackages = [f for f in os.listdir(unitypackage_dir) if f.endswith(".unitypackage")]
print(unitypackages)
# リストを表示
if unitypackages:
    print("\n=== インポート可能な UnityPackage ===")
    for idx, package in enumerate(unitypackages, start=1):
        print(f"{idx}. {package}")
else:
    print("インポートする .unitypackage がありません。")

# 各 .unitypackage を開く
for package in unitypackages:
    print(f"インポート開始: {package}")

    # UnityPackage を開く
    package = os.path.join(unitypackage_dir, package)
    subprocess.Popen(["start", "", package], shell=True)

    # ウィンドウを確実に検出するためのリトライループ
    window_found = False
    for _ in range(10):  # 最大10回試行
        try:
            app = Application().connect(title=window_title)
            window = app.window(title=window_title)
            window_found = True
            print(f"ウィンドウを検出: {window_title}")
            break
        except Exception:
            print(f"ウィンドウが見つかりません。再試行中...")
            time.sleep(5)  # 2秒待機して再試行

    if not window_found:
        print(f"最終的に {window_title} が見つかりませんでした。スキップします。")
        continue

    # ウィンドウをアクティブにする
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

    # Importボタンをクリック（ボタンが押されるまでリトライ）
    button_clicked = False
    for _ in range(5):  # 最大5回試行
        pyautogui.click(button_x, button_y)
        print("Importボタンをクリックしました！")
        time.sleep(3)  # 3秒待機して確認

        # ボタンを押したか確認するためにウィンドウが閉じたかチェック
        try:
            app = Application().connect(title=window_title)
            print("Importウィンドウがまだ開いています。もう一度試行...")
        except Exception:
            print("Importウィンドウが閉じました。成功！")
            button_clicked = True
            break

    if not button_clicked:
        print("Importボタンのクリックに失敗しました。手動で確認してください。")

    # インポート完了を待つ
    time.sleep(5)
