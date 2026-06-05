using UnityEngine;

namespace TaikoAssist
{
    public class AudioController : Singleton<AudioController>
    {
        [SerializeField] private AudioSource AudioSource;

        public void LoadClip(AudioClip clip)
        {
            AudioSource.clip = clip;
            AudioSource.Stop();
            Timer.SetLength(clip.length);
        }

        public void Resume(float currentTime)
        {
            SetTime(currentTime);
            AudioSource.Play();
        }

        public void Pause()
        {
            AudioSource.Pause();
        }

        public void SetTime(float time)
        {
            AudioSource.time = Mathf.Clamp(time, 0, AudioSource.clip.length);
        }

        public void SetSpeed(float speed)
        {
            AudioSource.pitch = speed;
        }

        public void SetVolume(float volume)
        {
            AudioSource.volume = Mathf.Clamp01(volume);
        }
    }
}
