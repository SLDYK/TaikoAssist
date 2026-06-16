using System.Collections.Generic;
using UnityEngine;

namespace TaikoAssist
{
    // 连打/风船类音符的创建与管理器。
    // 与 NoteCreator 类似，但专门处理 Balloon / Kusudama / Roll / BigRoll 四种类型，
    // 所有 RendaInfo 音符创建在 Separator 的 RendaLine 轨道上。
    public class RendaCreator : Singleton<RendaCreator>
    {
        [Header("加载配置")]
        [SerializeField] private float LoadRange = 5f;
        [SerializeField] private float ScrollSpeed = 1f;

        [Header("运行时")]
        [SerializeField] private List<RendaInfo> ActiveRendas;

        [Header("Renda资源")]
        [SerializeField] private Sprite BalloonSprite;
        [SerializeField] private Sprite RollSprite;
        [SerializeField] private Sprite RollBodySprite;
        [SerializeField] private Sprite BigRollSprite;
        [SerializeField] private Sprite BigRollBodySprite;
        [SerializeField] private Transform RendaLine;  // 连打轨道（Separator 的 RendaLine）

        private Vector3 NormalScale = Vector3.one * 0.9f;
        private Vector3 BigScale = Vector3.one * 0.7f;

        private List<PendingRenda> _pendingRendas = new();
        private bool _dirty = false;

        public IReadOnlyList<RendaInfo> Rendas => ActiveRendas;

        private struct PendingRenda
        {
            public NoteType type;
            public float timeSec;
            public float endTimeSec;    // 结束时间（秒），连打有效
            public int requiredHits;    // 风船所需击打次数
            public float scroll;
        }

        protected override void Awake()
        {
            base.Awake();
            ChartLoader.Instance.OnChartLoaded += MarkDirty;
        }

        private void Update()
        {
            if (_dirty)
                RebuildPending();

            if (_pendingRendas.Count == 0)
                return;

            float elapsed = Timer.GetElapsedTime();

            // 步骤一：扫描全部待加载 Renda，找出当前应在可视范围内的索引。
            HashSet<int> desired = new();
            for (int i = 0; i < _pendingRendas.Count; i++)
            {
                PendingRenda p = _pendingRendas[i];
                float loadValue = (p.timeSec - elapsed) * p.scroll * ScrollSpeed;

                if (loadValue < 0f)
                    continue;
                if (loadValue > LoadRange)
                    continue;

                desired.Add(i);
            }

            // 步骤二：遍历当前活跃 Renda，保留仍需要的，移除不再需要的。
            for (int i = ActiveRendas.Count - 1; i >= 0; i--)
            {
                RendaInfo renda = ActiveRendas[i];
                if (renda == null)
                {
                    ActiveRendas.RemoveAt(i);
                    continue;
                }

                if (desired.Contains(renda.PendingIndex))
                {
                    // StartTime 尚未到达判定线，仍在加载范围内，继续保留。
                    desired.Remove(renda.PendingIndex);
                }
                else
                {
                    // StartTime 已过判定线，根据类型判断是否应继续存活。
                    bool keepAlive = ShouldKeepAlive(renda, elapsed);

                    if (!keepAlive)
                    {
                        ActiveRendas.RemoveAt(i);
                        NotePool.Instance.ReleaseRenda(renda);
                    }
                }
            }

            // 步骤三：创建新的 RendaInfo。
            foreach (int idx in desired)
            {
                PendingRenda p = _pendingRendas[idx];
                Transform track = GetRendaTrack();
                RendaInfo renda = NotePool.Instance.GetRenda(track);
                renda.Type = p.type;
                renda.Speed = p.scroll;
                renda.StartTime = p.timeSec;
                renda.EndTime = p.endTimeSec;
                renda.RequiredHits = p.requiredHits;
                renda.PendingIndex = idx;
                ApplyRendaResources(renda);
                ActiveRendas.Add(renda);
            }

            // 步骤四：更新所有活跃 Renda 的轨道位置。
            // 到达 StartTime 后锁定在 X=0，不再继续向左移动。
            foreach (RendaInfo renda in ActiveRendas)
            {
                float loadValue = (renda.StartTime - elapsed) * renda.Speed * ScrollSpeed;
                renda.transform.localPosition = new Vector3(Mathf.Max(loadValue, 0f), 0f, 0f);
            }

            // 步骤五：渲染层级由 NoteCreator.LateUpdate 统一混合排序分配。
        }

        // 获取 Renda 轨道。
        private Transform GetRendaTrack()
        {
            return RendaLine;
        }

        // 根据 Renda 类型应用对应的精灵、缩放和中文 Caption。
        private void ApplyRendaResources(RendaInfo renda)
        {
            switch (renda.Type)
            {
                case NoteType.Balloon:
                    renda.Head.sprite = BalloonSprite;
                    renda.Body.sprite = null;
                    renda.transform.localScale = NormalScale;
                    renda.Caption.text = "风船";
                    break;
                case NoteType.Kusudama:
                    renda.Head.sprite = BalloonSprite;
                    renda.Body.sprite = null;
                    renda.transform.localScale = NormalScale;
                    renda.Caption.text = "九素玉";
                    break;
                case NoteType.Roll:
                    renda.Head.sprite = RollSprite;
                    renda.Body.sprite = RollBodySprite;
                    renda.transform.localScale = NormalScale;
                    renda.Caption.text = "连打";
                    break;
                case NoteType.BigRoll:
                    renda.Head.sprite = BigRollSprite;
                    renda.Body.sprite = BigRollBodySprite;
                    renda.transform.localScale = BigScale;
                    renda.Caption.text = "大连打";
                    break;
                default:
                    renda.Caption.text = "";
                    break;
            }
        }

        // 判断 StartTime 已过判定线的 Renda 是否应继续存活。
        // 连打：EndTime 未过则存活；风船：EndTime 未过且打击次数未满足则存活。
        private bool ShouldKeepAlive(RendaInfo renda, float elapsed)
        {
            float endValue = (renda.EndTime - elapsed) * renda.Speed * ScrollSpeed;

            switch (renda.Type)
            {
                case NoteType.Roll:
                case NoteType.BigRoll:
                    return endValue > 0f;
                case NoteType.Balloon:
                case NoteType.Kusudama:
                    return endValue > 0f && renda.RequiredHits > 0;
                default:
                    return false;
            }
        }

        // 从当前谱面重建 _pendingRendas 并清空场景中的旧 Renda。
        private void RebuildPending()
        {
            _pendingRendas.Clear();
            _dirty = false;

            // 回收场景中所有旧 Renda
            for (int i = ActiveRendas.Count - 1; i >= 0; i--)
            {
                if (ActiveRendas[i] != null)
                    NotePool.Instance.ReleaseRenda(ActiveRendas[i]);
            }
            ActiveRendas.Clear();

            TaikoChartData chart = ChartLoader.CurrentChart;
            if (chart == null) return;

            List<ChartMeasure> measures = chart.chart.measures;
            float currentScroll = chart.chart.initialScroll;

            for (int i = 0; i < measures.Count; i++)
            {
                ChartMeasure m = measures[i];
                if (m.scroll > 0) currentScroll = m.scroll;

                foreach (ChartNote cn in m.notes)
                {
                    if (!IsRendaNote(cn.type))
                        continue;

                    // 计算结束时间：连打类用 endTime，风船类直接用 startTime（瞬时判定）
                    float endSec = 0f;
                    if (cn.endTime != null && cn.endTime.Count > 0)
                        endSec = ChartTime.Time2Sec(cn.endTime);

                    _pendingRendas.Add(new PendingRenda
                    {
                        type = cn.type,
                        timeSec = ChartTime.Time2Sec(cn.startTime),
                        endTimeSec = endSec,
                        requiredHits = cn.requiredHits,
                        scroll = currentScroll,
                    });
                }
            }
        }

        // 判断是否为 Renda 类型（风船 / 连打 / 九素玉）。
        private static bool IsRendaNote(NoteType type)
        {
            return type == NoteType.Balloon
                || type == NoteType.Roll
                || type == NoteType.BigRoll
                || type == NoteType.Kusudama;
        }

        // 外部调用：标记谱面数据已变更。
        public void MarkDirty()
        {
            _dirty = true;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (ActiveRendas == null) return;
            Gizmos.color = Color.yellow;
            foreach (RendaInfo renda in ActiveRendas)
            {
                if (renda != null)
                    Gizmos.DrawWireSphere(renda.transform.position, 0.2f);
            }
        }
#endif
    }
}
