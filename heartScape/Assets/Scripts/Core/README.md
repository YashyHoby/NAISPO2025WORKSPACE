# Core

エージェント（心オブジェクト）の生成・破棄、見た目の決定、空間イベント適用の中核。

---

## Agents

### HeartAgent.cs
- **役割**: 心オブジェクトのID・プロファイル保持、捕獲状態のフラグ。
- **インスペクター**
  - `id (int)`: 0 の場合は Awake 時にランダム割り当て。
  - `profile (HeartProfile)`: 心拍・変動係数などの元データ。
  - （内部・Hide）`isCaptured (bool)`, `capturedBy (BubbleZone)`
- **内部調整**: なし。

### HeartManager.cs
- **役割**: `Spawn/Despawn/ClearAll`。画面外デスポーン。
- **インスペクター**
  - `agents (List<HeartAgent>)`: 実行時に追加されるランタイムリスト。
  - `agentPrefab (GameObject)`: 生成するプレハブ。
  - `maxAgents (int)`: 生成上限（既定 140）。
  - `despawnOffscreen (bool)`: 画面外で破棄するか。
  - `despawnExtents (Vector2)`: 破棄矩形の ±X, ±Y。
- **Spawn 内の初期化**
  - `HeartAgent.profile/id` の設定
  - `HeartVisual`:
    - **現行実装**では `shapeType` を `Random.Range(0,3)` でランダム化、`color` を `hp.hr` から HSV 生成。
    - ※ 形状を心情報から決定したい場合は `HeartAppearanceMapper` を呼ぶ実装へ差し替え推奨（下記）。
  - `HeartGrowth`: `baseRadius` を `hp.mean` から設定し、`ApplyStageScale()` で `HeartVisual.radius` を更新。
  - `Rigidbody2D.linearVelocity` に初速を設定。
- **内部調整ポイント**
  - **形状ランダム化を止める**: `vis.shapeType = (ShapeType)Random.Range(0,3);` を削除。
  - **AppearanceMapper連携**: `appearance.Apply(hp, vis)` を挿入し、色・形・半径を心情報から決定論的に。
  - **デスポーン**: `DespawnOffscreen()` は逆順 `for` で安全に削除。

### HeartGrowth.cs
- **役割**: FOOD の蓄積による成長段階（スケール）と、最大段階での分裂。
- **インスペクター**
  - `stageScales (float[])`: 段階ごとのスケール係数（例: 5要素で0〜4段階）。
  - `baseRadius (float)`: 基準半径。
  - `foodPerStage (float)`: 1段階の必要 FOOD 量。
  - `foodGainMul (float)`: FOOD 獲得倍率。
  - `growthStage (int)`: 現在段階（0〜）。
- **内部調整**
  - `SplitFromGrowth()` の距離/方向/初速（子の出現演出）は本関数内の加算ベクトルを変更。

---

## Appearance

### HeartVisual.cs
- **役割**: 実描画に使う最終プロパティ（形・色・半径）とマテリアルインスタンスの管理。
- **インスペクター**
  - `shapeType (ShapeType)`: Circle/Triangle/Box。
  - `color (Color)`: 発色。
  - `radius (float)`: 当たり/見た目半径（`HeartGrowth` やマッパから更新）。
  - `profile (HeartProfile)`: 任意参照、なければ既定。
  - `agent (HeartAgent)`: 任意参照。
  - `baseMaterial (Material)`: 元マテリアル（実行時にインスタンス化）。
- **内部調整**
  - マテリアルプロパティの適用部分（`Awake/Update`）にシェーダの property 名を合わせる。

### HeartAppearanceMapper.cs
- **役割**: `HeartProfile` → **形/色/半径**を**決定論的**に算出し、見た目の偏りをガンマ等で均す。
- **インスペクター**
  - *Expected Ranges*（正規化レンジ）: `hrRange`, `cvRange`, `rngRange`, `meanRange`
  - *Distribution Shaping*（分布補正）: `hrGamma`, `cvGamma`, `rngGamma`, `meanGamma`（<1 広げる）
  - *Color (HSV)*: `satRange`, `valRange`, `hueWeights (Vector3: hr,cv,range)`
  - *Radius*: `radiusRange`, `setRadiusOnSpawn (bool)`
  - *Shape*: `shapeCount (int)`（`HeartVisual` の総種類に合わせる）
- **内部調整**
  - 形割当の均し係数（`0.754877...` など）は `Apply()` の合成式を変更。
  - UID/スイッチ番号も混ぜたい場合は、`Apply()` に引数追加 or 外側で `vis.shapeType` を上書き。

---

## Systems

### EventApplier.cs
- **役割**: `zones` を全 `agents` に毎 FixedUpdate で適用。
- **インスペクター**
  - `manager (HeartManager)`
  - `zones (List<EventZone>)`
- **注意**
  - `foreach (manager.agents)` 中に `agents` が変更されると例外化。**スナップショット**で回す/実行順を後ろにするなどで回避。

### Models
- HeartProfile.cs：UID, hr, cv, range, mean を持つシリアライズ可能なデータ構造。
  - Inspector で参照される場合あり（ScriptableObject ではなく POCO）。
