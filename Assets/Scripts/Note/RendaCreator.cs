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
        [SerializeField] private Transform RendaLine;

        private Vector3 NormalScale = Vector3.one * 0.9f;
        private Vector3 BigScale = Vector3.one * 0.7f;

        private List<PendingRenda> ChartRendas = new();
        private bool IsDirty = false;

        // 缓存的 GlobalSettings 值，避免每帧 PlayerPrefs 读取
        private float _cachedScrollSpeed;
        private float _cachedLoadRange;

        public IReadOnlyList<PendingRenda> PendingRendas => ChartRendas;

        [System.Serializable]
        public class PendingRenda : ChartNote
        {
            public float StartTimeSec;
            public float EndTimeSec;
            public float Scroll;
            public RendaInfo RendaInstance;
            public bool IsStarted;
            public bool IsFinished;
        }

        protected override void Awake()
        {
            base.Awake();
            ChartLoader.Instance.OnChartLoaded += MarkDirty;
            RefreshCachedSettings();
            GlobalSettings.OnSettingsChanged += RefreshCachedSettings;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            GlobalSettings.OnSettingsChanged -= RefreshCachedSettings;
        }

        private void RefreshCachedSettings()
        {
            _cachedScrollSpeed = GlobalSettings.ScrollSpeed;
            _cachedLoadRange = GlobalSettings.LoadRange;
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
                if ((InDistance(Renda, Elapsed) || InTimeRange(Renda, Elapsed)) && !Renda.IsFinished && Renda.RendaInstance == null)
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
                else if (Renda.RendaInstance != null && (!InDistance(Renda, Elapsed) && !InTimeRange(Renda, Elapsed) || Renda.IsFinished))
                {
                    PrefabPool.Instance.ReleaseRenda(Renda.RendaInstance);
                    Renda.RendaInstance = null;
                }
                else if (Renda.RendaInstance != null)
                {
                    float Distance = (Renda.StartTimeSec - Elapsed) * Renda.Scroll * _cachedScrollSpeed;
                    Renda.RendaInstance.transform.localPosition = new Vector3(Mathf.Max(Distance, 0f), 0f, 0f);

                    float RemainingTime = Renda.EndTimeSec - Mathf.Max(Renda.StartTimeSec, Elapsed);
                    float BodyLength = RemainingTime * Renda.Scroll * _cachedScrollSpeed;
                    Renda.RendaInstance.SetBodyLength(Mathf.Max(BodyLength, 0f));
                }
            }
        }

        public static PendingRenda GetEarliest()
        {
            foreach (PendingRenda Renda in Instance.ChartRendas)
            {
                if (Renda.RendaInstance != null && !Renda.IsFinished)
                    return Renda;
            }
            return null;
        }

        private Transform GetRendaTrack()
        {
            return RendaLine;
        }

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
                    renda.Caption.text = "连打(大)";
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
                        IsStarted = false,
                        IsFinished = false,
                    });
                }
            }
        }

        private static bool BalloonRoll(NoteType type)
        {
            return type == NoteType.Balloon
                || type == NoteType.Roll
                || type == NoteType.BigRoll
                || type == NoteType.Kusudama;
        }

        private bool InTimeRange(PendingRenda Renda, float elapsed)
        {
            return elapsed > Renda.StartTimeSec && elapsed < Renda.EndTimeSec;
        }

        private bool InDistance(PendingRenda Renda, float elapsed)
        {
            float StartDistance = (Renda.StartTimeSec - elapsed) * Renda.Scroll * _cachedScrollSpeed;
            float EndDistance = (Renda.EndTimeSec - elapsed) * Renda.Scroll * _cachedScrollSpeed;
            return !(StartDistance > _cachedLoadRange || EndDistance < 0f);
        }

        public void MarkDirty()
        {
            IsDirty = true;
        }
    }
}
