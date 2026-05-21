using UnityEngine;
namespace PrsEditor
{
    public class Timer : Singleton<Timer>
    {
        [SerializeField] private float ElapsedTime;
        private float StartTime;
        public float DelayTime = 0;
        public float Length;
        public bool Paused = false;
        public float Multiplier = 1;
        private float TargetTime;
        
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
            {
                ElapsedTime += (TargetTime - ElapsedTime) / 7f;
            }
        }
        
        public void SetRatio(float Ratio)
        {
            bool wasPaused = Paused;
            PauseTimer();
            Multiplier = Ratio;
            if (!wasPaused)
            {
                ResumeTimer();
            }
        }
        
        public void PauseTimer()
        {
            Paused = true;
        }
        
        public void ResumeTimer()
        {
            Paused = false;
            StartTime = Time.time - ElapsedTime / Multiplier;
        }
        
        public void SetTimer(float SetTime)
        {
            PauseTimer();
            TargetTime = Mathf.Max(SetTime, 0);
        }
        
        public float GetElapsedTime()
        {
            return ElapsedTime;
        }
    }
}