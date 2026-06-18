using TMPro;
using UnityEngine;

namespace TaikoAssist
{
    public class NoteInfo : MonoBehaviour
    {
        public SpriteRenderer Sprite;
        public TMP_Text Caption;
        public NoteType Type;
        public float Speed;
        public float TargetTime;
        public int ID;
        public int PendingIndex;
    }
}
