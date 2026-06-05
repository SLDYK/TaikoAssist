using UnityEngine;
using UnityEngine.Networking;
using Cysharp.Threading.Tasks;

namespace TaikoAssist
{
    public class ChartLoader : Singleton<ChartLoader>
    {
        [SerializeField] private string AudioPath = "TestCharts/モンスターハンターメドレー.ogg";

        // 使用序列化字段中的路径加载
        public void LoadAudio()
        {
            LoadAudioAsync(AudioPath).Forget();
        }

        private async UniTaskVoid LoadAudioAsync(string Path)
        {
            string FullPath = System.IO.Path.Combine(Application.streamingAssetsPath, Path);
            string URI = "file://" + FullPath;
            string EXT = System.IO.Path.GetExtension(Path).ToLower();
            AudioType AudioType = EXT switch
            {
                ".ogg"  => AudioType.OGGVORBIS,
                ".mp3"  => AudioType.MPEG,
                ".wav"  => AudioType.WAV,
                ".aiff" => AudioType.AIFF,
                ".aif"  => AudioType.AIFF,
                _       => AudioType.UNKNOWN,
            };

            using UnityWebRequest Request = UnityWebRequestMultimedia.GetAudioClip(URI, AudioType);
            await Request.SendWebRequest();

            if (Request.result == UnityWebRequest.Result.Success)
            {
                AudioClip Clip = DownloadHandlerAudioClip.GetContent(Request);
                Clip.name = System.IO.Path.GetFileNameWithoutExtension(Path);
                AudioController.Instance.LoadClip(Clip);
                Debug.Log($"[ChartLoader] 音频加载成功: {Clip.name} ({Clip.length:F2}s)");
            }
            else
            {
                Debug.LogError($"[ChartLoader] 音频加载失败: {FullPath}\n{Request.error}");
            }
        }
    }
}