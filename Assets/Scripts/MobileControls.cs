using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MobileControls : MonoBehaviour
{
    public static Vector2 Move;
    static bool jumpQueued;
    public static Canvas Root;
    public Transform playerT;
    Text districtLabel;
    float labelTimer;

    public static bool ConsumeJump() { bool j = jumpQueued; jumpQueued = false; return j; }
    public static void QueueJump() { jumpQueued = true; }

    public static Font UiFont()
    {
        return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }

    public static Sprite MakeCircle(int size, float ring)
    {
        Texture2D t = new Texture2D(size, size, TextureFormat.RGBA32, false);
        t.wrapMode = TextureWrapMode.Clamp;
        float r = size * 0.5f;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = x + 0.5f - r, dy = y + 0.5f - r;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Mathf.Clamp01(r - dist);
                if (ring > 0f) a *= Mathf.Clamp01(dist - (r - ring));
                t.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        t.Apply();
        return Sprite.Create(t, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    public static Canvas EnsureCanvas()
    {
        if (Root != null) return Root;
        if (FindObjectOfType<EventSystem>() == null)
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        GameObject go = new GameObject("UI");
        Canvas c = go.AddComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        c.sortingOrder = 10;
        CanvasScaler cs = go.AddComponent<CanvasScaler>();
        cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1920, 1080);
        cs.matchWidthOrHeight = 0.5f;
        go.AddComponent<GraphicRaycaster>();
        Root = c;
        return c;
    }

    void Start()
    {
        Canvas canvas = EnsureCanvas();
        Sprite ring = MakeCircle(256, 6f), disc = MakeCircle(128, 0f);

        // Yuzen joystick alani (sol alt)
        GameObject zone = new GameObject("JoystickZone", typeof(RectTransform), typeof(Image), typeof(VirtualJoystick));
        zone.transform.SetParent(canvas.transform, false);
        RectTransform zr = zone.GetComponent<RectTransform>();
        zr.anchorMin = zr.anchorMax = Vector2.zero; zr.pivot = Vector2.zero;
        zr.anchoredPosition = Vector2.zero; zr.sizeDelta = new Vector2(900, 800);
        zone.GetComponent<Image>().color = new Color(0, 0, 0, 0);

        GameObject bg = new GameObject("Bg", typeof(RectTransform), typeof(Image));
        bg.transform.SetParent(zone.transform, false);
        RectTransform br = bg.GetComponent<RectTransform>();
        br.anchorMin = br.anchorMax = Vector2.zero; br.pivot = new Vector2(0.5f, 0.5f);
        br.sizeDelta = new Vector2(300, 300); br.anchoredPosition = new Vector2(260, 260);
        Image bgi = bg.GetComponent<Image>(); bgi.sprite = ring; bgi.color = new Color(1, 1, 1, 0.45f); bgi.raycastTarget = false;

        GameObject knob = new GameObject("Knob", typeof(RectTransform), typeof(Image));
        knob.transform.SetParent(bg.transform, false);
        RectTransform kr = knob.GetComponent<RectTransform>();
        kr.sizeDelta = new Vector2(130, 130); kr.anchoredPosition = Vector2.zero;
        Image ki = knob.GetComponent<Image>(); ki.sprite = disc; ki.color = new Color(1, 1, 1, 0.65f); ki.raycastTarget = false;

        VirtualJoystick vj = zone.GetComponent<VirtualJoystick>();
        vj.zone = zr; vj.bg = br; vj.knob = kr; vj.radius = 110f;

        // Ziplama dugmesi (sol tarafta, joystickin ustunde)
        GameObject jb = new GameObject("JumpButton", typeof(RectTransform), typeof(Image), typeof(JumpButton));
        jb.transform.SetParent(canvas.transform, false);
        RectTransform jr = jb.GetComponent<RectTransform>();
        jr.anchorMin = jr.anchorMax = Vector2.zero; jr.pivot = new Vector2(0.5f, 0.5f);
        jr.sizeDelta = new Vector2(190, 190); jr.anchoredPosition = new Vector2(560, 440);
        Image ji = jb.GetComponent<Image>(); ji.sprite = disc; ji.color = new Color(1, 1, 1, 0.35f);
        GameObject jt = new GameObject("Label", typeof(RectTransform), typeof(Text));
        jt.transform.SetParent(jb.transform, false);
        Text jtx = jt.GetComponent<Text>();
        jtx.font = UiFont(); jtx.text = "ZIPLA"; jtx.fontSize = 38; jtx.alignment = TextAnchor.MiddleCenter;
        jtx.color = new Color(1, 1, 1, 0.95f); jtx.raycastTarget = false;
        RectTransform jtr = jt.GetComponent<RectTransform>();
        jtr.anchorMin = Vector2.zero; jtr.anchorMax = Vector2.one; jtr.offsetMin = jtr.offsetMax = Vector2.zero;

        // Semt adi (ust orta)
        GameObject lb = new GameObject("District", typeof(RectTransform), typeof(Text));
        lb.transform.SetParent(canvas.transform, false);
        districtLabel = lb.GetComponent<Text>();
        districtLabel.font = UiFont(); districtLabel.fontSize = 44; districtLabel.alignment = TextAnchor.MiddleCenter;
        districtLabel.color = new Color(1, 1, 1, 0.9f); districtLabel.raycastTarget = false;
        Shadow sh = lb.AddComponent<Shadow>(); sh.effectColor = new Color(0, 0, 0, 0.8f); sh.effectDistance = new Vector2(2, -2);
        RectTransform lr = lb.GetComponent<RectTransform>();
        lr.anchorMin = new Vector2(0.5f, 1f); lr.anchorMax = new Vector2(0.5f, 1f); lr.pivot = new Vector2(0.5f, 1f);
        lr.sizeDelta = new Vector2(900, 70); lr.anchoredPosition = new Vector2(0, -24);
    }

    void Update()
    {
        if (playerT == null || districtLabel == null) return;
        labelTimer -= Time.deltaTime;
        if (labelTimer > 0f) return;
        labelTimer = 0.5f;
        Vector3 p = playerT.position;
        float d = IstanbulGeo.SignedDist(p.x, p.z);
        if (d < -4f) { districtLabel.text = "İstanbul Boğazı"; return; }
        District dom; float u = IstanbulGeo.Urban(p.x, p.z, out dom);
        districtLabel.text = (dom != null && u > 0.15f) ? dom.name : "Yeşil Alan";
    }
}

public class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    public RectTransform zone, bg, knob;
    public float radius = 110f;
    int pid = int.MinValue;
    Vector2 home;

    void Awake() { }

    public void OnPointerDown(PointerEventData e)
    {
        if (pid != int.MinValue) return;
        pid = e.pointerId;
        if (home == Vector2.zero) home = bg.anchoredPosition;
        Vector2 local;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(zone, e.position, e.pressEventCamera, out local);
        local.x = Mathf.Clamp(local.x, radius + 20f, zone.sizeDelta.x - radius);
        local.y = Mathf.Clamp(local.y, radius + 20f, zone.sizeDelta.y - radius);
        bg.anchoredPosition = local;
        OnDrag(e);
    }

    public void OnDrag(PointerEventData e)
    {
        if (e.pointerId != pid) return;
        Vector2 local;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(bg, e.position, e.pressEventCamera, out local);
        Vector2 v = Vector2.ClampMagnitude(local / radius, 1f);
        knob.anchoredPosition = v * radius;
        MobileControls.Move = v;
    }

    public void OnPointerUp(PointerEventData e)
    {
        if (e.pointerId != pid) return;
        pid = int.MinValue;
        MobileControls.Move = Vector2.zero;
        knob.anchoredPosition = Vector2.zero;
        bg.anchoredPosition = home;
    }
}

public class JumpButton : MonoBehaviour, IPointerDownHandler
{
    public void OnPointerDown(PointerEventData e) { MobileControls.QueueJump(); }
}
