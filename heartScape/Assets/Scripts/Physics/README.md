# Physics

流れ（Flow）・抗力・擬似重力・拍動推進など、Rigidbody2D の運動。

## HeartPhysics.cs
- **役割**: `FlowManager2D` から速度場をサンプルし、押し流し＋抗力＋拍動などを合成して `rb.AddForce()`。
- **インスペクター**
  - `flow (FlowManager2D)`: 未設定なら自動検索。
  - **Refs (optional)**
    - `profile (HeartProfile)`: 未設定なら `HeartVisual.profile` を拝借、さらに無ければ `defaultBpm`。
    - `defaultBpm (float)`：プロファイル未設定時の BPM。
  - **Flow Push (B-plan)**
    - `alignK (float)`: 流速uの押し係数。
  - **Quadratic Drag**
    - `quadDragK (float)`: 2次抗力係数。0で不使用。
  - **Effective Gravity**
    - `effectiveGravity (float)`: 疑似重力（下向き等）。
  - **Clamp**
    - `clampSpeed (bool)`, `maxSpeed (float)`: 最高速制限。
  - **Min Speed (keep alive)**
    - `enforceMinSpeed (bool)`, `minSpeed (float)`,
    - `setVelocityHard (bool)`: 低速補正を速度直接/力付与で選択。
    - `keepAliveAccel (float)`: 力モード時の加速強さ。
  - **Pulse Swim (jellyfish)**
    - `pulseSwimEnabled (bool)`
    - `pulseForce (float)`, `pulseDuration (float)`
    - `pulseDirVelBias (0..1)`: 0=流れ方向,1=現在速度方向へ強調。
    - `pulseAsImpulse (bool)`: インパルスか連続力か。
    - `pulseBpmRange (Vector2)`: 拍動周波数変換用の BPM 範囲。
- **内部調整**
  - Random ノイズのシード/周波数（`RandomDir()` / Perlin 周波数）、
  - `FixedUpdate()` の合力合成式をカスタム可能。
