using System.Collections.Generic;
using UnityEngine;

namespace TaikoAssist
{
    public class RendaCreator : Singleton<RendaCreator>
    {
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

        private List<PendingRenda> ChartRendas = new();
        private bool IsDirty = false;

        public IReadOnlyList<PendingRenda> PendingRendas => ChartRendas;

        [System.Serializable]
        public class PendingRenda : ChartNote
        {
            public float StartTimeSec;
            public float EndTimeSec;
            public float Scroll;
            public RendaInfo RendaInstance;
            public bool IsHit;
            public bool IsFinished;
        }

        protected override void Awake()
        {
            base.Awake();
            ChartLoader.Instance.OnChartLoaded += MarkDirty;
        }

        private void Update()
        {
            if (IsDirty)
                RebuildPending();

            if (ChartRendas.Count == 0)
                return;

            float Elapsed = Timer.GetElapsedTime();

            foreach (PendingRenda Renda in ChartRendas)
            {
                if ((InDistance(Renda) || InTimeRange(Renda)) && !Renda.IsFinished && Renda.RendaInstance == null)
                {
                    Transform Track = GetRendaTrack();
                    RendaInfo Instance = PrefabPool.Instance.GetRenda(Track);
                    Instance.Type = Renda.Type;
                    Instance.Speed = Renda.Scroll;
                    Instance.StartTime = Renda.StartTimeSec;
                    Instance.EndTime = Renda.EndTimeSec;
                    Instance.ID = Renda.ID;
                    Instance.RequiredHits = Renda.RequiredHits;
                    Renda.RendaInstance = Instance;
                    Renda.IsFinished = false;
                    SetTexture(Instance);
                }
                else if (Renda.RendaInstance != null && (!InDistance(Renda) && !InTimeRange(Renda) || Renda.IsFinished))
                {
                    PrefabPool.Instance.ReleaseRenda(Renda.RendaInstance);
                    Renda.RendaInstance = null;
                }
                else if (Renda.RendaInstance != null)
                {
                    float Distance = (Renda.StartTimeSec - Elapsed) * Renda.Scroll * GlobalSettings.ScrollSpeed;
                    Renda.RendaInstance.transform.localPosition = new Vector3(Mathf.Max(Distance, 0f), 0f, 0f);

                    float remainingTime = Renda.EndTimeSec - Mathf.Max(Renda.StartTimeSec, Elapsed);
                    float bodyLength = remainingTime * Renda.Scroll * GlobalSettings.ScrollSpeed;
                    Renda.RendaInstance.SetBodyLength(Mathf.Max(bodyLength, 0f));
                }
            }
        }

        // 获取当前最早且实例存在的一个未完成 Renda
        public static PendingRenda GetEarliest()
        {
            foreach (PendingRenda renda in Instance.ChartRendas)
            {
                if (renda.RendaInstance != null && !renda.IsFinished)
                    return renda;
            }
            return null;
        }

        // 获取 Renda 轨道。
        private Transform GetRendaTrack()
        {
            return RendaLine;
        }

        // 根据 Renda 类型应用对应的精灵、缩放和中文 Caption。
        private void SetTexture(RendaInfo renda)
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

        private void RebuildPending()
        {
            ChartRendas.Clear();
            IsDirty = false;

            TaikoChartData Chart = ChartLoader.CurrentChart;
            List<ChartMeasure> Measures = Chart.chart.measures;
            float CurrentScroll = Chart.chart.initialScroll;

            for (int i = 0; i < Measures.Count; i++)
            {
                ChartMeasure M = Measures[i];
                if (M.scroll > 0) CurrentScroll = M.scroll;

                foreach (ChartNote Note in M.notes)
                {
                    if (!BalloonRoll(Note.Type))
                        continue;

                    ChartRendas.Add(new PendingRenda
                    {
                        Type = Note.Type,
                        StartTime = Note.StartTime,
                        EndTime = Note.EndTime,
                        RequiredHits = Note.RequiredHits,
                        StartTimeSec = ChartTime.Time2Sec(Note.StartTime),
                        EndTimeSec = ChartTime.Time2Sec(Note.EndTime),
                        Scroll = CurrentScroll,
                        IsHit = false,
                        IsFinished = false,
                    });
                }
            }
        }

        // 仅 Balloon / Kusudama / Roll / BigRoll 四种 Renda 音符需要加载。
        private static bool BalloonRoll(NoteType type)
        {
            return type == NoteType.Balloon
                || type == NoteType.Roll
                || type == NoteType.BigRoll
                || type == NoteType.Kusudama;
        }

        // 当前时间是否在 Renda 的 JudgeOk 判定窗口内。
        private static bool InTimeRange(PendingRenda Renda)
        {
            float Elapsed = Timer.GetElapsedTime();
            float JudgeWindow = Renda.Type switch
            {
                NoteType.BigRoll => GlobalSettings.JudgeOkBig,
                _ => GlobalSettings.JudgeOkNormal,
            };
            return Mathf.Abs(Elapsed - Renda.StartTimeSec) < JudgeWindow;
        }

        // Renda 是否在可视距离范围内。
        private static bool InDistance(PendingRenda Renda)
        {
            float Elapsed = Timer.GetElapsedTime();
            float StartDistance = (Renda.StartTimeSec - Elapsed) * Renda.Scroll * GlobalSettings.ScrollSpeed;
            float EndDistance = (Renda.EndTimeSec - Elapsed) * Renda.Scroll * GlobalSettings.ScrollSpeed;
            return !(StartDistance > GlobalSettings.LoadRange || EndDistance < 0f);
        }

        public void MarkDirty()
        {
            IsDirty = true;
        }
    }
}
