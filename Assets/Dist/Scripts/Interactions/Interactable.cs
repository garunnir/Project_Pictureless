using UnityEngine;
namespace Interactions
{
public interface IInteractableFocusVisual
{
    void OnFocusVisual(GameObject interactor);
    void OnUnfocusVisual(GameObject interactor);
}

public interface IInteractable
{
    Transform InteractTransform { get; } // UI용 위치 등
    /// <summary>빠른 상호작용(E) 힌트 문구. 없으면 빈 문자열.</summary>
    string HintText { get; }
    bool CanInteract(GameObject interactor);
    void Interact(GameObject interactor);
    void OnFocus(GameObject interactor);   // 조준 시작
    void OnUnfocus(GameObject interactor); // 조준 종료
}

public abstract class Interactable : MonoBehaviour, IInteractable
{
    [SerializeField] protected string displayName;
    [SerializeField] protected string hintText;   // "E 키: 문 열기" 이런 거
    IInteractableFocusVisual _focusVisual;

    public virtual Transform InteractTransform => transform;
    protected virtual void Awake()
    {
        MonoBehaviour[] behaviours = GetComponentsInChildren<MonoBehaviour>(true);
        for (int i = 0; i < behaviours.Length; i++)
        {
            if (behaviours[i] is IInteractableFocusVisual visual)
            {
                _focusVisual = visual;
                break;
            }
        }
    }
    public virtual bool CanInteract(GameObject interactor) => true;

    public abstract void Interact(GameObject interactor);

    public virtual void OnFocus(GameObject interactor)
    {
        UIEvents.RequestPopup(UIPopupType.InteractionHint, this);
        _focusVisual?.OnFocusVisual(interactor);
    }

    public virtual void OnUnfocus(GameObject interactor)
    {
        UIEvents.RequestPopup(UIPopupType.none, this);
        _focusVisual?.OnUnfocusVisual(interactor);
    }

    public string DisplayName =>
        string.IsNullOrEmpty(displayName) ? string.Empty : Loc.Get(displayName);

    public string HintText =>
        string.IsNullOrEmpty(hintText) ? string.Empty : Loc.Get(hintText);
}

}
