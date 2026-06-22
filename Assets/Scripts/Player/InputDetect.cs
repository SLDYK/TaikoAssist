using UnityEngine;
using UnityEngine.InputSystem;
using DG.Tweening;
using System.Collections.Generic;
using NC = TaikoAssist.NoteCreator;
using RC = TaikoAssist.RendaCreator;
using System;

namespace TaikoAssist
{
    public class InputDetect : Singleton<InputDetect>
    {
        [SerializeField] private SpriteRenderer[] InputSprites;
        [SerializeField] private AudioClip[] InputSounds;
        [SerializeField] private AudioSource AudioSource;

        private InputAction[] InputActions;
        private bool Available;
        private bool IsAutoPlay;

        private void OnEnable()
        {
            GlobalSettings.OnSettingsChanged += ResetActions;
            InitActions();
        }

        private void OnDisable()
        {
            GlobalSettings.OnSettingsChanged -= ResetActions;
            DestroyActions();
        }

        private void ResetActions()
        {
            DestroyActions();
            InitActions();
            IsAutoPlay = GlobalSettings.AutoPlay;
        }

        private void InitActions()
        {
            var Keys = GlobalSettings.AllKeyArrays;
            InputActions = new InputAction[Keys.Length];
            for (int i = 0; i < Keys.Length; i++)
            {
                var KeyArray = Keys[i];
                if (KeyArray == null || KeyArray.Length == 0) continue;
                var Action = new InputAction($"DrumArea_{i}", InputActionType.Button);
                for (int j = 0; j < KeyArray.Length; j++)
                {
                    Action.AddBinding($"<Keyboard>/{KeyArray[j]}");
                }
                int areaIndex = i;
                Action.started += ctx => OnKeyPressed(areaIndex, ctx.time);
                Action.Enable();

                InputActions[i] = Action;
            }
            IsAutoPlay = GlobalSettings.AutoPlay;
            Available = true;
        }

        private void DestroyActions()
        {
            if (!Available || InputActions == null) return;
            for (int i = 0; i < InputActions.Length; i++)
            {
                InputActions[i]?.Disable();
                InputActions[i]?.Dispose();
            }
            InputActions = null;
            Available = false;
        }

        private void OnKeyPressed(int Index, double time)
        {
            float Elapsed = Timer.GetRelativeTime(time);
            var Sprite = InputSprites[Index];
            Sprite.DOKill();
            Sprite.color = Color.white;
            Sprite.DOFade(0f, 0.2f).SetEase(Ease.InQuad);
            int Sound = Index % 2;
            AudioSource.PlayOneShot(InputSounds[Sound]);
        }

        private void Update()
        {
            if (!Available) return;

            var Note = NC.GetEarliest();
            var Renda = RC.GetEarliest();

            if (Keyboard.current.aKey.wasPressedThisFrame)
            {
                IsAutoPlay = !IsAutoPlay;
                GlobalSettings.AutoPlay = IsAutoPlay;
                Debug.Log($"AutoPlay: {IsAutoPlay}");
            }

            // AutoPlay
            if (!IsAutoPlay)
                return;

            if (Note != null && Renda != null)
            {
                if (Note.TimeSec <= Renda.StartTimeSec)
                {
                    HitNote(Note);
                }
                else
                {
                    HitRenda(Renda);
                }
            }
            else if (Note != null)
            {
                HitNote(Note);
            }
            else if (Renda != null)
            {
                HitRenda(Renda);
            }
        }

        // NoteType → { 左Index, 右Index }
        private static readonly Dictionary<NoteType, int[]> HitAreaMap = new()
        {
            { NoteType.Don,    new[] { 0, 2 } },
            { NoteType.BigDon, new[] { 0, 2 } },
            { NoteType.Kat,    new[] { 1, 3 } },
            { NoteType.BigKat, new[] { 1, 3 } },
        };

        private void HitNote(NC.PendingNote Note)
        {
            if (Note.TimeSec > Timer.GetElapsedTime())
                return;

            if (Note.Type is NoteType.Don or NoteType.BigDon)
            {
                HitDon();
            }
            else
            {
                HitKat();
            }
            Note.IsHit = true;
        }
        private void HitRenda(RC.PendingRenda Renda)
        {
            if (Renda.StartTimeSec > Timer.GetElapsedTime() || Renda.IsStarted)
                return;
            HitDon();
            Renda.IsStarted = true;
        }

        private int HitArea = 0;

        private void HitDon()
        {
            if (HitArea++ % 2 == 0)
                OnKeyPressed(0, 0);
            else
                OnKeyPressed(2, 0);
        }

        private void HitKat()
        {
            if (HitArea++ % 2 == 0)
                OnKeyPressed(1, 0);
            else
                OnKeyPressed(3, 0);
        }
    }
}

