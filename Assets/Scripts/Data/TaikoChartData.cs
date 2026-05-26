using System;
using System.Collections.Generic;
using UnityEngine;

namespace TaikoAssist
{
    /// <summary>
    /// 太鼓达人谱面JSON根对象——与TJA格式可无损互转。
    /// 
    /// 设计原则：
    /// 1. 保留TJA的所有语义信息（音符类型/位置、BPM变化、谱面事件、分支谱面等）
    /// 2. 音符按"小节+拍位"组织，方便运行时解析
    /// 3. 全局事件（BPM变化/SCROLL/GOGO等）带绝对位置信息
    /// 4. 分支谱面（分岐譜面）完整支持
    /// 5. 未知字段通过 extra/customFields 保留，确保无损
    /// </summary>
    [Serializable]
    public class TaikoChartData
    {
        public string formatVersion = "1.0";
        public ChartMetadata metadata = new();
        public ChartBody chart = new();
    }

    // ======================== 元数据 ========================

    /// <summary>
    /// TJA 文件头部的元数据（#START 之前的所有字段）
    /// </summary>
    [Serializable]
    public class ChartMetadata
    {
        /// <summary>曲目标题（英文/罗马字）</summary>
        public string title = "";

        /// <summary>曲目标题（日文）</summary>
        public string titleJa = "";

        /// <summary>副标题（英文/罗马字）</summary>
        public string subtitle = "";

        /// <summary>副标题（日文）</summary>
        public string subtitleJa = "";

        /// <summary>基准BPM</summary>
        public float bpm = 120f;

        /// <summary>音频文件名（如 "song.ogg"）</summary>
        public string wave = "";

        /// <summary>音频偏移量（秒），影响音符判定时机</summary>
        public float offset = 0f;

        /// <summary>试听开始时间（秒）</summary>
        public float demoStart = 0f;

        /// <summary>
        /// 难度标识：
        /// "Easy" / "Normal" / "Hard" / "Oni" / "Ura"
        /// </summary>
        public string course = "Oni";

        /// <summary>星级难度（1-10）</summary>
        public int level = 1;

        /// <summary>
        /// 连打音符（气球/风船）的所需击打数，按出现顺序排列。
        /// 例如 [5, 8, 12] 表示第1个气球打5下，第2个打8下，第3个打12下。
        /// </summary>
        public List<int> balloonHits = new();

        /// <summary>初始连击分数</summary>
        public int scoreInit = 0;

        /// <summary>连击分数增量</summary>
        public int scoreDiff = 0;

        /// <summary>歌曲音量 (0.0-1.0)</summary>
        public float songVol = 1f;

        /// <summary>音效音量 (0.0-1.0)</summary>
        public float seVol = 1f;

        /// <summary>谱面作者</summary>
        public string maker = "";

        /// <summary>风格/类别（如 J-POP, アニメ, ボーカロイド等）</summary>
        public string genre = "";

        // ---- 以下为TJA可能有的其他字段，统一收纳保证无损 ----

        /// <summary>
        /// 所有无法映射到上述字段的TJA头部键值对。
        /// key为原始TJA字段名（如 "SIDE", "LIFE" 等），value为原始值。
        /// 转换回TJA时按原始顺序输出（通过 extraOrder 列表保持顺序）。
        /// </summary>
        [NonSerialized]
        public Dictionary<string, string> extra = new();

        /// <summary>保持extra字段的原始出现顺序（用于序列化）</summary>
        public List<StringPair> extraOrdered = new();

        /// <summary>将extra字典同步到可序列化的列表中</summary>
        public void SyncExtraForSerialization()
        {
            extraOrdered.Clear();
            foreach (var kv in extra)
                extraOrdered.Add(new StringPair { key = kv.Key, value = kv.Value });
        }

        /// <summary>从序列化列表恢复到字典中</summary>
        public void SyncExtraFromSerialization()
        {
            extra.Clear();
            foreach (var pair in extraOrdered)
                extra[pair.key] = pair.value;
        }
    }

    /// <summary>可序列化的字符串键值对（Unity JsonUtility 不支持 Dictionary）</summary>
    [Serializable]
    public class StringPair
    {
        public string key = "";
        public string value = "";
    }

    // ======================== 谱面主体 ========================

    /// <summary>
    /// 谱面主体（#START 到 #END 之间的所有内容）
    /// </summary>
    [Serializable]
    public class ChartBody
    {
        /// <summary>谱面起始BPM（如无 #BPMCHANGE，则全程使用此BPM）</summary>
        public float initialBpm = 120f;

        /// <summary>初始滚动速度倍率（默认 1.0）</summary>
        public float initialScroll = 1f;

        /// <summary>初始拍号 [分子, 分母]，默认 4/4</summary>
        public int[] initialTimeSignature = { 4, 4 };

        // ---- 单分支谱面（大多数谱面使用此字段） ----

        /// <summary>
        /// 小节列表——适用于无分支的谱面。
        /// 如果有分支，此字段为 null，改用 branches。
        /// </summary>
        public List<ChartMeasure> measures = new();

        // ---- 分支谱面（分岐譜面） ----

        /// <summary>
        /// 分支谱面字典。key 为分支标识：
        /// "normal" (普通/ #N), "professional" (玄人/ #E), "master" (达人/ #M)
        /// 每个分支包含完整的小节序列。
        /// 如果为 null 表示此谱面无分支。
        /// </summary>
        public ChartBranches branches = null;
    }

    /// <summary>
    /// 分支谱面容器。
    /// TJA中分岐譜面由 #BRANCHSTART ... #BRANCHEND 包裹，内含 #N/#E/#M 三轨。
    /// </summary>
    [Serializable]
    public class ChartBranches
    {
        /// <summary>分支条件表达式（来自 #BRANCHSTART 后的参数，如 "r,1,2"）</summary>
        public string condition = "";

        /// <summary>普通分支（#N）的小节序列</summary>
        public List<ChartMeasure> normal = new();

        /// <summary>玄人分支（#E）的小节序列</summary>
        public List<ChartMeasure> professional = new();

        /// <summary>达人分支（#M）的小节序列</summary>
        public List<ChartMeasure> master = new();
    }

    // ======================== 小节 ========================

    /// <summary>
    /// 单个小节。
    /// TJA中以逗号分隔各拍，逗号数量+1=beatCount。
    /// 例如 "1,2,1,2," 表示4拍（4个逗号分隔位，beatCount=4）。
    /// 又如 "10002000" 整行无逗号表示1拍（beatCount=1，但内部有多个16分音符）。
    /// 通常TJA谱面的beatCount=16（16分音符精度，4/4拍每小节16个位置）。
    /// </summary>
    [Serializable]
    public class ChartMeasure
    {
        /// <summary>
        /// 该小节的总拍位数（整数）。
        /// 在4/4拍、16分音符精度下通常为16。
        /// 若为3/4拍则为12，以此类推。
        /// </summary>
        public int beatCount = 16;

        /// <summary>覆盖拍号 [分子, 分母]。null 表示不覆盖，沿用当前拍号。</summary>
        public int[] timeSignature = null;

        /// <summary>覆盖BPM。null 表示不覆盖，沿用当前BPM。</summary>
        public float? bpm = null;

        /// <summary>覆盖滚动速度。null 表示不覆盖。</summary>
        public float? scroll = null;

        /// <summary>Go-Go Time 是否在此小节生效。null 表示沿用前一状态。</summary>
        public bool? gogo = null;

        /// <summary>小节线是否可见。null 表示沿用前一状态。</summary>
        public bool? barline = null;

        /// <summary>
        /// 小节内按顺序排列的音符。
        /// 仅包含有音符的位置（beat），空拍不存储。
        /// beat 值为 0 ~ (beatCount-1) 的整数。
        /// </summary>
        public List<ChartNote> notes = new();

        // ---- 分支相关 ----
        // 分支信息在 ChartBody 层级管理，小节本身不感知分支归属。
        // 当 ChartBody.branches != null 时，各分支的小节列表独立存储。
    }

    // ======================== 音符 ========================

    /// <summary>
    /// 单个音符。
    /// </summary>
    [Serializable]
    public class ChartNote
    {
        /// <summary>音符在小节内的拍位（0-based，必须 &lt; beatCount）</summary>
        public int beat;

        /// <summary>音符类型</summary>
        public NoteType type;

        /// <summary>
        /// 连打/气球的所需击打次数。
        /// 仅当 type 为 Balloon / BigBalloon / Kusudama 时有效。
        /// 非连打音符此值为1或无意义。
        /// </summary>
        public int hits = 1;

        /// <summary>
        /// 是否为Ad-lib音符（里谱/分岐专用）。
        /// 在TJA中，普通谱面的Don=1、Kat=2，而分岐里谱用 A=Don、B=Kat 标记。
        /// </summary>
        public bool isAdlib = false;
    }

    /// <summary>
    /// 音符类型枚举。
    /// 数值与TJA原始字符对应：
    /// 0=空拍, 1=ドン, 2=カッ, 3=大ドン, 4=大カッ,
    /// 5=风船/连打, 6=大风船/大连打, 7=风船结束, 8=连打结束, 9=くすだま/芋连打
    /// </summary>
    public enum NoteType
    {
        /// <summary>0 - 空拍（仅在需要显式标记空位时使用，通常不存储）</summary>
        Rest = 0,

        /// <summary>1 - ドン（Don）—— 敲击鼓面</summary>
        Don = 1,

        /// <summary>2 - カッ（Kat）—— 敲击鼓边</summary>
        Kat = 2,

        /// <summary>3 - 大ドン（Big Don）—— 大力敲击鼓面，得分×2</summary>
        BigDon = 3,

        /// <summary>4 - 大カッ（Big Kat）—— 大力敲击鼓边，得分×2</summary>
        BigKat = 4,

        /// <summary>5 - 风船/连打音符（Balloon/Roll）</summary>
        Balloon = 5,

        /// <summary>6 - 大风船/大连打（Big Balloon/Big Roll）</summary>
        BigBalloon = 6,

        /// <summary>7 - 风船结束标记（Balloon End）</summary>
        BalloonEnd = 7,

        /// <summary>8 - 连打结束标记（Roll End）</summary>
        RollEnd = 8,

        /// <summary>9 - くすだま/芋连打（Kusudama）</summary>
        Kusudama = 9,
    }

}
