using System;
using System.Collections.Generic;

namespace TaikoAssist
{
    // 单个可播放谱面对象：一个对象只对应“一个难度 + 一条分支路径”。
    [Serializable]
    public class TaikoChartData
    {
        // 数据版本号，便于后续结构演进时做迁移。
        public string formatVersion = "1.0";
        // 曲目与谱面元信息。
        public ChartMetadata metadata = new();
        // 谱面主体（小节与音符数据）。
        public ChartBody chart = new();
    }

    // TJA 头部元信息（#START 之前）。
    [Serializable]
    public class ChartMetadata
    {
        // 曲名（主标题，通常为日文或英文）。
        public string title = "";
        // 日文标题（可选）。
        public string titleJa = "";
        // 副标题。
        public string subtitle = "";
        // 日文副标题（可选）。
        public string subtitleJa = "";
        // 基准 BPM。
        public float bpm = 120f;
        // 音频文件名（如 song.ogg）。
        public string wave = "";
        // 音频偏移（秒）。
        public float offset = 0f;
        // 试听开始时间（秒）。
        public float demoStart = 0f;
        // 难度：Easy / Normal / Hard / Oni / Ura。
        public string course = "Oni";
        // 星级难度（常见 1-10）。
        public int level = 1;
        // 分支名：Main / Normal / Professional / Master。
        public string branch = "Main";
        // 分支条件表达式（来自 #BRANCHSTART），无分支时为空。
        public string branchCondition = "";
        // 解析期临时保存 BALLOON 列表，不参与序列化；最终会写入具体气球音符。
        [NonSerialized]
        public List<int> parsedBalloonHits = new();
        // 计分参数。
        public int scoreInit = 0;
        public int scoreDiff = 0;
        // 音量参数。
        public float songVol = 1f;
        public float seVol = 1f;
        // 谱师与分类。
        public string maker = "";
        public string genre = "";

        // 未知头字段保留区，保障回写时尽量无损。
        [NonSerialized]
        public Dictionary<string, string> extra = new();

        // Unity 不直接序列化 Dictionary，用列表桥接。
        public List<StringPair> extraOrdered = new();

        // 写入前：把 extra 字典同步到可序列化列表。
        public void SyncExtraForSerialization()
        {
            extraOrdered.Clear();
            foreach (var kv in extra)
                extraOrdered.Add(new StringPair { key = kv.Key, value = kv.Value });
        }

        // 读取后：把可序列化列表恢复为字典。
        public void SyncExtraFromSerialization()
        {
            extra.Clear();
            foreach (var pair in extraOrdered)
                extra[pair.key] = pair.value;
        }
    }

    // 字符串键值对（用于序列化 extra 字段）。
    [Serializable]
    public class StringPair
    {
        public string key = "";
        public string value = "";
    }

    // 谱面主体。
    [Serializable]
    public class ChartBody
    {
        // 初始状态（若后续小节有覆盖字段，以小节为准）。
        public float initialBpm = 120f;
        public float initialScroll = 1f;
        public int[] initialTimeSignature = { 4, 4 };
        // 小节序列。
        public List<ChartMeasure> measures = new();
    }

    // 单个小节。
    [Serializable]
    public class ChartMeasure
    {
        // 小节拍位总数。
        public int beatCount = 16;
        // 小节状态：拍号 / BPM / 滚速 / GOGO / 小节线显示。
        public int[] timeSignature = null;
        public float bpm = 120f;
        public float scroll = 1f;
        public bool gogo = false;
        public bool barline = true;
        // 小节内音符列表。仅保存有音符的位置。
        public List<ChartNote> notes = new();
    }

    // 单个音符。
    [Serializable]
    public class ChartNote
    {
        // 判定时间三元组 [a,b,c]，表示第 a 又 b/c 拍。
        public List<int> timing = new() { 0, 0, 1 };
        // 音符类型。
        public NoteType type;
        // 气球/风船类音符的目标连打次数（仅 5/6/9 有效）。
        public int balloonHitsRequired = 0;
        // 是否为 Ad-lib（A/B/C/D/F/G）来源音符。
        public bool isAdlib = false;
    }

    // 音符类型，数值与 TJA 字符 0-9 对应。
    public enum NoteType
    {
        Rest = 0,
        Don = 1,
        Kat = 2,
        BigDon = 3,
        BigKat = 4,
        Balloon = 5,
        BigBalloon = 6,
        BalloonEnd = 7,
        RollEnd = 8,
        Kusudama = 9,
    }

}
