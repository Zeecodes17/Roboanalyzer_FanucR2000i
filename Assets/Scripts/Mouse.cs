using UnityEngine;

public class MouseRaycastTester : MonoBehaviour
{
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            var cam = Camera.main;
            if (cam == null) { Debug.Log("MouseRaycastTester: NO Camera.main (check Tag)"); return; }
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 100f))
                Debug.Log($"MouseRaycastTester: Hit {hit.collider.gameObject.name} layer={LayerMask.LayerToName(hit.collider.gameObject.layer)} at {hit.point}");
            else
                Debug.Log("MouseRaycastTester: Hit NOTHING (ray missed)");
        }
    }
}
