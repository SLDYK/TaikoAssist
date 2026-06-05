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

        private Key[] Keys = new Key[] { Key.F, Key.D, Key.J, Key.K };

        private void Update()
        {
            for (int i = 0; i < Keys.Length && i < InputSprites.Length; i++)
            {
                if (Keyboard.current[Keys[i]].wasPressedThisFrame)
                {
                    var TriggerSprite = InputSprites[i];
                    TriggerSprite.DOKill();
                    TriggerSprite.color = Color.white;
                    TriggerSprite.DOFade(0f, 0.2f).SetEase(Ease.InQuad);
                    int SoundIndex = i % 2;
                    AudioSource.PlayOneShot(InputSounds[SoundIndex]);
                }
            }
        }
    }
}
