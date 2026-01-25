using UnityEngine;
using UnityEngine.EventSystems;

public enum ActionType { Fire, Dash }
//script cho hành động bắn và lướt trên đt
public class ActionButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public ActionType actionType;

    public void OnPointerDown(PointerEventData eventData)
    {
        if (MobileInputManager.Instance == null) return;

        if (actionType == ActionType.Fire) MobileInputManager.Instance.SetFiring(true);
        else if (actionType == ActionType.Dash) MobileInputManager.Instance.SetDashing(true);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (MobileInputManager.Instance == null) return;

        if (actionType == ActionType.Fire) MobileInputManager.Instance.SetFiring(false);
        else if (actionType == ActionType.Dash) MobileInputManager.Instance.SetDashing(false);
    }
}