using UnityEngine;

public class CameraRig : MonoBehaviour
{
    public Transform target;
    public float distance = 5.2f, height = 1.55f, sensitivity = 0.13f;
    public float Yaw, Pitch = 16f;
    int touchId = -1;

    void LateUpdate()
    {
        if (target == null) return;
        ReadInput();
        Quaternion rot = Quaternion.Euler(Pitch, Yaw, 0f);
        Vector3 focus = target.position + Vector3.up * height;
        Vector3 back = rot * Vector3.back;
        float dist = distance;
        RaycastHit hit;
        if (Physics.SphereCast(focus, 0.25f, back, out hit, distance, ~0, QueryTriggerInteraction.Ignore))
            dist = Mathf.Max(0.8f, hit.distance);
        Vector3 want = focus + back * dist;
        transform.position = Vector3.Lerp(transform.position, want, 1f - Mathf.Exp(-20f * Time.deltaTime));
        transform.rotation = rot;
    }

    void ReadInput()
    {
        var ptrs = PointerInput.Get();
        bool found = false;
        for (int i = 0; i < ptrs.Count; i++)
        {
            Ptr t = ptrs[i];
            if (t.phase == TouchPhase.Began)
            {
                // Sag yarida baslayan, ziplama dugmesi olmayan dokunus kamerayi cevirir
                if (touchId == -1 && t.pos.x > Screen.width * 0.5f && !MobileControls.IsJumpTouch(t.pos)) touchId = t.id;
            }
            if (t.id == touchId)
            {
                if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled) { touchId = -1; }
                else
                {
                    found = true;
                    Yaw += t.delta.x * sensitivity;
                    Pitch = Mathf.Clamp(Pitch - t.delta.y * sensitivity, -8f, 62f);
                }
            }
        }
        if (touchId != -1 && !found) touchId = -1;
    }
}
