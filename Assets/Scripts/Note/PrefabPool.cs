using System.Collections.Generic;
using UnityEngine;

namespace TaikoAssist
{
    public class PrefabPool : Singleton<PrefabPool>
    {
        [Header("预制体")]
        [SerializeField] private NoteInfo NotePrefab;
        [SerializeField] private RendaInfo RendaPrefab;

        private readonly Queue<NoteInfo> NotePool = new();
        private readonly Queue<RendaInfo> RendaPool = new();

        public NoteInfo GetNote(Transform Track)
        {
            NoteInfo Note;
            if (NotePool.Count > 0)
            {
                Note = NotePool.Dequeue();
                Note.transform.SetParent(Track, false);
                Note.transform.localScale = Vector3.one;
                Note.gameObject.SetActive(true);
            }
            else
            {
                Note = Instantiate(NotePrefab, Track);
            }
            return Note;
        }

        public void ReleaseNote(NoteInfo Note)
        {
            ResetNote(Note);
            Note.gameObject.SetActive(false);
            Note.transform.SetParent(transform, false);
            NotePool.Enqueue(Note);
        }

        private static void ResetNote(NoteInfo note)
        {
            note.Type = NoteType.Don;
            note.Speed = 1f;
            note.TargetTime = 0f;
            note.PendingIndex = 0;
            note.Sprite.sprite = null;
            note.Caption.text = "";
        }

        public RendaInfo GetRenda(Transform Track)
        {
            RendaInfo Renda;
            if (RendaPool.Count > 0)
            {
                Renda = RendaPool.Dequeue();
                Renda.transform.SetParent(Track, false);
                Renda.transform.localScale = Vector3.one;
                Renda.gameObject.SetActive(true);
            }
            else
            {
                Renda = Instantiate(RendaPrefab, Track);
            }
            return Renda;
        }

        public void ReleaseRenda(RendaInfo Renda)
        {
            ResetRenda(Renda);
            Renda.gameObject.SetActive(false);
            Renda.transform.SetParent(transform, false);
            RendaPool.Enqueue(Renda);
        }

        private static void ResetRenda(RendaInfo renda)
        {
            renda.Type = NoteType.Balloon;
            renda.Speed = 1f;
            renda.StartTime = 0f;
            renda.EndTime = 0f;
            renda.RequiredHits = 0;
            renda.HitCount = 0;
            renda.IsHeadHit = false;
            renda.PendingIndex = 0;
            renda.Head.sprite = null;
            renda.Body.sprite = null;
            renda.Caption.text = "";
        }
    }
}
