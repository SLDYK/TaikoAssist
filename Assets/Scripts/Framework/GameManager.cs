using System;
using UnityEngine;

namespace TaikoAssist
{
    // 管理游戏全局状态，已委托给 GlobalSettings。
    // 保留此类以兼容已有引用，实际数据源为 GlobalSettings。
    public class GameManager : Singleton<GameManager>
    {
        // 当前是否为游玩模式。已委托给 GlobalSettings.AutoPlay（取反）。
        [System.Obsolete("请使用 GlobalSettings.IsPlayMode 代替")]
        public static bool PlayMode
        {
            get => GlobalSettings.IsPlayMode;
            set => GlobalSettings.AutoPlay = !value;
        }

        // 模式切换事件。
        public event Action<bool> OnPlayModeChanged;

        protected override void Awake()
        {
            base.Awake();
            GlobalSettings.OnSettingsChanged += OnSettingsChanged;
        }

        private void OnSettingsChanged()
        {
            OnPlayModeChanged?.Invoke(GlobalSettings.IsPlayMode);
        }

        protected override void OnDestroy()
        {
            GlobalSettings.OnSettingsChanged -= OnSettingsChanged;
            base.OnDestroy();
        }
    }
}
