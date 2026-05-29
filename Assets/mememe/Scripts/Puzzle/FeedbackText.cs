using System.Collections;
using TMPro;
using UnityEngine;

namespace Puzzle
{
    public class FeedbackText : MonoBehaviour
    {
        public TextMeshProUGUI textMesh;
        public float fadeInDuration = 0.4f;
        public float holdDuration = 2f;
        public float fadeOutDuration = 0.4f;

        Coroutine activeRoutine;

        void Awake()
        {
            if (textMesh == null)
                textMesh = GetComponentInChildren<TextMeshProUGUI>();
            SetAlpha(0f);
            gameObject.SetActive(false);
        }

        public void Show(string message)
        {
            if (activeRoutine != null)
                StopCoroutine(activeRoutine);
            textMesh.text = message;
            gameObject.SetActive(true);
            activeRoutine = StartCoroutine(FadeRoutine());
        }

        IEnumerator FadeRoutine()
        {
            yield return Fade(0f, 1f, fadeInDuration);
            yield return new WaitForSeconds(holdDuration);
            yield return Fade(1f, 0f, fadeOutDuration);
            gameObject.SetActive(false);
        }

        IEnumerator Fade(float from, float to, float duration)
        {
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                SetAlpha(Mathf.Lerp(from, to, t / duration));
                yield return null;
            }
            SetAlpha(to);
        }

        void SetAlpha(float a)
        {
            if (textMesh == null) return;
            var c = textMesh.color;
            c.a = a;
            textMesh.color = c;
        }
    }
}
