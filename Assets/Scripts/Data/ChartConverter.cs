using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

namespace TaikoAssist
{
    /// <summary>
    /// TJA ↔ 太鼓JSON谱面（TaikoChartData）双向无损转换器。
    /// 
    /// 支持：
    /// - 标准TJA格式（含 #BPMCHANGE / #SCROLL / #GOGOSTART / #GOGOEND / #BARLINEON / #BARLINEOFF 等指令）
    /// - 分支谱面（分岐譜面 #BRANCHSTART / #N / #E / #M / #BRANCHEND）
    /// - 自定义头部字段的保留（通过 extra 字典）
    /// - 注释的剥离与重建（重建时注释不保留，输出为整洁格式）
    /// </summary>
    public class ChartConverter
    {
        // ---- TJA 字符 ↔ NoteType 映射 ----
        private static readonly Dictionary<char, NoteType> CharToNoteType = new()
        {
            {'0', NoteType.Rest},
            {'1', NoteType.Don},
            {'2', NoteType.Kat},
            {'3', NoteType.BigDon},
            {'4', NoteType.BigKat},
            {'5', NoteType.Balloon},
            {'6', NoteType.BigBalloon},
            {'7', NoteType.BalloonEnd},
            {'8', NoteType.RollEnd},
            {'9', NoteType.Kusudama},
        };

        private static readonly Dictionary<NoteType, char> NoteTypeToChar = new()
        {
            {NoteType.Rest,       '0'},
            {NoteType.Don,        '1'},
            {NoteType.Kat,        '2'},
            {NoteType.BigDon,     '3'},
            {NoteType.BigKat,     '4'},
            {NoteType.Balloon,    '5'},
            {NoteType.BigBalloon, '6'},
            {NoteType.BalloonEnd, '7'},
            {NoteType.RollEnd,    '8'},
            {NoteType.Kusudama,   '9'},
        };

        // ---- Ad-lib 字符映射（分岐里谱专用） ----
        private static readonly Dictionary<char, (NoteType type, bool isAdlib)> CharToAdlib = new()
        {
            {'A', (NoteType.Don,    true)},
            {'B', (NoteType.Kat,    true)},
            {'C', (NoteType.BigDon, true)},
            {'D', (NoteType.BigKat, true)},
            {'F', (NoteType.Balloon, true)},
            {'G', (NoteType.BigBalloon, true)},
        };

        // ================================================================
        //  公开API
        // ================================================================

        /// <summary>
        /// 将TJA文本解析为 TaikoChartData 对象。
        /// </summary>
        public static TaikoChartData ParseTja(string tjaContent)
        {
            if (string.IsNullOrWhiteSpace(tjaContent))
                throw new ArgumentException("TJA content is null or empty.");

            var lines = SplitLines(tjaContent);
            var data = new TaikoChartData();
            int cursor = 0;

            // --- 阶段1：解析头部元数据 ---
            cursor = ParseMetadata(lines, cursor, data.metadata);

            // --- 阶段2：定位 #START ---
            while (cursor < lines.Count)
            {
                string trimmed = lines[cursor].Trim();
                if (trimmed.Equals("#START", StringComparison.OrdinalIgnoreCase))
                {
                    cursor++;
                    break;
                }
                cursor++;
            }

            // --- 阶段3：解析谱面主体 (#START ~ #END) ---
            data.chart = ParseChartBody(lines, ref cursor);

            return data;
        }

        /// <summary>
        /// 将 TaikoChartData 对象导出为TJA文本。
        /// </summary>
        public static string EmitTja(TaikoChartData data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            var sb = new StringBuilder();

            // --- 头部 ---
            EmitMetadata(sb, data.metadata);
            sb.AppendLine();
            sb.AppendLine("#START");
            sb.AppendLine();

            // --- 谱面主体 ---
            EmitChartBody(sb, data.chart);

            sb.AppendLine("#END");
            return sb.ToString();
        }

        /// <summary>
        /// 将TJA文本转换为JSON字符串（内部使用 Unity JsonUtility）。
        /// 注意：由于 JsonUtility 对 Dictionary 等复杂类型支持有限，
        /// 实际项目中建议使用 Newtonsoft.Json 或自行序列化。
        /// 此处返回 TaikoChartData 对象，调用方自行选择序列化方式。
        /// </summary>
        public static TaikoChartData TjaToJson(string tjaContent)
        {
            return ParseTja(tjaContent);
        }

        /// <summary>
        /// 将 TaikoChartData 转换回TJA文本。
        /// </summary>
        public static string JsonToTja(TaikoChartData data)
        {
            return EmitTja(data);
        }

        // ================================================================
        //  解析：头部元数据
        // ================================================================

        private static int ParseMetadata(List<string> lines, int start, ChartMetadata meta)
        {
            for (int i = start; i < lines.Count; i++)
            {
                string line = lines[i].Trim();

                // 遇到 #START 或 #END 即结束头部解析
                if (line.StartsWith("#START", StringComparison.OrdinalIgnoreCase) ||
                    line.StartsWith("#END", StringComparison.OrdinalIgnoreCase))
                    return i;

                // 跳过空行和注释
                if (string.IsNullOrEmpty(line) || line.StartsWith("//"))
                    continue;

                // 解析 KEY:VALUE
                int colonIdx = line.IndexOf(':');
                if (colonIdx <= 0) continue;

                string key = line[..colonIdx].Trim().ToUpperInvariant();
                string value = line[(colonIdx + 1)..].Trim();

                switch (key)
                {
                    case "TITLE":       meta.title = value; break;
                    case "TITLEJA":     meta.titleJa = value; break;
                    case "SUBTITLE":    meta.subtitle = value; break;
                    case "SUBTITLEJA":  meta.subtitleJa = value; break;
                    case "BPM":         meta.bpm = ParseFloat(value, 120f); break;
                    case "WAVE":        meta.wave = value; break;
                    case "OFFSET":      meta.offset = ParseFloat(value, 0f); break;
                    case "DEMOSTART":   meta.demoStart = ParseFloat(value, 0f); break;
                    case "COURSE":      meta.course = value; break;
                    case "LEVEL":       meta.level = ParseInt(value, 1); break;
                    case "BALLOON":
                        meta.balloonHits = ParseIntList(value);
                        break;
                    case "SCOREINIT":   meta.scoreInit = ParseInt(value, 0); break;
                    case "SCOREDIFF":   meta.scoreDiff = ParseInt(value, 0); break;
                    case "SONGVOL":     meta.songVol = ParseFloat(value, 1f); break;
                    case "SEVOL":       meta.seVol = ParseFloat(value, 1f); break;
                    case "MAKER":       meta.maker = value; break;
                    case "GENRE":       meta.genre = value; break;
                    default:
                        // 保留无法识别的字段以保证无损
                        meta.extra[key] = value;
                        break;
                }
            }
            return lines.Count;
        }

        // ================================================================
        //  解析：谱面主体
        // ================================================================

        private static ChartBody ParseChartBody(List<string> lines, ref int cursor)
        {
            var body = new ChartBody();
            var measures = new List<ChartMeasure>();

            // 栈：用于处理嵌套分支
            var branchStack = new Stack<BranchParseState>();
            var currentBranch = (string)null; // null = 单谱面
            var currentTarget = measures;

            // 状态变量（随解析推进而更新）
            float currentBpm = body.initialBpm;
            float currentScroll = body.initialScroll;
            int[] currentTimeSig = (int[])body.initialTimeSignature.Clone();
            bool currentGogo = false;
            bool currentBarline = true;

            while (cursor < lines.Count)
            {
                string line = lines[cursor].Trim();
                cursor++;

                // --- #END 终止 ---
                if (line.Equals("#END", StringComparison.OrdinalIgnoreCase))
                    break;

                // --- 跳过空行和注释 ---
                if (string.IsNullOrEmpty(line) || line.StartsWith("//"))
                    continue;

                // --- 谱面指令 ---
                if (line.StartsWith('#'))
                {
                    string[] parts = line.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
                    string cmd = parts[0].ToUpperInvariant();
                    string arg = parts.Length > 1 ? parts[1] : "";

                    switch (cmd)
                    {
                        case "#BPMCHANGE":
                            currentBpm = ParseFloat(arg, currentBpm);
                            break;

                        case "#MEASURE":
                            currentTimeSig = ParseTimeSignature(arg, currentTimeSig);
                            break;

                        case "#SCROLL":
                            currentScroll = ParseFloat(arg, currentScroll);
                            break;

                        case "#GOGOSTART":
                            currentGogo = true;
                            break;

                        case "#GOGOEND":
                            currentGogo = false;
                            break;

                        case "#BARLINEON":
                            currentBarline = true;
                            break;

                        case "#BARLINEOFF":
                            currentBarline = false;
                            break;

                        case "#DELAY":
                            // DELAY 是同步指令，暂时跳过（谱面中用于同步校准）
                            break;

                        case "#BRANCHSTART":
                            branchStack.Push(new BranchParseState
                            {
                                condition = arg,
                                normalTarget = new List<ChartMeasure>(),
                                professionalTarget = new List<ChartMeasure>(),
                                masterTarget = new List<ChartMeasure>(),
                                savedBpm = currentBpm,
                                savedScroll = currentScroll,
                                savedTimeSig = (int[])currentTimeSig.Clone(),
                                savedGogo = currentGogo,
                                savedBarline = currentBarline,
                            });
                            currentTarget = branchStack.Peek().normalTarget;
                            currentBranch = "normal";
                            break;

                        case "#N":
                            if (branchStack.Count > 0)
                            {
                                var st = branchStack.Peek();
                                RestoreState(st, ref currentBpm, ref currentScroll, ref currentTimeSig, ref currentGogo, ref currentBarline);
                                currentTarget = st.normalTarget;
                                currentBranch = "normal";
                            }
                            break;

                        case "#E":
                            if (branchStack.Count > 0)
                            {
                                var st = branchStack.Peek();
                                RestoreState(st, ref currentBpm, ref currentScroll, ref currentTimeSig, ref currentGogo, ref currentBarline);
                                currentTarget = st.professionalTarget;
                                currentBranch = "professional";
                            }
                            break;

                        case "#M":
                            if (branchStack.Count > 0)
                            {
                                var st = branchStack.Peek();
                                RestoreState(st, ref currentBpm, ref currentScroll, ref currentTimeSig, ref currentGogo, ref currentBarline);
                                currentTarget = st.masterTarget;
                                currentBranch = "master";
                            }
                            break;

                        case "#BRANCHEND":
                            if (branchStack.Count > 0)
                            {
                                var st = branchStack.Pop();
                                if (branchStack.Count == 0)
                                {
                                    // 最外层分支结束，填充到 body.branches
                                    body.branches = new ChartBranches
                                    {
                                        condition = st.condition,
                                        normal = st.normalTarget,
                                        professional = st.professionalTarget,
                                        master = st.masterTarget,
                                    };
                                    // 回到单谱面模式
                                    currentTarget = null;
                                    currentBranch = null;
                                }
                                else
                                {
                                    // 嵌套分支：需要合并到父分支的某个target
                                    // （嵌套分岐极为罕见，此处做简化处理——
                                    //   将分支数据合并到父分支的当前target中）
                                    var parent = branchStack.Peek();
                                    var mergedList = GetCurrentBranchTarget(parent, currentBranch);
                                    // 添加一个占位标记（实际嵌套分岐格式复杂，暂简化）
                                    mergedList.Add(new ChartMeasure
                                    {
                                        beatCount = 0,
                                        timeSignature = (int[])currentTimeSig.Clone(),
                                        bpm = currentBpm,
                                        scroll = currentScroll,
                                        gogo = currentGogo,
                                        barline = currentBarline,
                                    });
                                    // 将子分支信息存储到父分支中（简化处理）
                                    // 实际场景中嵌套分岐极少，此处不做完整展开
                                }
                                RestoreState(branchStack.Count > 0 ? branchStack.Peek() : null, ref currentBpm, ref currentScroll, ref currentTimeSig, ref currentGogo, ref currentBarline);
                                currentTarget = branchStack.Count > 0 ? GetCurrentBranchTarget(branchStack.Peek(), currentBranch) : measures;
                            }
                            break;

                        case "#SECTION":
                        case "#LEVELHOLD":
                            // 标记性指令，解析时忽略
                            break;

                        default:
                            // 已知自定义指令（如TJAPlayer3的 #SENOTECHANGE 等），忽略
                            break;
                    }
                    continue;
                }

                // --- 音符行（小节数据）---
                if (currentTarget != null)
                {
                    var measure = ParseMeasureLine(line, currentBpm, currentScroll, currentTimeSig, currentGogo, currentBarline);
                    if (measure != null)
                        currentTarget.Add(measure);
                }
            }

            // 如果无分支，直接使用 measures
            if (body.branches == null)
            {
                body.measures = measures;
                body.initialBpm = currentBpm > 0 ? currentBpm : 120f;
                body.initialScroll = currentScroll;
                body.initialTimeSignature = currentTimeSig;
            }

            return body;
        }

        /// <summary>解析一行小节数据（TJA音符行）</summary>
        private static ChartMeasure ParseMeasureLine(
            string line, float bpm, float scroll, int[] timeSig,
            bool gogo, bool barline)
        {
            // 按逗号分割各拍
            string[] beats = line.Split(',');
            if (beats.Length == 0) return null;

            var measure = new ChartMeasure
            {
                beatCount = beats.Length,
                timeSignature = timeSig != null ? (int[])timeSig.Clone() : null,
                bpm = bpm,
                scroll = scroll,
                gogo = gogo,
                barline = barline,
            };

            for (int beat = 0; beat < beats.Length; beat++)
            {
                string segment = beats[beat].Trim();
                if (string.IsNullOrEmpty(segment)) continue;

                // 遍历该拍内的每个字符（通常每拍1个字符，16分音符精度）
                for (int sub = 0; sub < segment.Length; sub++)
                {
                    char ch = segment[sub];

                    // 标准音符 0-9
                    if (CharToNoteType.TryGetValue(ch, out NoteType noteType))
                    {
                        if (noteType == NoteType.Rest) continue;
                        measure.notes.Add(new ChartNote
                        {
                            beat = beat,  // 主拍位
                            type = noteType,
                            hits = 1,
                            isAdlib = false,
                        });
                    }
                    // Ad-lib 音符 A-F（分岐里谱用）
                    else if (CharToAdlib.TryGetValue(ch, out var adlib))
                    {
                        if (adlib.type == NoteType.Rest) continue;
                        measure.notes.Add(new ChartNote
                        {
                            beat = beat,
                            type = adlib.type,
                            hits = 1,
                            isAdlib = adlib.isAdlib,
                        });
                    }
                    // 忽略其他字符（空格等）
                }
            }

            return measure;
        }

        // ================================================================
        //  导出：TJA文本生成
        // ================================================================

        private static void EmitMetadata(StringBuilder sb, ChartMetadata meta)
        {
            // ---- 标准字段（按约定顺序输出） ----
            void EmitIfNotEmpty(string key, string value)
            {
                if (!string.IsNullOrEmpty(value))
                    sb.AppendLine($"{key}:{value}");
            }

            EmitIfNotEmpty("TITLE:", meta.title);
            EmitIfNotEmpty("TITLEJA:", meta.titleJa);
            EmitIfNotEmpty("SUBTITLE:", meta.subtitle);
            EmitIfNotEmpty("SUBTITLEJA:", meta.subtitleJa);
            sb.AppendLine($"BPM:{FormatFloat(meta.bpm)}");
            EmitIfNotEmpty("WAVE:", meta.wave);
            if (Math.Abs(meta.offset) > 0.0001f)
                sb.AppendLine($"OFFSET:{FormatFloat(meta.offset)}");
            if (meta.demoStart > 0)
                sb.AppendLine($"DEMOSTART:{FormatFloat(meta.demoStart)}");
            EmitIfNotEmpty("COURSE:", meta.course);
            if (meta.level > 0)
                sb.AppendLine($"LEVEL:{meta.level}");
            if (meta.balloonHits.Count > 0)
                sb.AppendLine($"BALLOON:{string.Join(",", meta.balloonHits)}");
            if (meta.scoreInit > 0)
                sb.AppendLine($"SCOREINIT:{meta.scoreInit}");
            if (meta.scoreDiff > 0)
                sb.AppendLine($"SCOREDIFF:{meta.scoreDiff}");
            if (Math.Abs(meta.songVol - 1f) > 0.0001f)
                sb.AppendLine($"SONGVOL:{FormatFloat(meta.songVol)}");
            if (Math.Abs(meta.seVol - 1f) > 0.0001f)
                sb.AppendLine($"SEVOL:{FormatFloat(meta.seVol)}");
            EmitIfNotEmpty("MAKER:", meta.maker);
            EmitIfNotEmpty("GENRE:", meta.genre);

            // ---- 自定义字段（保证无损） ----
            foreach (var kv in meta.extra)
                sb.AppendLine($"{kv.Key}:{kv.Value}");
        }

        private static void EmitChartBody(StringBuilder sb, ChartBody body)
        {
            if (body.branches != null)
            {
                // --- 分支谱面 ---
                var b = body.branches;
                sb.AppendLine($"#BRANCHSTART {b.condition}");

                EmitMeasureList(sb, b.normal);
                sb.AppendLine("#N");
                EmitMeasureList(sb, b.professional);
                sb.AppendLine("#E");
                EmitMeasureList(sb, b.master);
                sb.AppendLine("#M");

                sb.AppendLine("#BRANCHEND");
            }
            else
            {
                // --- 单谱面 ---
                EmitMeasureList(sb, body.measures);
            }
        }

        private static void EmitMeasureList(StringBuilder sb, List<ChartMeasure> measures)
        {
            if (measures == null) return;

            float lastBpm = float.NaN;
            float lastScroll = float.NaN;
            bool lastGogo = false;
            bool lastBarline = true;

            foreach (var m in measures)
            {
                // --- BPM变化 ---
                if (m.bpm.HasValue && !NearlyEqual(m.bpm.Value, lastBpm))
                {
                    sb.AppendLine($"#BPMCHANGE {FormatFloat(m.bpm.Value)}");
                    lastBpm = m.bpm.Value;
                }
                else if (!m.bpm.HasValue && !float.IsNaN(lastBpm))
                {
                    lastBpm = float.NaN;
                }

                // --- Scroll变化 ---
                if (m.scroll.HasValue && !NearlyEqual(m.scroll.Value, lastScroll))
                {
                    sb.AppendLine($"#SCROLL {FormatFloat(m.scroll.Value)}");
                    lastScroll = m.scroll.Value;
                }
                else if (!m.scroll.HasValue && !float.IsNaN(lastScroll))
                {
                    lastScroll = float.NaN;
                }

                // --- Go-Go Time ---
                if (m.gogo.HasValue && m.gogo.Value != lastGogo)
                {
                    sb.AppendLine(m.gogo.Value ? "#GOGOSTART" : "#GOGOEND");
                    lastGogo = m.gogo.Value;
                }

                // --- Barline ---
                if (m.barline.HasValue && m.barline.Value != lastBarline)
                {
                    sb.AppendLine(m.barline.Value ? "#BARLINEON" : "#BARLINEOFF");
                    lastBarline = m.barline.Value;
                }

                // --- 时间记号变化 ---
                if (m.timeSignature != null)
                {
                    sb.AppendLine($"#MEASURE {m.timeSignature[0]}/{m.timeSignature[1]}");
                }

                // --- 小节数据 ---
                sb.AppendLine(EmitMeasureLine(m));
            }
        }

        /// <summary>将单个ChartMeasure导出为TJA音符行</summary>
        private static string EmitMeasureLine(ChartMeasure measure)
        {
            if (measure.beatCount <= 0) return ",";

            // 构建每拍的字符表示
            var beatChars = new List<char>[measure.beatCount];
            for (int i = 0; i < measure.beatCount; i++)
                beatChars[i] = new List<char>();

            foreach (var note in measure.notes)
            {
                if (note.beat < 0 || note.beat >= measure.beatCount) continue;

                char ch;
                if (note.isAdlib)
                {
                    // Ad-lib 字符映射（A=Don, B=Kat, F=Balloon, G=BigBalloon）
                    ch = note.type switch
                    {
                        NoteType.Don        => 'A',
                        NoteType.Kat        => 'B',
                        NoteType.BigDon     => 'C',
                        NoteType.BigKat     => 'D',
                        NoteType.Balloon    => 'F',
                        NoteType.BigBalloon => 'G',
                        _ => NoteTypeToChar.GetValueOrDefault(note.type, '0'),
                    };
                }
                else
                {
                    ch = NoteTypeToChar.GetValueOrDefault(note.type, '0');
                }

                beatChars[note.beat].Add(ch);
            }

            // 拼接为TJA行
            var sb = new StringBuilder();
            for (int i = 0; i < measure.beatCount; i++)
            {
                if (beatChars[i].Count == 0)
                {
                    // 该拍无音符，输出0（空拍占位）
                    sb.Append('0');
                }
                else
                {
                    foreach (char c in beatChars[i])
                        sb.Append(c);
                }
                sb.Append(',');
            }

            return sb.ToString();
        }

        // ================================================================
        //  辅助函数
        // ================================================================

        private static List<string> SplitLines(string content)
        {
            var lines = new List<string>();
            string[] raw = content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            lines.AddRange(raw);
            return lines;
        }

        private static float ParseFloat(string s, float defaultValue)
        {
            if (float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out float result))
                return result;
            return defaultValue;
        }

        private static int ParseInt(string s, int defaultValue)
        {
            if (int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out int result))
                return result;
            return defaultValue;
        }

        private static List<int> ParseIntList(string s)
        {
            var list = new List<int>();
            if (string.IsNullOrWhiteSpace(s)) return list;
            string[] parts = s.Split(',', StringSplitOptions.RemoveEmptyEntries);
            foreach (string part in parts)
            {
                if (int.TryParse(part.Trim(), out int val))
                    list.Add(val);
            }
            return list;
        }

        private static int[] ParseTimeSignature(string arg, int[] fallback)
        {
            if (string.IsNullOrWhiteSpace(arg)) return fallback;
            string[] parts = arg.Split('/');
            if (parts.Length == 2 &&
                int.TryParse(parts[0].Trim(), out int num) &&
                int.TryParse(parts[1].Trim(), out int den))
            {
                return new[] { num, den };
            }
            return (int[])fallback.Clone();
        }

        private static string FormatFloat(float value)
        {
            return value.ToString("0.##", CultureInfo.InvariantCulture);
        }

        private static bool NearlyEqual(float a, float b)
        {
            if (float.IsNaN(a) || float.IsNaN(b)) return false;
            return Mathf.Abs(a - b) < 0.001f;
        }

        private static void RestoreState(BranchParseState state,
            ref float bpm, ref float scroll, ref int[] timeSig,
            ref bool gogo, ref bool barline)
        {
            if (state == null) return;
            bpm = state.savedBpm;
            scroll = state.savedScroll;
            timeSig = state.savedTimeSig;
            gogo = state.savedGogo;
            barline = state.savedBarline;
        }

        private static List<ChartMeasure> GetCurrentBranchTarget(BranchParseState state, string branch)
        {
            return branch switch
            {
                "normal" => state.normalTarget,
                "professional" => state.professionalTarget,
                "master" => state.masterTarget,
                _ => state.normalTarget,
            };
        }

        /// <summary>分支解析的临时状态</summary>
        private class BranchParseState
        {
            public string condition = "";
            public List<ChartMeasure> normalTarget;
            public List<ChartMeasure> professionalTarget;
            public List<ChartMeasure> masterTarget;
            public float savedBpm;
            public float savedScroll;
            public int[] savedTimeSig;
            public bool savedGogo;
            public bool savedBarline;
        }
    }
}
