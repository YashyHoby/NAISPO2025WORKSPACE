了解。**Main シーン構築～レンダリング設定～最適化**まで、迷わないように“クリック手順 + 具体値”で並べます。Unity 6（URP/2D）前提です。

---

# A. Main シーンを作る（ヒエラルキー構成）

1. **シーン作成**

* `File > New Scene` → 2D/URP テンプレで作成 → `Assets/Scenes/Main.unity` として保存。

2. **カメラ確認**

* `Main Camera` を選択

  * Projection: **Orthographic**
  * Size: **5**（例。ワールド±X=9〜10, ±Y=5〜6 で想定）
  * Background: **#000000** 近い暗色（とろけ表現が映える）

3. **管理オブジェクト作成**

* `GameObject > Create Empty` を 5つ作成して、下記の名前に変更：

  * `HeartManager`
  * `EventApplier`
  * `AudioHub`
  * `UdpReceiver`
  * `UnityMainThreadDispatcher`

4. **スクリプトをアタッチ**

* `HeartManager` に `HeartManager.cs`

  * `Agent Prefab` に `Assets/Prefabs/HeartAgent.prefab` をドラッグ
  * `Max Agents`: **120**（最初は 100〜140 の範囲）
  * `Cell Size`: **1.0**（後で調整）
* `EventApplier` に `EventApplier.cs`

  * `Manager` に `HeartManager` をドラッグ
  * `Zones` は後で Event を作ってからドラッグで追加
* `AudioHub` に `AudioHub.cs`

  * `Add Component > AudioSource` を追加 → `Output` は空でもOK
  * `srcOneShot` にいま追加した `AudioSource` をドラッグ
  * `hitSoft/hitHard/bubbleBurst/metal/wood/membrane/water` に AudioClip を割り当て（なければ一旦空でも動きます）
* `UdpReceiver` に `UdpReceiver.cs`

  * `Listen Port`: **9000**（ESP32 側と合わせる）
  * `Manager` に `HeartManager` をドラッグ
* `UnityMainThreadDispatcher` に `UnityMainThreadDispatcher.cs`（スクリプトだけ。Inspectorに項目はありません）

---

# B. イベントオブジェクト（Zones）を置く

> 各ゾーンは **Empty + 可視化用のGizmo** でOK。半径は `EventZone.radius` を使います。

1. **FoodZone（餌）を複数**

* `GameObject > Create Empty` → 名前 `Food_A`
* 位置例：`(x= -2.0, y= 2.5, z=0)`
* `Add Component > FoodZone`

  * `Radius`: **1.2**
  * `Grow`: **0.04**
  * `R Max`: **1.2**
* 同様に `Food_B` `Food_C` など 2〜4個、画面の上下左右に配置

2. **BubbleZone（捕獲→破裂）**

* `GameObject > Create Empty` → `Bubble_A`
* 位置例：`(x= 3.0, y= -1.5, z=0)`
* `Add Component > BubbleZone`

  * `Radius`: **1.3**
  * `Threshold`: **10**（10個で破裂）
* 必要なら 2個目 `Bubble_B` を配置（位置は離す）

3. **FlowFieldZone（海流）**

* `GameObject > Create Empty` → `Flow_A`
* 位置例：`(x= 0.0, y= 0.0)`
* `Add Component > FlowFieldZone`

  * `Radius`: **3.0**
  * `Strength`: **2.0**
* 2個目 `Flow_B` を画面端に設置し、押し流す方向が偏らない配置に

4. **SoundObstacle（音障害物）**

* `GameObject > Create Empty` → `Snd_Metal`
* 位置例：`(x= -4.0, y= 0.0)`
* `Add Component > SoundObstacle`

  * `Radius`: **1.0**
  * `Sound Key`: **metal**（`AudioHub` に対応するキー）
* 素材を変えて複数：

  * `Snd_Wood`（`wood`）, `Snd_Membrane`（`membrane`）, `Snd_Water`（`water`）

5. **EventApplier に Zones を登録**

* `EventApplier` を選択 → `Zones (Size)` を **配置数** に設定（例：8〜12）
* 生成した `Food_*`, `Bubble_*`, `Flow_*`, `Snd_*` を **順にドラッグ**して埋める
  → これで `FixedUpdate()` 毎に `Contains()`→`Apply()` が呼ばれます

> **Gizmos表示**：`Scene` ビューの左上 `Gizmos` をオンにすると `EventZone` の半径が水色のワイヤで見えます。微調整に便利。

---

# C. URP：Full Screen Pass（メタボール閾値）

> ※ **まずは無しでもOK**（加算合成だけで“とろけ”ます）。見た目を上げたいタイミングで追加して下さい。

1. **Renderer Data を開く**

* `Project > Assets/Settings/UniversalRenderPipelineAsset` または
  `Project Settings > Graphics > Scriptable Render Pipeline Settings` から
  **URP Renderer（2D Renderer Data）** を見つけてクリック（`*_Renderer2D.asset` のような名前）

2. **Full Screen Pass Renderer Feature を追加**

* `Add Renderer Feature` → **Full Screen Pass** を追加（名称 `MetaballPass` など）
* `Material` に `MetaballThreshold` マテリアルを割り当て
* `Injection Point` は **After Rendering Transparents** が無難
* `Pass Name` は空でOK（Materialのパス名使用）
* **Opaque Texture** が **On** になっているか 2D Renderer の設定で確認

3. **`MetaballThreshold` の調整**

* `_Thresh`: **0.55 \~ 0.70**（加算輝度の閾値。下げる＝とろけ広がる／上げる＝締まる）
* `_Edge`: **0.10 \~ 0.20**（簡易エッジ強度）

---

# D. プレハブ＆スポーン確認

1. `HeartManager` の `Agent Prefab` に `HeartAgent.prefab` が入っているか確認
2. **テストスポーン**：`HeartManager` にテスト関数を作らなくても、`UdpReceiver` の `Handle()` を一時的に `Start()` で呼び、仮UIDでスポーンさせると動き確認が早いです。

   ```csharp
   // UdpReceiver.cs
   void Start(){
       HeartDB.Load();
       // テスト: 3体スポーン
       var hp = HeartDB.Get("AB12CD34");
       Handle("uid:AB12CD34,speed:0.7,button:1");
       Handle("uid:EF56AA99,speed:0.3,button:1");
       Handle("uid:TEST1234,speed:0.9,button:1");
       // その後に本来の受信を開始
       client = new UdpClient(listenPort);
       client.BeginReceive(OnRecv, null);
   }
   ```

   テスト後は戻すのを忘れないでください。

---

# E. 最適化・安定化（実践手順）

1. **最大個数の固定**

   * `HeartManager.maxAgents`: **120** から開始
   * 実測 FPS を見ながら、80〜160 の範囲で最適点を決める
   * **Spawn 前に上限チェック**して捨てる（実装済）

2. **セルサイズの調整**

   * `cellSize ≈ 平均半径 × 2` が目安
   * 例：平均半径 0.5 なら **1.0**
   * too small → セル数増え過ぎ（オーバーヘッド）
     too large → 近傍が粗くなり無駄判定が増える
   * ステージの密度に合わせて **0.8〜1.4** を試す

3. **ソフト粒度（シェーダ）**

   * `AgentSDF_Unlit` の `_Softness`: **0.015〜0.03**
   * 小さい→輪郭シャープ・合成弱／大きい→ふわっと・加算増
   * まず **0.02**、混み合うなら **0.015** に下げる

4. **オブジェクトプール（W2〜W3）**

   * 連続 Spawn/Destroy が増えて GC スパイクする場合に導入
   * 最小実装方針：

     * `Queue<GameObject> pool;`
     * `Spawn()`：`pool.Count>0` なら `Dequeue()` して再利用、無ければ `Instantiate`
     * `Despawn()`：`SetActive(false)` にして `pool.Enqueue()`
   * **注意**：`HeartAgent` のマテリアルは Awake で複製しているので、再利用時に**色や半径を毎回上書き**する

5. **ログとメトリクス**

   * `Application.logMessageReceived += (c,s,t)=>{ /* ファイルに追記 */ };`
   * 1分毎に `agents.Count`, `UDP 受信数`, `平均FPS` を `Debug.Log`（のちテキストUIに表示してもOK）

6. **連続稼働テスト（ソーク）**

   * エディタではなく **ビルド版**でテスト
   * 2〜3時間連続で入力（自動スポーンに切替も可）
   * 観察ポイント：FPS の底、GC Alloc、LED 落ち、Wi‑Fi ドロップ、例外の有無
   * 落ち所が見えたら：`maxAgents` 減、`cellSize` 見直し、`_Softness` 下げ、FullScreenPass 一旦 OFF

---

# F. よくある詰まり & 解決

* **Agent の色や半径が全員同じになる**
  → `HeartAgent.Awake()` で **Material を個体ごとに new Material()** しているか確認
* **Full Screen Pass が効かない**
  → Renderer Data の **Opaque Texture ON**／Injection を **After Transparents** に
* **イベントが効いていない**
  → `EventApplier.zones` に**正しくドラッグ**したか／Gizmos 半径内に入っているか／`Radius` が小さすぎないか
* **音が鳴らない**
  → `AudioHub` に `AudioSource` が割り当て済みか、`PlayOnAwake` OFF・`Spatial Blend=0`（2D）か確認

---

# G. 画づくりの仕上げ（短時間で効く）

* 背景：**暗めの単色～緩いグラデ**（URP の 2D ライトは不要でもOK）
* Agent 色相：`Color.HSVToRGB( hr 正規化, 0.7〜0.8, 1.0 )`（実装済）
* Full Screen Pass：`_Thresh 0.6` / `_Edge 0.15` を出発点に微調整
* LED：WLED の輝度制限（**50〜70%**）と電源余裕（\*\*+30%\*\*目安）

---

必要なら、**プール実装の雛形**や、**FPS/エージェント数/UDP受信数を表示する極小UI（TextMeshPro）**、**URP Renderer Data のスクショ手順**も追加入れます。今の段階で一番つまずきそうなところはどこですか？
