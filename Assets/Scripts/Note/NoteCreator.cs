using System.Collections.Generic;
using UnityEngine;

namespace TaikoAssist
{
    public class NoteCreator : Singleton<NoteCreator>
    {
        [Header("Note资源")]
        [SerializeField] private Sprite DonSprite;
        [SerializeField] private Sprite KatSprite;
        [SerializeField] private Sprite BigDonSprite;
        [SerializeField] private Sprite BigKatSprite;
        [SerializeField] private Transform DonLine;  // Don 轨道（Don / BigDon）
        [SerializeField] private Transform KatLine;  // Kat 轨道（Kat / BigKat）
        private Vector3 NormalScale = Vector3.one * 0.9f;
        private Vector3 BigScale = Vector3.one * 0.7f;

        private List<PendingNote> ChartNotes = new();
        private bool IsDirty = false;

        // 缓存的 GlobalSettings 值，避免每帧 PlayerPrefs 读取
        private float _cachedScrollSpeed;
        private float _cachedLoadRange;
        private float _cachedJudgeOkNormal;
        private float _cachedJudgeOkBig;

        public IReadOnlyList<PendingNote> PendingNotes => ChartNotes;

        [System.Serializable]
        public class PendingNote : ChartNote
        {
            public float TimeSec;
            public float Scroll;
            public NoteInfo NoteInstance;
            public bool IsHit;
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
            _cachedJudgeOkNormal = GlobalSettings.JudgeOkNormal;
            _cachedJudgeOkBig = GlobalSettings.JudgeOkBig;
        }

        private void Update()
        {
            if (IsDirty)
                RebuildPending();

            if (ChartNotes.Count == 0)
                return;

            float Elapsed = Timer.GetElapsedTime();

            foreach (PendingNote Note in ChartNotes)
            {
                if ((InDistance(Note, Elapsed) || InTimeRange(Note, Elapsed)) && !Note.IsHit && Note.NoteInstance == null)
                {
                    Transform Track = AttachTrack(Note.Type);
                    NoteInfo Instance = PrefabPool.Instance.GetNote(Track);
                    Instance.TargetTime = Note.TimeSec;
                    Instance.Speed = Note.Scroll;
                    Instance.Type = Note.Type;
                    Note.NoteInstance = Instance;
                    Note.IsHit = false;
                    SetTexture(Instance);
                }
                else if (Note.NoteInstance != null && (!InDistance(Note, Elapsed) && !InTimeRange(Note, Elapsed) || Note.IsHit))
                {
                    PrefabPool.Instance.ReleaseNote(Note.NoteInstance);
                    Note.NoteInstance = null;
                }
                else if (Note.NoteInstance != null)
                {
                    float Distance = (Note.TimeSec - Elapsed) * Note.Scroll * _cachedScrollSpeed;
                    Note.NoteInstance.transform.localPosition = new Vector3(Distance, 0f, 0f);
                }
            }
        }

        public static PendingNote GetEarliest()
        {
            foreach (PendingNote Note in Instance.ChartNotes)
            {
                if (Note.NoteInstance != null && !Note.IsHit)
                    return Note;
            }
            return null;
        }

        private Transform AttachTrack(NoteType Type)
        {
            return Type switch
            {
                NoteType.Don or NoteType.BigDon => DonLine,
                _ => KatLine,
            };
        }

        private void SetTexture(NoteInfo Note)
        {
            switch (Note.Type)
            {
                case NoteType.Don:
                    Note.Sprite.sprite = DonSprite;
                    Note.transform.localScale = NormalScale;
                    Note.Caption.text = "咚";
                    break;
                case NoteType.Kat:
                    Note.Sprite.sprite = KatSprite;
                    Note.transform.localScale = NormalScale;
                    Note.Caption.text = "咔";
                    break;
                case NoteType.BigDon:
                    Note.Sprite.sprite = BigDonSprite;
                    Note.transform.localScale = BigScale;
                    Note.Caption.text = "大咚";
                    break;
                case NoteType.BigKat:
                    Note.Sprite.sprite = BigKatSprite;
                    Note.transform.localScale = BigScale;
                    Note.Caption.text = "大咔";
                    break;
                default:
                    Note.Caption.text = "";
                    break;
            }
        }

        private void RebuildPending()
        {
            ChartNotes.Clear();
            IsDirty = false;

            TaikoChartData Chart = ChartLoader.CurrentChart;
            List<ChartMeasure> Measures = Chart.chart.measures;
            float CurrentScroll = Chart.chart.initialScroll;

            for (int i = 0; i < Measures.Count; i++)
            {
                ChartMeasure M = Measures[i];
                if (M.scroll > 0) CurrentScroll = M.scroll; // 小节可覆盖滚速

                foreach (ChartNote Note in M.notes)
                {
                    if (!DonKat(Note.Type))
                        continue;

                    ChartNotes.Add(new PendingNote
                    {
                        ID = Note.ID,
                        Type = Note.Type,
                        StartTime = Note.StartTime,
                        EndTime = Note.EndTime,
                        RequiredHits = Note.RequiredHits,
                        TimeSec = ChartTime.Time2Sec(Note.StartTime),
                        Scroll = CurrentScroll,
                        IsHit = false,
                    });
                }
            }
        }

        private static bool DonKat(NoteType type)
        {
            return type == NoteType.Don
                || type == NoteType.Kat
                || type == NoteType.BigDon
                || type == NoteType.BigKat;
        }

        private bool InTimeRange(PendingNote Note, float Elapsed)
        {
            float JudgeWindow = Note.Type switch
            {
                NoteType.BigDon or NoteType.BigKat => _cachedJudgeOkBig,
                _ => _cachedJudgeOkNormal,
            };
            return Mathf.Abs(Elapsed - Note.TimeSec) < JudgeWindow;
        }

        private bool InDistance(PendingNote Note, float Elapsed)
        {
            float Distance = (Note.TimeSec - Elapsed) * Note.Scroll * _cachedScrollSpeed;
            return Distance > 0f && Distance < _cachedLoadRange;
        }

        public void MarkDirty()
        {
            IsDirty = true;
        }
    }
}