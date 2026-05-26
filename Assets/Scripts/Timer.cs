using UnityEngine;
namespace TaikoAssist
{
    public class Timer : Singleton<Timer>
    {
        [SerializeField] private float ElapsedTime;
        [SerializeField] private float StartTime;
        [SerializeField] private float DelayTime = 0;
        [SerializeField] private float Length;
        [SerializeField] private bool Paused = false;
        [SerializeField] private float Multiplier = 1;
        [SerializeField] private float TargetTime;
        
        void Start()
        {
            Time.fixedDeltaTime = 1f / 100f;
            StartTime = Time.time + DelayTime / 1000;
            ElapsedTime = Time.time - StartTime;
            PauseTimer();
        }
        
        void Update()
        {
            if (!Paused)
            {
                ElapsedTime = (Time.time - StartTime) * Multiplier;
                TargetTime = ElapsedTime;
            }
            if (Input.GetKeyDown(KeyCode.Space))
            {
                if (Paused)
                {
                    ResumeTimer();
                }
                else
                {
                    PauseTimer();
                }
            }
        }
        
        private void FixedUpdate()
        {
            if (Paused)
                ElapsedTime += (TargetTime - ElapsedTime) / 7f;
        }
        
        public static void SetMultiplier(float Multiplier)
        {
            bool PrePaused = Instance.Paused;
            PauseTimer();
            Instance.Multiplier = Multiplier;
            if (!PrePaused)
                ResumeTimer();
        }
        
        public static void PauseTimer()
        {
            Instance.Paused = true;
        }
        
        public static void ResumeTimer()
        {
            Instance.Paused = false;
            Instance.StartTime = Time.time - Instance.ElapsedTime / Instance.Multiplier;
        }
        
        public static void SetTimer(float SetTime)
        {
            PauseTimer();
            Instance.TargetTime = Mathf.Max(SetTime, 0);
        }
        
        public static float GetElapsedTime()
        {
            return Instance.ElapsedTime;
        }
    }
}