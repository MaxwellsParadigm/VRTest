using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace Puzzle
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(XRGrabInteractable))]
    public class Book : MonoBehaviour
    {
        [Header("Identity")]
        public int bookIndex; // 1–12

        [Header("Puzzle State")]
        public float heightMultiplier = 1f;
        public BookColor bookColor = BookColor.Red;

        [Header("References")]
        public Material[] colorMaterials;
        public Transform visualsTransform;

        public BookSlot CurrentSlot { get; private set; }

        Rigidbody rb;
        XRGrabInteractable grabInteractable;
        Vector3 baseVisualsScale;
        float baseWorldHalfHeight;

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            rb.isKinematic = true;
            grabInteractable = GetComponent<XRGrabInteractable>();

            if (visualsTransform != null)
            {
                baseVisualsScale = visualsTransform.localScale;
                baseWorldHalfHeight = visualsTransform.lossyScale.y * 0.5f;
            }

            grabInteractable.throwOnDetach = false;
            grabInteractable.selectEntered.AddListener(OnGrabbed);
            grabInteractable.selectExited.AddListener(OnReleased);
        }

        void OnGrabbed(SelectEnterEventArgs args)
        {
            Debug.Log($"[Puzzle] Book {bookIndex} grabbed (slot {(CurrentSlot != null ? CurrentSlot.slotIndex.ToString() : "none")})");
            if (CurrentSlot != null)
            {
                CurrentSlot.Vacate();
                CurrentSlot = null;
                BookshelfPuzzleManager.Instance?.OnBookRemoved();
            }
            rb.isKinematic = false;
        }

        void OnReleased(SelectExitEventArgs args)
        {
            BookshelfPuzzleManager.Instance?.OnBookReleased(this);
        }

        public void SnapToSlot(BookSlot slot)
        {
            CurrentSlot = slot;
            if (!rb.isKinematic)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            rb.isKinematic = true;
            transform.rotation = slot.transform.rotation;
            transform.position = new Vector3(
                slot.transform.position.x,
                slot.transform.position.y + 0.1f * (heightMultiplier - 1f),
                slot.transform.position.z);
        }

        public void DropToFloor(Vector3 velocity = default)
        {
            if (CurrentSlot != null)
            {
                CurrentSlot.Vacate();
                CurrentSlot = null;
            }
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.linearVelocity = velocity;
            Debug.Log($"[Puzzle] Book {bookIndex} dropped with velocity {velocity}");
        }

        public void ApplyHeight()
        {
            if (visualsTransform == null) return;
            var s = baseVisualsScale;
            visualsTransform.localScale = new Vector3(s.x, s.y * heightMultiplier, s.z);
        }

        public void ApplyColor()
        {
            if (visualsTransform == null) return;
            var mr = visualsTransform.GetComponent<MeshRenderer>();
            if (mr == null) return;
            int idx = (int)bookColor;
            if (colorMaterials != null && idx < colorMaterials.Length && colorMaterials[idx] != null)
                mr.material = colorMaterials[idx];
        }

        void OnDestroy()
        {
            if (grabInteractable != null)
            {
                grabInteractable.selectEntered.RemoveListener(OnGrabbed);
                grabInteractable.selectExited.RemoveListener(OnReleased);
            }
        }
    }
}
