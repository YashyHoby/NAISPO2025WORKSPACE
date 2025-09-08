# Interaction / Zones

空間に置く円形ゾーン。`EventApplier` が全エージェントへ適用。

## 共通: EventZone.cs
- **インスペクター**: `radius (float)`：半径。
- **共通API**
  - `bool Contains(Vector2 p)`：内判定。
  - `void Apply(HeartAgent a)`：効果本体。
  - `OnDrawGizmos()`：円のワイヤ表示。

## FoodZone.cs
- **効果**: `HeartGrowth.Feed(foodAmount)` を与える（成長段階に寄与）。
- **インスペクター**: `foodAmount (float)`。
- **補足**: 必要に応じて `HeartVisual.radius` を参照可能。

## BubbleZone.cs
- **効果**: 侵入で捕獲→内部で吸引・減速。閾値到達で一斉解放（バースト）。
- **インスペクター**
  - `threshold (int)`: 捕獲数で破裂。
  - `pullStrength (float)`: 中心への引き寄せ力。
  - `damping (float)`: 内部ダンピング（追加減速）。
  - `initialSlowdown (0..1)`: 初回吸い込み時の減速係数。
- **内部**: `HashSet<int> captured` で捕獲管理。`Burst()` で全解放。

## SoundObstacle.cs
- **効果**: SE 再生 + 法線反射（減衰）。
- **インスペクター**: `soundKey (string)`。
