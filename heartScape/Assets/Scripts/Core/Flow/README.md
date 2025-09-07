了解です 👍
開発者向けに「Flow 系コードの役割と使い方」が一目で分かる **README（簡易版）** を書きます。

---

# 🌊 Flow System README

このディレクトリには、**水流を力場として表現し、心オブジェクト（HeartAgent 等）を自然に漂わせる仕組み**を構成するスクリプト群が含まれています。

---

## 📂 構成ファイル

* **`IFlowSource2D.cs`**
  流速場を提供するインターフェース。
  任意の流源はこれを実装し、`SampleVelocity(Vector2 pos)` で位置ごとの流速ベクトルを返す。

* **`FlowRectSource2D.cs`**
  長方形の水流源（入→出の一辺方向に流れを生成）。
  *Inspector 設定*:

  * `length, width, strength` … 矩形サイズと基準強さ
  * `lengthProfile, widthProfile` … 長手・幅方向の減衰カーブ
  * `inflow` 設定 … 根元吸い込みの範囲と強さ
    → Gizmo で矩形枠と方向を可視化。

* **`FlowWall2D.cs`**
  壁の影響を表現。Collider2D に付与して、**壁に突っ込む成分を弱め、接線成分を残す**。
  *Inspector 設定*:

  * `effectRadius` … 壁から何m以内で効かせるか
  * `noPenetration` … 法線成分を消す割合（1=完全に消す）
  * `slideBoost` … 接線成分を強める係数

* **`FlowManager2D.cs`**
  力場の統合管理。

  * 子オブジェクトやリストから FlowSource を集めて合成
  * 壁補正を適用
  * `SampleVelocity(pos)` で最終的な流速ベクトルを返す
  * Gizmo でグリッド矢印を描画（流れを確認可能）

* **`FlowBody2D.cs`**
  Rigidbody2D に付与して「流れに乗る」挙動を与える。

  * `alignK` … 流速 u と現在速度 v のズレを埋める力
  * `quadDragK` … 速度²比例の抗力（暴走防止）
  * `effectiveGravity` … 下向きに沈む力（弱い重力）
  * `noiseForce` … 少し揺らぎを加えて水中っぽさを出す
  * `clampSpeed` … 最大速度を制限する安全装置

---

## ⚙️ 典型的な使い方

1. シーンに空のオブジェクトを作り、**FlowManager2D** を付与

   * `autoCollect = true` で子の FlowRectSource2D / FlowWall2D を自動収集
   * Gizmo 範囲・グリッド間隔を調整して流れを可視化

2. FlowManager の子として **FlowRectSource2D** を配置（複数可）

   * 長さ・幅・強さを調整
   * 矢印で流れの向きを確認

3. 壁オブジェクト（EdgeCollider2D, PolygonCollider2D など）に **FlowWall2D** を付与

   * 壁近くでは「突っ込まない／沿って流れる」挙動になる

4. 心オブジェクト（HeartAgentなど）に **Rigidbody2D + FlowBody2D** を付与

   * FlowBody2D の `flow` にシーンの FlowManager をアサイン
   * パラメータを調整して自然な漂いを実現

---

## 🧪 推奨パラメータ（初期値目安）

* **FlowBody2D**

  * `alignK = 6〜8`
  * `quadDragK = 0.3〜0.5`
  * `effectiveGravity = -3`
  * `maxSpeed = 10`

* **FlowRectSource2D**

  * `strength = 4〜6`
  * `length = 5`
  * `width = 2`

* **FlowWall2D**

  * `effectRadius = 1.0`
  * `noPenetration = 1.0`
  * `slideBoost = 0.25`

---

## 💡 今後の拡張ポイント

* Bounce 型の壁（法線成分を反転して弾く）
* 渦源・点流源など FlowSource の追加
* Streamline（流線）可視化モード

---

👉 この README は簡易版です。
もう少し利用者向けに「**実際にシーンに入れて動かす手順**」まで図解入りで書く方がよいですか？
