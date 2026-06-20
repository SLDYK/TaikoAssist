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
        public float EndTime;
        public int ID;
        public int RequiredHits;  // 风船/九素玉：还需击打多少次才算完成（递减）
        public int HitCount;       // 连打：回收前累计击打次数（递增）
        public int PendingIndex;

        [Header("Body Length Settings")]
        [SerializeField] private float RendaBodyX;
        [SerializeField] private float RendaBodyW;
        [SerializeField] private float BigRendaBodyX;
        [SerializeField] private float BigRendaBodyW;

        public void SetBodyLength(float N)
        {
            float baseX, baseW;
            switch (Type)
            {
                case NoteType.BigRoll:
                    baseX = BigRendaBodyX;
                    baseW = BigRendaBodyW;
                    break;
                default:
                    baseX = RendaBodyX;
                    baseW = RendaBodyW;
                    break;
            }

            float scale = transform.lossyScale.x;
            float localN = N / scale;

            var Size = Body.size;
            Size.x = baseW + localN;
            Body.size = Size;

            var Pos = Body.transform.localPosition;
            Pos.x = baseX + 0.5f * localN;
            Body.transform.localPosition = Pos;
        }
    }
}