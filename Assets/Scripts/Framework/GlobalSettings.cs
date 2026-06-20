using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TaikoAssist
{
    public static class GlobalSettings
    {
        // --- PlayerPrefs Keys ---
        private const string KeyScrollSpeed = "GS_ScrollSpeed";
        private const string KeyAutoPlay = "GS_AutoPlay";
        private const string KeyJudgeGood = "GS_JudgeGood";
        private const string KeyJudgeOk = "GS_JudgeOk";
        private const string KeyJudgeOkBig = "GS_JudgeOkBig";
        private const string KeyTrackBlend = "GS_TrackBlend";
        private const string KeyLoadRange = "GS_LoadRange";
        private const string KeyLeftDonKeys = "GS_LeftDonKeys";
        private const string KeyRightDonKeys = "GS_RightDonKeys";
        private const string KeyLeftKatKeys = "GS_LeftKatKeys";
        private const string KeyRightKatKeys = "GS_RightKatKeys";

        // --- 默认值 ---
        public const float DefaultScrollSpeed = 10f;
        public const bool DefaultAutoPlay = false;
        public const float DefaultJudgeGood = 0.025f;
        public const float DefaultJudgeOk = 0.075f;
        public const float DefaultJudgeOkBig = 0.108f;
        public const float DefaultTrackBlend = 0f;
        public const float DefaultLoadRange = 15f;

        // 默认按键配置：四个鼓面/鼓边的按键。
        public static readonly Key[] DefaultLeftDonKeys = { Key.F };
        public static readonly Key[] DefaultRightDonKeys = { Key.J };
        public static readonly Key[] DefaultLeftKatKeys = { Key.D };
        public static readonly Key[] DefaultRightKatKeys = { Key.K };

        // 轨道分离挡位（与 Separator 对齐）。
        public static readonly float[] TrackBlendSteps = { 0f, 0.25f, 0.5f, 0.75f, 1f };

        // --- 设置变更事件 ---
        public static event Action OnSettingsChanged;

        // ==================== 属性 ====================

        // 音符滚动速度倍率（>1 加速，<1 减速）。
        public static float ScrollSpeed
        {
            get => PlayerPrefs.GetFloat(KeyScrollSpeed, DefaultScrollSpeed);
            set
            {
                float Clamped = Mathf.Max(0.1f, value);
                PlayerPrefs.SetFloat(KeyScrollSpeed, Clamped);
                PlayerPrefs.Save();
                OnSettingsChanged?.Invoke();
            }
        }

        // 自动演示模式（true=自动演示/AutoPlay，false=手动游玩）。
        public static bool AutoPlay
        {
            get => PlayerPrefs.GetInt(KeyAutoPlay, DefaultAutoPlay ? 1 : 0) == 1;
            set
            {
                PlayerPrefs.SetInt(KeyAutoPlay, value ? 1 : 0);
                PlayerPrefs.Save();
                OnSettingsChanged?.Invoke();
            }
        }

        // 游玩模式下是否为手动游玩（AutoPlay 的反义，便于阅读）。
        public static bool IsPlayMode => !AutoPlay;

        // 良判定窗口（秒），命中在此范围内为"良"。
        public static float JudgeGood
        {
            get => PlayerPrefs.GetFloat(KeyJudgeGood, DefaultJudgeGood);
            set
            {
                float Clamped = Mathf.Max(0.005f, Mathf.Min(value, JudgeOkNormal));
                PlayerPrefs.SetFloat(KeyJudgeGood, Clamped);
                PlayerPrefs.Save();
                OnSettingsChanged?.Invoke();
            }
        }

        // 可判定窗口（秒）- 通常音符。命中在此范围内为"可"，超过为不可。
        public static float JudgeOkNormal
        {
            get => PlayerPrefs.GetFloat(KeyJudgeOk, DefaultJudgeOk);
            set
            {
                float Clamped = Mathf.Max(JudgeGood, value);
                PlayerPrefs.SetFloat(KeyJudgeOk, Clamped);
                PlayerPrefs.Save();
                OnSettingsChanged?.Invoke();
            }
        }

        // 可判定窗口（秒）- 大音符（BigDon/BigKat）。大音符的"可"窗口更宽。
        public static float JudgeOkBig
        {
            get => PlayerPrefs.GetFloat(KeyJudgeOkBig, DefaultJudgeOkBig);
            set
            {
                float Clamped = Mathf.Max(JudgeGood, value);
                PlayerPrefs.SetFloat(KeyJudgeOkBig, Clamped);
                PlayerPrefs.Save();
                OnSettingsChanged?.Invoke();
            }
        }

        // 轨道分离挡位（0/0.25/0.5/0.75/1）。
        public static float TrackBlend
        {
            get => PlayerPrefs.GetFloat(KeyTrackBlend, DefaultTrackBlend);
            set
            {
                float Clamped = Mathf.Clamp01(value);
                PlayerPrefs.SetFloat(KeyTrackBlend, Clamped);
                PlayerPrefs.Save();
                OnSettingsChanged?.Invoke();
            }
        }

        // 音符加载范围（单位：Unity 世界坐标），超出此范围的音符不创建/被回收。
        // NoteCreator 和 RendaCreator 共用此设置。
        public static float LoadRange
        {
            get => PlayerPrefs.GetFloat(KeyLoadRange, DefaultLoadRange);
            set
            {
                float Clamped = Mathf.Max(1f, value);
                PlayerPrefs.SetFloat(KeyLoadRange, Clamped);
                PlayerPrefs.Save();
                OnSettingsChanged?.Invoke();
            }
        }

        // ==================== 按键配置 ====================

        // 四个鼓面/鼓边的按键数组，以逗号分隔的 Key 整数值存储在 PlayerPrefs 中。

        public static Key[] LeftDonKeys
        {
            get => ParseKeys(PlayerPrefs.GetString(KeyLeftDonKeys, ""), DefaultLeftDonKeys);
            set => SaveKeys(KeyLeftDonKeys, value);
        }

        public static Key[] RightDonKeys
        {
            get => ParseKeys(PlayerPrefs.GetString(KeyRightDonKeys, ""), DefaultRightDonKeys);
            set => SaveKeys(KeyRightDonKeys, value);
        }

        public static Key[] LeftKatKeys
        {
            get => ParseKeys(PlayerPrefs.GetString(KeyLeftKatKeys, ""), DefaultLeftKatKeys);
            set => SaveKeys(KeyLeftKatKeys, value);
        }

        public static Key[] RightKatKeys
        {
            get => ParseKeys(PlayerPrefs.GetString(KeyRightKatKeys, ""), DefaultRightKatKeys);
            set => SaveKeys(KeyRightKatKeys, value);
        }

        // 将四个按键数组合并为一个二维数组，便于遍历。
        public static Key[][] AllKeyArrays =>
            new Key[][] { LeftDonKeys, LeftKatKeys, RightDonKeys, RightKatKeys };

        // 将 Key 数组序列化为逗号分隔的整数字符串。
        private static string SerializeKeys(Key[] keys)
        {
            if (keys == null || keys.Length == 0) return "";
            var parts = new string[keys.Length];
            for (int i = 0; i < keys.Length; i++)
                parts[i] = ((int)keys[i]).ToString();
            return string.Join(",", parts);
        }

        // 从逗号分隔的整数字符串反序列化为 Key 数组；为空或解析失败时返回默认值。
        private static Key[] ParseKeys(string stored, Key[] defaults)
        {
            if (string.IsNullOrEmpty(stored)) return defaults;
            var parts = stored.Split(',');
            var keys = new Key[parts.Length];
            for (int i = 0; i < parts.Length; i++)
            {
                if (!int.TryParse(parts[i], out int val))
                    return defaults;
                keys[i] = (Key)val;
            }
            return keys;
        }

        private static void SaveKeys(string key, Key[] keys)
        {
            PlayerPrefs.SetString(key, SerializeKeys(keys));
            PlayerPrefs.Save();
            OnSettingsChanged?.Invoke();
        }

        // 重置所有设置为默认值。
        public static void ResetAll()
        {
            ScrollSpeed = DefaultScrollSpeed;
            AutoPlay = DefaultAutoPlay;
            JudgeGood = DefaultJudgeGood;
            JudgeOkNormal = DefaultJudgeOk;
            JudgeOkBig = DefaultJudgeOkBig;
            TrackBlend = DefaultTrackBlend;
            LoadRange = DefaultLoadRange;
            LeftDonKeys = DefaultLeftDonKeys;
            RightDonKeys = DefaultRightDonKeys;
            LeftKatKeys = DefaultLeftKatKeys;
            RightKatKeys = DefaultRightKatKeys;
        }
    }
}
