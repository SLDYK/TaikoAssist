using System.Collections.Generic;
using UnityEngine;

namespace TaikoAssist
{
    // 维护当前屏幕上所有 Note/Renda 的 Caption 位置，并统一分配渲染层级。
    public class CaptionPos : Singleton<CaptionPos>
    {
        private const int MinSortingOrder = 6;

        private void Update()
        {
            float captionY = Separator.Instance.CurrentCaptionHight;

            // 收集所有 Note 和 Renda 的 (时间, SpriteRenderer, Caption)
            var all = new List<(float time, SpriteRenderer sr, Transform captionT)>();

            foreach (NoteInfo n in NoteCreator.Instance.Notes)
            {
                if (n == null) continue;
                all.Add((n.TargetTime, n.Sprite, n.Caption.transform));
            }

            if (RendaCreator.Instance != null)
            {
                foreach (RendaInfo r in RendaCreator.Instance.Rendas)
                {
                    if (r == null) continue;
                    all.Add((r.StartTime, r.Head, r.Caption.transform));
                }
            }

            // 按打击时间降序排序，分配渲染层级
            all.Sort((a, b) => b.time.CompareTo(a.time));

            for (int i = 0; i < all.Count; i++)
            {
                var item = all[i];
                item.sr.sortingOrder = MinSortingOrder + i;

                // 同步 Caption 的世界 Y 坐标
                Transform t = item.captionT;
                Vector3 pos = t.position;
                pos.y = captionY;
                t.position = pos;

                // 抵消父级缩放，保持 Caption 大小不变
                Vector3 parentScale = t.parent.lossyScale;
                t.localScale = new Vector3(1f / parentScale.x, 1f / parentScale.y, 1f);
            }
        }
    }
}