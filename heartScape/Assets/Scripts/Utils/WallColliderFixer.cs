using UnityEngine;

/// <summary>
/// 壁オブジェクトのコライダー設定を自動修正するスクリプト
/// </summary>
public class WallColliderFixer : MonoBehaviour
{
    [Header("自動修正設定")]
    [Tooltip("自動でコライダー設定を修正する")]
    public bool autoFixOnAwake = true;
    
    [Tooltip("修正対象のタグ")]
    public string[] targetTags = { "Wall", "Bumper", "Obstacle" };
    
    [Tooltip("修正対象の名前パターン")]
    public string[] targetNamePatterns = { "Wall_", "Bumper", "Obstacle" };
    
    void Awake()
    {
        if (autoFixOnAwake)
        {
            FixColliderSettings();
        }
    }
    
    /// <summary>
    /// コライダー設定を修正
    /// </summary>
    public void FixColliderSettings()
    {
        // このオブジェクトのコライダーを修正
        FixObjectCollider(gameObject);
        
        // 子オブジェクトのコライダーも修正
        Collider2D[] childColliders = GetComponentsInChildren<Collider2D>();
        foreach (Collider2D collider in childColliders)
        {
            FixObjectCollider(collider.gameObject);
        }
    }
    
    /// <summary>
    /// 指定オブジェクトのコライダー設定を修正
    /// </summary>
    void FixObjectCollider(GameObject obj)
    {
        // タグチェック
        bool isTarget = false;
        foreach (string tag in targetTags)
        {
            try
            {
                if (obj.CompareTag(tag))
                {
                    isTarget = true;
                    break;
                }
            }
            catch (System.Exception)
            {
                // タグが存在しない場合はスキップ
            }
        }
        
        // 名前パターンチェック
        if (!isTarget)
        {
            foreach (string pattern in targetNamePatterns)
            {
                if (obj.name.Contains(pattern))
                {
                    isTarget = true;
                    break;
                }
            }
        }
        
        // 対象でない場合はスキップ
        if (!isTarget)
            return;
        
        // Collider2Dの設定を修正
        Collider2D collider = obj.GetComponent<Collider2D>();
        if (collider != null)
        {
            // Is Triggerをオフにする
            if (collider.isTrigger)
            {
                collider.isTrigger = false;
                Debug.Log($"[WallColliderFixer] Fixed trigger setting for {obj.name}");
            }
        }
        
        // Rigidbody2Dの設定を確認・追加
        Rigidbody2D rb = obj.GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            // 壁オブジェクトにはStatic Rigidbody2Dを追加
            rb = obj.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Static;
            rb.gravityScale = 0f;
            Debug.Log($"[WallColliderFixer] Added Static Rigidbody2D to {obj.name}");
        }
        else
        {
            // 既存のRigidbody2Dの設定を確認
            if (rb.bodyType != RigidbodyType2D.Static && rb.bodyType != RigidbodyType2D.Kinematic)
            {
                rb.bodyType = RigidbodyType2D.Static;
                Debug.Log($"[WallColliderFixer] Fixed Rigidbody2D type for {obj.name}");
            }
        }
    }
    
    /// <summary>
    /// シーン内のすべての壁オブジェクトを修正
    /// </summary>
    [ContextMenu("Fix All Wall Colliders")]
    public void FixAllWallColliders()
    {
        // シーン内のすべてのオブジェクトをチェック
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        
        foreach (GameObject obj in allObjects)
        {
            // タグチェック
            bool isTarget = false;
            foreach (string tag in targetTags)
            {
                try
                {
                    if (obj.CompareTag(tag))
                    {
                        isTarget = true;
                        break;
                    }
                }
                catch (System.Exception)
                {
                    // タグが存在しない場合はスキップ
                }
            }
            
            // 名前パターンチェック
            if (!isTarget)
            {
                foreach (string pattern in targetNamePatterns)
                {
                    if (obj.name.Contains(pattern))
                    {
                        isTarget = true;
                        break;
                    }
                }
            }
            
            if (isTarget)
            {
                FixObjectCollider(obj);
            }
        }
        
        Debug.Log("[WallColliderFixer] Fixed all wall colliders in scene");
    }
}
