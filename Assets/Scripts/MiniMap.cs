using UnityEngine;
using UnityEngine.UI;

public class MiniMapFogOff : MonoBehaviour
{
    bool f;
    void OnPreRender() { f = RenderSettings.fog; RenderSettings.fog = false; }
    void OnPostRender() { RenderSettings.fog = f; }
}

public class MiniMap : MonoBehaviour
{
    public Transform target;
    public CameraRig rig;
    Camera cam;

    void Start()
    {
        RenderTexture rt = new RenderTexture(320, 320, 16);
        GameObject go = new GameObject("MiniMapCamera");
        cam = go.AddComponent<Camera>();
        cam.orthographic = true; cam.orthographicSize = 240f;
        cam.targetTexture = rt;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.10f, 0.25f, 0.35f);
        cam.nearClipPlane = 1f; cam.farClipPlane = 900f;
        cam.depth = -10f; cam.allowMSAA = false; cam.allowHDR = false;
        go.AddComponent<MiniMapFogOff>();

        Canvas canvas = MobileControls.EnsureCanvas();
        Sprite disc = MobileControls.MakeCircle(256, 0f), ring = MobileControls.MakeCircle(256, 8f);

        GameObject mask = new GameObject("MiniMap", typeof(RectTransform), typeof(Image), typeof(Mask));
        mask.transform.SetParent(canvas.transform, false);
        RectTransform mr = mask.GetComponent<RectTransform>();
        mr.anchorMin = mr.anchorMax = new Vector2(0, 1); mr.pivot = new Vector2(0, 1);
        mr.sizeDelta = new Vector2(320, 320); mr.anchoredPosition = new Vector2(40, -40);
        Image mi = mask.GetComponent<Image>(); mi.sprite = disc; mi.raycastTarget = true;
        mask.GetComponent<Mask>().showMaskGraphic = false;

        GameObject img = new GameObject("View", typeof(RectTransform), typeof(RawImage));
        img.transform.SetParent(mask.transform, false);
        RectTransform ir = img.GetComponent<RectTransform>();
        ir.anchorMin = Vector2.zero; ir.anchorMax = Vector2.one; ir.offsetMin = ir.offsetMax = Vector2.zero;
        RawImage ri = img.GetComponent<RawImage>(); ri.texture = rt; ri.raycastTarget = false;

        GameObject arrow = new GameObject("Player", typeof(RectTransform), typeof(Image));
        arrow.transform.SetParent(mask.transform, false);
        RectTransform ar = arrow.GetComponent<RectTransform>();
        ar.sizeDelta = new Vector2(26, 26); ar.anchoredPosition = Vector2.zero;
        Image ai = arrow.GetComponent<Image>(); ai.sprite = disc; ai.color = new Color(1f, 0.25f, 0.2f); ai.raycastTarget = false;

        GameObject border = new GameObject("Border", typeof(RectTransform), typeof(Image));
        border.transform.SetParent(canvas.transform, false);
        RectTransform brr = border.GetComponent<RectTransform>();
        brr.anchorMin = brr.anchorMax = new Vector2(0, 1); brr.pivot = new Vector2(0, 1);
        brr.sizeDelta = new Vector2(320, 320); brr.anchoredPosition = new Vector2(40, -40);
        Image bi = border.GetComponent<Image>(); bi.sprite = ring; bi.color = new Color(1, 1, 1, 0.85f); bi.raycastTarget = false;
    }

    void LateUpdate()
    {
        if (cam == null || target == null) return;
        cam.transform.position = target.position + Vector3.up * 400f;
        cam.transform.rotation = Quaternion.Euler(90f, rig != null ? rig.Yaw : 0f, 0f);
    }
}
