using System.Collections;
using UnityEngine;

namespace HeartScape.Interaction.Bumper
{
    [RequireComponent(typeof(Collider2D))]
    public class HeartBumper2D : MonoBehaviour
    {
        [Header("バウンド設定")]
        [Tooltip("衝突したハートに与える速度（m/s）。")]
        public float bounceSpeed = 12f;

        [Tooltip("既存の速度を上書きするかどうか。オフの場合は速度に加算します。")]
        public bool overrideVelocity = true;

        [Tooltip("押し出す方向に追加する上向きバイアスです（0 で無効）。")]
        public float upwardBias = 0f;

        [Header("ビジュアル演出")]
        [Tooltip("拡大／振動演出を適用する対象。未設定時はこのオブジェクトを使用します。")]
        public Transform visualTarget;

        [Tooltip("衝突時の最大拡大倍率です。1 を超える値を指定してください。")]
        public float visualScaleMultiplier = 1.15f;

        [Tooltip("振動演出が収束するまでの時間（秒）。")]
        public float visualDuration = 0.8f;

        [Tooltip("振動演出の減衰係数です。大きいほど早く収束します。")]
        public float visualShakeDamping = 2.5f;

        [Tooltip("振動演出の振動数（Hz）です。")]
        public float visualShakeFrequency = 7f;
        
        [Header("長方形コライダー設定")]
        [Tooltip("長方形の幅")]
        [Range(0.1f, 5.0f)]
        public float colliderWidth = 1.2f;
        
        [Tooltip("長方形の高さ")]
        [Range(0.1f, 5.0f)]
        public float colliderHeight = 0.8f;
        
        [Header("ビジュアル管理")]
        [Tooltip("スライムビジュアルオブジェクト")]
        public HeartScape.Interaction.Bumper.SlimeBumperVisual slimeVisual;

        // プライベート変数
        private Collider2D bumperCollider;
        private Vector3 baseScale = Vector3.one;
        private Coroutine visualRoutine;
        private BoxCollider2D boxCollider;

        void Awake()
        {
            InitializeBumper();
        }

        void Start()
        {
            UpdateColliderSize();
        }

        void OnValidate()
        {
            // OnValidateでは何もしない（エラーを避けるため）
            // 手動で「パラメータ更新」ボタンを使用してください
        }

        void InitializeBumper()
        {
            // BoxCollider2Dを確保
            EnsureBoxCollider();
            
            // ビジュアルオブジェクトを自動作成
            if (slimeVisual == null)
            {
                CreateSlimeVisualChild();
            }
        }

        void EnsureBoxCollider()
        {
            // CircleCollider2Dを削除してBoxCollider2Dに変更
            CircleCollider2D circleCollider = GetComponent<CircleCollider2D>();
            if (circleCollider != null)
            {
                bool wasTrigger = circleCollider.isTrigger;
                PhysicsMaterial2D physicsMaterial = circleCollider.sharedMaterial;
                
                if (Application.isPlaying)
                {
                    Destroy(circleCollider);
                }
                else
                {
                    DestroyImmediate(circleCollider);
                }
                
                boxCollider = gameObject.AddComponent<BoxCollider2D>();
                boxCollider.isTrigger = wasTrigger;
                boxCollider.sharedMaterial = physicsMaterial;
            }
            else
            {
                boxCollider = GetComponent<BoxCollider2D>();
                if (boxCollider == null)
                {
                    boxCollider = gameObject.AddComponent<BoxCollider2D>();
                }
            }

            bumperCollider = boxCollider;
        }

        void CreateSlimeVisualChild()
        {
            // 子オブジェクトを作成
            GameObject visualObj = new GameObject("SlimeVisual");
            visualObj.transform.SetParent(transform, false);
            visualObj.transform.localPosition = Vector3.zero;
            visualObj.transform.localRotation = Quaternion.identity;
            visualObj.transform.localScale = Vector3.one;

            // SlimeBumperVisualコンポーネントを追加
            slimeVisual = visualObj.AddComponent<HeartScape.Interaction.Bumper.SlimeBumperVisual>();
            
            // 初期設定
            slimeVisual.rectWidth = colliderWidth;
            slimeVisual.rectHeight = colliderHeight;
        }

        public void UpdateColliderSize()
        {
            if (boxCollider != null)
            {
                boxCollider.size = new Vector2(colliderWidth, colliderHeight);
            }
            
            // ビジュアルのサイズも同期（実行時のみ）
            if (slimeVisual != null && Application.isPlaying)
            {
                slimeVisual.rectWidth = colliderWidth;
                slimeVisual.rectHeight = colliderHeight;
                slimeVisual.UpdateVisualSize();
            }
        }

        void OnEnable()
        {
            if (visualTarget != null)
                baseScale = visualTarget.localScale;
        }

        void OnCollisionEnter2D(Collision2D collision)
        {
            if (!collision.collider) return;

            var heart = collision.collider.GetComponentInParent<HeartPhysics>();
            if (heart == null) return;

            var rb = heart.GetComponent<Rigidbody2D>();
            if (rb == null) return;

            Vector2 bumperPos = transform.position;
            Vector2 heartPos = rb.position;
            Vector2 dir = heartPos - bumperPos;

            if (dir.sqrMagnitude <= 1e-6f)
                dir = Random.insideUnitCircle;

            if (Mathf.Abs(upwardBias) > 1e-6f)
                dir += Vector2.up * upwardBias;

            dir = dir.normalized;

            Vector2 impulse = dir * bounceSpeed;

            if (overrideVelocity)
                rb.linearVelocity = impulse;
            else
                rb.linearVelocity += impulse;

            heart.NotifyExternalBounce(rb.linearVelocity.normalized, bounceSpeed);

            StartVisualShake();
            
            // ビジュアルにも衝突を通知
            if (slimeVisual != null)
            {
                slimeVisual.OnBumperCollision();
            }
        }

        void StartVisualShake()
        {
            if (visualTarget == null || visualDuration <= 0f)
                return;

            if (visualRoutine != null)
                StopCoroutine(visualRoutine);

            visualRoutine = StartCoroutine(VisualShakeRoutine());
        }

        IEnumerator VisualShakeRoutine()
        {
            float elapsed = 0f;
            Vector3 originalScale = baseScale;
            
            // ゼリー風の振動パラメータ
            float jellyFrequency = visualShakeFrequency;
            float jellyDamping = visualShakeDamping;
            float jellyAmplitude = (visualScaleMultiplier - 1.0f);

            while (elapsed < visualDuration)
            {
                float progress = elapsed / visualDuration;
                
                // ゼリー風の減衰振動
                float damping = Mathf.Exp(-jellyDamping * progress);
                
                // X軸とY軸で異なる振動（ゼリーらしい非対称な動き）
                float xShake = Mathf.Sin(elapsed * jellyFrequency * Mathf.PI * 2f) * damping * jellyAmplitude;
                float yShake = Mathf.Sin(elapsed * jellyFrequency * 1.3f * Mathf.PI * 2f + 0.5f) * damping * jellyAmplitude * 0.7f;
                
                // ゼリーの反発効果（最初に少し縮んでから拡大）
                float rebound = 1.0f;
                if (progress < 0.1f)
                {
                    float reboundProgress = progress / 0.1f;
                    rebound = 1.0f - (1.0f - reboundProgress) * 0.1f; // 最初に少し縮む
                }
                
                // 最終的なスケール計算
                Vector3 currentScale = originalScale * rebound;
                currentScale.x += xShake;
                currentScale.y += yShake;
                
                // 最小スケールを制限（負の値を防ぐ）
                currentScale.x = Mathf.Max(currentScale.x, originalScale.x * 0.8f);
                currentScale.y = Mathf.Max(currentScale.y, originalScale.y * 0.8f);

                if (visualTarget != null)
                    visualTarget.localScale = currentScale;

                elapsed += Time.deltaTime;
                yield return null;
            }

            // 最終的に元のスケールに戻す（ゼリーらしい弾性）
            float finalTime = 0f;
            float finalDuration = 0.2f;
            Vector3 currentFinalScale = visualTarget != null ? visualTarget.localScale : originalScale;
            
            while (finalTime < finalDuration)
            {
                float t = finalTime / finalDuration;
                
                // ゼリーらしい弾性のあるイージング
                float elasticT = t < 0.5f 
                    ? 2f * t * t 
                    : 1f - Mathf.Pow(-2f * t + 2f, 2f) / 2f;
                
                // 最後の小さな振動
                float finalShake = Mathf.Sin(t * 15f) * (1f - t) * 0.02f;
                Vector3 smoothScale = Vector3.Lerp(currentFinalScale, originalScale, elasticT);
                smoothScale += Vector3.one * finalShake;
                
                if (visualTarget != null)
                    visualTarget.localScale = smoothScale;
                
                finalTime += Time.deltaTime;
                yield return null;
            }

            if (visualTarget != null)
                visualTarget.localScale = originalScale;

            visualRoutine = null;
        }

        // 外部から呼び出し可能なメソッド
        public void ForceCreateVisualChild()
        {
            if (slimeVisual != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(slimeVisual.gameObject);
                }
                else
                {
                    DestroyImmediate(slimeVisual.gameObject);
                }
            }
            CreateSlimeVisualChild();
        }

        public void ForceConvertToBoxCollider()
        {
            EnsureBoxCollider();
            UpdateColliderSize();
        }
    }
}