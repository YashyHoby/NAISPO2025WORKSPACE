using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class AbsorberFloatingPhysics : MonoBehaviour
{
    [Header("浮遊設定")]
    [Tooltip("浮力の強さ")]
    [Range(0f, 20f)]
    public float buoyancy = 8f;
    
    [Tooltip("浮遊の安定性（高いほど安定）")]
    [Range(0f, 10f)]
    public float stability = 3f;
    
    [Tooltip("浮遊の減衰（高いほど早く安定）")]
    [Range(0f, 5f)]
    public float damping = 1f;
    
    [Header("移動設定")]
    [Tooltip("目標速度")]
    [Range(0f, 10f)]
    public float targetSpeed = 2f;
    
    [Tooltip("速度調整の強さ")]
    [Range(0f, 10f)]
    public float speedAdjustment = 5f;
    
    [Header("流れの影響")]
    [Tooltip("流れの影響を受ける強さ")]
    [Range(0f, 5f)]
    public float flowInfluence = 1f;
    
    [Tooltip("FlowManager2Dへの参照（未設定なら自動取得）")]
    public FlowManager2D flowManager;
    
    [Header("反射設定")]
    [Tooltip("反射の強さ")]
    [Range(0f, 2f)]
    public float reflectionStrength = 1f;
    
    [Tooltip("反射の減衰")]
    [Range(0f, 1f)]
    public float reflectionDamping = 0.8f;
    
    [Tooltip("心オブジェクトのタグ")]
    public string heartTag = "Heart";
    
    [Tooltip("心オブジェクトの名前パターン")]
    public string[] heartNamePatterns = { "HeartAgent", "Heart" };
    
    [Tooltip("反射しないオブジェクトの名前パターン")]
    public string[] ignoreReflectionPatterns = { "Bumper", "HeartAgent" };
    
    [Tooltip("壁オブジェクトの名前パターン（物理衝突を優先）")]
    public string[] wallNamePatterns = { "Wall_" };
    
    [Header("デバッグ")]
    [Tooltip("デバッグ情報を表示")]
    public bool showDebugInfo = false;
    
    private Rigidbody2D rb;
    private Vector2 lastVelocity;
    private Vector2 targetPosition;
    private float floatingHeight = 0f;
    private float lastReflectionTime = 0f;
    
    void Awake()
    {
        InitializeComponents();
    }
    
    void Start()
    {
        // Startでも初期化を確認
        if (rb == null)
        {
            InitializeComponents();
        }
    }
    
    void InitializeComponents()
    {
        rb = GetComponent<Rigidbody2D>();
        
        if (rb == null)
        {
            Debug.LogError("[AbsorberFloatingPhysics] Rigidbody2D component not found!");
            return;
        }
        
        // 重力を無効化
        rb.gravityScale = 0f;
        
        // コライダーの貫通を防ぐ設定
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        
        // Body Typeを確実にDynamicに設定
        rb.bodyType = RigidbodyType2D.Dynamic;
        
        // コライダーがTriggerでないことを確認
        Collider2D collider = GetComponent<Collider2D>();
        if (collider != null)
        {
            collider.isTrigger = false;
        }
        
        // 自動でFlowManagerを取得
        if (flowManager == null)
        {
            flowManager = FindObjectOfType<FlowManager2D>();
        }
        
        // 初期位置を記録
        targetPosition = transform.position;
        floatingHeight = transform.position.y;
    }
    
    void FixedUpdate()
    {
        if (rb == null)
            return;
            
        ApplyFloatingForce();
        ApplyFlowInfluence();
        ApplySpeedControl();
        ApplyReflection();
        ApplyWallDetection();
        UpdateFloatingHeight();
    }
    
    /// <summary>
    /// 浮遊力を適用
    /// </summary>
    void ApplyFloatingForce()
    {
        Vector2 currentPos = transform.position;
        Vector2 targetPos = new Vector2(currentPos.x, floatingHeight);
        
        // 目標位置への浮力
        Vector2 buoyancyForce = (targetPos - currentPos) * buoyancy;
        
        // 速度の減衰
        Vector2 dampingForce = -rb.linearVelocity * damping;
        
        // 安定性のための微調整
        Vector2 stabilityForce = Vector2.zero;
        if (Mathf.Abs(rb.linearVelocity.y) < 0.1f)
        {
            stabilityForce = (targetPos - currentPos) * stability * 0.1f;
        }
        
        rb.AddForce(buoyancyForce + dampingForce + stabilityForce);
    }
    
    /// <summary>
    /// 流れの影響を適用
    /// </summary>
    void ApplyFlowInfluence()
    {
        if (flowManager == null || flowInfluence <= 0f)
            return;
            
        Vector2 flowVelocity = flowManager.SampleVelocity(transform.position);
        Vector2 flowForce = flowVelocity * flowInfluence;
        
        rb.AddForce(flowForce);
    }
    
    /// <summary>
    /// 速度制御を適用
    /// </summary>
    void ApplySpeedControl()
    {
        if (targetSpeed <= 0f)
            return;
            
        Vector2 currentVelocity = rb.linearVelocity;
        float currentSpeed = currentVelocity.magnitude;
        
        // 目標速度との差を計算
        float speedDifference = targetSpeed - currentSpeed;
        
        if (Mathf.Abs(speedDifference) > 0.1f)
        {
            // 速度を調整
            Vector2 velocityDirection = currentVelocity.normalized;
            if (velocityDirection == Vector2.zero)
            {
                // 速度がゼロの場合はランダムな方向を設定
                velocityDirection = Random.insideUnitCircle.normalized;
            }
            
            Vector2 targetVelocity = velocityDirection * targetSpeed;
            Vector2 speedAdjustmentForce = (targetVelocity - currentVelocity) * speedAdjustment;
            
            rb.AddForce(speedAdjustmentForce);
        }
    }
    
    /// <summary>
    /// 反射を適用
    /// </summary>
    void ApplyReflection()
    {
        if (reflectionStrength <= 0f)
            return;
            
        // 現在の速度を記録
        Vector2 currentVelocity = rb.linearVelocity;
        
        // 速度が変化した場合、反射を適用
        if (lastVelocity != Vector2.zero && currentVelocity != lastVelocity)
        {
            Vector2 velocityChange = currentVelocity - lastVelocity;
            
            // 速度変化が大きい場合、反射として処理
            if (velocityChange.magnitude > 0.5f)
            {
                Vector2 reflectionForce = -velocityChange * reflectionStrength;
                rb.AddForce(reflectionForce);
                
                // 速度を減衰
                rb.linearVelocity *= reflectionDamping;
            }
        }
        
        lastVelocity = currentVelocity;
    }
    
    /// <summary>
    /// 浮遊高さを更新
    /// </summary>
    void UpdateFloatingHeight()
    {
        // 流れの影響で高さを微調整
        if (flowManager != null)
        {
            Vector2 flowVelocity = flowManager.SampleVelocity(transform.position);
            floatingHeight += flowVelocity.y * Time.fixedDeltaTime * 0.1f;
            
            // 高さの制限
            floatingHeight = Mathf.Clamp(floatingHeight, -10f, 10f);
        }
    }
    
    void OnCollisionEnter2D(Collision2D collision)
    {
        // 心オブジェクト以外のコライダーで反射
        if (collision.gameObject.CompareTag(heartTag))
            return;
            
        // 反射処理
        if (collision.contacts.Length > 0)
        {
            Vector2 normal = collision.contacts[0].normal;
            Vector2 currentVelocity = rb.linearVelocity;
            
            // 反射方向を計算
            Vector2 reflectionDirection = Vector2.Reflect(currentVelocity.normalized, normal);
            
            // 反射後の速度を設定
            Vector2 reflectedVelocity = reflectionDirection * currentVelocity.magnitude * reflectionStrength;
            rb.linearVelocity = reflectedVelocity * reflectionDamping;
            
            // 追加の反射力を適用
            Vector2 reflectionForce = reflectionDirection * targetSpeed * reflectionStrength;
            rb.AddForce(reflectionForce);
        }
    }
    
    void OnCollisionStay2D(Collision2D collision)
    {
        // 心オブジェクト以外のコライダーで継続的な反射
        if (collision.gameObject.CompareTag(heartTag))
            return;
            
        // 押し出し力を適用
        if (collision.contacts.Length > 0)
        {
            Vector2 normal = collision.contacts[0].normal;
            Vector2 pushForce = -normal * 5f; // 押し出し力
            rb.AddForce(pushForce);
        }
    }
    
    void OnTriggerEnter2D(Collider2D other)
    {
        // 心オブジェクトかどうかをチェック
        bool isHeart = IsHeartObject(other);
        
        // 心オブジェクトの場合は警告を表示しない
        if (isHeart)
            return;
            
        // 壁オブジェクトの場合は物理衝突を優先（警告を抑制）
        bool isWall = false;
        foreach (string pattern in wallNamePatterns)
        {
            if (other.name.Contains(pattern))
            {
                isWall = true;
                break;
            }
        }
        
        if (isWall)
        {
            // 壁の場合は物理的な押し出しのみ
            Vector2 direction = (transform.position - other.transform.position).normalized;
            Vector2 pushForce = direction * 10f;
            rb.AddForce(pushForce);
            return;
        }
            
        // デバッグ: 心オブジェクト以外のTriggerに入った場合の警告
        if (showDebugInfo)
        {
            Debug.LogWarning($"[AbsorberFloatingPhysics] Trigger detected with {other.name}. Collider might be set as Trigger!");
        }
        
        // その他のオブジェクトの場合は物理的な衝突として処理
        Vector2 direction2 = (transform.position - other.transform.position).normalized;
        Vector2 pushForce2 = direction2 * 10f; // 押し出し力
        rb.AddForce(pushForce2);
    }
    
    /// <summary>
    /// 心オブジェクトかどうかを判定
    /// </summary>
    bool IsHeartObject(Collider2D other)
    {
        // タグチェック
        if (!string.IsNullOrEmpty(heartTag))
        {
            try
            {
                if (other.CompareTag(heartTag))
                    return true;
            }
            catch (System.Exception)
            {
                // タグが存在しない場合はスキップ
            }
        }
        
        // 名前パターンチェック
        foreach (string pattern in heartNamePatterns)
        {
            if (other.name.Contains(pattern))
                return true;
        }
        
        // HeartPhysicsコンポーネントの存在チェック
        if (other.GetComponent<HeartPhysics>() != null)
            return true;
            
        return false;
    }
    
    /// <summary>
    /// 反射しないオブジェクトかどうかを判定
    /// </summary>
    bool ShouldIgnoreReflection(Collider2D other)
    {
        // 名前パターンチェック
        foreach (string pattern in ignoreReflectionPatterns)
        {
            if (other.name.Contains(pattern))
                return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// 壁検出システム（物理衝突が発生しない場合の代替手段）
    /// </summary>
    void ApplyWallDetection()
    {
        // 現在の位置から壁を検出
        Vector2 currentPos = transform.position;
        Vector2 velocity = rb.linearVelocity;
        
        if (velocity.magnitude < 0.1f)
            return;
            
        // 進行方向にRaycastを実行
        float rayDistance = 0.5f; // 距離を短くして自分自身との衝突を避ける
        Vector2 rayDirection = velocity.normalized;
        
        // 少し前方から開始して自分自身を除外
        Vector2 rayStart = currentPos + rayDirection * 0.1f;
        RaycastHit2D hit = Physics2D.Raycast(rayStart, rayDirection, rayDistance);
        
        if (hit.collider != null)
        {
            // 自分自身を除外
            if (hit.collider.gameObject == gameObject)
                return;
                
            // 心オブジェクトでない、かつ反射しないオブジェクトでない場合のみ反射
            if (!IsHeartObject(hit.collider) && !ShouldIgnoreReflection(hit.collider))
            {
                // 反射の頻度を制限（0.1秒以内の連続反射を防ぐ）
                if (Time.time - lastReflectionTime < 0.1f)
                    return;
                    
                // より正確な法線計算
                Vector2 normal = hit.normal;
                
                // 法線が正しくない場合は手動で計算
                if (normal.magnitude < 0.1f)
                {
                    // 壁の種類に応じて法線を設定
                    if (hit.collider.name.Contains("Top") || hit.collider.name.Contains("Ceiling"))
                        normal = Vector2.down; // 天井は下向き
                    else if (hit.collider.name.Contains("Bottom") || hit.collider.name.Contains("Floor"))
                        normal = Vector2.up; // 床は上向き
                    else if (hit.collider.name.Contains("Left"))
                        normal = Vector2.right; // 左壁は右向き（内側向き）
                    else if (hit.collider.name.Contains("Right"))
                        normal = Vector2.left; // 右壁は左向き（内側向き）
                    else
                        normal = hit.normal.normalized;
                }
                else
                {
                    normal = normal.normalized;
                }
                
                // 壁の位置関係を考慮した法線補正
                Vector2 wallToAbsorber = (currentPos - (Vector2)hit.collider.transform.position).normalized;
                
                // 左右の壁の場合、X座標の関係で法線を補正
                if (hit.collider.name.Contains("Left") || hit.collider.name.Contains("Right"))
                {
                    if (wallToAbsorber.x > 0) // 壁が左側にある場合
                        normal = Vector2.right; // 右向きの法線
                    else // 壁が右側にある場合
                        normal = Vector2.left; // 左向きの法線
                }
                // 上下の壁の場合、Y座標の関係で法線を補正
                else if (hit.collider.name.Contains("Top") || hit.collider.name.Contains("Bottom"))
                {
                    if (wallToAbsorber.y > 0) // 壁が下側にある場合
                        normal = Vector2.up; // 上向きの法線
                    else // 壁が上側にある場合
                        normal = Vector2.down; // 下向きの法線
                }
                
                // デバッグ用：法線の方向を確認
                if (showDebugInfo)
                {
                    Debug.Log($"[AbsorberFloatingPhysics] Wall: {hit.collider.name}, Original Normal: {hit.normal}, Calculated Normal: {normal}, WallToAbsorber: {wallToAbsorber}");
                }
                
                // 反射方向を計算
                Vector2 reflectionDirection = Vector2.Reflect(rayDirection, normal);
                
                // 現在の速度の大きさを保持
                float currentSpeed = rb.linearVelocity.magnitude;
                if (currentSpeed < 0.1f) currentSpeed = targetSpeed;
                
                // 反射後の速度を設定
                Vector2 reflectedVelocity = reflectionDirection.normalized * currentSpeed * reflectionDamping;
                rb.linearVelocity = reflectedVelocity;
                
                if (showDebugInfo)
                {
                    Debug.Log($"[AbsorberFloatingPhysics] Wall: {hit.collider.name}, Normal: {normal}, Reflection: {reflectionDirection}");
                }
                
                // 反射時間を記録
                lastReflectionTime = Time.time;
            }
        }
    }
    
    void OnDrawGizmos()
    {
        if (!showDebugInfo || rb == null)
            return;
            
        // 浮遊高さを表示
        Gizmos.color = Color.cyan;
        Vector3 floatingPos = new Vector3(transform.position.x, floatingHeight, transform.position.z);
        Gizmos.DrawWireSphere(floatingPos, 0.2f);
        
        // 流れの影響を表示
        if (flowManager != null)
        {
            Vector2 flowVelocity = flowManager.SampleVelocity(transform.position);
            if (flowVelocity.magnitude > 0.1f)
            {
                Gizmos.color = Color.yellow;
                Vector3 flowEnd = transform.position + (Vector3)flowVelocity * 0.5f;
                Gizmos.DrawLine(transform.position, flowEnd);
            }
        }
        
        // 速度ベクトルを表示
        Gizmos.color = Color.red;
        Vector3 velocityEnd = transform.position + (Vector3)rb.linearVelocity * 0.3f;
        Gizmos.DrawLine(transform.position, velocityEnd);
        
        // 目標速度を表示
        Gizmos.color = Color.green;
        Vector2 currentVelocity = rb.linearVelocity;
        Vector2 velocityDirection = currentVelocity.normalized;
        if (velocityDirection == Vector2.zero)
        {
            velocityDirection = Vector2.right; // デフォルト方向
        }
        Vector3 targetVelocityEnd = transform.position + (Vector3)(velocityDirection * targetSpeed) * 0.3f;
        Gizmos.DrawLine(transform.position, targetVelocityEnd);
        
        // Raycastを表示
        Gizmos.color = Color.magenta;
        Vector3 rayStart = transform.position + (Vector3)(velocityDirection * 0.1f);
        Vector3 rayEnd = rayStart + (Vector3)(velocityDirection * 0.5f);
        Gizmos.DrawLine(rayStart, rayEnd);
    }
}
