using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ferry : MonoBehaviour
{
    public Channel path; public float s, speed, lateral; int dir = 1;
    void Update()
    {
        s += dir * speed * Time.deltaTime;
        if (s > path.Length - 150f) { s = path.Length - 150f; dir = -1; }
        if (s < 150f) { s = 150f; dir = 1; }
        Vector2 pos, d; float hw;
        path.At(s, out pos, out d, out hw);
        Vector2 right = new Vector2(d.y, -d.x);
        Vector2 q = pos + right * lateral * hw;
        transform.position = new Vector3(q.x, -0.2f + Mathf.Sin(Time.time * 0.9f + s) * 0.08f, q.y);
        Vector2 f = d * dir;
        transform.rotation = Quaternion.Euler(Mathf.Sin(Time.time * 0.7f + s) * 1.2f, Mathf.Atan2(f.x, f.y) * Mathf.Rad2Deg, Mathf.Sin(Time.time * 0.5f) * 1.5f);
    }
}

public class WorldBuilder : MonoBehaviour
{
    const float ChunkSize = 400f;
    class Chunk { public int cx, cz; public MeshBuilder solid = new MeshBuilder(), walls = new MeshBuilder(); }
    readonly Dictionary<int, Chunk> chunks = new Dictionary<int, Chunk>();
    readonly List<Vector4> clear = new List<Vector4>();
    readonly Dictionary<int, List<Vector2>> bGrid = new Dictionary<int, List<Vector2>>();
    System.Random rng = new System.Random(1453);
    Material solidMat, facadeMat, waterMat;
    Transform root;
    public Vector3 SpawnPoint;
    public System.Action<string, float> Progress;
    public bool Done;

    float R(float a, float b) { return a + (float)rng.NextDouble() * (b - a); }
    float R01() { return (float)rng.NextDouble(); }
    int RI(int a, int b) { return rng.Next(a, b + 1); }
    static Color32 K(float r, float g, float b) { return Landmarks.K(r, g, b); }
    static float Sm(float a, float b, float x) { float t = Mathf.Clamp01((x - a) / (b - a)); return t * t * (3f - 2f * t); }
    void Report(string m, float p) { if (Progress != null) Progress(m, p); }

    Chunk GetChunk(float x, float z)
    {
        int cx = Mathf.FloorToInt(x / ChunkSize), cz = Mathf.FloorToInt(z / ChunkSize);
        int k = (cx + 100) * 1000 + (cz + 100);
        Chunk c;
        if (!chunks.TryGetValue(k, out c)) { c = new Chunk(); c.cx = cx; c.cz = cz; chunks[k] = c; }
        return c;
    }

    bool IsCleared(float x, float z, float pad)
    {
        for (int i = 0; i < clear.Count; i++)
        {
            float dx = x - clear[i].x, dz = z - clear[i].y;
            float rr = clear[i].z + pad;
            if (dx * dx + dz * dz < rr * rr) return true;
        }
        return false;
    }
    void AddClear(Vector2 p, float r) { clear.Add(new Vector4(p.x, p.y, r, 0)); }

    // ------------------------------------------------------------------
    public IEnumerator Build()
    {
        LoadMaterials();
        root = new GameObject("IstanbulWorld").transform;
        IstanbulGeo.BuildRoadList();
        Report("Zemin ve Boğaz hazırlanıyor", 0.02f);
        yield return null;
        yield return BuildTerrain();
        BuildWater();
        yield return null;

        Report("Anıtlar dikiliyor", 0.45f);
        PlanLandmarks();
        BuildBridges();
        IstanbulGeo.IndexRoads();
        yield return null;

        Report("Yollar çiziliyor", 0.52f);
        BuildRoads();
        yield return null;

        yield return BuildBuildings();
        yield return BuildTrees();
        yield return FlushChunks();
        SpawnFerries();
        Report("Hazır", 1f);
        Done = true;
    }

    void LoadMaterials()
    {
        solidMat = Resources.Load<Material>("Materials/Solid");
        Material f = Resources.Load<Material>("Materials/Facade");
        waterMat = Resources.Load<Material>("Materials/Water");
        if (solidMat == null) solidMat = new Material(Shader.Find("Istanbul/VertexColorTex"));
        if (f == null) f = new Material(Shader.Find("Istanbul/VertexColorTex"));
        facadeMat = new Material(f);
        facadeMat.mainTexture = MakeFacadeTexture();
        if (waterMat == null) waterMat = new Material(Shader.Find("Istanbul/Water"));
    }

    Texture2D MakeFacadeTexture()
    {
        int S = 128;
        Texture2D t = new Texture2D(S, S, TextureFormat.RGBA32, true);
        t.wrapMode = TextureWrapMode.Repeat; t.filterMode = FilterMode.Trilinear; t.anisoLevel = 4;
        Color32[] px = new Color32[S * S];
        for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                float u = (x / (float)S * 2f) % 1f, v = (y / (float)S * 3f) % 1f;
                bool win = u > 0.2f && u < 0.8f && v > 0.28f && v < 0.78f;
                bool frame = u > 0.16f && u < 0.84f && v > 0.24f && v < 0.82f;
                Color32 c;
                if (win) { float g = (v - 0.28f) * 2f; c = new Color32((byte)(38 + g * 40), (byte)(52 + g * 45), (byte)(72 + g * 50), 255); }
                else if (frame) c = new Color32(205, 202, 196, 255);
                else c = new Color32(244, 242, 238, 255);
                px[y * S + x] = c;
            }
        t.SetPixels32(px); t.Apply(true);
        return t;
    }

    // ------------------------- ARAZİ -------------------------
    IEnumerator BuildTerrain()
    {
        int cx0 = Mathf.FloorToInt(IstanbulGeo.Min.x / ChunkSize), cx1 = Mathf.CeilToInt(IstanbulGeo.Max.x / ChunkSize);
        int cz0 = Mathf.FloorToInt(IstanbulGeo.Min.y / ChunkSize), cz1 = Mathf.CeilToInt(IstanbulGeo.Max.y / ChunkSize);
        int total = (cx1 - cx0) * (cz1 - cz0), n = 0;
        for (int cz = cz0; cz < cz1; cz++)
            for (int cx = cx0; cx < cx1; cx++)
            {
                TerrainChunk(cx, cz);
                n++;
                Report("Arazi oluşturuluyor", 0.05f + 0.38f * n / total);
                yield return null;
            }
    }

    void TerrainChunk(int cx, int cz)
    {
        const int N = 40;
        float cell = ChunkSize / N, x0 = cx * ChunkSize, z0 = cz * ChunkSize;
        int W = N + 3;
        float[] hs = new float[W * W], ds = new float[W * W];
        float maxD = -999f;
        for (int j = 0; j < W; j++)
            for (int i = 0; i < W; i++)
            {
                float d;
                hs[j * W + i] = IstanbulGeo.Height(x0 + (i - 1) * cell, z0 + (j - 1) * cell, out d);
                ds[j * W + i] = d;
                if (d > maxD) maxD = d;
            }
        Vector3[] verts = new Vector3[(N + 1) * (N + 1)];
        Vector3[] norms = new Vector3[verts.Length];
        Color32[] cols = new Color32[verts.Length];
        for (int j = 0; j <= N; j++)
            for (int i = 0; i <= N; i++)
            {
                int gi = (j + 1) * W + (i + 1);
                float x = x0 + i * cell, z = z0 + j * cell, h = hs[gi];
                Vector3 nrm = new Vector3(hs[gi - 1] - hs[gi + 1], 2f * cell, hs[gi - W] - hs[gi + W]).normalized;
                int vi = j * (N + 1) + i;
                verts[vi] = new Vector3(x, h, z); norms[vi] = nrm;
                cols[vi] = TerrainColor(x, z, ds[gi], nrm.y);
            }
        int[] tris = new int[N * N * 6]; int t = 0;
        for (int j = 0; j < N; j++)
            for (int i = 0; i < N; i++)
            {
                int i00 = j * (N + 1) + i, i01 = (j + 1) * (N + 1) + i, i10 = j * (N + 1) + i + 1, i11 = (j + 1) * (N + 1) + i + 1;
                tris[t++] = i00; tris[t++] = i01; tris[t++] = i11;
                tris[t++] = i00; tris[t++] = i11; tris[t++] = i10;
            }
        Mesh m = new Mesh(); m.name = "Terrain_" + cx + "_" + cz;
        m.vertices = verts; m.normals = norms; m.colors32 = cols; m.triangles = tris;
        m.uv = new Vector2[verts.Length];
        m.RecalculateBounds();
        GameObject go = new GameObject(m.name);
        go.transform.SetParent(root, false);
        go.AddComponent<MeshFilter>().sharedMesh = m;
        go.AddComponent<MeshRenderer>().sharedMaterial = solidMat;
        if (maxD > -20f) go.AddComponent<MeshCollider>().sharedMesh = m;
    }

    Color32 TerrainColor(float x, float z, float d, float ny)
    {
        if (d < -1f)
        {
            float k = Sm(-1f, -45f, d);
            return Color32.Lerp(K(.30f, .45f, .42f), K(.04f, .12f, .20f), k);
        }
        District dom; float u = IstanbulGeo.Urban(x, z, out dom);
        float park = IstanbulGeo.ParkWeight(x, z);
        float nz = Mathf.PerlinNoise(x * 0.02f + 9f, z * 0.02f + 4f);
        Color grass = Color.Lerp(new Color(.36f, .50f, .25f), new Color(.24f, .40f, .19f), nz);
        if (park > 0.6f) grass = Color.Lerp(grass, new Color(.17f, .32f, .15f), 0.6f);
        Color urban = Color.Lerp(new Color(.44f, .44f, .45f), new Color(.52f, .51f, .50f), nz);
        float uw = Mathf.Clamp01(u * 1.8f) * (1f - park);
        Color c = Color.Lerp(grass, urban, uw);
        if (ny < 0.80f) c = Color.Lerp(c, new Color(.50f, .46f, .40f), Mathf.Clamp01((0.80f - ny) * 5f));
        if (d < 16f)
        {
            Color quay = Color.Lerp(new Color(.80f, .74f, .57f), new Color(.58f, .58f, .58f), Mathf.Clamp01(u * 1.5f));
            c = Color.Lerp(quay, c, Sm(2f, 16f, d));
        }
        return c;
    }

    void BuildWater()
    {
        Vector2 mn = IstanbulGeo.Min - new Vector2(200, 200), mx = IstanbulGeo.Max + new Vector2(200, 200);
        float cell = 40f;
        int nx = Mathf.CeilToInt((mx.x - mn.x) / cell), nz = Mathf.CeilToInt((mx.y - mn.y) / cell);
        Vector3[] v = new Vector3[(nx + 1) * (nz + 1)];
        Vector3[] n = new Vector3[v.Length];
        for (int j = 0; j <= nz; j++)
            for (int i = 0; i <= nx; i++) { v[j * (nx + 1) + i] = new Vector3(mn.x + i * cell, 0f, mn.y + j * cell); n[j * (nx + 1) + i] = Vector3.up; }
        int[] t = new int[nx * nz * 6]; int k = 0;
        for (int j = 0; j < nz; j++)
            for (int i = 0; i < nx; i++)
            {
                int a = j * (nx + 1) + i, b = (j + 1) * (nx + 1) + i, c = (j + 1) * (nx + 1) + i + 1, d = j * (nx + 1) + i + 1;
                t[k++] = a; t[k++] = b; t[k++] = c; t[k++] = a; t[k++] = c; t[k++] = d;
            }
        Mesh m = new Mesh(); m.name = "Water";
        if (v.Length > 65000) m.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        m.vertices = v; m.normals = n; m.triangles = t; m.RecalculateBounds();
        GameObject go = new GameObject("Water");
        go.transform.SetParent(root, false);
        go.AddComponent<MeshFilter>().sharedMesh = m;
        MeshRenderer mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterial = waterMat;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;
    }

    // ------------------------- ANITLAR -------------------------
    Vector2 Spot(double lat, double lon, float minD) { return IstanbulGeo.SnapToLand(IstanbulGeo.LL(lat, lon), minD); }
    Vector3 Ground(Vector2 p) { return new Vector3(p.x, Mathf.Max(0.5f, IstanbulGeo.Height(p.x, p.y)), p.y); }
    float YawAlong(Channel ch, Vector2 p) { Vector2 d = ch.NearestDir(p); return Mathf.Atan2(d.x, d.y) * Mathf.Rad2Deg; }

    void PlanLandmarks()
    {
        Vector2 p; MeshBuilder s; MeshBuilder fw; float r;

        p = Spot(41.0086, 28.9802, 60f); s = GetChunk(p.x, p.y).solid;
        r = Landmarks.Ayasofya(s, Ground(p), 25f); AddClear(p, Mathf.Max(60f, r));
        Vector2 aya = p;

        p = Spot(41.0054, 28.9768, 60f); s = GetChunk(p.x, p.y).solid;
        r = Landmarks.Mosque(s, Ground(p), 25f, 44f, 22f, 14f, 6, 52f, Landmarks.K(.80f, .77f, .70f), Landmarks.K(.42f, .52f, .60f), true);
        AddClear(p, r * 0.8f + 20f);
        SpawnPoint = new Vector3((aya.x + p.x) * 0.5f, 0f, (aya.y + p.y) * 0.5f);
        Vector2 sp = IstanbulGeo.SnapToLand(new Vector2(SpawnPoint.x, SpawnPoint.z), 25f);
        SpawnPoint = new Vector3(sp.x, 0f, sp.y);
        AddClear(sp, 22f);

        p = Spot(41.0165, 28.9645, 70f); s = GetChunk(p.x, p.y).solid;
        r = Landmarks.Mosque(s, Ground(p), 40f, 54f, 24f, 15f, 4, 54f, Landmarks.K(.82f, .78f, .70f), Landmarks.K(.50f, .55f, .60f), true);
        AddClear(p, r * 0.8f + 20f);

        p = Spot(41.0166, 28.9709, 32f); s = GetChunk(p.x, p.y).solid;
        r = Landmarks.Mosque(s, Ground(p), 0f, 40f, 18f, 11f, 2, 46f, Landmarks.K(.82f, .78f, .70f), Landmarks.K(.48f, .53f, .58f), false);
        AddClear(p, 40f);

        p = Spot(41.0256, 28.9744, 45f); s = GetChunk(p.x, p.y).solid;
        Landmarks.GalataTower(s, Ground(p)); AddClear(p, 40f);

        p = IstanbulGeo.KizKulesi; s = GetChunk(p.x, p.y).solid;
        Landmarks.KizKulesi(s, new Vector3(p.x, 1.5f, p.y));

        p = Spot(41.0392, 29.0005, 40f);
        Chunk ch = GetChunk(p.x, p.y);
        r = Landmarks.Dolmabahce(ch.solid, ch.walls, Ground(p), YawAlong(IstanbulGeo.Bos, p)); AddClear(p, r);

        p = Spot(41.0475, 29.0268, 35f); s = GetChunk(p.x, p.y).solid;
        Landmarks.Mosque(s, Ground(p), YawAlong(IstanbulGeo.Bos, p), 22f, 14f, 7f, 2, 34f, Landmarks.K(.95f, .93f, .88f), Landmarks.K(.55f, .58f, .62f), false);
        AddClear(p, 35f);

        p = Spot(41.0846, 29.0561, 40f); s = GetChunk(p.x, p.y).solid;
        r = Landmarks.RumeliHisari(s, Ground(p), YawAlong(IstanbulGeo.Bos, p)); AddClear(p, r);

        p = Spot(41.0013, 29.0197, 55f); ch = GetChunk(p.x, p.y);
        r = Landmarks.Haydarpasa(ch.solid, ch.walls, Ground(p), 260f); AddClear(p, r);

        p = Spot(41.0272, 29.0689, 20f); s = GetChunk(p.x, p.y).solid;
        Landmarks.CamlicaTower(s, Ground(p)); AddClear(p, 40f);

        p = Spot(41.0125, 28.9845, 50f); s = GetChunk(p.x, p.y).solid;
        r = Landmarks.Topkapi(s, Ground(p), 20f); AddClear(p, r * 0.9f);

        // Levent gokdelenleri
        p = Spot(41.0838, 29.0113, 40f); ch = GetChunk(p.x, p.y);
        Landmarks.Skyscraper(ch.solid, ch.walls, Ground(p), 34f, 150f, Landmarks.K(.30f, .46f, .62f)); AddClear(p, 40f);
        p = Spot(41.0825, 29.0085, 40f); ch = GetChunk(p.x, p.y);
        Landmarks.Skyscraper(ch.solid, ch.walls, Ground(p), 30f, 110f, Landmarks.K(.50f, .58f, .64f)); AddClear(p, 38f);
    }

    // ------------------------- KOPRULER -------------------------
    void BuildBridges()
    {
        MakeBridge(41.0198, 28.9738, IstanbulGeo.Gh, 4.8f, 0f, false, 22f, 40f, null, null);
        MakeBridge(41.0415, 28.9440, IstanbulGeo.Gh, 15f, 38f, true, 16f, 120f, null, null);
        MakeBridge(41.0451, 29.0332, IstanbulGeo.Bos, 40f, 72f, true, 24f, 240f, new double[] { 41.0640, 29.0120 }, new double[] { 41.0235, 29.0500 });
        MakeBridge(41.0915, 29.0625, IstanbulGeo.Bos, 42f, 74f, true, 26f, 240f, new double[] { 41.0990, 29.0400 }, new double[] { 41.0880, 29.0880 });
    }

    void MakeBridge(double lat, double lon, Channel ch, float deckY, float towerH, bool suspension, float width, float ext, double[] hubA, double[] hubB)
    {
        Vector2 c = IstanbulGeo.LL(lat, lon);
        Vector2 cd = ch.NearestDir(c);
        Vector2 perp = new Vector2(-cd.y, cd.x);
        Vector2 a = c, b = c;
        for (float t = 0f; t < 1200f; t += 5f) { a = c + perp * t; if (IstanbulGeo.SignedDist(a.x, a.y) > 10f) break; }
        for (float t = 0f; t < 1200f; t += 5f) { b = c - perp * t; if (IstanbulGeo.SignedDist(b.x, b.y) > 10f) break; }
        Vector2 dir = (b - a).normalized;
        Vector2 p0 = a - dir * ext, p1 = b + dir * ext;
        float total = (p1 - p0).magnitude;
        int n = Mathf.CeilToInt(total / 10f);
        float y0 = IstanbulGeo.Height(p0.x, p0.y) + 0.4f, y1 = IstanbulGeo.Height(p1.x, p1.y) + 0.4f;
        Color32 asphalt = K(.19f, .19f, .21f), concrete = K(.62f, .62f, .60f), dark = K(.40f, .40f, .42f), line = K(.95f, .9f, .5f);
        Vector3 prev = Vector3.zero; bool havePrev = false;
        Vector2[] deckPts = new Vector2[n + 1];
        for (int k = 0; k <= n; k++)
        {
            float s = k * total / n;
            Vector2 pos = p0 + dir * s;
            float y;
            if (s < ext) y = Mathf.Lerp(y0, deckY, Sm(0f, ext, s));
            else if (s > total - ext) y = Mathf.Lerp(y1, deckY, Sm(0f, ext, total - s));
            else y = deckY;
            Vector3 cur = new Vector3(pos.x, y, pos.y);
            deckPts[k] = pos;
            if (havePrev)
            {
                MeshBuilder sb = GetChunk((cur.x + prev.x) * 0.5f, (cur.z + prev.z) * 0.5f).solid;
                DeckSegment(sb, prev, cur, width, asphalt, concrete, dark);
                if (k % 2 == 0) sb.Strip(prev + Vector3.up * 0.06f, Vector3.Lerp(prev, cur, 0.6f) + Vector3.up * 0.06f, 0.4f, line);
                float ground = IstanbulGeo.Height(cur.x, cur.z);
                if (k % 4 == 0 && cur.y - ground > 4f && (!suspension || ground > -0.5f))
                    sb.Box(new Vector3(cur.x, ground - 1f, cur.z), 3.2f, cur.y - 2.3f - ground + 1f, 3.2f, concrete);
            }
            prev = cur; havePrev = true;
        }
        Road blocker = new Road(); blocker.pts = deckPts; blocker.width = width + 6f; blocker.draw = false; IstanbulGeo.Roads.Add(blocker);
        if (hubA != null)
        {
            Road ra = new Road(); ra.pts = new Vector2[] { p0, IstanbulGeo.LL(hubA[0], hubA[1]) }; ra.width = 14f; ra.main = true; IstanbulGeo.Roads.Add(ra);
        }
        if (hubB != null)
        {
            Road rb = new Road(); rb.pts = new Vector2[] { p1, IstanbulGeo.LL(hubB[0], hubB[1]) }; rb.width = 14f; rb.main = true; IstanbulGeo.Roads.Add(rb);
        }
        if (towerH > 0f)
        {
            MeshBuilder tb = GetChunk(c.x, c.y).solid;
            Vector3 lat3 = new Vector3(perp.x, 0, perp.y);
            Vector3 d3 = new Vector3(dir.x, 0, dir.y);
            Vector2[] ends = { a, b };
            Vector3[] tops = new Vector3[2];
            for (int e = 0; e < 2; e++)
            {
                Vector2 tp = ends[e] + (e == 0 ? -dir : dir) * 18f;
                float g = IstanbulGeo.Height(tp.x, tp.y);
                for (int sd = -1; sd <= 1; sd += 2)
                {
                    Vector3 leg = new Vector3(tp.x, g - 2f, tp.y) + lat3 * sd * (width * 0.5f + 3f);
                    tb.SetTransform(leg, Mathf.Atan2(d3.x, d3.z) * Mathf.Rad2Deg, 1f);
                    tb.Box(Vector3.zero, 4.5f, deckY + towerH - (g - 2f), 6f, concrete);
                    tb.ResetTransform();
                }
                tops[e] = new Vector3(tp.x, deckY + towerH, tp.y);
                Vector3 bm = new Vector3(tp.x, deckY + towerH * 0.55f, tp.y);
                tb.Beam(bm + lat3 * -(width * 0.5f + 3f), bm + lat3 * (width * 0.5f + 3f), 3f, concrete);
                Vector3 bt = new Vector3(tp.x, deckY + towerH - 3f, tp.y);
                tb.Beam(bt + lat3 * -(width * 0.5f + 3f), bt + lat3 * (width * 0.5f + 3f), 3f, concrete);
            }
            // ana kablolar ve askilar
            for (int sd = -1; sd <= 1; sd += 2)
            {
                Vector3 off = lat3 * sd * (width * 0.5f + 3f);
                Vector3 pa = tops[0] + off, pb = tops[1] + off;
                int segs = 24; Vector3 pp = pa;
                for (int k = 1; k <= segs; k++)
                {
                    float t = (float)k / segs;
                    Vector3 q = Vector3.Lerp(pa, pb, t);
                    q.y -= (pa.y - (deckY + 4f)) * 4f * t * (1f - t);
                    MeshBuilder cb = GetChunk(q.x, q.z).solid;
                    cb.Beam(pp, q, 1.1f, dark);
                    if (k % 2 == 0 && k < segs) cb.Beam(q, new Vector3(q.x, deckY + 0.8f, q.z), 0.3f, dark);
                    pp = q;
                }
            }
        }
    }

    void DeckSegment(MeshBuilder s, Vector3 a, Vector3 b, float w, Color32 top, Color32 side, Color32 rail)
    {
        Vector3 dir = b - a; dir.y = 0f;
        Vector3 perp = new Vector3(-dir.z, 0, dir.x).normalized * (w * 0.5f);
        float th = 2.2f;
        Vector3 aL = a - perp, aR = a + perp, bL = b - perp, bR = b + perp;
        Vector3 dn = Vector3.down * th;
        s.Quad(aL, aR, bR, bL, Vector3.up, top);
        s.Quad(aL + dn, aR + dn, bR + dn, bL + dn, Vector3.down, side);
        s.Quad(aL, bL, bL + dn, aL + dn, -perp, side);
        s.Quad(aR, bR, bR + dn, aR + dn, perp, side);
        Vector3 up1 = Vector3.up * 1.0f;
        s.Beam(aL + up1, bL + up1, 0.25f, rail);
        s.Beam(aR + up1, bR + up1, 0.25f, rail);
    }

    // ------------------------- YOLLAR -------------------------
    void BuildRoads()
    {
        Color32 asphalt = K(.20f, .20f, .22f), walk = K(.62f, .60f, .58f), mark = K(.95f, .90f, .55f);
        for (int r = 0; r < IstanbulGeo.Roads.Count; r++)
        {
            Road rd = IstanbulGeo.Roads[r];
            if (!rd.draw) continue;
            List<Vector2> pts = new List<Vector2>();
            for (int i = 0; i < rd.pts.Length - 1; i++)
            {
                float len = Vector2.Distance(rd.pts[i], rd.pts[i + 1]);
                int steps = Mathf.Max(1, Mathf.CeilToInt(len / 8f));
                for (int k = 0; k < steps; k++) pts.Add(Vector2.Lerp(rd.pts[i], rd.pts[i + 1], (float)k / steps));
            }
            pts.Add(rd.pts[rd.pts.Length - 1]);
            float[] ds = new float[pts.Count]; float[] hs = new float[pts.Count];
            for (int i = 0; i < pts.Count; i++) hs[i] = IstanbulGeo.Height(pts[i].x, pts[i].y, out ds[i]);
            for (int i = 0; i < pts.Count - 1; i++)
            {
                if (ds[i] < 6f || ds[i + 1] < 6f) continue;
                Vector3 p0 = new Vector3(pts[i].x, hs[i] + 0.3f, pts[i].y), p1 = new Vector3(pts[i + 1].x, hs[i + 1] + 0.3f, pts[i + 1].y);
                MeshBuilder s = GetChunk((p0.x + p1.x) * 0.5f, (p0.z + p1.z) * 0.5f).solid;
                s.Strip(p0, p1, rd.width + 4f, walk);
                s.Strip(p0 + Vector3.up * 0.06f, p1 + Vector3.up * 0.06f, rd.width, asphalt);
                if (rd.main && i % 2 == 0)
                    s.Strip(p0 + Vector3.up * 0.12f, Vector3.Lerp(p0, p1, 0.6f) + Vector3.up * 0.12f, 0.35f, mark);
            }
        }
    }

    // ------------------------- BINALAR -------------------------
    static readonly Color32[] OldWalls = { Landmarks.K(.86f, .78f, .62f), Landmarks.K(.82f, .68f, .52f), Landmarks.K(.72f, .55f, .42f), Landmarks.K(.88f, .84f, .76f), Landmarks.K(.70f, .60f, .50f), Landmarks.K(.80f, .62f, .50f), Landmarks.K(.90f, .80f, .68f) };
    static readonly Color32[] ModWalls = { Landmarks.K(.80f, .80f, .78f), Landmarks.K(.72f, .74f, .76f), Landmarks.K(.85f, .82f, .76f), Landmarks.K(.70f, .68f, .64f), Landmarks.K(.78f, .74f, .70f), Landmarks.K(.88f, .88f, .86f) };
    static readonly Color32[] GlassWalls = { Landmarks.K(.36f, .52f, .64f), Landmarks.K(.44f, .58f, .68f), Landmarks.K(.30f, .42f, .52f), Landmarks.K(.55f, .62f, .68f) };
    static readonly Color32[] Roofs = { Landmarks.K(.62f, .30f, .22f), Landmarks.K(.55f, .30f, .25f), Landmarks.K(.50f, .28f, .24f), Landmarks.K(.68f, .36f, .26f) };

    Color32 Jit(Color32 c, float amt)
    {
        float k = 1f + R(-amt, amt);
        return new Color32((byte)Mathf.Clamp(c.r * k, 0, 255), (byte)Mathf.Clamp(c.g * k, 0, 255), (byte)Mathf.Clamp(c.b * k, 0, 255), 255);
    }
    Color32 Pick(Color32[] a) { return a[rng.Next(a.Length)]; }
    int BKey(float x, float z) { return (Mathf.FloorToInt(x / 32f) + 500) * 2000 + (Mathf.FloorToInt(z / 32f) + 500); }

    IEnumerator BuildBuildings()
    {
        const float pitch = 30f;
        int rows = 0;
        float zspan = IstanbulGeo.Max.y - IstanbulGeo.Min.y;
        for (float gz = IstanbulGeo.Min.y; gz < IstanbulGeo.Max.y; gz += pitch)
        {
            for (float gx = IstanbulGeo.Min.x; gx < IstanbulGeo.Max.x; gx += pitch)
            {
                float x = gx + R(-3f, 3f), z = gz + R(-3f, 3f);
                District dom; float u = IstanbulGeo.Urban(x, z, out dom);
                if (dom == null || u < 0.10f) continue;
                if (R01() > u * 1.05f) continue;
                float d; float h = IstanbulGeo.Height(x, z, out d);
                if (d < 20f) continue;
                if (IsCleared(x, z, 24f)) continue;
                if (IstanbulGeo.RoadClearance(x, z) < 13f) continue;
                float hx = IstanbulGeo.Height(x + 14f, z) - IstanbulGeo.Height(x - 14f, z);
                float hz = IstanbulGeo.Height(x, z + 14f) - IstanbulGeo.Height(x, z - 14f);
                if (Mathf.Abs(hx) > 13f || Mathf.Abs(hz) > 13f) continue;
                PlaceBuilding(x, z, h, dom);
            }
            rows++;
            if (rows % 6 == 0) { Report("Binalar dikiliyor", 0.55f + 0.30f * (gz - IstanbulGeo.Min.y) / zspan); yield return null; }
        }
    }

    void PlaceBuilding(float x, float z, float h, District dom)
    {
        float bw = R(14f, 22f), bd = R(14f, 22f);
        float yaw = R(-10f, 10f) + dom.rot * 0.3f;
        int floors = Mathf.RoundToInt(Mathf.Lerp(dom.minF, dom.maxF, Mathf.Pow(R01(), 1.5f)));
        bool tower = false;
        if (dom.towers && R01() < 0.22f) { floors = RI(16, 36); bw = R(22f, 30f); bd = R(22f, 30f); tower = true; }
        float hgt = floors * 3.1f;
        float minH = h;
        minH = Mathf.Min(minH, IstanbulGeo.Height(x - bw * .5f, z - bd * .5f));
        minH = Mathf.Min(minH, IstanbulGeo.Height(x + bw * .5f, z - bd * .5f));
        minH = Mathf.Min(minH, IstanbulGeo.Height(x - bw * .5f, z + bd * .5f));
        minH = Mathf.Min(minH, IstanbulGeo.Height(x + bw * .5f, z + bd * .5f));
        Chunk ch = GetChunk(x, z);
        Color32 wall;
        if (tower) wall = Jit(Pick(GlassWalls), 0.06f);
        else if (dom.style == 1 || dom.style == 4) wall = Jit(Pick(OldWalls), 0.07f);
        else wall = Jit(Pick(ModWalls), 0.07f);
        ch.walls.SetTransform(new Vector3(x, minH - 3f, z), yaw, 1f);
        ch.walls.Box(Vector3.zero, bw, hgt + 2.8f, bd, wall, true, false);
        ch.walls.ResetTransform();
        float topY = minH - 0.2f + hgt;
        ch.solid.SetTransform(new Vector3(x, topY, z), yaw, 1f);
        if ((dom.style == 1 || dom.style == 4) && !tower)
            ch.solid.HipRoof(Vector3.zero, bw + 1.2f, bd + 1.2f, R(2.5f, 4.5f), Jit(Pick(Roofs), 0.06f), 0.3f);
        else
        {
            ch.solid.Box(Vector3.zero, bw + 0.4f, 0.5f, bd + 0.4f, K(.46f, .47f, .48f));
            if (R01() < 0.5f) ch.solid.Box(new Vector3(R(-3f, 3f), 0.5f, R(-3f, 3f)), R(2f, 4f), R(1.5f, 3f), R(2f, 4f), K(.55f, .55f, .57f));
        }
        ch.solid.ResetTransform();
        List<Vector2> l; int key = BKey(x, z);
        if (!bGrid.TryGetValue(key, out l)) { l = new List<Vector2>(); bGrid[key] = l; }
        l.Add(new Vector2(x, z));
    }

    bool NearBuilding(float x, float z, float r)
    {
        int cx = Mathf.FloorToInt(x / 32f), cz = Mathf.FloorToInt(z / 32f);
        for (int i = -1; i <= 1; i++)
            for (int j = -1; j <= 1; j++)
            {
                List<Vector2> l;
                if (!bGrid.TryGetValue((cx + i + 500) * 2000 + (cz + j + 500), out l)) continue;
                for (int k = 0; k < l.Count; k++)
                {
                    float dx = l[k].x - x, dz = l[k].y - z;
                    if (dx * dx + dz * dz < r * r) return true;
                }
            }
        return false;
    }

    // ------------------------- AGACLAR -------------------------
    IEnumerator BuildTrees()
    {
        const float pitch = 24f;
        int rows = 0;
        float zspan = IstanbulGeo.Max.y - IstanbulGeo.Min.y;
        for (float gz = IstanbulGeo.Min.y; gz < IstanbulGeo.Max.y; gz += pitch)
        {
            for (float gx = IstanbulGeo.Min.x; gx < IstanbulGeo.Max.x; gx += pitch)
            {
                float x = gx + R(-9f, 9f), z = gz + R(-9f, 9f);
                float d; float h = IstanbulGeo.Height(x, z, out d);
                if (d < 14f) continue;
                District dom; float u = IstanbulGeo.Urban(x, z, out dom);
                float park = IstanbulGeo.ParkWeight(x, z);
                float p = Mathf.Max(park * 0.95f, (1f - u) * 0.25f);
                if (R01() > p) continue;
                if (IsCleared(x, z, 8f)) continue;
                if (IstanbulGeo.RoadClearance(x, z) < 8f) continue;
                if (NearBuilding(x, z, 17f)) continue;
                Tree(GetChunk(x, z).solid, x, h, z);
            }
            rows++;
            if (rows % 8 == 0) { Report("Ağaçlar dikiliyor", 0.85f + 0.08f * (gz - IstanbulGeo.Min.y) / zspan); yield return null; }
        }
    }

    void Tree(MeshBuilder s, float x, float y, float z)
    {
        float sc = R(0.8f, 1.5f);
        s.SetTransform(new Vector3(x, y - 0.3f, z), R(0f, 360f), sc);
        s.Box(Vector3.zero, 0.7f, 2.8f, 0.7f, K(.35f, .25f, .16f), false, false);
        Color32 g = Jit(R01() < 0.5f ? K(.12f, .30f, .16f) : K(.22f, .40f, .18f), 0.12f);
        if (R01() < 0.5f)
        {
            s.Cone(new Vector3(0, 1.6f, 0), 2.6f, 4.0f, 6, g);
            s.Cone(new Vector3(0, 3.8f, 0), 1.9f, 3.4f, 6, g);
        }
        else s.Hemisphere(new Vector3(0, 2.2f, 0), 2.9f, 2, 7, g, 1.1f, true);
        s.ResetTransform();
    }

    // ------------------------- MESH'LERI OLUSTUR -------------------------
    IEnumerator FlushChunks()
    {
        Report("Şehir birleştiriliyor", 0.94f);
        int n = 0;
        foreach (KeyValuePair<int, Chunk> kv in chunks)
        {
            Chunk c = kv.Value;
            if (c.solid.Count > 0) MakeObj("City_solid_" + c.cx + "_" + c.cz, c.solid.ToMesh("solid"), solidMat);
            if (c.walls.Count > 0) MakeObj("City_walls_" + c.cx + "_" + c.cz, c.walls.ToMesh("walls"), facadeMat);
            if (++n % 3 == 0) yield return null;
        }
    }

    void MakeObj(string name, Mesh m, Material mat)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(root, false);
        go.AddComponent<MeshFilter>().sharedMesh = m;
        go.AddComponent<MeshRenderer>().sharedMaterial = mat;
        m.UploadMeshData(true);
    }

    // ------------------------- VAPURLAR -------------------------
    void SpawnFerries()
    {
        MeshBuilder s = new MeshBuilder(), fw = new MeshBuilder();
        s.Box(new Vector3(0, -1.5f, 0), 8.5f, 1.8f, 30f, K(.12f, .12f, .14f));
        s.Box(new Vector3(0, 0.3f, 0), 9f, 3.2f, 31f, K(.93f, .93f, .92f));
        s.Box(new Vector3(0, 0.3f, 15.5f), 4f, 3.2f, 4f, K(.93f, .93f, .92f));
        s.Box(new Vector3(0, 3.5f, -1f), 8.4f, 0.4f, 24f, K(.7f, .7f, .7f));
        fw.Box(new Vector3(0, 3.9f, -1f), 7.4f, 2.8f, 22f, K(.95f, .95f, .94f), true, true);
        s.Box(new Vector3(0, 6.7f, 3f), 5f, 0.3f, 10f, K(.7f, .7f, .7f));
        s.Cylinder(new Vector3(0, 6.7f, -4f), 1.5f, 4f, 10, K(.88f, .72f, .15f));
        s.Cylinder(new Vector3(0, 10.7f, -4f), 1.55f, 0.9f, 10, K(.1f, .1f, .1f));
        Mesh ms = s.ToMesh("ferry_solid"), mw = fw.ToMesh("ferry_walls");
        float[] lat = { -0.6f, -0.35f, 0.7f, -0.55f, 0.65f, -0.4f };
        for (int i = 0; i < lat.Length; i++)
        {
            GameObject f = new GameObject("Vapur_" + i);
            f.transform.SetParent(root, false);
            GameObject a = new GameObject("g"); a.transform.SetParent(f.transform, false);
            a.AddComponent<MeshFilter>().sharedMesh = ms; a.AddComponent<MeshRenderer>().sharedMaterial = solidMat;
            GameObject b = new GameObject("w"); b.transform.SetParent(f.transform, false);
            b.AddComponent<MeshFilter>().sharedMesh = mw; b.AddComponent<MeshRenderer>().sharedMaterial = facadeMat;
            Ferry fe = f.AddComponent<Ferry>();
            fe.path = IstanbulGeo.Bos; fe.s = R(300f, IstanbulGeo.Bos.Length - 300f); fe.speed = R(6f, 9f); fe.lateral = lat[i];
        }
    }
}
