using UnityEngine;
using UnityEngine.Events;

namespace liveasnotes.L4Variant
{
    public class L4VColliderEventInvoker : MonoBehaviour
    {
        public enum Detectable
        {
            Everything,
            Tag,
            Object,
            Nothing,
        }

        public Detectable filter;
        public string targetTag = "Untagged";
        // このコンポーネント自身をアタッチしてるゲームオブジェクトを対象にする
        public GameObject targetObject;

        // Objectフィルタ用: 対象オブジェクトのリスト
        public GameObject[] targetObjects;

        [Header("Collision Event"), Space(10)]
        public UnityEvent onCollisionEnter = new UnityEvent();
        public UnityEvent onCollisionExit = new UnityEvent();

        [Header("Trigger Event")]
        public UnityEvent onTriggerEnter = new UnityEvent();
        public UnityEvent onTriggerExit = new UnityEvent();

        void Awake()
        {
            // targetObjectをこのコンポーネントがアタッチされているGameObjectに初期化
            targetObject = this.gameObject;
        }

        void InvokeIfValid(UnityEvent _handler, GameObject _)
        {
            if (_handler != null)
            {
                switch (filter)
                {
                    case Detectable.Everything:
                        _handler.Invoke();
                        break;

                    case Detectable.Tag:
                        if (_.CompareTag(targetTag))
                        {
                            _handler.Invoke();
                        }
                        break;

                    case Detectable.Object:
                        if (targetObjects != null && System.Array.Exists(targetObjects, obj => obj == _))
                        {
                            _handler.Invoke();
                        }
                        break;

                    case Detectable.Nothing:
                        break;
                }
            }
        }

        void OnCollisionEnter(Collision _other)
        {
            InvokeIfValid(onCollisionEnter, _other.gameObject);
        }

        void OnCollisionExit(Collision _other)
        {
            InvokeIfValid(onCollisionExit, _other.gameObject);
        }

        void OnTriggerEnter(Collider _other)
        {
            InvokeIfValid(onTriggerEnter, _other.gameObject);
        }

        void OnTriggerExit(Collider _other)
        {
            InvokeIfValid(onTriggerExit, _other.gameObject);
        }
    }
}
