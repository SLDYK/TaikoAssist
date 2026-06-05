using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
namespace TaikoAssist
{
    public class Timer : Singleton<Timer>
    {
        [SerializeField] private float ElapsedTime;
        [SerializeField] private float StartTime;
        [SerializeField] private float DelayTime = 0;
        [SerializeField] private float Length = 0;
        [SerializeField] private bool Paused = false;
        [SerializeField] private float Multiplier = 1;
        [SerializeField] private float TargetTime;
        [SerializeField] private Slider ProgressBar;

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
                if (Length > 0)
                    ProgressBar.value = ElapsedTime / Length;
            }
            if (Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                if (Paused)
                    ResumeTimer();
                else
                    PauseTimer();
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
            AudioController.Instance.SetSpeed(Multiplier);
            if (!PrePaused)
                ResumeTimer();
        }

        public static void PauseTimer()
        {
            Instance.Paused = true;
            AudioController.Instance.Pause();
        }

        public static void ResumeTimer()
        {
            Instance.Paused = false;
            Instance.StartTime = Time.time - Instance.ElapsedTime / Instance.Multiplier;
            AudioController.Instance.Resume(Instance.ElapsedTime);
        }

        public static void SetTimer(float SetTime)
        {
            PauseTimer();
            Instance.TargetTime = Mathf.Max(SetTime, 0);
            AudioController.Instance.SetTime(SetTime);
        }
        public static void SetLength(float Length)
        {
            Instance.Length = Length;
        }

        public static float GetElapsedTime()
        {
            if (Instance.Paused)
                return Instance.ElapsedTime;
            return (Time.time - Instance.StartTime) * Instance.Multiplier;
        }
    }
}
