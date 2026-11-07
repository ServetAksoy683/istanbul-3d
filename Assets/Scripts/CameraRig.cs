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
        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch t = Input.GetTouch(i);
            if (t.phase == TouchPhase.Began)
            {
                // Sag yarida baslayan, ziplama dugmesi olmayan dokunus kamerayi cevirir
                if (touchId == -1 && t.position.x > Screen.width * 0.5f && !MobileControls.IsJumpTouch(t.position)) touchId = t.fingerId;
            }
            else if (t.fingerId == touchId)
            {
                if (t.phase == TouchPhase.Moved)
                {
                    Yaw += t.deltaPosition.x * sensitivity;
                    Pitch = Mathf.Clamp(Pitch - t.deltaPosition.y * sensitivity, -8f, 62f);
                }
                else if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled) touchId = -1;
            }
        }
#if UNITY_EDITOR || UNITY_STANDALONE
        if (Input.GetMouseButton(1))
        {
            Yaw += Input.GetAxis("Mouse X") * 3f;
            Pitch = Mathf.Clamp(Pitch - Input.GetAxis("Mouse Y") * 3f, -8f, 62f);
        }
#endif
    }
}
