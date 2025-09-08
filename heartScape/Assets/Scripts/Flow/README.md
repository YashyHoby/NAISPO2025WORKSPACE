# Flow

2D の速度場（ベクトル場）を合成し、壁の滑り効果を与える。

## FlowManager2D.cs
- **役割**: 子孫から Flow ソースを収集し、任意座標 `u(x)` を返す。壁の Slide 補正も適用。
- **インスペクター**
  - **Sampling**
    - `autoCollect (bool)`: 子孫から自動収集する。
    - `sourceBehaviours (List<MonoBehaviour>)`: 手動で登録する場合に使用。
    - `walls (List<FlowWall2D>)`: 壁の一覧。
  - **Saturation (optional)**
    - `saturate (bool)`: 飽和（大きさを抑える）を有効化。
    - `satScale (float)`: 飽和スケール。
  - **Debug Gizmo**
    - `drawGizmos (bool)`: グリッド描画。
    - `center (Vector2)`, `size (Vector2)`: ギズモ領域。
    - `step (float)`: サンプル間隔。
    - `arrowScale (float)`: 矢印長さ倍率。
    - `minMagnitudeToDraw (float)`: 閾値未満は非表示。
- **内部調整**
  - `SampleVelocity()` 内の合成順序・重み付けを変更可能。

## FlowRectSource2D.cs（現ファイル名: FlowReceSource2D.cs）
- **役割**: 矩形ノズルに近い流れを生成。長手/幅方向のプロファイルで形状化。任意で根元からの逆流（inflow）。
- **インスペクター**
  - **Shape / Strength**
    - `length (float)`: +X 方向長さ
    - `width (float)`: 幅（±Y/2）
    - `strength (float)`: 基本流速
  - **Profiles (0..1)**
    - `lengthProfile (AnimationCurve)`: t=x/L (0:根元,1:出口)
    - `widthProfile (AnimationCurve)`: s=|2y/W| (0:中央,1:端)
  - **Inflow (optional)**
    - `inflowEnabled (bool)`, `inflowRadius (float)`, `inflowStrength (float)`, `inflowProfile (AnimationCurve)`
- **内部調整**
  - ローカル→ワールド変換は `transform.right` を主流方向と想定（回転で向きを決める）。
  - `OnDrawGizmos()` の可視化スケールは必要に応じて編集。

## FlowWall2D.cs
- **役割**: 壁近傍で法線成分を抑制、接線成分を残して「滑る」ように補正。
- **インスペクター**
  - `effectRadius (float)`: 壁効果の及ぶ距離。
  - `noPenetration (float)`: 法線成分の抑制（1=完全遮断）。
  - `slideBoost (float)`: 接線強調。0 で無効。
- **内部調整**
  - 近傍点の取得は `Collider2D.ClosestPoint`。壁形状に応じて Collider を選定。
