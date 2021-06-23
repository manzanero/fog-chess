using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InputNavigator : MonoBehaviour
{
    private EventSystem system;
 
    private void Start()
    {
        system = EventSystem.current;
    }
 
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            if (system.currentSelectedGameObject != null
                && system.currentSelectedGameObject.GetComponent<Selectable>() != null)
            {
                Selectable next = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) ?
                    system.currentSelectedGameObject.GetComponent<Selectable>().FindSelectableOnUp() :
                    system.currentSelectedGameObject.GetComponent<Selectable>().FindSelectableOnDown();
 
                if (next != null)
                {
                    InputField inputfield = next.GetComponent<InputField>();
                    if (inputfield != null)
                    {
                        inputfield.OnPointerClick(new PointerEventData(system));
                    }
 
                    system.SetSelectedGameObject(next.gameObject);
                }
 
                //Here is the navigating back part:
                else
                {
                    next = Selectable.allSelectablesArray[0];
                    system.SetSelectedGameObject(next.gameObject);
                }
 
            }
        }
    }
}
