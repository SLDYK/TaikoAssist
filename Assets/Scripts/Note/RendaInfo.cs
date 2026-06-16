using TMPro;
using UnityEngine;

namespace TaikoAssist
{
    // 连打/风船类音符的 MonoBehaviour。
    // 与 NoteInfo 类似，但额外包含持续时间型音符所需的结束时间和连打次数。
    public class RendaInfo : MonoBehaviour
    {
        public SpriteRenderer Head;
        public SpriteRenderer Body;
        public TMP_Text Caption;
        public NoteType Type;
        public float Speed;
        public float StartTime;
        public float EndTime;       // 连打/风船的结束时间（秒）
        public int RequiredHits;    // 风船所需击打次数
        public int PendingIndex;
    }
}
