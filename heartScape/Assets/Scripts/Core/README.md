# Core

エージェント（heartagent オブジェクト）の生成・破棄、見た目の決定、空間イベント適用の中核である。

---

## Agents

### HeartAgent.cs
- **役割**: HeartAgent オブジェクトの ID・プロファイル保持、捕獲状態のフラグ管理である。
- **インスペクター**
  - `id (int)`: 0 の場合は Awake 時にランダム割当を行う。
  - `profile (HeartProfile)`: 心拍・変動係数などの基礎データである。
  - 内部・HideInInspector: `isCaptured (bool)`, `capturedBy (BubbleZone)`。
- **実装調整**: なし。

### HeartManager.cs
- **役割**: `Spawn/Despawn/ClearAll` を担い、画面外デスポーンを制御する。
- **インスペクター**
  - `agents (List<HeartAgent>)`: 実行時に追加されるランタイムリストである。
  - `agentPrefab (GameObject)`: 生成するプレハブである。
  - `maxAgents (int)`: 生成上限（既定 140）。
  - `despawnOffscreen (bool)`: 画面外で破棄するかどうかを切り替える。
  - `despawnExtents (Vector2)`: 破棄判定領域の ±X, ±Y。
- **Spawn 時の初期化**
  - `HeartAgent.profile/id` を設定する。
  - `HeartVisual`:
    - `HeartAppearanceMapper` が設定されていれば `Apply()` を呼び、形／色／半径を決定する。
    - マッパーが無い場合は `hp.hr` を HSV に変換して色を計算する。
  - `HeartGrowth`:
    - マッパーが半径を決定しない場合は `baseRadius` を `hp.mean` から補間し、`ApplyStageScale()` で `HeartVisual.SetRadius()` を更新する。
  - `Rigidbody2D.linearVelocity` に初速を設定する。
- **実装調整ポイント**
  - `appearance` を差し替えることで決定論的な見た目生成に移行できる。
  - `DespawnOffscreen()` は for ループで安全に削除する。

### HeartGrowth.cs
- **役割**: FOOD 蓄積による成長段階（スケール変化）と最大段階での分裂処理を担当する。
- **インスペクター**
  - `stageScales (float[])`: 段階ごとのスケール係数（既定 5 要素）。
  - `baseRadius (float)`: 基準半径。
  - `foodPerStage (float)`: 1 段階あたりの必要 FOOD 量。
  - `foodGainMul (float)`: FOOD 獲得倍率。
  - `growthStage (int)`: 現在段階（0〜）。
- **実装調整**
  - `SplitFromGrowth()` の距離・方向・初速（子オブジェクト出現演出）を変更できる。

---

## Appearance

### HeartVisual.cs
- **役割**: 実描画に使う最終プロパティ（形・色・半径）とマテリアルインスタンスを管理する。
- **インスペクター**
  - `shapeType (ShapeType)`: Circle/Triangle/Box。
  - `color (Color)`: 発色。
  - `radius (float)`: 当たり判定と見た目半径（`HeartGrowth` や `HeartAppearanceMapper` から更新）。
  - `profile (HeartProfile)`: 任意参照（設定が無い場合は既定 70 bpm を使用）。
  - `agent (HeartAgent)`: 任意参照。
  - `baseMaterial (Material)`: 基本マテリアル（実行時にインスタンス化）。
- **Transform・コライダー同期**
  1. `Awake()` で `transform.localScale` を `baseScale` としてキャッシュし、`CircleCollider2D` があれば現在半径を `referenceRadiusForScale` として保持する。
  2. `SetRadius()` → `ApplyRadiusToScale()` を通じて半径を更新するたびに `scaleFactor = radius / referenceRadiusForScale` を求め、`transform.localScale = baseScale * scaleFactor` とする。
  3. `CircleCollider2D` が存在する場合は物理半径を `referenceRadiusForScale` に固定し、Transform のスケールで当たりを拡縮する。
  4. `Update()` では `profile.hr` から拍動周波数 `beatHz = Lerp(1.0, 2.4, InverseLerp(50,120,hr))` を計算し、シェーダーへ `_Pulse` (0〜1)、`_Radius` (実半径)、`_Tint`, `_ShapeType` を設定する。

### HeartAppearanceMapper.cs
- **役割**: `HeartProfile` から形／色／半径を決定論的に算出し、見た目の偏りをガンマ補正で整える。
- **入力パラメータ**: `HeartProfile` の `hr`（心拍数）、`cv`（変動係数）、`range`（拍間の振れ幅）、`mean`（平均心拍）。
- **正規化フェーズ**
  - 各値を `Mathf.InverseLerp(range.x, range.y, value)` で 0〜1 に正規化し、`PowNorm()` 内で `Mathf.Pow(t, gamma)` による分布調整を行う。
  - 結果を `h`（HR）、`c`（CV）、`r`（Range）、`m`（Mean）として次段に渡す。
- **形状決定**
  - `u = frac(h * 0.754877666 + c * 0.569840291 + r * 0.438695021 + m * 0.271828182)`。
  - `shapeIndex = clamp(floor(u * shapeCount), 0, shapeCount-1)`。
  - `vis.shapeType = (ShapeType)shapeIndex`。
- **色決定**
  - `hue = frac(h * hueWeights.x + c * hueWeights.y + r * hueWeights.z)`。
  - `sat = Lerp(satRange.x, satRange.y, c)`、`val = Lerp(valRange.x, valRange.y, r)`。
  - `vis.color = Color.HSVToRGB(hue, sat, val)`。
- **半径決定**
  - `setRadiusOnSpawn` が true のとき `radius = Lerp(radiusRange.x, radiusRange.y, m)` を求め、`vis.SetRadius()` に渡す。
  - `SetRadius()` から Transform スケールと当たり判定が同時に更新されるため、見た目と物理判定が一致する。

### HeartProfile.cs
- **役割**: heart_db の 1 レコードに相当するデータ構造であり、`uid`, `hr`, `cv`, `range`, `mean` を保持するシリアライズ可能クラスである。

---

## HeartAgent ビジュアル決定フローまとめ
1. heart_db から読み込まれた `HeartProfile` が `HeartManager.Spawn()` を通じて `HeartAgent.profile` に設定される。
2. `HeartAppearanceMapper.Apply()` が `HeartProfile` の数値を正規化し、以下を決定する。
   - `shapeType`: 正規化値の線形結合→frac→範囲内丸め。
   - `color`: `hr/cv/range` を HSV にマッピングして彩度・明度を調整する。
   - `radius`: `mean` の正規化値から線形補間して決定し、`SetRadius()` を通じて反映する。
3. `HeartVisual.SetRadius()` により Transform スケールが `scaleFactor = radius / referenceRadiusForScale` で更新され、`CircleCollider2D` の実半径は基準値を維持するため、見た目サイズと物理判定が同期する。
4. `HeartGrowth` が存在する場合、FOOD 取得量に応じて `baseRadius * stageScales[growthStage]` を再計算し、`SetRadius()` を通じて同じ経路でスケールが変化する。
5. `HeartVisual.Update()` では `profile.hr` から拍動アニメーション `_Pulse` を算出し、シェーダーパラメータへ渡すことで視覚的な呼吸・鼓動を表現する。

このフローにより、heart_db の値が形状・色・大きさ・アニメーションに一貫して反映され、ビジュアルと物理判定が共通スケールで扱われるようになっている。
