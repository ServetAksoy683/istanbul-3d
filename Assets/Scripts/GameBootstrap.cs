using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class GameBootstrap : MonoBehaviour
{
    public PlayerController player;
    public CameraRig cameraRig;
    public Light sun;
    Text loadText; Image loadBar; GameObject loadRoot;

    void Awake() { DebugLog.Hook(); }
    void Start() { StartCoroutine(Run()); }

    void Update()
    {
        // Yukleme sirasinda hata olursa ekranda goster
        if (loadText != null && !string.IsNullOrEmpty(DebugLog.LastError))
            loadText.text = "HATA: " + DebugLog.LastError;
    }

    IEnumerator FitLater()
    {
        yield return null; yield return null; yield return null;
        player.FitModel();
    }

    IEnumerator Run()
    {
        Application.targetFrameRate = 60;
        QualitySettings.vSyncCount = 0;
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        Screen.orientation = ScreenOrientation.LandscapeLeft;
        Screen.autorotateToPortrait = false; Screen.autorotateToPortraitUpsideDown = false;
        Screen.autorotateToLandscapeLeft = true; Screen.autorotateToLandscapeRight = true;
        Screen.orientation = ScreenOrientation.AutoRotation;
        QualitySettings.shadowDistance = 130f;
        QualitySettings.shadowCascades = 2;
        QualitySettings.shadowResolution = ShadowResolution.Medium;
        QualitySettings.anisotropicFiltering = AnisotropicFiltering.Enable;

        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = 0.0011f;
        RenderSettings.fogColor = new Color(0.74f, 0.82f, 0.90f);
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.62f, 0.74f, 0.90f);
        RenderSettings.ambientEquatorColor = new Color(0.56f, 0.58f, 0.60f);
        RenderSettings.ambientGroundColor = new Color(0.32f, 0.30f, 0.28f);
        Material sky = Resources.Load<Material>("Materials/Sky");
        if (sky != null) RenderSettings.skybox = sky;

        BuildLoadingUI();
        WorldBuilder world = gameObject.AddComponent<WorldBuilder>();
        world.Progress = (m, p) => { if (loadText != null) loadText.text = m + "  %" + Mathf.RoundToInt(p * 100f); if (loadBar != null) loadBar.fillAmount = p; };
        yield return StartCoroutine(world.Build());

        Vector3 sp = world.SpawnPoint;
        sp.y = IstanbulGeo.Height(sp.x, sp.z) + 0.6f;
        Physics.SyncTransforms();
        player.Teleport(sp);
        cameraRig.Yaw = 200f;
        cameraRig.transform.position = sp + new Vector3(0, 3f, 5f);

        MobileControls mc = gameObject.AddComponent<MobileControls>();
        mc.playerT = player.transform;
        mc.player = player;
        StartCoroutine(FitLater());
        MiniMap mm = gameObject.AddComponent<MiniMap>();
        mm.target = player.transform; mm.rig = cameraRig;
        if (loadRoot != null) Destroy(loadRoot);
    }

    void BuildLoadingUI()
    {
        Canvas c = MobileControls.EnsureCanvas();
        loadRoot = new GameObject("Loading", typeof(RectTransform), typeof(Image));
        loadRoot.transform.SetParent(c.transform, false);
        RectTransform r = loadRoot.GetComponent<RectTransform>();
        r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero;
        loadRoot.GetComponent<Image>().color = new Color(0.05f, 0.10f, 0.16f, 1f);

        GameObject t = new GameObject("Text", typeof(RectTransform), typeof(Text));
        t.transform.SetParent(loadRoot.transform, false);
        loadText = t.GetComponent<Text>();
        loadText.font = MobileControls.UiFont(); loadText.fontSize = 48; loadText.alignment = TextAnchor.MiddleCenter;
        loadText.color = Color.white; loadText.text = "İstanbul yükleniyor";
        RectTransform tr = t.GetComponent<RectTransform>();
        tr.anchorMin = tr.anchorMax = new Vector2(0.5f, 0.55f); tr.sizeDelta = new Vector2(1400, 90);

        GameObject back = new GameObject("BarBack", typeof(RectTransform), typeof(Image));
        back.transform.SetParent(loadRoot.transform, false);
        RectTransform br = back.GetComponent<RectTransform>();
        br.anchorMin = br.anchorMax = new Vector2(0.5f, 0.42f); br.sizeDelta = new Vector2(900, 22);
        back.GetComponent<Image>().color = new Color(1, 1, 1, 0.15f);
        GameObject fill = new GameObject("BarFill", typeof(RectTransform), typeof(Image));
        fill.transform.SetParent(back.transform, false);
        RectTransform fr = fill.GetComponent<RectTransform>();
        fr.anchorMin = Vector2.zero; fr.anchorMax = Vector2.one; fr.offsetMin = fr.offsetMax = Vector2.zero;
        loadBar = fill.GetComponent<Image>();
        loadBar.color = new Color(0.95f, 0.75f, 0.25f);
        Texture2D tex = new Texture2D(1, 1); tex.SetPixel(0, 0, Color.white); tex.Apply();
        loadBar.sprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f));
        loadBar.type = Image.Type.Filled; loadBar.fillMethod = Image.FillMethod.Horizontal; loadBar.fillAmount = 0f;
    }
}
