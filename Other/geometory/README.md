# Equirect 360動画 → パースペクティブ画像 変換ツール

Equirectangular形式の360度動画をフレーム抽出し、複数の直線的パースペクティブ画像に変換するツールです。

## 概要

このプロジェクトは以下の処理を実装しています：

1. **フレーム抽出** - 動画をフレーム単位で画像に分割
2. **顔ぼかし** - 抽出されたフレーム内の顔を検出してぼかす（オプション）
3. **Equirect→パースペクティブ変換** - 360度画像を複数の通常視点画像に変換

## システム構成

### 主要なファイル

| ファイル | 説明 |
|---------|------|
| `main.py` | メインスクリプト。各コマンドを実行します |
| `config.py` | プロジェクト全体の設定（パス、パラメータ） |
| `video_frame_extractor.py` | 動画をフレーム画像に分割 |
| `face_blur.py` | 画像の顔検出とぼかし処理 |
| `batch_equirect2persp_ffmpeg.py` | ffmpegを使用した360度画像の変換 |
| `video_player.py` | 動画再生用ユーティリティ |

### ディレクトリ構造

```
data/
  ├─ input/        # 入力動画を配置
  └─ output/       # 処理結果を出力
src/
  ├─ main.py
  ├─ config.py
  └─ その他のモジュール
```

## 使用方法

### 1. フレーム抽出のみ

```bash
uv run src/main.py extract <video_file>
```

例：
```bash
uv run src/main.py extract video.mp4
```

- `data/input/` 配下の動画ファイルを指定
- 抽出されたフレームは `data/output/` に保存される

### 2. 顔ぼかし処理のみ

```bash
uv run src/main.py blur <output_folder>
```

例：
```bash
uv run src/main.py blur video_145frames_0min4sec_20260122_220022
```

- `data/output/<output_folder>/temp/frames/` のフレームに顔ぼかしを適用
- 結果は `data/output/<output_folder>/face_blurred_frames/` に保存

### 3. パースペクティブ変換のみ

```bash
uv run src/main.py convert <output_folder> [--dense]
```

例：
```bash
uv run src/main.py convert video_145frames_0min4sec_20260122_220022
uv run src/main.py convert video_145frames_0min4sec_20260122_220022 --dense
```

**オプション：**
- `--dense` : 密集度高く変換（ring_step_degごとにリング状に変換）

### 4. パイプライン実行（全処理）

```bash
uv run src/main.py pipeline <video_file> [--blur] [--dense]
```

例：
```bash
# 顔ぼかしなし（標準的な14方向変換）
uv run src/main.py pipeline video.mp4

# 顔ぼかしあり、密集度高い変換
uv run src/main.py pipeline video.mp4 --blur --dense
```

**オプション：**
- `--blur` : 顔ぼかし処理を含める
- `--dense` : 密集度高く変換

## 設定項目

[config.py](src/config.py) で以下をカスタマイズ可能：

### フレーム抽出
- `FRAME_PREFIX` : 出力ファイル名のプレフィックス（デフォルト: "frame"）
- `FRAME_EXTENSION` : 出力画像の拡張子（デフォルト: "jpg"）

### 顔ぼかし
- `BLUR_STRENGTH` : ぼかしの強さ（奇数、デフォルト: 51）

### パースペクティブ変換
- `PERSPECTIVE_WIDTH` : 出力画像の幅（デフォルト: 1024）
- `PERSPECTIVE_HEIGHT` : 出力画像の高さ（デフォルト: 1024）
- `HORIZONTAL_FOV` : 水平視野角（デフォルト: 90度）
- `VERTICAL_FOV` : 鉛直視野角（デフォルト: 90度）
- `USE_DENSE_RING` : 密集度高い変換を使用するか（デフォルト: False）
- `RING_STEP_DEG` : 密集変換の角度ステップ（デフォルト: 30度）

## 依存関係

- Python 3.8+
- OpenCV (cv2)
- ffmpeg

## 技術仕様

### フレーム抽出
- OpenCVを使用して動画を解析
- 出力フォルダは自動的に `video_name_○○frames_○min○sec_YYYYMMDD_HHMMSS` 形式で作成
- fps、解像度、総フレーム数などをログに表示

### 顔ぼかし
- Haar Cascade分類器（OpenCV）で顔検出
- Gaussian Blurで顔領域をぼかし処理

### Equirect→パースペクティブ変換
- ffmpegの `v360` フィルタを使用
- 標準モード：上下左右、斜め4方向、上下後ろを含む14方向に変換
- 密集モード：指定した角度ステップでリング状に変換

## トラブルシューティング

### ffmpegが見つからない場合
```
RuntimeError: ffmpeg が見つかりません
```
→ ffmpegをシステムPATHに追加するか、インストールしてください

### フォルダが見つからない場合
```
❌ エラー: フォルダが見つかりません
```
→ 先に `extract` コマンドでフレーム抽出を実行してください

## ライセンス

指定なし（プロジェクト参照）
