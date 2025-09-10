using UnityEngine;

public class MouseEventLogger : MonoBehaviour
{
    void OnMouseDown() { Debug.Log("MouseEventLogger: OnMouseDown on " + gameObject.name); }
    void OnMouseDrag() { Debug.Log("MouseEventLogger: OnMouseDrag on " + gameObject.name); }
    void OnMouseUp() { Debug.Log("MouseEventLogger: OnMouseUp on " + gameObject.name); }
}
