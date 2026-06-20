using UnityEngine;
using UnityEngine.InputSystem;
using DG.Tweening;

namespace TaikoAssist
{
    public class InputDetect : Singleton<InputDetect>
    {
        [SerializeField] private SpriteRenderer[] InputSprites;
        [SerializeField] private AudioClip[] InputSounds;
        [SerializeField] private AudioSource AudioSource;

        private InputAction[] InputActions;
        private bool Available;

        private void OnEnable()
        {
            GlobalSettings.OnSettingsChanged += ResetActions;
            InitActions();
        }

        private void OnDisable()
        {
            GlobalSettings.OnSettingsChanged -= ResetActions;
            DestroyActions();
        }

        private void ResetActions()
        {
            DestroyActions();
            InitActions();
        }

        private void InitActions()
        {
            var Keys = GlobalSettings.AllKeyArrays;
            InputActions = new InputAction[Keys.Length];
            for (int i = 0; i < Keys.Length; i++)
            {
                var KeyArray = Keys[i];
                if (KeyArray == null || KeyArray.Length == 0) continue;
                var Action = new InputAction($"DrumArea_{i}", InputActionType.Button);
                for (int j = 0; j < KeyArray.Length; j++)
                {
                    Action.AddBinding($"<Keyboard>/{KeyArray[j]}");
                }
                int areaIndex = i;
                Action.started += _ => OnKeyPressed(areaIndex);
                Action.Enable();

                InputActions[i] = Action;
            }
            Available = true;
        }

        private void DestroyActions()
        {
            if (!Available || InputActions == null) return;
            for (int i = 0; i < InputActions.Length; i++)
            {
                InputActions[i]?.Disable();
                InputActions[i]?.Dispose();
            }
            InputActions = null;
            Available = false;
        }

        private void OnKeyPressed(int Index)
        {
            var Sprite = InputSprites[Index];
            Sprite.DOKill();
            Sprite.color = Color.white;
            Sprite.DOFade(0f, 0.2f).SetEase(Ease.InQuad);
            int Sound = Index % 2;
            AudioSource.PlayOneShot(InputSounds[Sound]);
        }
    }
}

