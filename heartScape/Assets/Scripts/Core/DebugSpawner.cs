using UnityEngine;

// 1/2/3/4 キーで A/B/C/D スイッチ由来の心・位置情報を模擬し、Gateway に投げる。
// Space 長押しでランダム、R=10連射、C=全消去。
public class DebugSpawner : MonoBehaviour
{
    [Header("Refs")]
    public HeartGateway gateway;
    public HeartManager manager; // Cキーの全消去に使用（任意）:contentReference[oaicite:9]{index=9}

    [Header("Presets (for 1-4)")]
    public HeartProfile presetA = new HeartProfile { uid="DBG_01", hr=72,  cv=0.18f, range=16, mean=78 };
    public HeartProfile presetB = new HeartProfile { uid="DBG_02", hr=95,  cv=0.32f, range=28, mean=88 };
    public HeartProfile presetC = new HeartProfile { uid="DBG_03", hr=60,  cv=0.10f, range=12, mean=70 };
    public HeartProfile presetD = new HeartProfile { uid="DBG_04", hr=110, cv=0.45f, range=35, mean=92 };

    [Header("Continuous Spawn")]
    public bool  holdToSpawn = true;
    public float spawnPerSec = 6f;
    float spawnTimer;

    void Awake()
    {
        HeartDB.Load(); // DB を使う場合に備えて初期化（存在しなければ既定値を返す）。:contentReference[oaicite:10]{index=10}
        if (gateway == null) gateway = FindFirstObjectByType<HeartGateway>();
        if (manager == null) manager = FindFirstObjectByType<HeartManager>();
    }

    void Update()
    {
        if (gateway == null) return;

        // ワンショット（プリセット）
        if (Input.GetKeyDown(KeyCode.Alpha1)) SendPreset(1, presetA);
        if (Input.GetKeyDown(KeyCode.Alpha2)) SendPreset(2, presetB);
        if (Input.GetKeyDown(KeyCode.Alpha3)) SendPreset(3, presetC);
        if (Input.GetKeyDown(KeyCode.Alpha4)) SendPreset(4, presetD);

        // ランダム
        if (Input.GetKeyDown(KeyCode.Space)) SendRandom();

        // 連続（Space 長押し）
        if (holdToSpawn && Input.GetKey(KeyCode.Space))
        {
            spawnTimer += Time.deltaTime * spawnPerSec;
            while (spawnTimer >= 1f)
            {
                spawnTimer -= 1f;
                SendRandom();
            }
        }
        else spawnTimer = 0f;

        // 便利キー
        if (Input.GetKeyDown(KeyCode.R)) for (int i = 0; i < 10; i++) SendRandom();
        if (Input.GetKeyDown(KeyCode.C) && manager != null) manager.ClearAll();
    }

    void SendPreset(int sw, HeartProfile hp)
    {
        var msg = new HeartGateway.HeartInput {
            uid = hp.uid, switchNo = sw,
            hr  = hp.hr,  cv = hp.cv, range = hp.range, mean = hp.mean
        };
        gateway.Inject(msg);
    }

    void SendRandom()
    {
        int sw = Random.Range(1, 5);
        var hp = new HeartProfile {
            uid = "DBG_RND_" + Random.Range(0, 999999).ToString("D6"),
            hr = Random.Range(55f, 115f),
            cv = Random.Range(0.05f, 0.5f),
            range = Random.Range(10f, 35f),
            mean = Random.Range(65f, 95f)
        };
        var msg = new HeartGateway.HeartInput {
            uid = hp.uid, switchNo = sw,
            hr = hp.hr, cv = hp.cv, range = hp.range, mean = hp.mean
        };
        gateway.Inject(msg);
    }
}
