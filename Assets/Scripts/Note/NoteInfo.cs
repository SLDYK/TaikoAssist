using UnityEngine;

namespace TaikoAssist
{
    public class NoteInfo : MonoBehaviour
    {
        public SpriteRenderer Sprite;
        public Transform Transform;
        public NoteType Type;
        public float Speed;
        public float TargetTime;

        // 在 _pendingNotes 中的索引，用于与预计算列表同步
        public int PendingIndex;
    }
}
