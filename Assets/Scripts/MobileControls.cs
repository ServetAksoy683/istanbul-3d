using UnityEngine;
using UnityEngine.UI;

// Ekrandaki son hatayi tutar (telefonda log goremedigimiz icin ekranda gosteriyoruz)
public static class DebugLog
{
    public static string LastError;
    static bool hooked;
    public static void Hook()
    {
        if (hooked) return;
        hooked = true;
        Application.logMessageReceived += (cond, stack, type) =>
        {
            if (type == LogType.Exception || type == LogType.Error)
            {
                string s = cond;
                if (type == LogType.Exception && stack != null)
                {
                    int nl = stack.IndexOf('\n');
                    s += " @ " + (nl > 0 ? stack.Substring(0, nl) : stack);
                }
                LastError = s.Length > 220 ? s.Substring(0, 220) : s;
            }
        };
    }
}

// Joystick ve ziplama dugmesi: EventSystem'e bagimli degil, dogrudan dokunmalari okur.
public class MobileControls : MonoBehaviour
{
    public static Vector2 Move;
    static bool jumpQueued;
    public static Canvas Root;
    static Vector2 jumpCenter;
    static float jumpRadius = 1f;

    public Transform playerT;
    public PlayerController player;

    Text districtLabel, debugLabel;
    RectTransform joyBg, joyKnob;
    int joyId = -1;
    Vector2 joyCenter, joyHome;
    float joyRadius, uiK = 1f, labelTimer;

    public static bool ConsumeJump() { bool j = jumpQueued; jumpQueued = false; return j; }
    public static void QueueJump() { jumpQueued = true; }
    public static bool IsJumpTouch(Vector2 p) { return (p - jumpCenter).sqrMagnitude <= jumpRadius * jumpRadius * 1.4f; }

    public static Font UiFont() { return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); }

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
        GameObject go = new GameObject("UI");
        Canvas c = go.AddComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        c.sortingOrder = 10;
        CanvasScaler cs = go.AddComponent<CanvasScaler>();
        cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1920, 1080);
        cs.matchWidthOrHeight = 0.5f;
        Root = c;
        return c;
    }

    RectTransform MakeImage(string name, Transform parent, Sprite sp, Color col, float sizePx, bool centerAnchor)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        Vector2 a = centerAnchor ? new Vector2(0.5f, 0.5f) : Vector2.zero;
        rt.anchorMin = a; rt.anchorMax = a; rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(sizePx, sizePx) * uiK;
        Image im = go.GetComponent<Image>(); im.sprite = sp; im.color = col; im.raycastTarget = false;
        return rt;
    }

    Text MakeText(string name, Transform parent, int size, TextAnchor anchor, Color col, Vector2 pos, float w)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        Text t = go.GetComponent<Text>();
        t.font = UiFont(); t.fontSize = size; t.alignment = anchor; t.color = col; t.raycastTarget = false;
        t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Overflow;
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f); rt.pivot = new Vector2(0.5f, 1f);
        rt.sizeDelta = new Vector2(w, 60); rt.anchoredPosition = pos;
        return t;
    }

    void SetPos(RectTransform rt, Vector2 px) { rt.anchoredPosition = px * uiK; }

    void Start()
    {
        Canvas canvas = EnsureCanvas();
        uiK = 1f / Mathf.Max(0.01f, canvas.scaleFactor);
        float W = Screen.width, H = Screen.height;
        Sprite ring = MakeCircle(256, 6f), disc = MakeCircle(128, 0f);

        joyRadius = H * 0.12f;
        joyHome = new Vector2(W * 0.13f, H * 0.27f);
        joyBg = MakeImage("JoyBg", canvas.transform, ring, new Color(1, 1, 1, 0.45f), joyRadius * 2.6f, false);
        joyKnob = MakeImage("JoyKnob", joyBg, disc, new Color(1, 1, 1, 0.65f), joyRadius * 1.1f, true);
        joyKnob.anchoredPosition = Vector2.zero;
        SetPos(joyBg, joyHome);

        // Ziplama dugmesi: SAG altta
        jumpCenter = new Vector2(W - H * 0.20f, H * 0.25f);
        jumpRadius = H * 0.10f;
        RectTransform jr = MakeImage("Jump", canvas.transform, disc, new Color(1, 1, 1, 0.35f), jumpRadius * 2f, false);
        SetPos(jr, jumpCenter);
        GameObject jt = new GameObject("Label", typeof(RectTransform), typeof(Text));
        jt.transform.SetParent(jr, false);
        Text jtx = jt.GetComponent<Text>();
        jtx.font = UiFont(); jtx.text = "ZIPLA"; jtx.fontSize = 34; jtx.alignment = TextAnchor.MiddleCenter;
        jtx.color = new Color(1, 1, 1, 0.95f); jtx.raycastTarget = false;
        RectTransform jtr = jt.GetComponent<RectTransform>();
        jtr.anchorMin = Vector2.zero; jtr.anchorMax = Vector2.one; jtr.offsetMin = jtr.offsetMax = Vector2.zero;

        districtLabel = MakeText("District", canvas.transform, 44, TextAnchor.UpperCenter, new Color(1, 1, 1, 0.92f), new Vector2(0, -24), 900);
        Shadow sh = districtLabel.gameObject.AddComponent<Shadow>(); sh.effectColor = new Color(0, 0, 0, 0.8f); sh.effectDistance = new Vector2(2, -2);
        debugLabel = MakeText("Debug", canvas.transform, 24, TextAnchor.UpperCenter, new Color(1f, 0.95f, 0.3f, 0.95f), new Vector2(0, -85), 1600);
    }

    Vector2 ClampJoy(Vector2 p)
    {
        float m = joyRadius * 1.4f;
        return new Vector2(Mathf.Clamp(p.x, m, Screen.width * 0.5f), Mathf.Clamp(p.y, m, Screen.height * 0.68f));
    }

    void ReleaseJoy()
    {
        joyId = -1; Move = Vector2.zero;
        if (joyKnob != null) joyKnob.anchoredPosition = Vector2.zero;
        if (joyBg != null) SetPos(joyBg, joyHome);
    }

    void UpdateJoy(Vector2 pos)
    {
        Vector2 d = (pos - joyCenter) / joyRadius;
        Move = Vector2.ClampMagnitude(d, 1f);
        joyKnob.anchoredPosition = Move * joyRadius * uiK;
    }

    void Update()
    {
        if (joyBg == null) return;
        float W = Screen.width, H = Screen.height;
        bool joyFound = false;

        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch t = Input.GetTouch(i);
            if (t.phase == TouchPhase.Began)
            {
                if (IsJumpTouch(t.position)) { jumpQueued = true; continue; }
                if (joyId == -1 && t.position.x < W * 0.5f && t.position.y < H * 0.68f)
                {
                    joyId = t.fingerId; joyCenter = ClampJoy(t.position); SetPos(joyBg, joyCenter);
                }
            }
            if (t.fingerId == joyId)
            {
                if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled) { ReleaseJoy(); }
                else { joyFound = true; UpdateJoy(t.position); }
            }
        }
#if UNITY_EDITOR
        if (Input.touchCount == 0)
        {
            Vector2 mp = Input.mousePosition;
            if (Input.GetMouseButtonDown(0))
            {
                if (IsJumpTouch(mp)) jumpQueued = true;
                else if (mp.x < W * 0.5f && mp.y < H * 0.68f) { joyId = 999; joyCenter = ClampJoy(mp); SetPos(joyBg, joyCenter); }
            }
            if (joyId == 999)
            {
                if (Input.GetMouseButton(0)) { joyFound = true; UpdateJoy(mp); }
            }
        }
#endif
        if (joyId != -1 && !joyFound) ReleaseJoy();

        labelTimer -= Time.deltaTime;
        if (labelTimer > 0f || playerT == null) return;
        labelTimer = 0.25f;
        Vector3 p = playerT.position;
        float d = IstanbulGeo.SignedDist(p.x, p.z);
        if (d < -4f) districtLabel.text = "İstanbul Boğazı";
        else
        {
            District dom; float u = IstanbulGeo.Urban(p.x, p.z, out dom);
            districtLabel.text = (dom != null && u > 0.15f) ? dom.name : "Yeşil Alan";
        }
        string dbg = string.Format("y={0:F2} zemin={1:F2} yerde={2} yuzme={3} joy=({4:F2},{5:F2}) dokunma={6} kare={7}",
            p.y, player != null ? player.DbgGround : 0f, player != null && player.DbgGrounded, player != null && player.DbgSwimming,
            Move.x, Move.y, Input.touchCount, player != null ? player.DbgFrames : 0);
        if (!string.IsNullOrEmpty(DebugLog.LastError)) dbg += "\nHATA: " + DebugLog.LastError;
        debugLabel.text = dbg;
    }
}
