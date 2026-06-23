using System;
using UnityEngine;
using NC = TaikoAssist.NoteCreator;
using RC = TaikoAssist.RendaCreator;
namespace TaikoAssist
{
    public class HitEvaluator : Singleton<HitEvaluator>
    {
        internal void Evaluate(int Index, float Elapsed)
        {
            AreaIndex = Index;
            TimeElapsed = Elapsed;
            var Note = NC.GetEarliest();
            var Renda = RC.GetEarliest();
            if (Note != null && Renda != null)
            {
                if (Note.TimeSec <= Renda.StartTimeSec)
                {
                    JudgeNote(Note);
                }
                else
                {
                    JudgeRenda(Renda);
                }
            }
            else if (Note != null)
            {
                JudgeNote(Note);
            }
            else if (Renda != null)
            {
                JudgeRenda(Renda);
            }
        }

        private int AreaIndex;
        private float TimeElapsed;

        private void JudgeNote(NC.PendingNote Note)
        {
            float TimingDelta = Mathf.Abs(Note.TimeSec - TimeElapsed);

            NoteType Type = Note.NoteInstance.Type;
            bool Condition1 = AreaIndex % 2 == 0; // 0=左咚, 2=右咚
            bool Condition2 = Type == NoteType.Don || Type == NoteType.BigDon;

            if (Condition1 != Condition2)
            {
                // Missed the note due to wrong hit area
                Note.IsHit = true;
                return;
            }
            else if (TimingDelta < GlobalSettings.JudgeGood)
            {
                // Good hit
                Note.IsHit = true;
                return;
            }
            else if (TimingDelta < GlobalSettings.JudgeOk)
            {
                // Ok hit
                Note.IsHit = true;
                return;
            }
            else
            {
                // Missed the note due to timing
                Note.IsHit = true;
                return;
            }
        }

        private void JudgeRenda(RC.PendingRenda Renda)
        {
            Renda.IsStarted = true;

            NoteType Type = Renda.RendaInstance.Type;
            bool Condition = Type == NoteType.Balloon || Type == NoteType.Kusudama;

            if (Condition)
            {
                Renda.IsStarted = true;
                Renda.RendaInstance.RequiredHits--;
                if (Renda.RendaInstance.RequiredHits <= 0)
                {
                    Renda.IsFinished = true;
                }
                return;
            }
        }

    }
}