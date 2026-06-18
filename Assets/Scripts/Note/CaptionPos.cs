using System.Collections.Generic;
using UnityEngine;

namespace TaikoAssist
{
    public class CaptionPos : Singleton<CaptionPos>
    {
        private const int MinLayer = 6;

        private void Update()
        {
            float CaptionY = Separator.Instance.CurrentCaptionHight;

            var All = new List<(float Time, SpriteRenderer Sprite, Transform CaptionT)>();

            foreach (var Note in NoteCreator.Instance.PendingNotes)
            {
                if (Note.NoteInstance == null) continue;
                All.Add((Note.TimeSec, Note.NoteInstance.Sprite, Note.NoteInstance.Caption.transform));
            }

            foreach (var Renda in RendaCreator.Instance.PendingRendas)
            {
                if (Renda.RendaInstance == null) continue;
                All.Add((Renda.StartTimeSec, Renda.RendaInstance.Head, Renda.RendaInstance.Caption.transform));
            }

            All.Sort((a, b) => b.Time.CompareTo(a.Time));

            int Layer = MinLayer;
            foreach (var Item in All)
            {
                Item.Sprite.sortingOrder = Layer++;

                Transform T = Item.CaptionT;
                Vector3 Pos = T.position;
                Pos.y = CaptionY;
                T.position = Pos;

                Vector3 GlobalScale = T.parent.lossyScale;
                T.localScale = new Vector3(1f / GlobalScale.x, 1f / GlobalScale.y, 1f);
            }
        }
    }
}