"""
プロジェクト全体の設定ファイル
"""

from pathlib import Path

# ==================== パス設定 ====================
PROJECT_ROOT = Path(__file__).parent.parent

# フォルダパス
DATA_DIR = PROJECT_ROOT / "data"
DATA_INPUT_DIR = DATA_DIR / "input"
OUTPUT_DIR = DATA_DIR / "output"

# ==================== フレーム抽出設定 ====================
# video_frame_extractor.py で使用
FRAME_PREFIX = "frame"
FRAME_EXTENSION = "jpg"

# ==================== 顔ぼかし設定 ====================
# face_blur.py で使用
BLUR_STRENGTH = 51  # ぼかしの強さ（奇数）

# ==================== Equirect→パースペクティブ変換設定 ====================
# batch_equirect2persp_ffmpeg.py で使用

# 出力画像の解像度
PERSPECTIVE_WIDTH = 1024
PERSPECTIVE_HEIGHT = 1024

# FOV (視野角)
HORIZONTAL_FOV = 90
VERTICAL_FOV = 90

# ffmpeg オーバーライト設定
OVERWRITE = True

# ========== 変換方法選択 ==========
# False: 14方向（上下左右+斜めなど標準的）
# True: 密集度高く（ring_step_deg ごとにリング状）
USE_DENSE_RING = False
RING_STEP_DEG = 30  # USE_DENSE_RING=True の場合に使用

# ==================== 共通設定 ====================
# ログレベル（OpenCV や ffmpeg へのオプション）
FFMPEG_LOGLEVEL = "error"

# 一度に処理する画像数など（今後の拡張用）
BATCH_SIZE = None  # None = 制限なし
