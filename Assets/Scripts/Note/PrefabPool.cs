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
                Note.transform.position = 9999 * Vector3.one;
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

        private static void ResetNote(NoteInfo Note)
        {
            Note.Type = NoteType.Don;
            Note.Speed = 1f;
            Note.TargetTime = 0f;
            Note.PendingIndex = 0;
            Note.Sprite.sprite = null;
            Note.Caption.text = "";
            Note.transform.position = 9999 * Vector3.one;
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
                Renda.transform.position = 9999 * Vector3.one;
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

        private static void ResetRenda(RendaInfo Renda)
        {
            Renda.Type = NoteType.Balloon;
            Renda.Speed = 1f;
            Renda.StartTime = 0f;
            Renda.EndTime = 0f;
            Renda.RequiredHits = 0;
            Renda.HitCount = 0;
            Renda.PendingIndex = 0;
            Renda.Head.sprite = null;
            Renda.Body.sprite = null;
            Renda.Caption.text = "";
            Renda.transform.position = 9999 * Vector3.one;
        }
    }
}
