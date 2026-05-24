using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using System.Collections;
using System.Linq;

public class TreasureBoxController : MonoBehaviour
{
    [Header("Барабаны")]
    public DrumController[] drums;               //все барабаны на шкатулке
    public int[] correctCombination = { 3, 5, 1, 7 }; //правильный код

    [Header("Кнопка открытия")]
    public GameObject openButton;                 //кнопка для проверки кода
    public AudioClip buttonClickSound;           //звук нажатия кнопки
    public AudioClip wrongCombinationSound;      //звук неправильной комбинации

    [Header("Анимация")]
    public Transform lid;                        //крышка
    public Transform leftWall;                   //левая стенка
    public Transform rightWall;                  //правая стенка
    public Transform frontWall;                  //передняя стенка
    public Transform backWall;                   //задняя стенка
    public float flyDistance = 2f;               //как далеко разлетаются части
    public float animationTime = 1f;             //время анимации

    [Header("Приз")]
    public GameObject prize;                     //кубок или другой приз внутри

    [Header("Звуки")]
    public AudioClip openSound;                  //звук открытия шкатулки
    public AudioClip victorySound;               //победный фанфар
    public float victorySoundVolume = 0.7f;      //громкость победного звука

    [Header("Музыка")]
    public AudioSource backgroundMusic;          //фоновая музыка (остановится когда откроем)

    private bool isOpen = false;
    private bool[] isDigitMatched;               //какие цифры уже угаданы
    private AudioSource audioSource;
    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable buttonInteractable;

    void Start()
    {
        if (prize != null) prize.SetActive(false);
        isDigitMatched = new bool[10];

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        //настраиваем кнопку открытия
        if (openButton != null)
        {
            buttonInteractable = openButton.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable>();
            if (buttonInteractable != null)
                buttonInteractable.selectEntered.AddListener(OnCheckButtonPressed);
        }

        //сбрасываем цвета барабанов (все серые)
        ResetDrumsColor();
    }

    void ResetDrumsColor()
    {
        foreach (var drum in drums)
        {
            if (drum != null)
                drum.ResetColor();
        }
    }

    //когда нажали на кнопку открытия
    void OnCheckButtonPressed(SelectEnterEventArgs args)
    {
        if (isOpen) return;

        StartCoroutine(AnimateButtonPress()); //анимация нажатия

        if (buttonClickSound != null)
            audioSource.PlayOneShot(buttonClickSound);

        //вибрация на контроллере
        var controller = args.interactorObject.transform.GetComponent<XRBaseController>();
        if (controller != null)
        {
            controller.SendHapticImpulse(0.4f, 0.1f);
        }

        CheckCombination(); //проверяем код
    }

    //анимация нажатия кнопки (смещение по оси X)
    IEnumerator AnimateButtonPress()
    {
        if (openButton == null) yield break;

        Vector3 originalPosition = openButton.transform.localPosition;
        Vector3 pressedPosition = originalPosition + new Vector3(-0.1f, 0, 0);

        //нажали
        float elapsed = 0;
        float pressDuration = 0.05f;
        while (elapsed < pressDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / pressDuration;
            openButton.transform.localPosition = Vector3.Lerp(originalPosition, pressedPosition, t);
            yield return null;
        }

        //возврат
        elapsed = 0;
        float returnDuration = 0.1f;
        while (elapsed < returnDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / returnDuration;
            openButton.transform.localPosition = Vector3.Lerp(pressedPosition, originalPosition, t);
            yield return null;
        }

        openButton.transform.localPosition = originalPosition;
    }

    //проверка кода
    void CheckCombination()
    {
        //сбрасываем массив угаданных цифр
        for (int i = 0; i < isDigitMatched.Length; i++)
            isDigitMatched[i] = false;

        //отмечаем цифры которые стоят на правильных местах
        for (int i = 0; i < drums.Length; i++)
        {
            if (drums[i].currentDigit == correctCombination[i])
            {
                isDigitMatched[drums[i].currentDigit] = true;
            }
        }

        bool allCorrect = true;

        for (int i = 0; i < drums.Length; i++)
        {
            int digit = drums[i].currentDigit;
            int correct = correctCombination[i];

            if (digit == correct)
            {
                drums[i].SetState(DrumState.Correct); //зелёный
            }
            else if (IsDigitAvailableInCombination(digit, i))
            {
                drums[i].SetState(DrumState.WrongPosition); //жёлтый
                allCorrect = false;
            }
            else
            {
                drums[i].SetState(DrumState.NotExist); //красный
                allCorrect = false;
            }
        }

        if (allCorrect)
        {
            if (openSound != null)
                audioSource.PlayOneShot(openSound);

            OpenBox(); //открываем шкатулку
        }
        else
        {
            if (wrongCombinationSound != null)
                audioSource.PlayOneShot(wrongCombinationSound);

            //через 2 секунды сбрасываем цвета в серый
            StartCoroutine(ResetColorsAfterDelay(2f));
        }
    }

    IEnumerator ResetColorsAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        ResetDrumsColor();
    }

    //проверяем есть ли цифра в комбинации и не использована ли уже
    bool IsDigitAvailableInCombination(int digit, int currentIndex)
    {
        if (!correctCombination.Contains(digit))
            return false;

        for (int i = 0; i < drums.Length; i++)
        {
            if (i != currentIndex && drums[i].currentDigit == digit && drums[i].currentDigit == correctCombination[i])
            {
                return false; //цифра уже угадана на другом барабане
            }
        }
        return true;
    }

    //открываем шкатулку
    void OpenBox()
    {
        isOpen = true;

        //останавливаем фоновую музыку
        if (backgroundMusic != null)
        {
            backgroundMusic.Stop();
        }

        //отключаем кнопку и отправляем её в полёт
        if (openButton != null)
        {
            var interactable = openButton.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable>();
            if (interactable != null)
                interactable.enabled = false;

            StartCoroutine(AnimateAndDestroy(openButton.transform, openButton.transform.position + Vector3.up * 3f + Vector3.left * 2f, animationTime, true));
        }

        //крышка поднимается
        StartCoroutine(AnimatePosition(lid, lid.position, lid.position + Vector3.up * 1.5f, animationTime));
        StartCoroutine(AnimateRotation(lid, lid.rotation, Quaternion.Euler(-90, lid.eulerAngles.y, lid.eulerAngles.z), animationTime));

        //стенки разлетаются
        if (leftWall != null) StartCoroutine(AnimateAndDestroy(leftWall, leftWall.position + Vector3.left * flyDistance, animationTime));
        if (rightWall != null) StartCoroutine(AnimateAndDestroy(rightWall, rightWall.position + Vector3.right * flyDistance, animationTime));
        if (frontWall != null) StartCoroutine(AnimateAndDestroy(frontWall, frontWall.position + Vector3.forward * flyDistance, animationTime));
        if (backWall != null) StartCoroutine(AnimateAndDestroy(backWall, backWall.position + Vector3.back * flyDistance, animationTime));

        //барабаны разлетаются
        foreach (var drum in drums)
        {
            if (drum != null)
            {
                Vector3 randomDir = Random.insideUnitSphere.normalized;
                StartCoroutine(AnimateAndDestroy(drum.transform, drum.transform.position + randomDir * flyDistance, animationTime, true));
            }
        }

        StartCoroutine(PlayVictorySound());
        StartCoroutine(ShowPrize());

        //затемнение экрана через 2 секунды
        StartCoroutine(StartFadeAfterDelay(2f));
    }

    //анимация полёта с уничтожением
    IEnumerator AnimateAndDestroy(Transform obj, Vector3 endPosition, float time, bool rotate = false)
    {
        if (obj == null) yield break;

        Vector3 startPos = obj.position;
        float elapsed = 0;

        while (elapsed < time)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / time;
            obj.position = Vector3.Lerp(startPos, endPosition, t);

            if (rotate)
            {
                obj.Rotate(200 * Time.deltaTime, 300 * Time.deltaTime, 250 * Time.deltaTime);
            }

            yield return null;
        }

        Destroy(obj.gameObject);
    }

    IEnumerator AnimatePosition(Transform obj, Vector3 start, Vector3 end, float time)
    {
        float elapsed = 0;
        while (elapsed < time)
        {
            elapsed += Time.deltaTime;
            obj.position = Vector3.Lerp(start, end, elapsed / time);
            yield return null;
        }
    }

    IEnumerator AnimateRotation(Transform obj, Quaternion start, Quaternion end, float time)
    {
        float elapsed = 0;
        while (elapsed < time)
        {
            elapsed += Time.deltaTime;
            obj.rotation = Quaternion.Slerp(start, end, elapsed / time);
            yield return null;
        }
    }

    IEnumerator PlayVictorySound()
    {
        yield return new WaitForSeconds(0.5f);
        if (victorySound != null)
            audioSource.PlayOneShot(victorySound, victorySoundVolume);
    }

    IEnumerator ShowPrize()
    {
        yield return new WaitForSeconds(animationTime);
        if (prize != null)
        {
            prize.SetActive(true);
            StartCoroutine(FloatPrize()); //приз парит и вращается
        }
    }

    IEnumerator FloatPrize()
    {
        Vector3 startPos = prize.transform.position;
        while (true)
        {
            float newY = startPos.y + Mathf.Sin(Time.time * 2f) * 0.2f;
            prize.transform.position = new Vector3(startPos.x, newY, startPos.z);
            prize.transform.Rotate(0, 90 * Time.deltaTime, 0);
            yield return null;
        }
    }

    IEnumerator StartFadeAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (FadeEffect.Instance != null)
        {
            FadeEffect.Instance.StartFade(); //затемняем экран и завершаем игру
        }
    }
}