using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TapOrb.Backgrounds
{
    [RequireComponent(typeof(Image))]
    public class AnimatedBackgroundController : MonoBehaviour
    {
        [SerializeField]
        private float tapZoomScale = 1.01f;
        [SerializeField]
        private float zoomLerpSpeed = 8f;

        private Image image;
        private Coroutine playRoutine;
        private List<GifFrame> gifFrames;
        private bool isPressing;
        private Vector3 baseScale;

        private void Awake()
        {
            image = GetComponent<Image>();
            baseScale = transform.localScale;
        }

        private void OnEnable()
        {
            InputController.OnPressStateChanged += HandlePressState;
        }

        private void OnDisable()
        {
            InputController.OnPressStateChanged -= HandlePressState;
        }

        public void StopAnimation()
        {
            if (playRoutine != null)
                StopCoroutine(playRoutine);
            playRoutine = null;
            gifFrames = null;
        }

        public void ApplySprite(Sprite sprite)
        {
            StopAnimation();
            image.sprite = sprite;
            image.enabled = sprite != null;
        }

        public void ApplyGif(List<GifFrame> frames)
        {
            StopAnimation();
            gifFrames = frames;
            if (gifFrames == null || gifFrames.Count == 0)
            {
                image.enabled = false;
                return;
            }

            image.enabled = true;
            playRoutine = StartCoroutine(PlayGif());
        }

        private IEnumerator PlayGif()
        {
            int index = 0;
            while (gifFrames != null && gifFrames.Count > 0)
            {
                var frame = gifFrames[index];
                if (frame.Texture != null)
                {
                    var sprite = Sprite.Create(frame.Texture, new Rect(0, 0, frame.Texture.width, frame.Texture.height), new Vector2(0.5f, 0.5f));
                    image.sprite = sprite;
                }

                yield return new WaitForSeconds(frame.Delay);
                index = (index + 1) % gifFrames.Count;
            }
        }

        private void HandlePressState(bool pressed)
        {
            isPressing = pressed;
            StopAllCoroutines();
            if (gifFrames != null && gifFrames.Count > 0)
                playRoutine = StartCoroutine(PlayGif());
            StartCoroutine(AnimateZoom());
        }

        private IEnumerator AnimateZoom()
        {
            Vector3 target = isPressing ? baseScale * tapZoomScale : baseScale;
            while ((transform.localScale - target).sqrMagnitude > 0.0001f)
            {
                transform.localScale = Vector3.Lerp(transform.localScale, target, Time.deltaTime * zoomLerpSpeed);
                yield return null;
            }
            transform.localScale = target;
        }
    }
}
