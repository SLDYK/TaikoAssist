using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;

namespace TaikoAssist
{
    public class Separator : Singleton<Separator>
    {
        [Header("State 1")]
        [SerializeField] private Vector3 DonLinePos1;
        [SerializeField] private Vector3 KaLinePos1;
        [SerializeField] private Vector3 TrackPos1;
        [SerializeField] private float TrackHight1;

        [Header("State 2")]
        [SerializeField] private Vector3 DonLinePos2;
        [SerializeField] private Vector3 KaLinePos2;
        [SerializeField] private Vector3 TrackPos2;
        [SerializeField] private float TrackHight2;

        [Header("Objects")]
        [SerializeField] private Transform DonLine;
        [SerializeField] private Transform KaLine;
        [SerializeField] private SpriteRenderer Track;
        [SerializeField] private Slider BlendLerp;
        [SerializeField] private SliderReleaseNotifier Notifier;

        [Header("Animation")]
        [SerializeField] private float Duration = 0.5f;
        [SerializeField] private Ease BlendEase = Ease.OutQuad;

        private Sequence BlendTween;
        private Tween BlendLerpTween;
        private static readonly float[] BlendSteps = { 0f, 0.25f, 0.5f, 0.75f, 1f };

        protected override void Awake()
        {
            base.Awake();
            ApplyBlendInstant(0f);
            SetupBlendSlider();
        }

        private void SetupBlendSlider()
        {
            BlendLerp.minValue = 0f;
            BlendLerp.maxValue = 1f;
            BlendLerp.wholeNumbers = false;
            BlendLerp.SetValueWithoutNotify(0f);
        }

        public void BlendTo(float Target)
        {
            Target = Mathf.Clamp01(Target);
            Vector3 DonTarget = Vector3.Lerp(DonLinePos1, DonLinePos2, Target);
            Vector3 KaTarget = Vector3.Lerp(KaLinePos1, KaLinePos2, Target);
            Vector3 TrackTarget = Vector3.Lerp(TrackPos1, TrackPos2, Target);
            float HeightTarget = Mathf.Lerp(TrackHight1, TrackHight2, Target);

            BlendTween?.Kill();
            BlendTween = DOTween.Sequence()
                .Join(DonLine.DOLocalMove(DonTarget, Duration).SetEase(BlendEase))
                .Join(KaLine.DOLocalMove(KaTarget, Duration).SetEase(BlendEase))
                .Join(Track.transform.DOLocalMove(TrackTarget, Duration).SetEase(BlendEase))
                .Join(DOTween.To(() => Track.size.y, y => Track.size = new Vector2(Track.size.x, y), HeightTarget, Duration).SetEase(BlendEase));
        }

        public void BlendToWithSlider(float target)
        {
            target = Mathf.Clamp01(target);
            AnimateSliderTo(target);
            BlendTo(target);
        }

        internal void SnapSliderAndBlend()
        {
            float Snapped = GetNearestBlendStep(BlendLerp.value);

            AnimateSliderTo(Snapped);
            BlendTo(Snapped);
        }

        private void AnimateSliderTo(float target)
        {
            if (BlendLerp == null)
            {
                return;
            }

            BlendLerpTween?.Kill();
            BlendLerpTween = DOTween.To(
                    () => BlendLerp.value,
                    Value => BlendLerp.SetValueWithoutNotify(Value),
                    target,
                    Duration)
                .SetEase(BlendEase);
        }

        private static float GetNearestBlendStep(float value)
        {
            float Closest = BlendSteps[0];
            float MinDistance = Mathf.Abs(value - Closest);

            for (int i = 1; i < BlendSteps.Length; i++)
            {
                float Candidate = BlendSteps[i];
                float Distance = Mathf.Abs(value - Candidate);
                if (Distance < MinDistance)
                {
                    MinDistance = Distance;
                    Closest = Candidate;
                }
            }

            return Closest;
        }

        private void ApplyBlendInstant(float target)
        {
            DonLine.localPosition = Vector3.Lerp(DonLinePos1, DonLinePos2, target);
            KaLine.localPosition = Vector3.Lerp(KaLinePos1, KaLinePos2, target);
            Track.transform.localPosition = Vector3.Lerp(TrackPos1, TrackPos2, target);
            Track.size = new Vector2(Track.size.x, Mathf.Lerp(TrackHight1, TrackHight2, target));
        }

        protected override void OnDestroy()
        {
            BlendLerpTween?.Kill();
            BlendTween?.Kill();
            base.OnDestroy();
        }
    }
}