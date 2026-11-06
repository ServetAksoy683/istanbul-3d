using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// Kodla mesh uretmek icin yardimci sinif. Tum yuzeyler vertex rengi tasir.
public class MeshBuilder
{
    public readonly List<Vector3> V = new List<Vector3>();
    public readonly List<Vector3> N = new List<Vector3>();
    public readonly List<Color32> C = new List<Color32>();
    public readonly List<Vector2> UV = new List<Vector2>();
    public readonly List<int> T = new List<int>();
    Matrix4x4 M = Matrix4x4.identity;

    public int Count { get { return V.Count; } }

    public void SetTransform(Vector3 pos, float yawDeg, float scale)
    {
        M = Matrix4x4.TRS(pos, Quaternion.Euler(0f, yawDeg, 0f), Vector3.one * scale);
    }
    public void ResetTransform() { M = Matrix4x4.identity; }

    public void TriN(Vector3 a, Vector3 b, Vector3 c, Vector3 na, Vector3 nb, Vector3 nc, Color32 col)
    {
        a = M.MultiplyPoint3x4(a); b = M.MultiplyPoint3x4(b); c = M.MultiplyPoint3x4(c);
        na = M.MultiplyVector(na).normalized; nb = M.MultiplyVector(nb).normalized; nc = M.MultiplyVector(nc).normalized;
        Vector3 g = Vector3.Cross(b - a, c - a);
        bool flip = Vector3.Dot(g, na + nb + nc) < 0f;
        int i = V.Count;
        V.Add(a); V.Add(b); V.Add(c);
        N.Add(na); N.Add(nb); N.Add(nc);
        C.Add(col); C.Add(col); C.Add(col);
        UV.Add(Vector2.zero); UV.Add(Vector2.zero); UV.Add(Vector2.zero);
        if (!flip) { T.Add(i); T.Add(i + 1); T.Add(i + 2); }
        else { T.Add(i); T.Add(i + 2); T.Add(i + 1); }
    }

    public void QuadN(Vector3 a, Vector3 b, Vector3 c, Vector3 d,
                      Vector3 na, Vector3 nb, Vector3 nc, Vector3 nd,
                      Color32 col, Vector2 ua, Vector2 ub, Vector2 uc, Vector2 ud)
    {
        a = M.MultiplyPoint3x4(a); b = M.MultiplyPoint3x4(b); c = M.MultiplyPoint3x4(c); d = M.MultiplyPoint3x4(d);
        na = M.MultiplyVector(na).normalized; nb = M.MultiplyVector(nb).normalized;
        nc = M.MultiplyVector(nc).normalized; nd = M.MultiplyVector(nd).normalized;
        Vector3 g = Vector3.Cross(b - a, c - a);
        if (g.sqrMagnitude < 1e-10f) g = Vector3.Cross(c - a, d - a);
        bool flip = Vector3.Dot(g, na + nb + nc + nd) < 0f;
        int i = V.Count;
        V.Add(a); V.Add(b); V.Add(c); V.Add(d);
        N.Add(na); N.Add(nb); N.Add(nc); N.Add(nd);
        C.Add(col); C.Add(col); C.Add(col); C.Add(col);
        UV.Add(ua); UV.Add(ub); UV.Add(uc); UV.Add(ud);
        if (!flip) { T.Add(i); T.Add(i + 1); T.Add(i + 2); T.Add(i); T.Add(i + 2); T.Add(i + 3); }
        else { T.Add(i); T.Add(i + 2); T.Add(i + 1); T.Add(i); T.Add(i + 3); T.Add(i + 2); }
    }

    // Duz yuzey: normal geometriden hesaplanir, hint yonune bakar.
    public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 hint, Color32 col,
                     Vector2 ua, Vector2 ub, Vector2 uc, Vector2 ud)
    {
        Vector3 n = Vector3.Cross(b - a, c - a);
        if (n.sqrMagnitude < 1e-10f) n = Vector3.Cross(c - a, d - a);
        n.Normalize();
        if (Vector3.Dot(n, hint) < 0f) n = -n;
        QuadN(a, b, c, d, n, n, n, n, col, ua, ub, uc, ud);
    }
    public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 hint, Color32 col)
    {
        Quad(a, b, c, d, hint, col, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
    }

    // bc = taban merkezi. tiled=true ise duvarlara pencere dokusu UV'si verilir.
    public void Box(Vector3 bc, float w, float h, float d, Color32 col, bool tiled = false, bool top = true)
    {
        float x0 = bc.x - w * 0.5f, x1 = bc.x + w * 0.5f, z0 = bc.z - d * 0.5f, z1 = bc.z + d * 0.5f;
        float y0 = bc.y, y1 = bc.y + h;
        float uw = tiled ? w / 6.4f : 0f, ud = tiled ? d / 6.4f : 0f, vh = tiled ? h / 9.6f : 0f;
        Quad(new Vector3(x1, y0, z0), new Vector3(x1, y0, z1), new Vector3(x1, y1, z1), new Vector3(x1, y1, z0), Vector3.right, col,
             new Vector2(0, 0), new Vector2(ud, 0), new Vector2(ud, vh), new Vector2(0, vh));
        Quad(new Vector3(x0, y0, z1), new Vector3(x0, y0, z0), new Vector3(x0, y1, z0), new Vector3(x0, y1, z1), Vector3.left, col,
             new Vector2(0, 0), new Vector2(ud, 0), new Vector2(ud, vh), new Vector2(0, vh));
        Quad(new Vector3(x1, y0, z1), new Vector3(x0, y0, z1), new Vector3(x0, y1, z1), new Vector3(x1, y1, z1), Vector3.forward, col,
             new Vector2(0, 0), new Vector2(uw, 0), new Vector2(uw, vh), new Vector2(0, vh));
        Quad(new Vector3(x0, y0, z0), new Vector3(x1, y0, z0), new Vector3(x1, y1, z0), new Vector3(x0, y1, z0), Vector3.back, col,
             new Vector2(0, 0), new Vector2(uw, 0), new Vector2(uw, vh), new Vector2(0, vh));
        if (top)
            Quad(new Vector3(x0, y1, z0), new Vector3(x1, y1, z0), new Vector3(x1, y1, z1), new Vector3(x0, y1, z1), Vector3.up, col);
    }

    // Kesik koni / silindir. r2 < 0 ise silindir.
    public void Cylinder(Vector3 bc, float r, float h, int seg, Color32 col, bool capTop = true, float r2 = -1f)
    {
        if (r2 < 0f) r2 = r;
        float slope = (r - r2) / h;
        Vector3 up = Vector3.up;
        for (int i = 0; i < seg; i++)
        {
            float a0 = i * Mathf.PI * 2f / seg, a1 = (i + 1) * Mathf.PI * 2f / seg;
            Vector3 c0 = new Vector3(Mathf.Cos(a0), 0, Mathf.Sin(a0));
            Vector3 c1 = new Vector3(Mathf.Cos(a1), 0, Mathf.Sin(a1));
            Vector3 p00 = bc + c0 * r, p10 = bc + c1 * r;
            Vector3 p11 = bc + c1 * r2 + up * h, p01 = bc + c0 * r2 + up * h;
            Vector3 n0 = (c0 + up * slope).normalized, n1 = (c1 + up * slope).normalized;
            QuadN(p00, p10, p11, p01, n0, n1, n1, n0, col, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
            if (capTop && r2 > 0.001f)
                TriN(bc + up * h, p01, p11, up, up, up, col);
        }
    }

    public void Cone(Vector3 bc, float r, float h, int seg, Color32 col, bool capBottom = false)
    {
        Vector3 tip = bc + Vector3.up * h;
        for (int i = 0; i < seg; i++)
        {
            float a0 = i * Mathf.PI * 2f / seg, a1 = (i + 1) * Mathf.PI * 2f / seg;
            Vector3 c0 = new Vector3(Mathf.Cos(a0), 0, Mathf.Sin(a0));
            Vector3 c1 = new Vector3(Mathf.Cos(a1), 0, Mathf.Sin(a1));
            Vector3 n0 = (c0 * h + Vector3.up * r).normalized, n1 = (c1 * h + Vector3.up * r).normalized;
            Vector3 nt = (n0 + n1).normalized;
            TriN(tip, bc + c0 * r, bc + c1 * r, nt, n0, n1, col);
            if (capBottom) TriN(bc, bc + c1 * r, bc + c0 * r, Vector3.down, Vector3.down, Vector3.down, col);
        }
    }

    public void Hemisphere(Vector3 bc, float r, int rings, int seg, Color32 col, float yScale = 1f, bool capBottom = false)
    {
        for (int k = 0; k < rings; k++)
        {
            float p0 = (float)k / rings * Mathf.PI * 0.5f, p1 = (float)(k + 1) / rings * Mathf.PI * 0.5f;
            for (int i = 0; i < seg; i++)
            {
                float t0 = i * Mathf.PI * 2f / seg, t1 = (i + 1) * Mathf.PI * 2f / seg;
                Vector3 n00 = Dir(p0, t0), n01 = Dir(p0, t1), n10 = Dir(p1, t0), n11 = Dir(p1, t1);
                Vector3 a = bc + Pt(n00, r, yScale), b = bc + Pt(n01, r, yScale);
                Vector3 c = bc + Pt(n11, r, yScale), d = bc + Pt(n10, r, yScale);
                QuadN(a, b, c, d, n00, n01, n11, n10, col, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
            }
        }
        if (capBottom)
        {
            for (int i = 0; i < seg; i++)
            {
                float t0 = i * Mathf.PI * 2f / seg, t1 = (i + 1) * Mathf.PI * 2f / seg;
                Vector3 a = bc + new Vector3(Mathf.Cos(t0), 0, Mathf.Sin(t0)) * r;
                Vector3 b = bc + new Vector3(Mathf.Cos(t1), 0, Mathf.Sin(t1)) * r;
                TriN(bc, b, a, Vector3.down, Vector3.down, Vector3.down, col);
            }
        }
    }
    static Vector3 Dir(float phi, float th) { return new Vector3(Mathf.Cos(phi) * Mathf.Cos(th), Mathf.Sin(phi), Mathf.Cos(phi) * Mathf.Sin(th)); }
    static Vector3 Pt(Vector3 n, float r, float ys) { return new Vector3(n.x * r, n.y * r * ys, n.z * r); }

    // Kesik piramit cati
    public void HipRoof(Vector3 bc, float w, float d, float h, Color32 col, float inset)
    {
        float tw = w * inset, td = d * inset;
        Vector3 b00 = bc + new Vector3(-w * .5f, 0, -d * .5f), b10 = bc + new Vector3(w * .5f, 0, -d * .5f);
        Vector3 b11 = bc + new Vector3(w * .5f, 0, d * .5f), b01 = bc + new Vector3(-w * .5f, 0, d * .5f);
        Vector3 t00 = bc + new Vector3(-tw * .5f, h, -td * .5f), t10 = bc + new Vector3(tw * .5f, h, -td * .5f);
        Vector3 t11 = bc + new Vector3(tw * .5f, h, td * .5f), t01 = bc + new Vector3(-tw * .5f, h, td * .5f);
        Quad(b00, b10, t10, t00, new Vector3(0, .6f, -1), col);
        Quad(b10, b11, t11, t10, new Vector3(1, .6f, 0), col);
        Quad(b11, b01, t01, t11, new Vector3(0, .6f, 1), col);
        Quad(b01, b00, t00, t01, new Vector3(-1, .6f, 0), col);
        Quad(t00, t10, t11, t01, Vector3.up, col);
    }

    // Iki nokta arasinda ince kiris (kablo, askı vb.)
    public void Beam(Vector3 p, Vector3 q, float thick, Color32 col)
    {
        Vector3 dir = (q - p);
        if (dir.sqrMagnitude < 1e-6f) return;
        dir.Normalize();
        Vector3 v = Vector3.Cross(Vector3.up, dir);
        if (v.sqrMagnitude < 1e-4f) v = Vector3.right;
        v.Normalize();
        Vector3 w = Vector3.Cross(dir, v).normalized;
        float t = thick * 0.5f;
        Vector3 a0 = p - v * t - w * t, a1 = p + v * t - w * t, a2 = p + v * t + w * t, a3 = p - v * t + w * t;
        Vector3 b0 = q - v * t - w * t, b1 = q + v * t - w * t, b2 = q + v * t + w * t, b3 = q - v * t + w * t;
        Quad(a0, a1, b1, b0, -w, col);
        Quad(a1, a2, b2, b1, v, col);
        Quad(a2, a3, b3, b2, w, col);
        Quad(a3, a0, b0, b3, -v, col);
    }

    // Yol / serit
    public void Strip(Vector3 p0, Vector3 p1, float width, Color32 col)
    {
        Vector3 dir = p1 - p0; dir.y = 0f;
        if (dir.sqrMagnitude < 1e-6f) return;
        Vector3 perp = new Vector3(-dir.z, 0, dir.x).normalized * (width * 0.5f);
        Quad(p0 - perp, p0 + perp, p1 + perp, p1 - perp, Vector3.up, col);
    }

    public Mesh ToMesh(string name)
    {
        Mesh m = new Mesh();
        m.name = name;
        if (V.Count > 65000) m.indexFormat = IndexFormat.UInt32;
        m.SetVertices(V);
        m.SetNormals(N);
        m.SetColors(C);
        m.SetUVs(0, UV);
        m.SetTriangles(T, 0);
        m.RecalculateBounds();
        return m;
    }
}
