using System.Collections.Generic;
using UnityEngine;

public class District
{
    public string name;
    public Vector2 c;
    public float r, density, rot;
    public int minF, maxF, style; // style: 1 eski, 2 modern, 3 gokdelen, 4 villa
    public bool towers;
}

public class Park
{
    public Vector2 c; public float r, density;
}

public class Road
{
    public Vector2[] pts; public float width; public bool draw = true; public bool main;
}

// Bir kanal (Bogaz / Halic): merkez cizgisi + yari genislikler
public class Channel
{
    public readonly Vector2[] P; public readonly float[] Hw; public readonly float[] Cum; public readonly float Length;
    public Channel(Vector2[] p, float[] hw)
    {
        P = p; Hw = hw; Cum = new float[p.Length];
        for (int i = 1; i < p.Length; i++) Cum[i] = Cum[i - 1] + Vector2.Distance(p[i - 1], p[i]);
        Length = Cum[p.Length - 1];
    }
    public float D(Vector2 p)
    {
        float best = 1e9f;
        for (int i = 0; i < P.Length - 1; i++)
        {
            Vector2 a = P[i], b = P[i + 1], ab = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
            float dist = (p - (a + ab * t)).magnitude;
            float w = Mathf.Lerp(Hw[i], Hw[i + 1], t);
            float v = dist - w;
            if (v < best) best = v;
        }
        return best;
    }
    public Vector2 NearestDir(Vector2 p)
    {
        float best = 1e9f; Vector2 dir = Vector2.up;
        for (int i = 0; i < P.Length - 1; i++)
        {
            Vector2 a = P[i], b = P[i + 1], ab = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
            float dist = (p - (a + ab * t)).magnitude;
            if (dist < best) { best = dist; dir = ab.normalized; }
        }
        return dir;
    }
    public void At(float s, out Vector2 pos, out Vector2 dir, out float hw)
    {
        s = Mathf.Clamp(s, 0f, Length);
        int i = 1;
        while (i < P.Length - 1 && Cum[i] < s) i++;
        float t = (s - Cum[i - 1]) / Mathf.Max(0.0001f, Cum[i] - Cum[i - 1]);
        pos = Vector2.Lerp(P[i - 1], P[i], t);
        dir = (P[i] - P[i - 1]).normalized;
        hw = Mathf.Lerp(Hw[i - 1], Hw[i], t);
    }
}

public static class IstanbulGeo
{
    // Gercek mesafeler 0.25 ile kucultulur.
    public const double Scale = 0.25;
    const double OLat = 41.0300, OLon = 29.0000;
    static readonly double MLat = 111200.0;
    static readonly double MLon = 111320.0 * System.Math.Cos(41.03 * System.Math.PI / 180.0);

    public static Vector2 LL(double lat, double lon)
    {
        return new Vector2((float)((lon - OLon) * MLon * Scale), (float)((lat - OLat) * MLat * Scale));
    }
    static Vector2[] Pts(params double[] a)
    {
        Vector2[] r = new Vector2[a.Length / 2];
        for (int i = 0; i < r.Length; i++) r[i] = LL(a[2 * i], a[2 * i + 1]);
        return r;
    }

    public static readonly Vector2 Min = LL(40.945, 28.915);
    public static readonly Vector2 Max = LL(41.115, 29.095);

    // Istanbul Bogazi (kuzeyden guneye)
    public static readonly Channel Bos = new Channel(
        Pts(41.135, 29.073, 41.115, 29.067, 41.0915, 29.0625, 41.0846, 29.0612, 41.0640, 29.0515,
            41.045, 29.0332, 41.040, 29.015, 41.031, 29.004, 41.020, 28.999, 41.005, 28.996, 40.985, 28.996),
        new float[] { 130, 125, 120, 100, 130, 140, 160, 190, 200, 215, 240 });

    // Haliç (agizdan ic kisma)
    public static readonly Channel Gh = new Channel(
        Pts(41.0225, 28.9890, 41.0205, 28.9800, 41.0198, 28.9738, 41.0245, 28.9655,
            41.0310, 28.9545, 41.0410, 28.9445, 41.0500, 28.9355, 41.0600, 28.9325),
        new float[] { 100, 75, 70, 65, 60, 55, 50, 45 });

    // Marmara kiyisi (boylama gore sirali). Kiyinin kuzeyi kara, guneyi deniz.
    static readonly Vector2[] Coast = Pts(
        40.9950, 28.8800, 40.9950, 28.9150, 40.9960, 28.9400, 40.9985, 28.9550, 40.9990, 28.9680,
        41.0000, 28.9780, 41.0065, 28.9815, 41.0140, 28.9855,
        41.0170, 29.0110, 41.0090, 29.0150, 41.0005, 29.0197, 40.9915, 29.0230, 40.9830, 29.0240,
        40.9730, 29.0280, 40.9690, 29.0360, 40.9660, 29.0500, 40.9600, 29.0680, 40.9510, 29.0950, 40.9460, 29.1200);

    // Kiz Kulesi adaciği
    static readonly Vector2 IslandC = LL(41.0211, 29.0041);
    const float IslandR = 14f;
    public static Vector2 KizKulesi { get { return IslandC; } }

    static float CoastD(Vector2 p)
    {
        int n = Coast.Length; float cz;
        if (p.x <= Coast[0].x) cz = Coast[0].y;
        else if (p.x >= Coast[n - 1].x) cz = Coast[n - 1].y;
        else
        {
            int i = 1;
            while (Coast[i].x < p.x) i++;
            float t = (p.x - Coast[i - 1].x) / (Coast[i].x - Coast[i - 1].x);
            cz = Mathf.Lerp(Coast[i - 1].y, Coast[i].y, t);
        }
        return (p.y - cz) * 0.85f;
    }

    // Pozitif = kara (kiyidan metre), negatif = su
    public static float SignedDist(float x, float z)
    {
        Vector2 p = new Vector2(x, z);
        float d = Mathf.Min(CoastD(p), Mathf.Min(Bos.D(p), Gh.D(p)));
        float di = IslandR - Vector2.Distance(p, IslandC);
        if (di > d) d = di;
        return d;
    }

    public static Vector2 SnapToLand(Vector2 p, float minD)
    {
        if (SignedDist(p.x, p.y) >= minD) return p;
        for (float r = 15f; r <= 700f; r += 15f)
            for (int k = 0; k < 24; k++)
            {
                float a = k * Mathf.PI * 2f / 24f;
                Vector2 q = p + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
                if (SignedDist(q.x, q.y) >= minD) return q;
            }
        return p;
    }

    static float S(float a, float b, float x)
    {
        float t = Mathf.Clamp01((x - a) / (b - a));
        return t * t * (3f - 2f * t);
    }

    struct Hill { public Vector2 c; public float amp, sg; }
    static Hill H(double lat, double lon, float amp, float sg) { Hill h; h.c = LL(lat, lon); h.amp = amp; h.sg = sg; return h; }
    static readonly Hill[] Hills = {
        H(41.0272, 29.0689, 55, 320), H(41.0330, 28.9780, 32, 260), H(41.0086, 28.9802, 16, 160),
        H(41.0165, 28.9645, 32, 170), H(41.0150, 28.9500, 20, 250), H(41.0530, 28.9330, 35, 260),
        H(41.0500, 29.0130, 32, 260), H(41.0800, 29.0100, 38, 420), H(41.1000, 28.9600, 45, 600),
        H(41.0850, 29.0450, 35, 400), H(41.1000, 29.0800, 45, 450), H(41.0050, 29.0450, 24, 300),
        H(41.0080, 29.0600, 30, 300)
    };

    public static float Height(float x, float z) { float d; return Height(x, z, out d); }

    public static float Height(float x, float z, out float d)
    {
        d = SignedDist(x, z);
        float shore = S(-40f, 6f, d);
        float h = Mathf.Lerp(-6f, 1.5f, shore);
        if (d > 6f)
        {
            float inl = S(6f, 160f, d);
            float n = Mathf.PerlinNoise(x * 0.0023f + 31.7f, z * 0.0023f + 11.3f) * 16f
                    + Mathf.PerlinNoise(x * 0.011f + 3.1f, z * 0.011f + 5.7f) * 3f;
            h += n * inl;
            float hs = 0f;
            for (int i = 0; i < Hills.Length; i++)
            {
                float dx = x - Hills[i].c.x, dz = z - Hills[i].c.y;
                hs += Hills[i].amp * Mathf.Exp(-(dx * dx + dz * dz) / (2f * Hills[i].sg * Hills[i].sg));
            }
            h += hs * S(10f, 110f, d);
        }
        return h;
    }

    // ---------------- Semtler ----------------
    static District D(string name, double lat, double lon, float r, float dens, int minF, int maxF, float rot, int style, bool towers = false)
    {
        District d = new District();
        d.name = name; d.c = LL(lat, lon); d.r = r; d.density = dens; d.minF = minF; d.maxF = maxF; d.rot = rot; d.style = style; d.towers = towers;
        return d;
    }
    public static readonly District[] Districts = {
        D("Sultanahmet", 41.0100, 28.9760, 450, 0.95f, 3, 5, 15, 1),
        D("Fatih", 41.0120, 28.9600, 750, 0.95f, 3, 6, 20, 1),
        D("Eminönü", 41.0170, 28.9730, 300, 0.95f, 4, 7, 0, 2),
        D("Beyoğlu", 41.0310, 28.9760, 650, 0.95f, 4, 8, -15, 2),
        D("Taksim", 41.0420, 28.9880, 700, 0.92f, 6, 12, 10, 2),
        D("Şişli", 41.0600, 28.9930, 800, 0.90f, 6, 14, 10, 2),
        D("Beşiktaş", 41.0430, 29.0020, 480, 0.90f, 4, 9, -25, 2),
        D("Levent", 41.0830, 29.0110, 750, 0.85f, 6, 12, 5, 3, true),
        D("Etiler", 41.0750, 29.0300, 600, 0.80f, 5, 9, 0, 2),
        D("Eyüp", 41.0450, 28.9420, 800, 0.80f, 3, 7, 30, 1),
        D("Kasımpaşa", 41.0330, 28.9600, 600, 0.85f, 3, 6, 20, 1),
        D("Zeytinburnu", 41.0050, 28.9200, 700, 0.85f, 5, 9, -10, 2),
        D("Bebek", 41.0800, 29.0450, 450, 0.55f, 2, 4, 35, 4),
        D("Üsküdar", 41.0250, 29.0300, 650, 0.90f, 3, 7, -10, 1),
        D("Kuzguncuk", 41.0350, 29.0250, 350, 0.80f, 2, 4, 20, 1),
        D("Beylerbeyi", 41.0450, 29.0450, 450, 0.60f, 2, 5, 30, 4),
        D("Altunizade", 41.0120, 29.0500, 650, 0.85f, 5, 9, 0, 2),
        D("Kadıköy", 40.9900, 29.0300, 650, 0.95f, 4, 8, 5, 2),
        D("Kozyatağı", 40.9780, 29.0750, 700, 0.85f, 6, 12, 8, 3, true),
        D("Suadiye", 40.9680, 29.0600, 500, 0.85f, 4, 7, 20, 2),
        D("Çamlıca", 41.0200, 29.0650, 600, 0.40f, 2, 4, 0, 4),
        D("Beykoz", 41.0950, 29.0850, 600, 0.60f, 2, 5, 0, 4)
    };

    static Park PK(double lat, double lon, float r, float dens) { Park p = new Park(); p.c = LL(lat, lon); p.r = r; p.density = dens; return p; }
    public static readonly Park[] Parks = {
        PK(41.0135, 28.9830, 140, 0.80f), PK(41.0495, 29.0140, 260, 0.90f), PK(41.0450, 28.9970, 110, 0.80f),
        PK(41.1070, 29.0570, 260, 0.90f), PK(41.1050, 28.9600, 750, 0.95f), PK(41.1050, 29.0480, 450, 0.90f),
        PK(41.1050, 29.0800, 600, 0.95f), PK(41.0272, 29.0689, 380, 0.85f), PK(40.9720, 29.0380, 200, 0.80f),
        PK(41.0540, 28.9330, 220, 0.80f), PK(40.9740, 29.0600, 230, 0.70f)
    };

    public static float ParkWeight(float x, float z)
    {
        float best = 0f;
        for (int i = 0; i < Parks.Length; i++)
        {
            float dist = Mathf.Sqrt((x - Parks[i].c.x) * (x - Parks[i].c.x) + (z - Parks[i].c.y) * (z - Parks[i].c.y));
            float w = Mathf.Clamp01((1f - dist / Parks[i].r) * 2.2f) * Parks[i].density;
            if (w > best) best = w;
        }
        return best;
    }

    public static float Urban(float x, float z, out District dom)
    {
        float best = 0f; dom = null;
        for (int i = 0; i < Districts.Length; i++)
        {
            District dd = Districts[i];
            float dx = x - dd.c.x, dz = z - dd.c.y;
            float dist = Mathf.Sqrt(dx * dx + dz * dz);
            float w = 1f - dist / dd.r;
            if (w <= 0f) continue;
            w = w * w * (3f - 2f * w);
            float val = dd.density * Mathf.Clamp01(w * 1.6f);
            if (val > best) { best = val; dom = dd; }
        }
        best *= 1f - 0.95f * ParkWeight(x, z);
        return best;
    }

    // ---------------- Yollar ----------------
    public static readonly List<Road> Roads = new List<Road>();

    static void AddRoad(float width, bool main, params double[] latlon)
    {
        Road r = new Road(); r.pts = Pts(latlon); r.width = width; r.main = main; Roads.Add(r);
    }

    // Kanalin iki kiyisini izleyen sahil yolu. side: +1 = bati/sag, -1 = dogu/sol
    static void ShoreRoad(Channel ch, int side, float off, float width)
    {
        List<Vector2> pts = new List<Vector2>();
        for (float s = 0f; s <= ch.Length; s += 8f)
        {
            Vector2 pos, dir; float hw;
            ch.At(s, out pos, out dir, out hw);
            Vector2 right = new Vector2(dir.y, -dir.x);
            pts.Add(pos + right * side * (hw + off));
        }
        Road r = new Road(); r.pts = pts.ToArray(); r.width = width; r.main = true; Roads.Add(r);
    }

    public static void BuildRoadList()
    {
        Roads.Clear();
        ShoreRoad(Bos, +1, 30f, 13f);   // Avrupa yakasi sahil yolu
        ShoreRoad(Bos, -1, 30f, 13f);   // Anadolu yakasi sahil yolu
        ShoreRoad(Gh, +1, 24f, 11f);
        ShoreRoad(Gh, -1, 24f, 11f);
        // Avrupa yakasi
        AddRoad(14, true, 41.0015, 28.9100, 41.0030, 28.9400, 41.0040, 28.9600, 41.0040, 28.9720, 41.0070, 28.9790, 41.0115, 28.9825);
        AddRoad(12, true, 41.0172, 28.9745, 41.0150, 28.9720, 41.0110, 28.9680, 41.0100, 28.9600, 41.0105, 28.9500, 41.0100, 28.9350, 41.0090, 28.9200);
        AddRoad(10, false, 41.0225, 28.9742, 41.0275, 28.9770, 41.0335, 28.9810, 41.0370, 28.9850);
        AddRoad(13, true, 41.0370, 28.9850, 41.0480, 28.9900, 41.0600, 28.9980, 41.0720, 29.0050, 41.0830, 29.0110, 41.0950, 29.0200, 41.1080, 29.0260);
        AddRoad(10, false, 41.0370, 28.9850, 41.0410, 28.9940, 41.0430, 29.0020);
        AddRoad(11, false, 41.0430, 29.0020, 41.0520, 29.0080, 41.0640, 29.0120, 41.0720, 29.0050);
        AddRoad(12, true, 41.1080, 29.0260, 41.0990, 29.0400, 41.0905, 29.0555);
        AddRoad(11, false, 41.0230, 28.9420, 41.0180, 28.9500, 41.0150, 28.9650);
        // Anadolu yakasi
        AddRoad(12, true, 41.0240, 29.0200, 41.0260, 29.0350, 41.0235, 29.0500, 41.0272, 29.0689);
        AddRoad(12, true, 40.9900, 29.0300, 40.9830, 29.0430, 40.9760, 29.0600, 40.9780, 29.0800, 40.9800, 29.0950);
        AddRoad(12, true, 40.9860, 29.0280, 40.9760, 29.0360, 40.9700, 29.0480, 40.9660, 29.0680, 40.9610, 29.0850, 40.9580, 29.0950);
        AddRoad(11, false, 41.0200, 29.0230, 41.0100, 29.0250, 41.0000, 29.0270, 40.9930, 29.0300);
        AddRoad(11, false, 41.0235, 29.0500, 41.0120, 29.0520, 41.0050, 29.0450, 40.9930, 29.0300);
    }

    // ---------------- Yol indeksi (hizli sorgu) ----------------
    static readonly List<Vector2> segA = new List<Vector2>(), segB = new List<Vector2>();
    static readonly List<float> segW = new List<float>();
    static readonly Dictionary<int, List<int>> segGrid = new Dictionary<int, List<int>>();
    const float GridCell = 60f;
    static int GKey(int cx, int cz) { return (cx + 500) * 2000 + (cz + 500); }

    public static void IndexRoads()
    {
        segA.Clear(); segB.Clear(); segW.Clear(); segGrid.Clear();
        for (int r = 0; r < Roads.Count; r++)
        {
            Road rd = Roads[r];
            for (int i = 0; i < rd.pts.Length - 1; i++)
            {
                int id = segA.Count;
                segA.Add(rd.pts[i]); segB.Add(rd.pts[i + 1]); segW.Add(rd.width);
                float pad = rd.width * 0.5f + 40f;
                float minx = Mathf.Min(rd.pts[i].x, rd.pts[i + 1].x) - pad, maxx = Mathf.Max(rd.pts[i].x, rd.pts[i + 1].x) + pad;
                float minz = Mathf.Min(rd.pts[i].y, rd.pts[i + 1].y) - pad, maxz = Mathf.Max(rd.pts[i].y, rd.pts[i + 1].y) + pad;
                for (int cx = Mathf.FloorToInt(minx / GridCell); cx <= Mathf.FloorToInt(maxx / GridCell); cx++)
                    for (int cz = Mathf.FloorToInt(minz / GridCell); cz <= Mathf.FloorToInt(maxz / GridCell); cz++)
                    {
                        List<int> l; int k = GKey(cx, cz);
                        if (!segGrid.TryGetValue(k, out l)) { l = new List<int>(); segGrid[k] = l; }
                        l.Add(id);
                    }
            }
        }
    }

    // Yolun kenarina olan mesafe (metre). Yol yoksa buyuk deger.
    public static float RoadClearance(float x, float z)
    {
        List<int> l;
        if (!segGrid.TryGetValue(GKey(Mathf.FloorToInt(x / GridCell), Mathf.FloorToInt(z / GridCell)), out l)) return 999f;
        float best = 999f; Vector2 p = new Vector2(x, z);
        for (int k = 0; k < l.Count; k++)
        {
            int i = l[k];
            Vector2 a = segA[i], b = segB[i], ab = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(0.0001f, ab.sqrMagnitude));
            float dist = (p - (a + ab * t)).magnitude - segW[i] * 0.5f;
            if (dist < best) best = dist;
        }
        return best;
    }
}
