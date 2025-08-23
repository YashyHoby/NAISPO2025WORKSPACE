using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// キーボード入力で仮ユーザをスポーンするデバッグ用コンポーネント。
/// Main シーンの空オブジェクトに付け、HeartManager を割り当てて使う。
/// </summary>
public class DebugSpawner : MonoBehaviour
{
    [Header("Refs")]
    public HeartManager manager;

    [Header("Spawn Settings")]
    public Vector2 spawnXRange = new Vector2(-8.5f, -7.5f);
    public Vector2 spawnYRange = new Vector2(-3.5f, 3.5f);
    public Vector2 speedRange = new Vector2(0.5f, 1.0f); // “押下速度”っぽい0-1規格値
    public Vector2 vxRange = new Vector2(1.0f, 4.0f); // 初速X（右向き）
    public Vector2 vyRange = new Vector2(-1.0f, 1.0f); // 初速Y

    [Header("Continuous Spawn")]
    public bool holdToSpawn = true;      // キー押しっぱなしで連続スポーン
    public float spawnPerSec = 6f;       // 1秒あたり生成数（押下中）
    float spawnTimer;

    // 仮ユーザ・プリセット（数字キー 1〜4 でスポーン）
    Dictionary<KeyCode, HeartProfile> presets;

    void Awake()
    {
        HeartDB.Load(); // Resources/heart_db.json を読む（無ければ Get() がデフォルト生成）
        presets = new Dictionary<KeyCode, HeartProfile> {
            { KeyCode.Alpha1, new HeartProfile{ uid="DBG_01", hr=72, cv=0.18f, range=16, mean=78 } }, // 穏やか
            { KeyCode.Alpha2, new HeartProfile{ uid="DBG_02", hr=95, cv=0.32f, range=28, mean=88 } }, // 活発
            { KeyCode.Alpha3, new HeartProfile{ uid="DBG_03", hr=60, cv=0.10f, range=12, mean=70 } }, // 低め
            { KeyCode.Alpha4, new HeartProfile{ uid="DBG_04", hr=110,cv=0.45f, range=35, mean=92 } }, // 高め＆乱れ
        };
    }

    void Update()
    {
        // ワンショット生成（Space = ランダム、1〜4 = プリセット）
        if (Input.GetKeyDown(KeyCode.Space)) SpawnRandom();
        if (Input.GetKeyDown(KeyCode.Alpha1)) SpawnPreset(KeyCode.Alpha1);
        if (Input.GetKeyDown(KeyCode.Alpha2)) SpawnPreset(KeyCode.Alpha2);
        if (Input.GetKeyDown(KeyCode.Alpha3)) SpawnPreset(KeyCode.Alpha3);
        if (Input.GetKeyDown(KeyCode.Alpha4)) SpawnPreset(KeyCode.Alpha4);

        // 連続生成（Space長押し）
        if (holdToSpawn && Input.GetKey(KeyCode.Space))
        {
            spawnTimer += Time.deltaTime * spawnPerSec;
            while (spawnTimer >= 1f)
            {
                spawnTimer -= 1f;
                SpawnRandom();
            }
        }
        else spawnTimer = 0f;

        // 便利キー：R=雨のように10体、C=全消去、T=しきい値調整の目安に1体スポーン
        if (Input.GetKeyDown(KeyCode.R)) for (int i = 0; i < 10; i++) SpawnRandom();
        if (Input.GetKeyDown(KeyCode.C)) manager.ClearAll();
        if (Input.GetKeyDown(KeyCode.T)) SpawnPreset(KeyCode.Alpha1);
    }

    void SpawnPreset(KeyCode key)
    {
        if (!presets.TryGetValue(key, out var hp)) return;
        Vector2 pos = new Vector2(Random.Range(spawnXRange.x, spawnXRange.y),
                                  Random.Range(spawnYRange.x, spawnYRange.y));
        float speed = Random.Range(speedRange.x, speedRange.y);
        Vector2 vel = new Vector2(Mathf.Lerp(vxRange.x, vxRange.y, speed),
                                  Random.Range(vyRange.x, vyRange.y));
        manager.Spawn(hp, pos, vel);
    }

    void SpawnRandom()
    {
        // ランダムな“仮ユーザ”を作成（色相=hr、形状乱数は HeartManager.Spawn 側で行う）
        var hp = new HeartProfile
        {
            uid = "DBG_RND_" + Random.Range(0, 999999).ToString("D6"),
            hr = Random.Range(55f, 115f),
            cv = Random.Range(0.05f, 0.5f),
            range = Random.Range(10f, 35f),
            mean = Random.Range(65f, 95f),
        };
        Vector2 pos = new Vector2(Random.Range(spawnXRange.x, spawnXRange.y),
                                  Random.Range(spawnYRange.x, spawnYRange.y));
        float speed = Random.Range(speedRange.x, speedRange.y);
        Vector2 vel = new Vector2(Mathf.Lerp(vxRange.x, vxRange.y, speed),
                                  Random.Range(vyRange.x, vyRange.y));

        manager.Spawn(hp, pos, vel);
    }
}
