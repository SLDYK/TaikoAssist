using System.Collections.Generic;
using UnityEngine;

namespace TaikoAssist
{
    public class NoteCreator : Singleton<NoteCreator>
    {
        [Header("加载配置")]
        [SerializeField] private float LoadRange = 5f;
        [SerializeField] private float ScrollSpeed = 1f; // 滚速倍率（>1 加速，<1 减速）
        [Header("运行时")]
        [SerializeField] private List<NoteInfo> ActiveNotes;

        [Header("Note资源")]
        [SerializeField] private Sprite DonSprite;
        [SerializeField] private Sprite KatSprite;
        [SerializeField] private Sprite BigDonSprite;
        [SerializeField] private Sprite BigKatSprite;
        [SerializeField] private Transform DonLine;  // Don 轨道（Don / BigDon）
        [SerializeField] private Transform KatLine;  // Kat 轨道（Kat / BigKat）
        private Vector3 NormalScale = Vector3.one * 0.9f;
        private Vector3 BigScale = Vector3.one * 0.7f;

        private List<PendingNote> _pendingNotes = new();
        private bool _dirty = false;

        public IReadOnlyList<NoteInfo> Notes => ActiveNotes;

        private struct PendingNote
        {
            public NoteType type;
            public float timeSec; // ChartTime.Time2Sec 换算后的绝对秒数
            public float scroll;  // 该 Note 所在小节的滚速
        }

        protected override void Awake()
        {
            base.Awake();
            ChartLoader.Instance.OnChartLoaded += MarkDirty;
        }

        private void Update()
        {
            // 谱面数据变更时重建预计算列表
            if (_dirty)
                RebuildPending();

            if (_pendingNotes.Count == 0)
                return;

            float elapsed = Timer.GetElapsedTime();

            // 步骤一：扫描全部待加载 Note，找出当前应在可视范围内的索引集合。
            // 条件：0 ≤ (目标时间 - 当前时间) × 滚速 × 倍率 ≤ LoadRange
            HashSet<int> desired = new();
            for (int i = 0; i < _pendingNotes.Count; i++)
            {
                PendingNote p = _pendingNotes[i];
                float loadValue = (p.timeSec - elapsed) * p.scroll * ScrollSpeed;

                if (loadValue < 0f)
                    continue; // 已过判定线，不在范围内
                if (loadValue > LoadRange)
                    continue; // 还太远，不在范围内

                desired.Add(i);
            }

            // 步骤二：遍历当前活跃 Note，保留仍需要的，移除不再需要的。
            for (int i = ActiveNotes.Count - 1; i >= 0; i--)
            {
                NoteInfo note = ActiveNotes[i];
                if (note == null)
                {
                    ActiveNotes.RemoveAt(i);
                    continue;
                }

                if (desired.Contains(note.PendingIndex))
                {
                    // 此 Note 仍在范围内，从 desired 中移除，避免步骤三重复创建。
                    desired.Remove(note.PendingIndex);
                }
                else
                {
                    // 已超出范围，回收至对象池。
                    ActiveNotes.RemoveAt(i);
                    NotePool.Instance.Release(note);
                }
            }

            // 步骤三：desired 中剩下的索引都是需要新创建的 Note（不在 ActiveNotes 中）。
            foreach (int idx in desired)
            {
                PendingNote p = _pendingNotes[idx];
                Transform track = GetTrackForType(p.type); // 根据类型选轨道
                NoteInfo note = NotePool.Instance.Get(track);
                note.Type = p.type;
                note.Speed = p.scroll;
                note.TargetTime = p.timeSec;
                note.PendingIndex = idx;
                ApplyNoteResources(note); // 根据类型设置精灵和缩放
                ActiveNotes.Add(note);
            }

            // 步骤四：更新所有活跃 Note 的轨道位置。
            // X=0 为判定线，Note 从右侧（X 正值）向左接近。
            foreach (NoteInfo note in ActiveNotes)
            {
                float loadValue = (note.TargetTime - elapsed) * note.Speed * ScrollSpeed;
                note.transform.localPosition = new Vector3(loadValue, 0f, 0f);
            }

        }

        // 根据 NoteType 返回所属轨道：Don→DonLine，Kat→KatLine。
        private Transform GetTrackForType(NoteType type)
        {
            return type switch
            {
                NoteType.Don or NoteType.BigDon => DonLine,
                _ => KatLine,
            };
        }

        // 根据 Note 类型应用对应的精灵、缩放和中文 Caption。
        private void ApplyNoteResources(NoteInfo note)
        {
            switch (note.Type)
            {
                case NoteType.Don:
                    note.Sprite.sprite = DonSprite;
                    note.transform.localScale = NormalScale;
                    note.Caption.text = "咚";
                    break;
                case NoteType.Kat:
                    note.Sprite.sprite = KatSprite;
                    note.transform.localScale = NormalScale;
                    note.Caption.text = "咔";
                    break;
                case NoteType.BigDon:
                    note.Sprite.sprite = BigDonSprite;
                    note.transform.localScale = BigScale;
                    note.Caption.text = "大咚";
                    break;
                case NoteType.BigKat:
                    note.Sprite.sprite = BigKatSprite;
                    note.transform.localScale = BigScale;
                    note.Caption.text = "大咔";
                    break;
                default:
                    note.Caption.text = "";
                    break;
            }
        }

        // 从当前谱面重建 _pendingNotes 并清空场景中的旧 Note。
        private void RebuildPending()
        {
            _pendingNotes.Clear();
            _dirty = false;

            // 回收场景中所有旧 Note
            for (int i = ActiveNotes.Count - 1; i >= 0; i--)
            {
                if (ActiveNotes[i] != null)
                    NotePool.Instance.Release(ActiveNotes[i]);
            }
            ActiveNotes.Clear();

            TaikoChartData chart = ChartLoader.CurrentChart;
            if (chart == null) return;

            List<ChartMeasure> measures = chart.chart.measures;
            float currentScroll = chart.chart.initialScroll;

            // 遍历每个小节的每个音符，筛选四种基础类型
            for (int i = 0; i < measures.Count; i++)
            {
                ChartMeasure m = measures[i];
                if (m.scroll > 0) currentScroll = m.scroll; // 小节可覆盖滚速

                foreach (ChartNote cn in m.notes)
                {
                    if (!IsBasicNote(cn.type))
                        continue;

                    _pendingNotes.Add(new PendingNote
                    {
                        type = cn.type,
                        timeSec = ChartTime.Time2Sec(cn.startTime),
                        scroll = currentScroll,
                    });
                }
            }
        }

        // 仅 Don / Kat / BigDon / BigKat 四种基础音符需要加载。
        private static bool IsBasicNote(NoteType type)
        {
            return type == NoteType.Don
                || type == NoteType.Kat
                || type == NoteType.BigDon
                || type == NoteType.BigKat;
        }

        // 外部调用：标记谱面数据已变更，下一帧会重建。
        public void MarkDirty()
        {
            _dirty = true;
        }

#if UNITY_EDITOR
        // 场景视图中高亮当前活跃的 Note，便于调试。
        private void OnDrawGizmosSelected()
        {
            if (ActiveNotes == null) return;
            Gizmos.color = Color.green;
            foreach (NoteInfo note in ActiveNotes)
            {
                if (note != null)
                    Gizmos.DrawWireSphere(note.transform.position, 0.2f);
            }
        }
#endif
    }
}