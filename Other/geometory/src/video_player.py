"""
簡易的な MP4 ビデオプレイヤー
OpenCV と Tkinter を使用してGUI付きプレイヤーを実装
"""

import tkinter as tk
from tkinter import filedialog, messagebox
from PIL import Image, ImageTk
import cv2
import threading
from pathlib import Path
import config


class VideoPlayer:
    def __init__(self, root):
        self.root = root
        self.root.title("MP4 Video Player")
        self.root.geometry("900x700")

        # ビデオキャプチャオブジェクト
        self.cap = None
        self.is_playing = False
        self.current_frame = 0
        self.total_frames = 0
        self.fps = 0
        self.video_path = None

        # GUI コンポーネントを作成
        self._create_widgets()

    def _create_widgets(self):
        """GUI コンポーネントを作成"""

        # === ファイル選択パネル ===
        file_frame = tk.Frame(self.root)
        file_frame.pack(pady=10)

        tk.Button(file_frame, text="ファイルを開く", command=self._open_file, width=15).pack(side=tk.LEFT, padx=5)
        self.file_label = tk.Label(file_frame, text="ファイルが選択されていません", fg="gray")
        self.file_label.pack(side=tk.LEFT, padx=10)

        # === ビデオ表示領域 ===
        self.canvas = tk.Canvas(self.root, bg="black", width=640, height=480)
        self.canvas.pack(pady=10)

        # === 情報パネル ===
        info_frame = tk.Frame(self.root)
        info_frame.pack(pady=5)

        self.info_label = tk.Label(info_frame, text="フレーム: 0 / 0 | FPS: 0 | 時間: 00:00 / 00:00",
                                   font=("Arial", 10))
        self.info_label.pack()

        # === シークバー ===
        self.seek_slider = tk.Scale(self.root, from_=0, to=100, orient=tk.HORIZONTAL, bg="lightgray")
        self.seek_slider.pack(fill=tk.X, padx=10, pady=5)
        self.seek_slider.bind("<Button-1>", lambda e: self._start_seek())
        self.seek_slider.bind("<B1-Motion>", lambda e: None)  # ドラッグ中は何もしない
        self.seek_slider.bind("<ButtonRelease-1>", lambda e: self._end_seek())

        # === コントロールパネル ===
        control_frame = tk.Frame(self.root)
        control_frame.pack(pady=10)

        self.play_button = tk.Button(control_frame, text="▶ 再生", command=self._toggle_play, width=10)
        self.play_button.pack(side=tk.LEFT, padx=5)

        tk.Button(control_frame, text="⏮ 最初", command=self._go_to_start, width=10).pack(side=tk.LEFT, padx=5)
        tk.Button(control_frame, text="⏭ 最後", command=self._go_to_end, width=10).pack(side=tk.LEFT, padx=5)
        tk.Button(control_frame, text="🔄 リセット", command=self._reset, width=10).pack(side=tk.LEFT, padx=5)

        # === 速度調整 ===
        speed_frame = tk.Frame(self.root)
        speed_frame.pack(pady=5)

        tk.Label(speed_frame, text="再生速度:").pack(side=tk.LEFT, padx=5)
        self.speed_slider = tk.Scale(speed_frame, from_=0.25, to=2.0, resolution=0.25,
                                      orient=tk.HORIZONTAL, length=150)
        self.speed_slider.set(1.0)
        self.speed_slider.pack(side=tk.LEFT, padx=5)

        tk.Label(speed_frame, text="倍速").pack(side=tk.LEFT)

    def _open_file(self):
        """ファイル選択ダイアログを開く"""
        file_path = filedialog.askopenfilename(
            title="MP4 ファイルを選択",
            initialdir=str(config.DATA_INPUT_DIR),
            filetypes=[("MP4 ファイル", "*.mp4"), ("すべてのファイル", "*.*")]
        )

        if file_path:
            self._load_video(file_path)

    def _load_video(self, file_path):
        """ビデオファイルを読み込む"""
        try:
            # 既に開いているビデオを閉じる
            if self.cap:
                self.cap.release()

            # ビデオを開く
            self.cap = cv2.VideoCapture(file_path)

            if not self.cap.isOpened():
                messagebox.showerror("エラー", "ビデオファイルを開けませんでした")
                return

            self.video_path = file_path
            self.total_frames = int(self.cap.get(cv2.CAP_PROP_FRAME_COUNT))
            self.fps = self.cap.get(cv2.CAP_PROP_FPS)
            self.current_frame = 0
            self.is_playing = False

            # GUI更新
            self.file_label.config(text=Path(file_path).name, fg="black")
            self.seek_slider.config(to=self.total_frames - 1)
            self.play_button.config(text="▶ 再生")

            # 最初のフレームを表示
            self._display_frame()
            self._update_info()

        except Exception as e:
            messagebox.showerror("エラー", f"ファイル読み込みエラー: {e}")

    def _display_frame(self):
        """現在のフレームをキャンバスに表示"""
        if not self.cap:
            return

        # 現在のフレーム位置に設定
        self.cap.set(cv2.CAP_PROP_POS_FRAMES, self.current_frame)
        ret, frame = self.cap.read()
        if not ret:
            return

        # フレームをリサイズして表示
        frame = cv2.cvtColor(frame, cv2.COLOR_BGR2RGB)
        frame = cv2.resize(frame, (640, 480))

        image = Image.fromarray(frame)
        photo = ImageTk.PhotoImage(image)

        self.canvas.create_image(0, 0, image=photo, anchor=tk.NW)
        self.canvas.image = photo

    def _update_info(self):
        """情報ラベルを更新"""
        if not self.cap:
            return

        frame_num = int(self.current_frame)
        total = int(self.total_frames)

        # 時間を計算
        current_sec = frame_num / self.fps if self.fps > 0 else 0
        total_sec = total / self.fps if self.fps > 0 else 0

        current_time = self._format_time(current_sec)
        total_time = self._format_time(total_sec)

        info_text = f"フレーム: {frame_num} / {total} | FPS: {self.fps:.1f} | 時間: {current_time} / {total_time}"
        self.info_label.config(text=info_text)

    @staticmethod
    def _format_time(seconds):
        """秒を MM:SS 形式にフォーマット"""
        minutes = int(seconds) // 60
        secs = int(seconds) % 60
        return f"{minutes:02d}:{secs:02d}"

    def _toggle_play(self):
        """再生/一時停止を切り替え"""
        if not self.cap:
            messagebox.showwarning("警告", "まずファイルを選択してください")
            return

        self.is_playing = not self.is_playing
        self.play_button.config(text="⏸ 一時停止" if self.is_playing else "▶ 再生")

        if self.is_playing:
            self._play_video()

    def _play_video(self):
        """ビデオを再生"""
        if not self.is_playing or not self.cap:
            return

        speed = self.speed_slider.get()
        delay = max(1, int(1000 / (self.fps * speed)))

        # 現在のフレーム位置から読み込み
        self.cap.set(cv2.CAP_PROP_POS_FRAMES, self.current_frame)
        ret, frame = self.cap.read()
        if not ret:
            # ビデオ終了
            self.is_playing = False
            self.play_button.config(text="▶ 再生")
            return

        # フレーム表示
        frame = cv2.cvtColor(frame, cv2.COLOR_BGR2RGB)
        frame = cv2.resize(frame, (640, 480))

        image = Image.fromarray(frame)
        photo = ImageTk.PhotoImage(image)

        self.canvas.create_image(0, 0, image=photo, anchor=tk.NW)
        self.canvas.image = photo

        # フレームカウント更新
        self.current_frame += 1
        self._update_info()

        # 次のフレームを再生（再帰的に呼び出し）
        if self.is_playing and self.current_frame < self.total_frames:
            self.root.after(delay, self._play_video)
        else:
            self.is_playing = False
            self.play_button.config(text="▶ 再生")

    def _seek_video(self, value):
        """シークバーでフレーム移動"""
        if not self.cap:
            return

        frame_num = int(float(value))
        self.cap.set(cv2.CAP_PROP_POS_FRAMES, frame_num)
        self.current_frame = frame_num
        self.is_playing = False
        self.play_button.config(text="▶ 再生")

        self._display_frame()
        self._update_info()

    def _start_seek(self):
        """シークバー操作開始（再生を一時停止）"""
        if self.is_playing:
            self.is_playing = False
            self.play_button.config(text="▶ 再生")

    def _end_seek(self):
        """シークバー操作終了（フレーム位置を更新）"""
        if not self.cap:
            return

        frame_num = self.seek_slider.get()
        self._seek_video(frame_num)

    def _go_to_start(self):
        """最初のフレームに移動"""
        if not self.cap:
            return
        self._seek_video(0)

    def _go_to_end(self):
        """最後のフレームに移動"""
        if not self.cap:
            return
        self._seek_video(self.total_frames - 1)

    def _reset(self):
        """ビデオをリセット"""
        if self.cap:
            self.cap.release()
            self.cap = None

        self.is_playing = False
        self.current_frame = 0
        self.video_path = None

        self.canvas.create_rectangle(0, 0, 640, 480, fill="black")
        self.file_label.config(text="ファイルが選択されていません", fg="gray")
        self.info_label.config(text="フレーム: 0 / 0 | FPS: 0 | 時間: 00:00 / 00:00")
        self.play_button.config(text="▶ 再生")
        self.seek_slider.set(0)


def main():
    """メイン関数"""
    root = tk.Tk()
    player = VideoPlayer(root)
    root.mainloop()


if __name__ == "__main__":
    main()
