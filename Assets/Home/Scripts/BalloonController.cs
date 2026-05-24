using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using System.Collections;

public class BalloonController : MonoBehaviour
{
    [Header("Настройки")]
    public int hintDigit = -1;           //цифра подсказки
    public float hintDuration = 3f;      //сколько секунд показывать цифру
    public AudioClip popSound;           //звук лопания
    public GameObject confettiPrefab;    //префаб конфетти

    [Header("Визуал")]
    public MeshRenderer balloonRenderer; //сам шарик чтобы менять цвет
    public GameObject stringObject;      //нитка чтобы падала

    [Header("UI подсказки")]
    public GameObject floatingNumberPrefab;  //префаб парящей цифры

    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable interactable;
    private AudioSource audioSource;
    private bool isPopped = false;

    void Start()
    {
        //получаем компонент для нажатия лучом
        interactable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable>();
        if (interactable != null)
        {
            interactable.selectEntered.AddListener(OnPop);
        }

        //настраиваем аудио
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        if (balloonRenderer == null)
            balloonRenderer = GetComponent<MeshRenderer>();

        //если красный шарик и цифра не задана - ставим случайную
        if (hintDigit == -1 && balloonRenderer != null && balloonRenderer.material.color == Color.red)
        {
            hintDigit = Random.Range(0, 10);
        }
    }

    //когда нажали на шарик
    void OnPop(SelectEnterEventArgs args)
    {
        if (isPopped) return;
        isPopped = true;

        //звук лопания
        if (popSound != null)
            audioSource.PlayOneShot(popSound);

        //конфетти
        if (confettiPrefab != null)
        {
            GameObject confetti = Instantiate(confettiPrefab, transform.position, Quaternion.identity);
            Destroy(confetti, 2f);
        }

        //показываем цифру подсказку
        if (hintDigit >= 0)
        {
            ShowFloatingNumber(hintDigit);
        }

        //вибрация на контроллере
        var controller = args.interactorObject.transform.GetComponent<XRBaseController>();
        if (controller != null)
        {
            controller.SendHapticImpulse(0.5f, 0.1f);
        }

        StartCoroutine(PopEffect());
        Destroy(gameObject, 0.3f);
    }

    //создаём парящую цифру
    void ShowFloatingNumber(int digit)
    {
        if (floatingNumberPrefab != null)
        {
            GameObject floating = Instantiate(floatingNumberPrefab, transform.position, Quaternion.identity);

            //ищем скрипт и передаём цифру
            FloatingNumberController floatController = floating.GetComponent<FloatingNumberController>();
            if (floatController != null)
            {
                floatController.SetDigit(digit);
            }
            else
            {
                //запасной вариант - ищем текст напрямую
                TMPro.TextMeshProUGUI text = floating.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                if (text != null)
                {
                    text.text = digit.ToString();
                }
            }

            Destroy(floating, hintDuration);
        }
    }

    //эффект лопания
    IEnumerator PopEffect()
    {
        //шарик сжимается
        Vector3 originalScale = transform.localScale;
        transform.localScale = Vector3.zero;

        //нитка падает вниз
        if (stringObject != null)
        {
            Rigidbody rb = stringObject.AddComponent<Rigidbody>();
            rb.AddForce(Vector3.down * 2f, ForceMode.Impulse);
            stringObject.transform.SetParent(null);
        }

        yield return new WaitForSeconds(0.1f);

        //скрываем шарик
        if (balloonRenderer != null)
            balloonRenderer.enabled = false;
    }
}