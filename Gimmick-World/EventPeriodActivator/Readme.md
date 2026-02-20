# EventPeriodActivator (UdonSharp)

指定した「日付範囲」かつ「時間帯」のときだけ、オブジェクトの ON/OFF を切り替える UdonSharp スクリプトです。  
VRChat ワールド内で、期間限定イベント・誕生日演出・季節装飾・ナイトモード等を自動で出し分けできます。

---

## できること

- `StartYear/Month/Day` ～ `EndYear/Month/Day`（両端含む）の **期間内** のみ有効
- 期間内かつ、`NightmodeStart` ～ `NightmodeEnd` の **時間帯** のみ有効
- 有効（Nightmode=true）な間：
  - `nightmodeOnObjects` を `SetActive(true)`
  - `nightmodeOffObjects` を `SetActive(false)`
- 無効（Nightmode=false）な間：
  - `nightmodeOnObjects` を `SetActive(false)`
  - `nightmodeOffObjects` を `SetActive(true)`
- 日付フィールドの `0` はワイルドカード（毎年/毎月/毎日）として扱う

---

## 仕組み（重要）

- 時刻・日付は **`Networking.GetNetworkDateTime()`** を使用します  
  → ローカルPC時刻ではなく、ネットワーク基準の時刻で判定します。

- 判定は `Update()` で毎フレーム実行されます  
  → オブジェクト数が多い場合は負荷になるので注意（後述）。

---

## インストール / 使い方

1. Unity に UdonSharp を導入済みであることを確認
2. `EventPeriodActivator.cs` を `Assets/` 配下へ配置
3. 任意の GameObject に `EventPeriodActivator` を追加
4. Inspector で以下を設定
   - `NightmodeStart`, `NightmodeEnd`
   - `StartYear/Month/Day`, `EndYear/Month/Day`
   - `nightmodeOnObjects`（有効時にONにする）
   - `nightmodeOffObjects`（有効時にOFFにする）

---

## 設定例

### 例1: 毎年 10/01 ～ 10/29 の間、22:00～翌6:00だけ点灯
- StartYear = 0  
- StartMonth = 10  
- StartDay = 1  
- EndYear = 0  
- EndMonth = 10  
- EndDay = 29  
- NightmodeStart = 22  
- NightmodeEnd = 6

※ `StartYear/EndYear = 0` のため「毎年同じ期間」で発動します。

### 例2: 24時間ON（期間内は常時ON）
- NightmodeStart = 0
- NightmodeEnd = 0

### 例3: 同日・同時刻のみON（ピンポイント）
- NightmodeStart = 22
- NightmodeEnd = 22  
→ `hour == 22` の時間だけON（22:00〜22:59相当）

---

## 日付ワイルドカード仕様（0の意味）

- 年 = 0 → 毎年一致扱い（年は判定に使わない）
- 月 = 0 → 毎月一致扱い（月は判定に使わない）
- 日 = 0 → 毎日一致扱い（日は判定に使わない）

例：StartYear=0, StartMonth=12, StartDay=25  
→ 「毎年12/25」

---

## 注意点（必ず読む）

### 1) 日付範囲の比較は「フィールド単位のワイルドカード比較」
このスクリプトは「完全な DateTime 同士の大小比較」ではなく、
`CompareDate(now, y, m, d)` が **年→月→日** の順に一致/大小を判定します。

そのため、例えば「年をワイルドカードにして跨年（12月→1月）イベント」などは
意図通りに動かない可能性があります。

### 2) `Update()` が毎フレーム動く
オブジェクトが多い場合は負荷になります。  
必要なら以下のように改善できます（例）：
- 1秒に1回だけ判定する（タイマー）
- 状態が変わった時だけ `SetActive` する（前回値を保持）

### 3) Debug.Log が毎フレーム出る
`Update()` の `Debug.Log` はログを大量に出します。  
公開用では削除/無効化推奨です。

---

## ライセンス
必要なら追記してください（MIT等）。