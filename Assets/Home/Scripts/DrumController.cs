using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using System.Collections;

public class DrumController : MonoBehaviour
{
    public int currentDigit = 0;           //текущая цифра на барабане
    public int correctDigit = 0;           //правильная цифра для этого барабана
    public TMPro.TextMeshProUGUI digitText; //текст с цифрой
    public MeshRenderer drumRenderer;       //сам барабан чтобы менять цвет

    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable interactable;
    private Color originalColor;            //исходный цвет (серый)

    void Start()
    {
        interactable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable>();
        if (interactable != null)
        {
            interactable.selectEntered.AddListener(OnClick);
        }

        UpdateDigit();

        //изначально барабан серый
        if (drumRenderer != null)
        {
            originalColor = Color.gray;
            drumRenderer.material.color = originalColor;
        }
    }

    //когда нажали на барабан
    void OnClick(SelectEnterEventArgs args)
    {
        //меняем цифру (0->1->2->...->9->0)
        currentDigit = (currentDigit + 1) % 10;
        UpdateDigit();

        //эффект нажатия (чуть сжимается)
        StartCoroutine(ClickAnimation());

        //вибрация на контроллере
        var controller = args.interactorObject.transform.GetComponent<XRBaseController>();
        if (controller != null)
        {
            controller.SendHapticImpulse(0.3f, 0.1f);
        }
    }

    //эффект нажатия
    IEnumerator ClickAnimation()
    {
        Vector3 originalScale = transform.localScale;
        transform.localScale = originalScale * 0.95f; //чуть сжимаем
        yield return new WaitForSeconds(0.05f);
        transform.localScale = originalScale; //возвращаем
    }

    //обновляем текст с цифрой
    void UpdateDigit()
    {
        if (digitText != null)
            digitText.text = currentDigit.ToString();
    }

    //вызывается из шкатулки чтобы изменить цвет барабана
    public void SetState(DrumState state)
    {
        if (drumRenderer != null)
        {
            switch (state)
            {
                case DrumState.Correct:
                    drumRenderer.material.color = Color.green;
                    break;
                case DrumState.WrongPosition:
                    drumRenderer.material.color = Color.yellow;
                    break;
                case DrumState.NotExist:
                    drumRenderer.material.color = Color.red;
                    break;
                default:
                    drumRenderer.material.color = originalColor;
                    break;
            }
        }
    }

    //сброс цвета в серый
    public void ResetColor()
    {
        if (drumRenderer != null)
            drumRenderer.material.color = originalColor;
    }
}

//состояния для подсветки барабана
public enum DrumState
{
    None,           //нет состояния
    Correct,        //цифра правильная и на своём месте (зелёный)
    WrongPosition,  //цифра есть в коде но не на своём месте (жёлтый)
    NotExist        //цифры нет в коде (красный)
}