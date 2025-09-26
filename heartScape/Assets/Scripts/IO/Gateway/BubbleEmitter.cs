using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace HeartScape.IO.Gateway
{
    /// <summary>
    /// シンプルな泡エミッター（HeartTrail方式を参考）
    /// </summary>
    public class BubbleEmitter : MonoBehaviour
    {
        [Header("泡の設定")]
        [SerializeField] public bool enableBubbleEmission = true;
        [SerializeField] public float bubbleEmissionRate = 2f;
        [SerializeField] public float bubbleLifetime = 3f;
        [SerializeField] public float bubbleSize = 0.1f;
        [SerializeField] public float bubbleSpeed = 2f;
        [SerializeField] public Color bubbleColor = new Color(0.7f, 0.7f, 0.7f, 0.6f);
        
        [Header("方向性の設定")]
        [SerializeField] public bool useEmitterDirection = true;
        [SerializeField] public float bubbleDirectionStrength = 1f;
        [SerializeField] public float bubbleSpreadAngle = 30f;
        [SerializeField] public float bubbleDirectionFocus = 0.8f;
        
        [Header("デバッグ")]
        [SerializeField] public bool enableDebugLogs = false;
        
        // 内部変数
        private float emissionTimer = 0f;
        private List<GameObject> activeBubbles = new List<GameObject>();
        private Transform bubbleContainer;
        
        void Awake()
        {
            // 泡のコンテナを作成
            CreateBubbleContainer();
        }
        
        void Update()
        {
            if (enableBubbleEmission)
            {
                UpdateBubbleEmission();
            }
            
            // 古い泡をクリーンアップ
            CleanupOldBubbles();
        }
        
        void CreateBubbleContainer()
        {
            GameObject container = new GameObject("BubbleContainer");
            container.transform.SetParent(transform);
            container.transform.localPosition = Vector3.zero;
            bubbleContainer = container.transform;
        }
        
        void UpdateBubbleEmission()
        {
            emissionTimer += Time.deltaTime;
            
            if (emissionTimer >= 1f / bubbleEmissionRate)
            {
                CreateBubble();
                emissionTimer = 0f;
            }
        }
        
        void CreateBubble()
        {
            // 泡のGameObjectを作成
            GameObject bubble = new GameObject("Bubble");
            bubble.transform.SetParent(bubbleContainer);
            bubble.transform.localPosition = Vector3.zero;
            
            // SpriteRendererを追加
            SpriteRenderer spriteRenderer = bubble.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = CreateBubbleSprite();
            spriteRenderer.color = bubbleColor;
            spriteRenderer.sortingOrder = 1;
            
            // Rigidbody2Dを追加
            Rigidbody2D rb = bubble.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.linearDamping = 0.5f;
            
            // Collider2Dを追加（泡のサイズに合わせて）
            CircleCollider2D collider = bubble.AddComponent<CircleCollider2D>();
            collider.radius = bubbleSize * 0.5f;
            collider.isTrigger = true;
            
            // 泡の動きを設定
            SetBubbleMovement(bubble, rb);
            
            // 泡のライフサイクルを開始
            StartCoroutine(BubbleLifecycle(bubble));
            
            // アクティブリストに追加
            activeBubbles.Add(bubble);
            
            if (enableDebugLogs)
            {
                Debug.Log($"[BubbleEmitter] Bubble created at {transform.position}");
            }
        }
        
        void SetBubbleMovement(GameObject bubble, Rigidbody2D rb)
        {
            // 基本方向を取得
            Vector2 baseDirection = useEmitterDirection ? transform.right : Vector2.right;
            
            // 広がり角度を計算
            float spreadRad = bubbleSpreadAngle * Mathf.Deg2Rad * 0.5f;
            float randomAngle = Random.Range(-spreadRad, spreadRad);
            
            // 方向の集中度を適用
            float focusFactor = 1f - bubbleDirectionFocus;
            randomAngle *= focusFactor;
            
            // 最終的な方向を計算
            Vector2 finalDirection = RotateVector(baseDirection, randomAngle);
            
            // 速度を設定
            float speed = bubbleSpeed * bubbleDirectionStrength;
            rb.linearVelocity = finalDirection * speed;
            
            // サイズを設定
            bubble.transform.localScale = Vector3.one * bubbleSize;
        }
        
        IEnumerator BubbleLifecycle(GameObject bubble)
        {
            float lifetime = bubbleLifetime;
            float timer = 0f;
            
            SpriteRenderer spriteRenderer = bubble.GetComponent<SpriteRenderer>();
            Color originalColor = spriteRenderer.color;
            
            while (timer < lifetime)
            {
                timer += Time.deltaTime;
                
                // フェードアウト
                float alpha = Mathf.Lerp(1f, 0f, timer / lifetime);
                spriteRenderer.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
                
                // サイズの変化（オプション）
                float sizeFactor = Mathf.Lerp(1f, 1.5f, timer / lifetime);
                bubble.transform.localScale = Vector3.one * bubbleSize * sizeFactor;
                
                yield return null;
            }
            
            // 泡を削除
            if (bubble != null)
            {
                activeBubbles.Remove(bubble);
                Destroy(bubble);
            }
        }
        
        void CleanupOldBubbles()
        {
            // nullの泡をリストから削除
            activeBubbles.RemoveAll(bubble => bubble == null);
        }
        
        Sprite CreateBubbleSprite()
        {
            // シンプルな円形スプライトを作成
            Texture2D texture = new Texture2D(32, 32);
            Color[] pixels = new Color[32 * 32];
            
            Vector2 center = new Vector2(16, 16);
            float radius = 15f;
            
            for (int y = 0; y < 32; y++)
            {
                for (int x = 0; x < 32; x++)
                {
                    Vector2 pos = new Vector2(x, y);
                    float distance = Vector2.Distance(pos, center);
                    
                    if (distance <= radius)
                    {
                        float alpha = 1f - (distance / radius) * 0.5f;
                        pixels[y * 32 + x] = new Color(1f, 1f, 1f, alpha);
                    }
                    else
                    {
                        pixels[y * 32 + x] = Color.clear;
                    }
                }
            }
            
            texture.SetPixels(pixels);
            texture.Apply();
            
            return Sprite.Create(texture, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f));
        }
        
        Vector2 RotateVector(Vector2 vector, float angle)
        {
            float cos = Mathf.Cos(angle);
            float sin = Mathf.Sin(angle);
            return new Vector2(
                vector.x * cos - vector.y * sin,
                vector.x * sin + vector.y * cos
            );
        }
        
        public void SetEmissionEnabled(bool enabled)
        {
            enableBubbleEmission = enabled;
        }
        
        public void SetBubbleSettings(float emissionRate, float lifetime, float size, float speed, Color color)
        {
            bubbleEmissionRate = emissionRate;
            bubbleLifetime = lifetime;
            bubbleSize = size;
            bubbleSpeed = speed;
            bubbleColor = color;
        }
        
        public void SetDirectionSettings(bool useDirection, float strength, float spreadAngle, float focus)
        {
            useEmitterDirection = useDirection;
            bubbleDirectionStrength = strength;
            bubbleSpreadAngle = spreadAngle;
            bubbleDirectionFocus = focus;
        }
        
        void OnDestroy()
        {
            // すべての泡をクリーンアップ
            foreach (GameObject bubble in activeBubbles)
            {
                if (bubble != null)
                {
                    Destroy(bubble);
                }
            }
            activeBubbles.Clear();
        }
    }
}

