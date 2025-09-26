using UnityEngine;
using HeartScape.IO.Gateway;

// 1/2/3/4 キーで A/B/C/D を発射。DBモード時は UID→DB 参照。
// 通常モード時はプリセット値をパイロード送信。
// Space 長押しでランダム、R=10連射、C=全消去、F5=DBリロード。
public class DebugSpawner : MonoBehaviour
{
    [Header("Refs")]
    public HeartGateway gateway;
    public HeartManager manager; // Cキーの全消去に使用（任意）

    [Header("Mode")]
    public bool useHeartDB = true;

    [Header("UIDs (DB mode for 1-4)")]
    public string uid1 = "DBG_01";
    public string uid2 = "DBG_02";
    public string uid3 = "DBG_03";
    public string uid4 = "DBG_04";

    [Header("Presets (payload mode for 1-4)")]
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
        HeartDB.Load();
        if (gateway == null) gateway = FindFirstObjectByType<HeartGateway>();
        if (manager == null) manager  = FindFirstObjectByType<HeartManager>();
    }

    void Update()
    {
        if (gateway == null) return;

        // 1..4
        if (Input.GetKeyDown(KeyCode.Alpha1)) FireKey(1, presetA, uid1);
        if (Input.GetKeyDown(KeyCode.Alpha2)) FireKey(2, presetB, uid2);
        if (Input.GetKeyDown(KeyCode.Alpha3)) FireKey(3, presetC, uid3);
        if (Input.GetKeyDown(KeyCode.Alpha4)) FireKey(4, presetD, uid4);

        // ランダム（Spaceを押した瞬間）
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
        if (Input.GetKeyDown(KeyCode.F5)) HeartDB.Reload();
    }

    void FireKey(int sw, HeartProfile preset, string uid)
    {
        HeartProfile profile = preset;

        if (useHeartDB && !string.IsNullOrEmpty(uid))
        {
            HeartDB.Load(true);
            profile = HeartDB.Get(uid);
        }

        if (profile == null)
        {
            Debug.LogWarning("[DebugSpawner] Profile not found for key " + sw + " (uid: " + uid + ")");
            return;
        }

        string resolvedUid = !string.IsNullOrEmpty(profile.uid) ? profile.uid : uid;
        if (string.IsNullOrEmpty(resolvedUid))
        {
            resolvedUid = $"DBG_{sw}";
        }

        var msg = new HeartGateway.HeartInput
        {
            uid      = resolvedUid,
            switchNo = sw,
            hr       = profile.hr,
            cv       = profile.cv,
            range    = profile.range,
            mean     = profile.mean
        };

        gateway.Inject(msg);
    }

    void SendRandom()
    {
        int sw = Random.Range(1, 5);
        if (useHeartDB)
        {
            string uid = "DBG_RND_" + Random.Range(0, 999999).ToString("D6");
            gateway.Inject(new HeartGateway.HeartInput { uid = uid, switchNo = sw, hr=0, cv=0, range=0, mean=0 });
        }
        else
        {
            var hp = new HeartProfile {
                uid = "DBG_RND_" + Random.Range(0, 999999).ToString("D6"),
                hr = Random.Range(55f, 115f),
                cv = Random.Range(0.05f, 0.5f),
                range = Random.Range(10f, 35f),
                mean = Random.Range(65f, 95f)
            };
            gateway.Inject(new HeartGateway.HeartInput {
                uid = hp.uid, switchNo = sw, hr = hp.hr, cv = hp.cv, range = hp.range, mean = hp.mean
            });
        }
    }
}
