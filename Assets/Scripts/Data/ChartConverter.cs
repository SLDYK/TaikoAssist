using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

namespace TaikoAssist
{
    // TJA 与 TaikoChartData 的转换器。
    // 解析时会按“难度 + 分支”拆成多个独立谱面对象。
    public class ChartConverter
    {
        // 标准 TJA 字符到音符类型的映射（不含 7/8，它们是气球/连打结束的特殊标记）。
        private static readonly Dictionary<char, NoteType> CharToNoteType = new()
        {
            {'0', NoteType.Rest},
            {'1', NoteType.Don},
            {'2', NoteType.Kat},
            {'3', NoteType.BigDon},
            {'4', NoteType.BigKat},
            {'5', NoteType.Balloon},
            {'6', NoteType.BigBalloon},
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
            {NoteType.Roll,       '8'},
            {NoteType.BigRoll,    '8'},
            {NoteType.Kusudama,   '9'},
        };

        // 分支里谱常见的字母扩展音符映射。
        private static readonly Dictionary<char, NoteType> CharToAdlib = new()
        {
            {'A', NoteType.HandDon},
            {'B', NoteType.HandKat},
            {'C', NoteType.Mine},
            {'D', NoteType.Fuse},
            {'F', NoteType.Adlib},
            {'G', NoteType.Kadon},
        };

        // 字母扩展音符 → TJA 字符（回写用）。
        private static readonly Dictionary<NoteType, char> AdlibToChar = new()
        {
            {NoteType.HandDon, 'A'},
            {NoteType.HandKat, 'B'},
            {NoteType.Mine,    'C'},
            {NoteType.Fuse,    'D'},
            {NoteType.Adlib,   'F'},
            {NoteType.Kadon,   'G'},
        };


        // 入口：把整份 TJA 文本拆分为多个可播放谱面对象。
        public static List<TaikoChartData> ParseTjaToCharts(string tjaContent)
        {
            if (string.IsNullOrWhiteSpace(tjaContent))
                throw new ArgumentException("TJA content is null or empty.");

            var lines = SplitLines(tjaContent);
            var charts = new List<TaikoChartData>();
            int cursor = 0;

            // 首段头信息作为所有难度段的默认元数据。
            var baseMeta = new ChartMetadata();
            cursor = ParseMetadata(lines, cursor, baseMeta);

            while (cursor < lines.Count)
            {
                var sectionMeta = CloneMetadata(baseMeta);
                cursor = ParseSectionMetadata(lines, cursor, sectionMeta);

                if (cursor >= lines.Count)
                    break;

                string marker = lines[cursor].Trim();
                if (!marker.Equals("#START", StringComparison.OrdinalIgnoreCase))
                {
                    cursor++;
                    continue;
                }

                // 进入谱面主体解析。
                cursor++; // consume #START
                var parsedBody = ParseChartBody(lines, ref cursor, sectionMeta.bpm, sectionMeta.parsedBalloonHits);

                if (parsedBody.Branches == null)
                {
                    charts.Add(BuildChartData(sectionMeta, parsedBody, "Main", "", parsedBody.Measures));
                }
                else
                {
                    charts.Add(BuildChartData(sectionMeta, parsedBody, "Normal", parsedBody.Branches.Condition, parsedBody.Branches.Normal));
                    charts.Add(BuildChartData(sectionMeta, parsedBody, "Professional", parsedBody.Branches.Condition, parsedBody.Branches.Professional));
                    charts.Add(BuildChartData(sectionMeta, parsedBody, "Master", parsedBody.Branches.Condition, parsedBody.Branches.Master));
                }
            }

            return charts;
        }

        // 把单个 TaikoChartData 导出为 TJA 文本。
        public static string EmitTja(TaikoChartData data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            var sb = new StringBuilder();

            EmitMetadata(sb, data.metadata, data.chart);
            sb.AppendLine();
            sb.AppendLine("#START");
            sb.AppendLine();

            EmitChartBody(sb, data.chart);

            sb.AppendLine("#END");
            return sb.ToString();
        }


        // 解析文件头部元数据（直到 #START/#END）。
        private static int ParseMetadata(List<string> lines, int start, ChartMetadata meta)
        {
            for (int i = start; i < lines.Count; i++)
            {
                string line = lines[i].Trim();

                if (line.StartsWith("#START", StringComparison.OrdinalIgnoreCase) ||
                    line.StartsWith("#END", StringComparison.OrdinalIgnoreCase))
                    return i;

                if (string.IsNullOrEmpty(line) || line.StartsWith("//"))
                    continue;

                int colonIdx = line.IndexOf(':');
                if (colonIdx <= 0) continue;

                ApplyMetadataLine(meta, line);
            }
            return lines.Count;
        }

        // 解析某个难度段前置元数据（覆盖 baseMeta）。
        private static int ParseSectionMetadata(List<string> lines, int start, ChartMetadata meta)
        {
            int i = start;
            while (i < lines.Count)
            {
                string line = lines[i].Trim();

                if (line.Equals("#START", StringComparison.OrdinalIgnoreCase))
                    return i;

                if (line.Equals("#END", StringComparison.OrdinalIgnoreCase))
                {
                    i++;
                    continue;
                }

                if (string.IsNullOrEmpty(line) || line.StartsWith("//") || line.StartsWith('#'))
                {
                    i++;
                    continue;
                }

                ApplyMetadataLine(meta, line);
                i++;
            }
            return i;
        }

        // 解析单条 KEY:VALUE 到元数据对象。
        private static void ApplyMetadataLine(ChartMetadata meta, string line)
        {
            int colonIdx = line.IndexOf(':');
            if (colonIdx <= 0) return;

            string key = line[..colonIdx].Trim().ToUpperInvariant();
            string value = line[(colonIdx + 1)..].Trim();

            switch (key)
            {
                case "TITLE": meta.title = value; break;
                case "TITLEJA": meta.titleJa = value; break;
                case "SUBTITLE": meta.subtitle = value; break;
                case "SUBTITLEJA": meta.subtitleJa = value; break;
                case "BPM": meta.bpm = ParseFloat(value, 120f); break;
                case "WAVE": meta.wave = value; break;
                case "OFFSET": meta.offset = ParseFloat(value, 0f); break;
                case "DEMOSTART": meta.demoStart = ParseFloat(value, 0f); break;
                case "COURSE": meta.course = value; break;
                case "LEVEL": meta.level = ParseInt(value, 1); break;
                case "BALLOON": meta.parsedBalloonHits = ParseIntList(value); break;
                case "SCOREINIT": meta.scoreInit = ParseInt(value, 0); break;
                case "SCOREDIFF": meta.scoreDiff = ParseInt(value, 0); break;
                case "SONGVOL": meta.songVol = ParseFloat(value, 1f); break;
                case "SEVOL": meta.seVol = ParseFloat(value, 1f); break;
                case "MAKER": meta.maker = value; break;
                case "GENRE": meta.genre = value; break;
                default:
                    meta.extra[key] = value;
                    break;
            }
        }


        // 解析 #START 到 #END 的主体。
        private static ParsedChartBody ParseChartBody(List<string> lines, ref int cursor, float initialBpm, List<int> balloonHits)
        {
            var body = new ParsedChartBody
            {
                InitialBpm = initialBpm,
                InitialScroll = 1f,
                InitialTimeSignature = new[] { 4, 4 }
            };
            var measures = new List<ChartMeasure>();
            var targetBeatOffsets = new Dictionary<List<ChartMeasure>, int>
            {
                [measures] = 0
            };

            var branchStack = new Stack<BranchParseState>();
            int balloonHitCursor = 0;
            ChartNote pendingNote = null;
            // null 表示当前在非分支主轨。
            var currentBranch = (string)null;
            var currentTarget = measures;

            float currentBpm = body.InitialBpm;
            float currentScroll = body.InitialScroll;
            int[] currentTimeSig = (int[])body.InitialTimeSignature.Clone();
            bool currentGogo = false;
            bool currentBarline = true;

            while (cursor < lines.Count)
            {
                string line = lines[cursor].Trim();
                cursor++;

                if (line.Equals("#END", StringComparison.OrdinalIgnoreCase))
                    break;

                if (string.IsNullOrEmpty(line) || line.StartsWith("//"))
                    continue;

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
                            break;

                        case "#BRANCHSTART":
                            int branchStartBeat = 0;
                            if (currentTarget != null && targetBeatOffsets.TryGetValue(currentTarget, out int currentOffset))
                                branchStartBeat = currentOffset;

                            // 最外层分支开始时，快照当前已累积的小节作为 pre-branch measures。
                            if (branchStack.Count == 0)
                            {
                                body.PreBranchMeasures = new List<ChartMeasure>(measures);
                            }

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

                            targetBeatOffsets[branchStack.Peek().normalTarget] = branchStartBeat;
                            targetBeatOffsets[branchStack.Peek().professionalTarget] = branchStartBeat;
                            targetBeatOffsets[branchStack.Peek().masterTarget] = branchStartBeat;

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
                                    body.Branches = new ParsedBranches
                                    {
                                        Condition = st.condition,
                                        Normal = st.normalTarget,
                                        Professional = st.professionalTarget,
                                        Master = st.masterTarget,
                                    };
                                    // 分支结束后，后续小节继续写入主轨 measures（作为 post-branch）。
                                    currentTarget = measures;
                                    currentBranch = null;
                                }
                                // 嵌套分支结束：恢复状态并回到父分支轨道，不插入零拍占位小节。
                                RestoreState(branchStack.Count > 0 ? branchStack.Peek() : null, ref currentBpm, ref currentScroll, ref currentTimeSig, ref currentGogo, ref currentBarline);
                                currentTarget = branchStack.Count > 0 ? GetCurrentBranchTarget(branchStack.Peek(), currentBranch) : measures;
                            }
                            break;

                        case "#SECTION":
                        case "#LEVELHOLD":
                            break;

                        default:
                            break;
                    }
                    continue;
                }

                if (currentTarget != null)
                {
                    if (!targetBeatOffsets.TryGetValue(currentTarget, out int measureBaseBeat))
                        measureBaseBeat = 0;

                    var measure = ParseMeasureLine(
                        line,
                        currentBpm,
                        currentScroll,
                        currentTimeSig,
                        currentGogo,
                        currentBarline,
                        measureBaseBeat,
                        balloonHits,
                        ref balloonHitCursor,
                        ref pendingNote);

                    if (measure != null)
                    {
                        currentTarget.Add(measure);
                        targetBeatOffsets[currentTarget] = measureBaseBeat + measure.beatCount;
                    }
                }
            }

            if (body.Branches == null)
            {
                body.Measures = measures;
            }
            else
            {
                // pre-branch 已在 #BRANCHSTART 时快照；post-branch 是 measures 中快照之后新增的部分。
                int preCount = body.PreBranchMeasures?.Count ?? 0;
                if (measures.Count > preCount)
                    body.PostBranchMeasures = measures.GetRange(preCount, measures.Count - preCount);
            }

            return body;
        }

        // 解析单行小节（逗号分拍，拍内按字符解析音符）。
        // pendingNote: 跨小节跟踪未闭合的气球/连打 ChartNote；解析到 7/8 时写入其 endTiming。
        private static ChartMeasure ParseMeasureLine(
            string line, float bpm, float scroll, int[] timeSig,
            bool gogo, bool barline, int measureBaseBeat,
            List<int> balloonHits, ref int balloonHitCursor,
            ref ChartNote pendingNote)
        {
            string[] beats = line.Split(',');
            if (beats.Length == 0) return null;

            // 寻找第一个非空的音符段（在 TJA 中，通常每行只有一个以逗号结尾的段，代表一整个小节）
            string segment = "";
            for (int i = 0; i < beats.Length; i++)
            {
                string text = beats[i].Trim();
                if (!string.IsNullOrEmpty(text))
                {
                    segment = text;
                    break;
                }
            }

            if (string.IsNullOrEmpty(segment))
            {
                return new ChartMeasure
                {
                    beatCount = 0,
                    timeSignature = timeSig != null ? (int[])timeSig.Clone() : null,
                    bpm = bpm,
                    scroll = scroll,
                    gogo = gogo,
                    barline = barline,
                };
            }

            var measure = new ChartMeasure
            {
                beatCount = segment.Length,
                timeSignature = timeSig != null ? (int[])timeSig.Clone() : null,
                bpm = bpm,
                scroll = scroll,
                gogo = gogo,
                barline = barline,
            };

            for (int sub = 0; sub < segment.Length; sub++)
            {
                char ch = segment[sub];
                List<int> currentTiming = new() { measureBaseBeat + sub, 0, 1 };

                // 气球结束符 7：将 pending 气球写入当前小节，设置 endTiming
                if (ch == '7')
                {
                    if (pendingNote != null && (pendingNote.type == NoteType.Balloon || pendingNote.type == NoteType.BigBalloon))
                    {
                        pendingNote.endTime = currentTiming;
                        pendingNote.requiredHits = TryConsumeBalloonHits(balloonHits, ref balloonHitCursor);
                        measure.notes.Add(pendingNote);
                        pendingNote = null;
                    }
                    continue;
                }

                // 连打结束符 8：在当前小节创建 Roll 音符，endTiming 取自 8 的位置
                if (ch == '8')
                {
                    var roll = new ChartNote
                    {
                        startTime = FindLastNoteTiming(measure) ?? currentTiming,
                        type = NoteType.Roll,
                        endTime = currentTiming,
                    };
                    measure.notes.Add(roll);
                    continue;
                }

                if (CharToNoteType.TryGetValue(ch, out NoteType noteType))
                {
                    if (noteType == NoteType.Rest) continue;

                    // 气球起始 5/6：先设为 pending，不立即添加到 notes
                    if (noteType == NoteType.Balloon || noteType == NoteType.BigBalloon)
                    {
                        // 若前一个 pending 未闭合，先以无 endTiming 的形式写入
                        if (pendingNote != null)
                            measure.notes.Add(pendingNote);

                        pendingNote = new ChartNote
                        {
                            startTime = currentTiming,
                            type = noteType,
                        };
                        continue;
                    }

                    measure.notes.Add(new ChartNote
                    {
                        startTime = currentTiming,
                        type = noteType,
                    });
                }
                else if (CharToAdlib.TryGetValue(ch, out NoteType adlibType))
                {
                    if (adlibType == NoteType.Rest) continue;

                    measure.notes.Add(new ChartNote
                    {
                        startTime = currentTiming,
                        type = adlibType,
                    });
                }
            }

            return measure;
        }

        // 在当前小节的 notes 列表中查找最后一个音符的 timing（供连打起止配对）。
        private static List<int> FindLastNoteTiming(ChartMeasure measure)
        {
            if (measure.notes != null && measure.notes.Count > 0)
                return new List<int>(measure.notes[^1].startTime);

            return null;
        }


        // 导出头部元信息。
        private static void EmitMetadata(StringBuilder sb, ChartMetadata meta, ChartBody chart)
        {
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
            var balloonHits = CollectBalloonHits(chart);
            if (balloonHits.Count > 0)
                sb.AppendLine($"BALLOON:{string.Join(",", balloonHits)}");
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

            foreach (var kv in meta.extra)
                sb.AppendLine($"{kv.Key}:{kv.Value}");
        }

        // 导出主体小节。
        private static void EmitChartBody(StringBuilder sb, ChartBody body)
        {
            EmitMeasureList(sb, body.measures);
        }

        // 导出小节列表，并按需输出状态切换指令。
        private static void EmitMeasureList(StringBuilder sb, List<ChartMeasure> measures)
        {
            if (measures == null) return;

            float lastBpm = float.NaN;
            float lastScroll = float.NaN;
            bool lastGogo = false;
            bool lastBarline = true;
            int measureBaseBeat = 0;

            foreach (var m in measures)
            {
                if (!NearlyEqual(m.bpm, lastBpm))
                {
                    sb.AppendLine($"#BPMCHANGE {FormatFloat(m.bpm)}");
                    lastBpm = m.bpm;
                }

                if (!NearlyEqual(m.scroll, lastScroll))
                {
                    sb.AppendLine($"#SCROLL {FormatFloat(m.scroll)}");
                    lastScroll = m.scroll;
                }

                if (m.gogo != lastGogo)
                {
                    sb.AppendLine(m.gogo ? "#GOGOSTART" : "#GOGOEND");
                    lastGogo = m.gogo;
                }

                if (m.barline != lastBarline)
                {
                    sb.AppendLine(m.barline ? "#BARLINEON" : "#BARLINEOFF");
                    lastBarline = m.barline;
                }

                if (m.timeSignature != null)
                {
                    sb.AppendLine($"#MEASURE {m.timeSignature[0]}/{m.timeSignature[1]}");
                }

                sb.AppendLine(EmitMeasureLine(m, measureBaseBeat));
                measureBaseBeat += m.beatCount;
            }
        }

        // 把单个小节转成 TJA 音符行。
        private static string EmitMeasureLine(ChartMeasure measure, int measureBaseBeat)
        {
            if (measure.beatCount <= 0) return ",";

            char[] chars = new char[measure.beatCount];
            for (int i = 0; i < chars.Length; i++)
                chars[i] = '0';

            foreach (var note in measure.notes)
            {
                int beatIndex = ResolveBeatIndexFromTiming(note.startTime, measureBaseBeat, measure.beatCount);
                if (beatIndex < 0 || beatIndex >= measure.beatCount) continue;

                char ch;
                if (AdlibToChar.TryGetValue(note.type, out ch))
                {
                    // 字母扩展音符
                }
                else
                {
                    ch = NoteTypeToChar.GetValueOrDefault(note.type, '0');
                }
                chars[beatIndex] = ch;

                // 气球/连打：在结束位置写入 7 或 8
                if (note.endTime != null)
                {
                    int endIdx = ResolveBeatIndexFromTiming(note.endTime, measureBaseBeat, measure.beatCount);
                    if (endIdx >= 0 && endIdx < measure.beatCount)
                    {
                        bool isBalloon = note.type == NoteType.Balloon || note.type == NoteType.BigBalloon;
                        chars[endIdx] = isBalloon ? '7' : '8';
                    }
                }
            }

            var sb = new StringBuilder();
            foreach (char c in chars)
            {
                sb.Append(c);
            }
            sb.Append(',');
            return sb.ToString();
        }


        // 工具：按行拆分文本。
        private static List<string> SplitLines(string content)
        {
            var lines = new List<string>();
            string[] raw = content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            lines.AddRange(raw);
            return lines;
        }

        // 工具：安全解析 float。
        private static float ParseFloat(string s, float defaultValue)
        {
            if (float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out float result))
                return result;
            return defaultValue;
        }

        // 工具：安全解析 int。
        private static int ParseInt(string s, int defaultValue)
        {
            if (int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out int result))
                return result;
            return defaultValue;
        }

        // 工具：解析逗号分隔整数列表。
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

        // 工具：解析拍号（如 4/4）。
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

        // 工具：格式化浮点数用于输出。
        private static string FormatFloat(float value)
        {
            return value.ToString("0.##", CultureInfo.InvariantCulture);
        }

        // 工具：浮点近似比较。
        private static bool NearlyEqual(float a, float b)
        {
            if (float.IsNaN(a) || float.IsNaN(b)) return false;
            return Mathf.Abs(a - b) < 0.001f;
        }

        // 按气球出现顺序收集 BALLOON 值（从 notes 列表中读取）。
        private static List<int> CollectBalloonHits(ChartBody chart)
        {
            var result = new List<int>();
            if (chart?.measures == null)
                return result;

            foreach (var measure in chart.measures)
            {
                if (measure?.notes == null)
                    continue;

                foreach (var note in measure.notes)
                {
                    if (note.type != NoteType.Balloon && note.type != NoteType.BigBalloon)
                        continue;
                    if (note.requiredHits > 0)
                        result.Add(note.requiredHits);
                }
            }

            return result;
        }

        // 从 BALLOON 列表消费一个目标次数（仅在新格式气球中使用）。
        private static int TryConsumeBalloonHits(List<int> balloonHits, ref int cursor)
        {
            if (balloonHits != null && cursor < balloonHits.Count)
                return balloonHits[cursor++];

            return 0;
        }

        // 将 timing=[a,b,c] 映射为当前小节内拍位索引。
        private static int ResolveBeatIndexFromTiming(List<int> timing, int measureBaseBeat, int beatCount)
        {
            if (timing == null || timing.Count < 3)
                return -1;

            int a = timing[0];
            int b = timing[1];
            int c = timing[2];

            if (c <= 0)
                return -1;

            double localBeat = (a + (double)b / c) - measureBaseBeat;
            int index = (int)Math.Floor(localBeat + 1e-9);

            if (index < 0 || index >= beatCount)
                return -1;

            return index;
        }

        // 分支切换时恢复解析状态。
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

        // 根据分支名拿到对应小节容器。
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

        // 解析分支时的临时状态快照。
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

        // 复制元数据，避免难度段之间互相污染。
        private static ChartMetadata CloneMetadata(ChartMetadata source)
        {
            var clone = new ChartMetadata
            {
                title = source.title,
                titleJa = source.titleJa,
                subtitle = source.subtitle,
                subtitleJa = source.subtitleJa,
                bpm = source.bpm,
                wave = source.wave,
                offset = source.offset,
                demoStart = source.demoStart,
                course = source.course,
                level = source.level,
                branch = source.branch,
                branchCondition = source.branchCondition,
                scoreInit = source.scoreInit,
                scoreDiff = source.scoreDiff,
                songVol = source.songVol,
                seVol = source.seVol,
                maker = source.maker,
                genre = source.genre,
                parsedBalloonHits = new List<int>(source.parsedBalloonHits)
            };

            foreach (var kv in source.extra)
                clone.extra[kv.Key] = kv.Value;

            return clone;
        }

        // 根据解析结果组装最终谱面对象。
        private static TaikoChartData BuildChartData(
            ChartMetadata sectionMeta,
            ParsedChartBody parsedBody,
            string branch,
            string branchCondition,
            List<ChartMeasure> branchMeasures)
        {
            var meta = CloneMetadata(sectionMeta);
            meta.branch = branch;
            meta.branchCondition = branchCondition ?? "";

            // 合并：pre-branch + 分支 + post-branch，保证 Time2Sec 可基于完整小节列表正确计算。
            var mergedMeasures = new List<ChartMeasure>();
            if (parsedBody.PreBranchMeasures != null)
                mergedMeasures.AddRange(parsedBody.PreBranchMeasures);
            if (branchMeasures != null)
                mergedMeasures.AddRange(branchMeasures);
            if (parsedBody.PostBranchMeasures != null)
                mergedMeasures.AddRange(parsedBody.PostBranchMeasures);

            var data = new TaikoChartData
            {
                metadata = meta,
                chart = new ChartBody
                {
                    initialBpm = parsedBody.InitialBpm,
                    initialScroll = parsedBody.InitialScroll,
                    initialTimeSignature = (int[])parsedBody.InitialTimeSignature.Clone(),
                    measures = mergedMeasures
                }
            };

            return data;
        }

        // 主体解析中间结构（尚未落到公开数据模型）。
        private class ParsedChartBody
        {
            public float InitialBpm;
            public float InitialScroll;
            public int[] InitialTimeSignature;
            // 分支前的小节快照（仅在含分支时非空）。
            public List<ChartMeasure> PreBranchMeasures;
            // 分支后的小节（仅在含分支时非空）。
            public List<ChartMeasure> PostBranchMeasures;
            // 无分支时的完整小节列表。
            public List<ChartMeasure> Measures = new();
            public ParsedBranches Branches;
        }

        // 分支解析中间结构。
        private class ParsedBranches
        {
            public string Condition = "";
            public List<ChartMeasure> Normal = new();
            public List<ChartMeasure> Professional = new();
            public List<ChartMeasure> Master = new();
        }
    }
}
