# IO / Gateway

外部アプリ（BLE→Python→UDP）からのメッセージを受け、指定の射出点からスポーン。

## HeartGateway.cs
- **役割**: UDP受信スレッド → ConcurrentQueue → `Update()` で安全に取り出し → `HeartManager.Spawn()`。
- **インスペクター**
  - **Refs**
    - `manager (HeartManager)`
  - **Emitters (A,B,C,D)**
    - `emitters[0..3] (Transform)`: 4スイッチの発射位置/向き。**`Transform.right` が前方**。
  - **Launch Params**
    - `baseSpeed (float)`: 基本速度
    - `speedJitter (float)`: 速度ぶれ（±）
    - `spreadDeg (float)`: 方向拡散角（±deg）
  - **UDP (optional)**
    - `useUdp (bool)`, `listenPort (int)`
- **メッセージ形式（JSON）**
  ```json
  {
    "uid": "DBG_01",
    "switchNo": 1,     // 1..4 (A=1,B=2,C=3,D=4)
    "hr": 72,
    "cv": 0.18,
    "range": 16,
    "mean": 78
  }
