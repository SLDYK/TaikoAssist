using UnityEngine;
namespace TaikoAssist
{
    public class ChartLoader : Singleton<ChartLoader>
    {
        [SerializeField] private AudioSource AudioSource;
        [SerializeField] private AudioClip AudioClip;
        public void LoadAudio()
        {
            AudioSource.clip = AudioClip;
        }
    }
}