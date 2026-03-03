using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Selectable))]
public class UIButtonHoverSfxEmitter : MonoBehaviour, IPointerEnterHandler
{
    [SerializeField] private bool onlyWhenInteractable = true;

    private Selectable selectable;

    private void Awake()
    {
        selectable = GetComponent<Selectable>();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (eventData == null)
            return;

        if (onlyWhenInteractable && selectable != null && !selectable.IsInteractable())
            return;

        if (GameFlowController.Instance != null)
            GameFlowController.Instance.PlayUIButtonHoverSfx();
    }
}
