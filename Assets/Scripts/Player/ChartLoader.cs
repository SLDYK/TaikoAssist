using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Cysharp.Threading.Tasks;

namespace TaikoAssist
{
    public class ChartLoader : Singleton<ChartLoader>
    {
        [SerializeField] private string AudioPath = "TestCharts/モンスターハンターメドレー.ogg";
        [SerializeField] private string TjaPath = "TestCharts/モンスターハンターメドレー.tja";

        public List<TaikoChartData> LoadedCharts;
        public int ChartIndex;

        // 根据 ChartIndex 返回对应的 TaikoChartData（静态属性）。
        public static TaikoChartData CurrentChart
        {
            get
            {
                return Instance.LoadedCharts[Instance.ChartIndex];
            }
        }

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
                ".ogg" => AudioType.OGGVORBIS,
                ".mp3" => AudioType.MPEG,
                ".wav" => AudioType.WAV,
                ".aiff" => AudioType.AIFF,
                ".aif" => AudioType.AIFF,
                _ => AudioType.UNKNOWN,
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

        public void LoadChart()
        {
            LoadChartAsync(TjaPath).Forget();
        }

        private async UniTaskVoid LoadChartAsync(string Path)
        {
            string FullPath = System.IO.Path.Combine(Application.streamingAssetsPath, Path);
            string URI = "file://" + FullPath;

            using UnityWebRequest Request = UnityWebRequest.Get(URI);
            await Request.SendWebRequest();

            if (Request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[ChartLoader] 谱面加载失败: {FullPath}\n{Request.error}");
                return;
            }

            string TjaContent = Request.downloadHandler.text;
            try
            {
                LoadedCharts = ChartConverter.ParseTjaToCharts(TjaContent);
                Debug.Log($"[ChartLoader] 谱面加载成功: {LoadedCharts.Count} 条谱面");
                NoteCreator.Instance?.MarkDirty();
            }
            catch (Exception Ex)
            {
                Debug.LogError($"[ChartLoader] 谱面解析失败: {Ex.Message}\n{Ex.StackTrace}");
            }
        }

        //检查器按钮调用
        public void LoadAll()
        {
            LoadAudio();
            LoadChart();
        }
    }
}