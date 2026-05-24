using UnityEngine;
using TMPro;
using UnityEngine.XR.Interaction.Toolkit;

public class FloatingNumberController : MonoBehaviour
{
    [Header("Настройки")]
    public float floatSpeed = 1f; //скорость подъёма цифры
    public float lifetime = 3f;    //через сколько секунд исчезнет
    public bool alwaysFacePlayer = true; //поворачивать к игроку

    [Header("Ссылки")]
    public TextMeshProUGUI text; //текст с цифрой

    private Camera playerCamera; //камера игрока

    void Start()
    {
        //если текст не назначен в инспекторе - ищем в дочерних объектах
        if (text == null)
        {
            text = GetComponentInChildren<TextMeshProUGUI>();
        }

        //находим камеру игрока
        if (alwaysFacePlayer)
        {
            playerCamera = Camera.main;
        }

        //цифра исчезнет через заданное время
        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        //медленно поднимаемся вверх
        transform.position += Vector3.up * Time.deltaTime * floatSpeed;

        //поворачиваем к игроку
        if (alwaysFacePlayer && playerCamera != null)
        {
            Vector3 direction = transform.position - playerCamera.transform.position;
            transform.rotation = Quaternion.LookRotation(direction);
        }

        //эффект мерцания
        if (text != null)
        {
            float alpha = 0.7f + Mathf.Sin(Time.time * 5f) * 0.3f;
            text.color = new Color(1, 0.8f, 0, alpha);
        }
    }

    //вызывается из скрипта шарика чтобы установить нужную цифру
    public void SetDigit(int digit)
    {
        if (text != null)
        {
            text.text = digit.ToString();
        }
    }
}