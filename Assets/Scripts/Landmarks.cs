using System.Collections.Generic;
using UnityEngine;

// Istanbul'un simge yapilari (primitiflerden). Hepsi yerel koordinatla cizilir, SetTransform ile yerlestirilir.
public static class Landmarks
{
    public static Color32 K(float r, float g, float b)
    {
        return new Color32((byte)(Mathf.Clamp01(r) * 255f), (byte)(Mathf.Clamp01(g) * 255f), (byte)(Mathf.Clamp01(b) * 255f), 255);
    }
    static readonly Color32 Stone = K(.80f, .76f, .68f), StoneDark = K(.60f, .56f, .50f), Paving = K(.70f, .67f, .62f);
    static readonly Color32 Lead = K(.50f, .55f, .60f), BlueLead = K(.42f, .52f, .60f), Terracotta = K(.72f, .46f, .36f);
    static readonly Color32 Cream = K(.93f, .90f, .82f), RoofRed = K(.60f, .30f, .22f), Slate = K(.36f, .42f, .48f);
    static readonly Color32 White = K(.95f, .95f, .93f), Gold = K(.85f, .70f, .25f), Grey = K(.55f, .55f, .57f);

    static void Minaret(MeshBuilder s, Vector3 b, float h)
    {
        s.Box(new Vector3(b.x, -6f, b.z), 4.4f, 8f, 4.4f, Stone);
        float y1 = 2f, sh = h * 0.80f;
        s.Cylinder(new Vector3(b.x, y1, b.z), 1.6f, sh, 10, Stone, true, 1.25f);
        s.Cylinder(new Vector3(b.x, y1 + sh * 0.78f, b.z), 2.5f, 0.9f, 10, StoneDark);
        s.Cone(new Vector3(b.x, y1 + sh, b.z), 1.9f, h * 0.24f, 10, Lead);
        s.Cone(new Vector3(b.x, y1 + sh + h * 0.24f, b.z), 0.2f, 3.5f, 4, Gold);
    }

    // Genel cami: W salon genisligi, H duvar yuksekligi
    public static float Mosque(MeshBuilder s, Vector3 pos, float yaw, float W, float H, float domeR,
                               int minarets, float minH, Color32 wall, Color32 dome, bool courtyard)
    {
        s.SetTransform(pos, yaw, 1f);
        s.Box(new Vector3(0, -8f, 0), W + 8f, 8.4f, W + 8f, StoneDark);
        s.Box(Vector3.zero, W, H, W, wall);
        s.Cylinder(new Vector3(0, H, 0), domeR * 1.02f, 3.5f, 20, wall);
        s.Hemisphere(new Vector3(0, H + 3.5f, 0), domeR, 5, 20, dome, 0.9f);
        float hr = domeR * 0.62f, off = domeR * 0.95f;
        s.Hemisphere(new Vector3(off, H, 0), hr, 4, 14, dome, 0.85f);
        s.Hemisphere(new Vector3(-off, H, 0), hr, 4, 14, dome, 0.85f);
        s.Hemisphere(new Vector3(0, H, off), hr, 4, 14, dome, 0.85f);
        s.Hemisphere(new Vector3(0, H, -off), hr, 4, 14, dome, 0.85f);
        float co = W * 0.40f, cr = domeR * 0.32f;
        for (int sx = -1; sx <= 1; sx += 2)
            for (int sz = -1; sz <= 1; sz += 2)
                s.Hemisphere(new Vector3(sx * co, H, sz * co), cr, 3, 10, dome, 0.9f);

        float mo = W * 0.5f + 3.5f;
        List<Vector2> mp = new List<Vector2>();
        if (minarets >= 4) { mp.Add(new Vector2(-mo, -mo)); mp.Add(new Vector2(mo, -mo)); mp.Add(new Vector2(-mo, mo)); mp.Add(new Vector2(mo, mo)); }
        else if (minarets >= 2) { mp.Add(new Vector2(-mo, mo)); mp.Add(new Vector2(mo, mo)); }
        else if (minarets == 1) mp.Add(new Vector2(mo, mo));
        for (int i = 0; i < mp.Count; i++) Minaret(s, new Vector3(mp[i].x, 0, mp[i].y), minH);

        float reach = W * 0.75f;
        if (courtyard)
        {
            float cd = W, cz = -(W * 0.5f + cd * 0.5f);
            s.Box(new Vector3(0, -8f, cz), cd, 8.4f, cd, Paving);
            s.Box(new Vector3(0, 0, cz - cd * 0.5f), cd, 6f, 2.5f, wall);
            s.Box(new Vector3(-cd * 0.5f, 0, cz), 2.5f, 6f, cd, wall);
            s.Box(new Vector3(cd * 0.5f, 0, cz), 2.5f, 6f, cd, wall);
            s.Box(new Vector3(0, 0, -(W * 0.5f + 1.2f)), cd * 0.6f, 6f, 2.5f, wall);
            s.Cylinder(new Vector3(0, 0, cz), 4.2f, 3f, 8, Lead);
            s.Cone(new Vector3(0, 3f, cz), 5f, 3.2f, 8, Lead);
            s.Hemisphere(new Vector3(-cd * 0.25f, 6f, cz - cd * 0.5f), 4f, 3, 10, dome, 0.8f);
            s.Hemisphere(new Vector3(cd * 0.25f, 6f, cz - cd * 0.5f), 4f, 3, 10, dome, 0.8f);
            if (minarets >= 6)
            {
                Minaret(s, new Vector3(-cd * 0.5f, 0, cz - cd * 0.5f), minH * 0.88f);
                Minaret(s, new Vector3(cd * 0.5f, 0, cz - cd * 0.5f), minH * 0.88f);
            }
            reach = W + cd;
        }
        s.ResetTransform();
        return reach;
    }

    public static float Ayasofya(MeshBuilder s, Vector3 pos, float yaw)
    {
        float W = 70f;
        float r = Mosque(s, pos, yaw, W, 26f, 17f, 4, 56f, Terracotta, Lead, false);
        s.SetTransform(pos, yaw, 1f);
        for (int i = -2; i <= 2; i++)
        {
            s.Box(new Vector3(W * 0.5f + 3f, 0, i * 14f), 6f, 19f, 5f, Terracotta);
            s.Box(new Vector3(-W * 0.5f - 3f, 0, i * 14f), 6f, 19f, 5f, Terracotta);
        }
        s.Box(new Vector3(0, 0, -(W * 0.5f + 5f)), W * 0.8f, 12f, 10f, Terracotta);
        s.ResetTransform();
        return r;
    }

    public static float GalataTower(MeshBuilder s, Vector3 pos)
    {
        s.SetTransform(pos, 0f, 1f);
        s.Cylinder(new Vector3(0, -8f, 0), 10.5f, 20f, 18, StoneDark);
        s.Cylinder(new Vector3(0, 0, 0), 9.2f, 44f, 18, K(.80f, .75f, .65f), true, 8.2f);
        s.Cylinder(new Vector3(0, 44f, 0), 11.2f, 3.2f, 18, StoneDark);
        s.Cylinder(new Vector3(0, 47.2f, 0), 8.6f, 6f, 18, K(.82f, .77f, .67f));
        s.Cone(new Vector3(0, 53.2f, 0), 10.2f, 15f, 18, K(.38f, .44f, .42f));
        s.Cone(new Vector3(0, 68.2f, 0), 0.3f, 5f, 4, Gold);
        s.ResetTransform();
        return 40f;
    }

    public static float KizKulesi(MeshBuilder s, Vector3 pos)
    {
        s.SetTransform(pos, 0f, 1f);
        s.Box(new Vector3(0, -8f, 0), 22f, 10.4f, 16f, StoneDark);
        s.Cylinder(new Vector3(0, 0, 0), 4.6f, 16f, 12, White, true, 4.0f);
        s.Cylinder(new Vector3(0, 16f, 0), 5.8f, 1.4f, 12, StoneDark);
        s.Hemisphere(new Vector3(0, 17.4f, 0), 4.2f, 3, 12, Lead, 1.0f);
        s.Cone(new Vector3(0, 21.5f, 0), 1.2f, 3.5f, 8, Gold);
        s.Box(new Vector3(-7f, 0, 0), 5f, 5f, 5f, White);
        s.Cone(new Vector3(-7f, 5f, 0), 4.2f, 3f, 4, RoofRed);
        s.ResetTransform();
        return 22f;
    }

    // Dolmabahce: uzun eksen yerel z
    public static float Dolmabahce(MeshBuilder s, MeshBuilder fw, Vector3 pos, float yaw)
    {
        s.SetTransform(pos, yaw, 1f);
        s.Box(new Vector3(0, -8f, 0), 56f, 8.4f, 236f, Paving);
        s.ResetTransform();
        fw.SetTransform(pos, yaw, 1f);
        fw.Box(new Vector3(0, 0, 0), 34f, 20f, 190f, Cream, true, false);
        fw.Box(new Vector3(0, 0, -105f), 38f, 26f, 40f, Cream, true, false);
        fw.Box(new Vector3(0, 0, 105f), 38f, 26f, 40f, Cream, true, false);
        fw.Box(new Vector3(-6f, 0, 0), 28f, 28f, 54f, K(.95f, .92f, .84f), true, false);
        fw.ResetTransform();
        s.SetTransform(pos, yaw, 1f);
        s.Box(new Vector3(0, 20f, 0), 36f, 1f, 192f, Grey);
        s.Box(new Vector3(0, 26f, -105f), 40f, 1f, 42f, Grey);
        s.Box(new Vector3(0, 26f, 105f), 40f, 1f, 42f, Grey);
        s.Box(new Vector3(-6f, 28f, 0), 30f, 1.2f, 56f, Grey);
        s.Hemisphere(new Vector3(-6f, 29.2f, 0), 11f, 4, 14, Lead, 0.7f);
        // saat kulesi
        s.Box(new Vector3(40f, 0, 40f), 7f, 28f, 7f, Cream);
        s.Cone(new Vector3(40f, 28f, 40f), 5.5f, 9f, 4, Lead);
        s.ResetTransform();
        return 140f;
    }

    public static float RumeliHisari(MeshBuilder s, Vector3 pos, float yaw)
    {
        s.SetTransform(pos, yaw, 1f);
        Color32 st = K(.78f, .73f, .64f);
        s.Box(new Vector3(0, -10f, 0), 70f, 10.4f, 150f, StoneDark);
        // uc buyuk kule
        Vector3[] t = { new Vector3(-22f, 0, -62f), new Vector3(22f, 0, 8f), new Vector3(-20f, 0, 62f) };
        float[] tr = { 9f, 10f, 8.5f }, th = { 30f, 34f, 28f };
        for (int i = 0; i < 3; i++)
        {
            s.Cylinder(new Vector3(t[i].x, -4f, t[i].z), tr[i], th[i] + 4f, 14, st, true);
            s.Cone(new Vector3(t[i].x, th[i], t[i].z), tr[i] + 1.2f, 11f, 14, RoofRed);
        }
        // duvarlar
        s.Box(new Vector3(-22f, 0, -27f), 3.2f, 12f, 72f, st);
        s.Box(new Vector3(0, 0, 35f), 3.2f, 12f, 76f, st);
        s.Box(new Vector3(-21f, 0, 8f), 3.2f, 11f, 36f, st);
        s.Box(new Vector3(22f, 0, -30f), 3.2f, 12f, 72f, st);
        s.Box(new Vector3(0, 0, -62f), 44f, 11f, 3.2f, st);
        for (int i = 0; i < 4; i++) s.Box(new Vector3(-6f + i * 7f, 0, 20f), 3f, 4f, 3f, st);
        // kucuk kuleler
        s.Cylinder(new Vector3(22f, 0, -40f), 4.5f, 18f, 10, st);
        s.Cone(new Vector3(22f, 18f, -40f), 5.5f, 6f, 10, RoofRed);
        s.ResetTransform();
        return 85f;
    }

    public static float Haydarpasa(MeshBuilder s, MeshBuilder fw, Vector3 pos, float yaw)
    {
        s.SetTransform(pos, yaw, 1f);
        s.Box(new Vector3(0, -8f, 10f), 150f, 8.4f, 110f, Paving);
        s.ResetTransform();
        Color32 st = K(.66f, .62f, .54f);
        fw.SetTransform(pos, yaw, 1f);
        fw.Box(new Vector3(0, 0, -20f), 130f, 24f, 30f, st, true, false);
        fw.Box(new Vector3(-53f, 0, 25f), 24f, 24f, 62f, st, true, false);
        fw.Box(new Vector3(53f, 0, 25f), 24f, 24f, 62f, st, true, false);
        fw.ResetTransform();
        s.SetTransform(pos, yaw, 1f);
        s.HipRoof(new Vector3(0, 24f, -20f), 134f, 34f, 14f, Slate, 0.5f);
        s.HipRoof(new Vector3(-53f, 24f, 25f), 28f, 66f, 12f, Slate, 0.45f);
        s.HipRoof(new Vector3(53f, 24f, 25f), 28f, 66f, 12f, Slate, 0.45f);
        float[] tx = { -66f, 66f, -66f, 66f }; float[] tz = { -34f, -34f, 54f, 54f };
        for (int i = 0; i < 4; i++)
        {
            s.Box(new Vector3(tx[i], 0, tz[i]), 12f, 40f, 12f, st);
            s.HipRoof(new Vector3(tx[i], 40f, tz[i]), 14f, 14f, 12f, Slate, 0.1f);
        }
        s.ResetTransform();
        return 110f;
    }

    public static float CamlicaTower(MeshBuilder s, Vector3 pos)
    {
        s.SetTransform(pos, 0f, 1f);
        s.Cylinder(new Vector3(0, -8f, 0), 14f, 12f, 16, Grey);
        s.Cylinder(new Vector3(0, 0, 0), 6.5f, 28f, 16, White, true, 4.5f);
        s.Cylinder(new Vector3(0, 28f, 0), 4.5f, 80f, 14, White, true, 3.2f);
        s.Cylinder(new Vector3(0, 100f, 0), 3.2f, 6f, 14, White, true, 14f);   // platform altı
        s.Cylinder(new Vector3(0, 106f, 0), 14f, 8f, 18, K(.30f, .40f, .55f), true, 14f);
        s.Cone(new Vector3(0, 114f, 0), 14f, 3f, 18, White);
        s.Cylinder(new Vector3(0, 117f, 0), 1.8f, 38f, 8, K(.9f, .15f, .15f), true, 0.5f);
        s.ResetTransform();
        return 30f;
    }

    public static float Topkapi(MeshBuilder s, Vector3 pos, float yaw)
    {
        s.SetTransform(pos, yaw, 1f);
        s.Box(new Vector3(0, -8f, 0), 130f, 8.4f, 110f, Paving);
        Color32 st = K(.86f, .80f, .68f);
        // dis duvar
        s.Box(new Vector3(0, 0, -55f), 130f, 9f, 2.5f, st);
        s.Box(new Vector3(0, 0, 55f), 130f, 9f, 2.5f, st);
        s.Box(new Vector3(-65f, 0, 0), 2.5f, 9f, 110f, st);
        s.Box(new Vector3(65f, 0, 0), 2.5f, 9f, 110f, st);
        // koskler
        float[] px = { -40f, -10f, 25f, 45f, -30f, 5f, 40f, -48f };
        float[] pz = { -35f, -30f, -38f, -10f, 5f, 15f, 30f, 35f };
        float[] pw = { 24f, 18f, 26f, 20f, 22f, 28f, 20f, 16f };
        float[] ph = { 9f, 12f, 8f, 10f, 8f, 11f, 9f, 10f };
        for (int i = 0; i < px.Length; i++)
        {
            s.Box(new Vector3(px[i], 0, pz[i]), pw[i], ph[i], pw[i] * 0.7f, st);
            s.HipRoof(new Vector3(px[i], ph[i], pz[i]), pw[i] + 2f, pw[i] * 0.7f + 2f, 4.5f, RoofRed, 0.4f);
        }
        // Adalet kulesi
        s.Box(new Vector3(-45f, 0, 8f), 9f, 36f, 9f, st);
        s.Cone(new Vector3(-45f, 36f, 8f), 7f, 12f, 8, Lead);
        s.Hemisphere(new Vector3(5f, 11f, 15f), 8f, 3, 12, Lead, 0.8f);
        s.ResetTransform();
        return 85f;
    }

    public static void Skyscraper(MeshBuilder s, MeshBuilder fw, Vector3 pos, float w, float h, Color32 col)
    {
        s.SetTransform(pos, 0f, 1f);
        s.Box(new Vector3(0, -8f, 0), w + 6f, 8.4f, w + 6f, Grey);
        s.ResetTransform();
        fw.SetTransform(pos, 0f, 1f);
        fw.Box(Vector3.zero, w, h, w, col, true, false);
        fw.ResetTransform();
        s.SetTransform(pos, 0f, 1f);
        s.Box(new Vector3(0, h, 0), w + 0.6f, 1.2f, w + 0.6f, Grey);
        s.Box(new Vector3(0, h + 1.2f, 0), w * 0.4f, 4f, w * 0.4f, Grey);
        s.Cylinder(new Vector3(0, h + 5.2f, 0), 0.4f, 14f, 6, Grey, true, 0.15f);
        s.ResetTransform();
    }
}
