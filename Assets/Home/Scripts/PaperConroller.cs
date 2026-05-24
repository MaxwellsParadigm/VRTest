using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using System.Collections;

public class PaperController : MonoBehaviour
{
    [Header("Настройки")]
    public float moveSpeed = 10f; //скорость возврата листка на место

    [Header("Звуки")]
    public AudioClip grabSound;    //звук когда взяли листок
    public AudioClip releaseSound; //звук когда отпустили

    //компонент для захвата объекта лучом
    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grabInteractable;
    private AudioSource audioSource;      //чтобы проигрывать звуки
    private Vector3 startPosition;        //запоминаем начальную позицию
    private Quaternion startRotation;     //запоминаем начальный поворот
    private bool isHeld = false;          //держат ли листок

    void Start()
    {
        //получаем компонент захвата
        grabInteractable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();

        //настраиваем аудио источник
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && (grabSound != null || releaseSound != null))
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        //запоминаем где стоит листок
        startPosition = transform.position;
        startRotation = transform.rotation;

        //подписываемся на события взятия и отпускания
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.AddListener(OnGrabbed);
            grabInteractable.selectExited.AddListener(OnReleased);
        }
    }

    //когда взяли листок
    void OnGrabbed(SelectEnterEventArgs args)
    {
        isHeld = true;

        //отключаем физику чтобы листок не падал
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        //звук взятия
        if (grabSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(grabSound);
        }
    }

    //когда отпустили листок
    void OnReleased(SelectExitEventArgs args)
    {
        isHeld = false;

        //звук отпускания
        if (releaseSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(releaseSound);
        }

        //возвращаем на место
        StartCoroutine(ReturnToStart());

        //включаем физику обратно
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
        }
    }

    //плавный возврат на место
    IEnumerator ReturnToStart()
    {
        float time = 0;
        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;

        while (time < 1f)
        {
            time += Time.deltaTime * moveSpeed;
            float t = Mathf.Clamp01(time);
            transform.position = Vector3.Lerp(startPos, startPosition, t);
            transform.rotation = Quaternion.Slerp(startRot, startRotation, t);
            yield return null;
        }

        transform.position = startPosition;
        transform.rotation = startRotation;
    }
}