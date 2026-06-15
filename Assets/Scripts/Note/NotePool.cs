using System.Collections.Generic;
using UnityEngine;

namespace TaikoAssist
{
    // NoteInfo 的 GameObject 对象池。
    // 获取 Note 时优先从池中取出已回收的实例，池空时才 Instantiate 新的。
    // 回收 Note 时将其 GameObject 设为 inactive 并放回池中，不调用 Destroy。
    public class NotePool : Singleton<NotePool>
    {
        [Header("预制体")]
        [SerializeField] private NoteInfo NotePrefab;

        private readonly Queue<NoteInfo> _pool = new();

        // 从池中获取一个 NoteInfo 实例（已激活），放在指定 parent 下。
        // 若池中有空闲实例则复用，否则 Instantiate 新的。
        public NoteInfo Get(Transform parent)
        {
            NoteInfo note;
            if (_pool.Count > 0)
            {
                note = _pool.Dequeue();
                // 重新挂到目标父节点下，并重置缩放
                note.Transform.SetParent(parent, false);
                note.Transform.localScale = Vector3.one;
                note.gameObject.SetActive(true);
            }
            else
            {
                note = Instantiate(NotePrefab, parent);
            }

            return note;
        }

        // 将 NoteInfo 回收至池中（GameObject 设为 inactive），并清除残留数据防止复用泄漏。
        public void Release(NoteInfo note)
        {
            if (note == null) return;

            ResetNote(note);

            note.gameObject.SetActive(false);
            // 挂到池对象下，避免场景层级混乱
            note.Transform.SetParent(transform, false);

            _pool.Enqueue(note);
        }

        // 重置 NoteInfo 的所有字段，防止复用时旧数据泄漏。
        private static void ResetNote(NoteInfo note)
        {
            note.Type = NoteType.Don;
            note.Speed = 1f;
            note.TargetTime = 0f;
            note.PendingIndex = 0;
            note.Sprite.sprite = null;
        }

        // 清空池中所有已回收的实例（真正 Destroy）。
        public void Clear()
        {
            while (_pool.Count > 0)
            {
                NoteInfo note = _pool.Dequeue();
                if (note != null)
                    Destroy(note.gameObject);
            }
        }
    }
}
