import tkinter as tk
from tkinter import ttk, messagebox, filedialog
import json
import time
import datetime
import pyautogui
import pygetwindow as gw
import mouse  # pip install mouse
from PIL import Image
import os

def load_settings():
    """JSON設定ファイルから設定を読み込む"""
    file_path = filedialog.askopenfilename(
        title="JSON設定ファイルを選択",
        filetypes=[("JSON Files", "*.json")]
    )
    if not file_path:
        return
    try:
        with open(file_path, 'r', encoding='utf-8') as f:
            settings = json.load(f)
        if "window_title" in settings:
            title_entry.delete(0, tk.END)
            title_entry.insert(0, settings["window_title"])
        if "num_frames" in settings:
            num_frames_entry.delete(0, tk.END)
            num_frames_entry.insert(0, str(settings["num_frames"]))
        if "frame_delay" in settings:
            frame_delay_entry.delete(0, tk.END)
            frame_delay_entry.insert(0, str(settings["frame_delay"]))
        messagebox.showinfo("設定読み込み", "設定が正常に読み込まれました。")
    except Exception as e:
        messagebox.showerror("読み込みエラー", f"設定ファイルの読み込みに失敗しました:\n{e}")

def select_window():
    """クリックされた位置のウィンドウを取得してタイトル欄に設定する"""
    messagebox.showinfo("ウィンドウ選択", "対象のウィンドウをクリックしてください。\n"
                                        "(このメッセージが閉じられた後に、対象ウィンドウをクリックしてください)")
    mouse.wait('left')
    pos = mouse.get_position()  # (x, y)座標を取得
    windows_at_pos = gw.getWindowsAt(pos[0], pos[1])
    if windows_at_pos:
        win = windows_at_pos[0]
        title_entry.delete(0, tk.END)
        title_entry.insert(0, win.title)
        messagebox.showinfo("ウィンドウ選択", f"選択されたウィンドウ: {win.title}")
    else:
        messagebox.showerror("ウィンドウ選択", "クリック位置にウィンドウが見つかりませんでした。")

def estimate_gif_size():
    """
    キャプチャ前に現在の設定から推定されるGIFのファイルサイズ（MB）と
    総再生時間（秒）を表示する。
    圧縮率は仮に20倍と見積もっています。
    """
    try:
        num_frames = int(num_frames_entry.get())
        frame_delay = float(frame_delay_entry.get())
    except Exception:
        messagebox.showerror("入力エラー", "フレーム数またはフレーム間隔の値が不正です。")
        return

    window_title = title_entry.get()
    windows = gw.getWindowsWithTitle(window_title)
    if not windows:
        messagebox.showerror("エラー", "ウィンドウが見つかりません。\nタイトルを確認してください。")
        return
    win = windows[0]

    # 4cmをピクセルに換算（96 DPIの場合）
    dpi = 96
    pixels_per_cm = dpi / 2.54
    offset = int(4 * pixels_per_cm)

    region_width = win.width
    region_height = win.height - offset
    if region_height <= 0:
        messagebox.showerror("エラー", "ウィンドウの高さが小さすぎます。")
        return

    # 各フレームの生データサイズ（バイト）を (幅×高さ) として、圧縮率20倍で推定
    raw_pixel_count = region_width * region_height * num_frames
    compression_ratio = 18.0  # 仮の圧縮率
    estimated_bytes = raw_pixel_count / compression_ratio
    estimated_mb = estimated_bytes / (1024 * 1024)
    total_duration = num_frames * frame_delay

    # 例えばDiscordの無料アップロードは約8MBが上限
    max_file_size_mb = 8.0

    msg = f"推定ファイルサイズ: {estimated_mb:.2f} MB\n総再生時間: {total_duration:.2f} 秒"
    if estimated_mb > max_file_size_mb:
        msg += f"\n警告: 推定ファイルサイズがDiscord/Twitterの最大容量({max_file_size_mb} MB)を超える可能性があります。"
    messagebox.showinfo("推定サイズ", msg)

def capture_gif():
    """
    設定に基づいてウィンドウの上部4cmを除いた部分のスクリーンショットを撮影し、
    GIFとして自動保存する。  
    キャプチャ後に実際のファイルサイズ（MB）をログに表示する。
    """
    window_title = title_entry.get()
    try:
        num_frames = int(num_frames_entry.get())
        frame_delay = float(frame_delay_entry.get())
    except Exception:
        messagebox.showerror("入力エラー", "フレーム数またはフレーム間隔の値が不正です。")
        return

    # 出力ファイル名に実行日時を付与（自動保存）
    timestamp = datetime.datetime.now().strftime("%Y%m%d_%H%M%S")
    gif_filename = f"output_{timestamp}.gif"

    # 4cmをピクセルに換算（96 DPIの場合）
    dpi = 96
    pixels_per_cm = dpi / 2.54
    offset = int(4 * pixels_per_cm)

    windows = gw.getWindowsWithTitle(window_title)
    if not windows:
        messagebox.showerror("エラー", "ウィンドウが見つかりませんでした。\nタイトルを確認してください。")
        return
    win = windows[0]
    frames = []  # PIL Imageオブジェクトを保存するリスト
    log_text.delete(1.0, tk.END)
    log_text.insert(tk.END, "キャプチャ開始...\n")
    root.update()
    time.sleep(2)

    for i in range(num_frames):
        if not win.isActive:
            win.activate()
            time.sleep(0.5)
        left, top, width, height = win.left, win.top, win.width, win.height
        # 上部4cm分を除外
        adjusted_top = top + offset
        adjusted_height = height - offset
        screenshot = pyautogui.screenshot(region=(left, adjusted_top, width, adjusted_height))
        frames.append(screenshot)
        log_text.insert(tk.END, f"フレーム {i+1} をキャプチャしました。\n")
        log_text.see(tk.END)
        root.update()
        time.sleep(frame_delay)

    if frames:
        frames[0].save(gif_filename, format='GIF', append_images=frames[1:], save_all=True,
                       duration=int(frame_delay * 1000), loop=0)
        # 保存したファイルのサイズを取得（MB単位）
        file_size = os.path.getsize(gif_filename)
        file_size_mb = file_size / (1024 * 1024)
        total_duration = num_frames * frame_delay
        log_text.insert(tk.END, f"\nGIFファイル '{gif_filename}' を作成しました。\n")
        log_text.insert(tk.END, f"総再生時間: {total_duration:.2f} 秒\n")
        log_text.insert(tk.END, f"実際のファイルサイズ: {file_size_mb:.2f} MB\n")
    else:
        log_text.insert(tk.END, "キャプチャに失敗しました。\n")
    root.update()

# GUIの構築
root = tk.Tk()
root.title("GifShoots")
root.attributes("-topmost", True)
main_frame = ttk.Frame(root, padding="15")
main_frame.grid(row=0, column=0, sticky=(tk.W, tk.E, tk.N, tk.S))

# ウィンドウタイトル入力
ttk.Label(main_frame, text="ウィンドウタイトル:").grid(row=0, column=0, sticky=tk.W)
title_entry = ttk.Entry(main_frame, width=30)
title_entry.grid(row=0, column=1, padx=5, pady=5)
title_entry.insert(0, "対象ウィンドウのタイトル")

# フレーム数入力
ttk.Label(main_frame, text="フレーム数:").grid(row=1, column=0, sticky=tk.W)
num_frames_entry = ttk.Entry(main_frame, width=30)
num_frames_entry.grid(row=1, column=1, padx=5, pady=5)
num_frames_entry.insert(0, "15")

# フレーム間隔入力（秒）
ttk.Label(main_frame, text="フレーム間隔 (秒):").grid(row=2, column=0, sticky=tk.W)
frame_delay_entry = ttk.Entry(main_frame, width=30)
frame_delay_entry.grid(row=2, column=1, padx=5, pady=5)
frame_delay_entry.insert(0, "0.6")

# JSON設定読み込みボタン
load_json_button = ttk.Button(main_frame, text="JSON設定読み込み", command=load_settings)
load_json_button.grid(row=3, column=0, padx=5, pady=5)

# クリックでウィンドウ選択ボタン
select_window_button = ttk.Button(main_frame, text="クリックでウィンドウ選択", command=select_window)
select_window_button.grid(row=3, column=1, padx=5, pady=5)

# 推定サイズ確認ボタン（撮影前にファイルサイズを推定）
estimate_button = ttk.Button(main_frame, text="推定サイズ確認", command=estimate_gif_size)
estimate_button.grid(row=4, column=0, columnspan=2, pady=5)

# GIFキャプチャ開始ボタン（自動保存）
capture_button = ttk.Button(main_frame, text="GIFキャプチャ開始", command=capture_gif)
capture_button.grid(row=5, column=0, columnspan=2, pady=10)

# ログ表示用テキストエリア
log_text = tk.Text(main_frame, width=50, height=10)
log_text.grid(row=6, column=0, columnspan=2, padx=5, pady=5)

root.mainloop()
