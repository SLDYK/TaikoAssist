using System;
using System.Collections.Generic;

namespace TaikoAssist
{
    // 将谱面 timing=[a,b,c] 换算为绝对秒数的工具类。
    public static class ChartTime
    {
        // 预计算缓存：每小节的起始拍位、起始时间、有效 BPM
        private static List<int> _cachedStartBeats;
        private static List<float> _cachedStartTimes;
        private static List<float> _cachedMeasureBpm;
        private static TaikoChartData _cachedChart;

        // 确保缓存与当前谱面一致
        private static void EnsureCache()
        {
            TaikoChartData chart = ChartLoader.CurrentChart;
            if (chart == null)
            {
                _cachedChart = null;
                _cachedStartBeats = null;
                _cachedStartTimes = null;
                _cachedMeasureBpm = null;
                return;
            }
            if (_cachedChart == chart)
                return;

            _cachedChart = chart;
            List<ChartMeasure> measures = chart.chart.measures;
            int count = measures.Count;
            _cachedStartBeats = new List<int>(count + 1);
            _cachedStartTimes = new List<float>(count + 1);
            _cachedMeasureBpm = new List<float>(count);

            float currentBpm = chart.chart.initialBpm;
            int accumulatedBeat = 0;
            float accumulatedTime = 0f;

            _cachedStartBeats.Add(0);
            _cachedStartTimes.Add(0f);

            for (int i = 0; i < count; i++)
            {
                ChartMeasure m = measures[i];
                if (m.bpm > 0) currentBpm = m.bpm;
                _cachedMeasureBpm.Add(currentBpm);

                // timeSignature 为 null 时回退到 4/4，防止空指针。
                int[] ts = m.timeSignature ?? new[] { 4, 4 };
                float qnPerMeasure = ts[0] * (4f / ts[1]);
                float measureDuration = qnPerMeasure * (60f / currentBpm);

                accumulatedBeat += m.beatCount;
                accumulatedTime += measureDuration;

                _cachedStartBeats.Add(accumulatedBeat);
                _cachedStartTimes.Add(accumulatedTime);
            }
        }

        // 二分查找：返回目标拍位所在的小节索引
        private static int FindMeasureIndex(float beatNum)
        {
            int lo = 0;
            int hi = _cachedStartBeats.Count - 1;
            while (lo < hi - 1)
            {
                int mid = (lo + hi) / 2;
                if (_cachedStartBeats[mid] <= beatNum)
                    lo = mid;
                else
                    hi = mid;
            }
            return lo;
        }

        // 将 timing=[a,b,c] 换算为绝对秒数（已计入 OFFSET）。
        // a = 绝对拍位序号（从谱面开头累计），b/c = 该拍内的偏移。
        public static float Time2Sec(List<int> timing)
        {
            if (timing == null || timing.Count < 3)
                return 0f;

            float BeatNum = timing[0] + (float)timing[1] / Math.Max(timing[2], 1);

            EnsureCache();
            if (_cachedStartBeats == null || _cachedStartBeats.Count <= 1)
                return 0f;

            int idx = FindMeasureIndex(BeatNum);
            ChartMeasure m = _cachedChart.chart.measures[idx];
            float measureStartTime = _cachedStartTimes[idx];
            int measureStartBeat = _cachedStartBeats[idx];
            float beatInMeasure = BeatNum - measureStartBeat;
            float bpm = _cachedMeasureBpm[idx];

            int[] ts = m.timeSignature ?? new[] { 4, 4 };
            float qnPerMeasure = ts[0] * (4f / ts[1]);
            float measureDuration = qnPerMeasure * (60f / bpm);

            float rawSec = measureStartTime;
            if (m.beatCount > 0)
                rawSec += beatInMeasure / m.beatCount * measureDuration;

            // TJA 的 OFFSET 为音频起点到首拍的时间偏移。
            // OFFSET 通常为负值，-offset 将谱面时间对齐到音频时间。
            return rawSec - _cachedChart.metadata.offset;
        }

        // 强制刷新缓存（切换谱面后调用）。
        public static void InvalidateCache()
        {
            _cachedChart = null;
        }
    }
}
