using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class FadeEffect : MonoBehaviour
{
    public static FadeEffect Instance; //синглтон чтобы вызвать из любого скрипта

    [Header("Компоненты")]
    public Image fadeImage;               //чёрная панель которая затемняет экран
    public TMPro.TextMeshProUGUI victoryText; //текст победы

    [Header("Настройки")]
    public float fadeDuration = 5f;       //за сколько секунд экран станет чёрным
    public float textAppearTime = 2f;     //через сколько секунд появляется текст победы

    private void Awake()
    {
        Instance = this;

        //на старте делаем затемнение прозрачным
        if (fadeImage != null)
        {
            Color color = fadeImage.color;
            color.a = 0f;
            fadeImage.color = color;
        }

        //текст победы тоже прозрачный и выключенный
        if (victoryText != null)
        {
            Color color = victoryText.color;
            color.a = 0f;
            victoryText.color = color;
            victoryText.gameObject.SetActive(false);
        }
    }

    //этот метод вызывается из шкатулки когда победа
    public void StartFade()
    {
        StartCoroutine(FadeCoroutine());
    }

    IEnumerator FadeCoroutine()
    {
        float elapsed = 0f;

        //показываем текст победы
        if (victoryText != null)
        {
            victoryText.gameObject.SetActive(true);
            yield return new WaitForSeconds(textAppearTime);
        }

        //плавно затемняем экран
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeDuration;

            //затемняем чёрную панель
            if (fadeImage != null)
            {
                Color color = fadeImage.color;
                color.a = Mathf.Lerp(0f, 1f, t);
                fadeImage.color = color;
            }

            //текст сначала появляется потом исчезает
            if (victoryText != null)
            {
                Color textColor = victoryText.color;
                if (t < 0.5f)
                {
                    textColor.a = Mathf.Lerp(0f, 1f, t * 2f); //появление
                }
                else
                {
                    textColor.a = Mathf.Lerp(1f, 0f, (t - 0.5f) * 2f); //исчезновение
                }
                victoryText.color = textColor;
            }

            yield return null;
        }

        //экран полностью чёрный
        if (fadeImage != null)
        {
            Color color = fadeImage.color;
            color.a = 1f;
            fadeImage.color = color;
        }

        //немного ждём и закрываем игру
        yield return new WaitForSeconds(1f);
        EndGame();
    }

    void EndGame()
    {
        Debug.Log("Игра завершена!");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false; //останавливаем игру в редакторе
#else
        Application.Quit(); //закрываем игру в билде
#endif
    }
}