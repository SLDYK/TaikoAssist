using UnityEngine;

namespace TaikoAssist
{
    public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T _instance;
        private static readonly object Lock = new object();
        private static bool IsQuitting = false;

        public static T Instance
        {
            get
            {
                if (IsQuitting)
                {
                    return null;
                }

                lock (Lock)
                {
                    if (_instance == null)
                    {
                        _instance = FindAnyObjectByType<T>(FindObjectsInactive.Include);
                    }
                    return _instance;
                }
            }
        }

        protected virtual void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Debug.LogWarning($"[Singleton] Multiple instances of '{typeof(T)}' detected. Destroying duplicate.");
                Destroy(gameObject);
                return;
            }

            _instance = this as T;
        }

        protected virtual void OnDestroy()
        {
            if (_instance == this)
            {
                IsQuitting = true;
            }
        }

        protected virtual void OnApplicationQuit()
        {
            IsQuitting = true;
        }
    }
}
