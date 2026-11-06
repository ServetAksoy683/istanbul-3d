using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class BuildScript
{
    const string FbxPath = "Assets/Characters/UAL1_Standard.fbx";
    const string ScenePath = "Assets/Scenes/Main.unity";
    const string ControllerPath = "Assets/Generated/PlayerAnimator.controller";

    // GameCI bu metodu cagirir: BuildScript.BuildAndroid
    public static void BuildAndroid()
    {
        try
        {
            Prepare();
            string outPath = "build/Android/IstanbulGame.apk";
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
                if (args[i] == "-customBuildPath")
                {
                    string v = args[i + 1];
                    outPath = v.EndsWith(".apk", StringComparison.OrdinalIgnoreCase) ? v : Path.Combine(v, "IstanbulGame.apk");
                }
            Directory.CreateDirectory(Path.GetDirectoryName(outPath));
            BuildPlayerOptions o = new BuildPlayerOptions();
            o.scenes = new[] { ScenePath };
            o.locationPathName = outPath;
            o.target = BuildTarget.Android;
            o.options = BuildOptions.None;
            var report = BuildPipeline.BuildPlayer(o);
            Debug.Log("Build sonucu: " + report.summary.result);
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded) EditorApplication.Exit(1);
        }
        catch (Exception e)
        {
            Debug.LogError("BUILD HATASI: " + e);
            EditorApplication.Exit(1);
        }
    }

    [MenuItem("Istanbul/Hazirla (sahne + animator)")]
    public static void Prepare()
    {
        EnsureFolder("Assets/Resources"); EnsureFolder("Assets/Resources/Materials");
        EnsureFolder("Assets/Generated"); EnsureFolder("Assets/Scenes");
        ConfigureCharacter();
        CreateMaterials();
        AnimatorController ctrl = CreateAnimator();
        CreateScene(ctrl);
        ApplyPlayerSettings();
        AssetDatabase.SaveAssets();
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }

    static void ConfigureCharacter()
    {
        ModelImporter imp = AssetImporter.GetAtPath(FbxPath) as ModelImporter;
        if (imp == null) throw new Exception("FBX bulunamadi: " + FbxPath);
        imp.animationType = ModelImporterAnimationType.Generic;
        imp.importAnimation = true;
        imp.optimizeGameObjects = false;
        ModelImporterClipAnimation[] clips = imp.defaultClipAnimations;
        foreach (ModelImporterClipAnimation c in clips) c.loopTime = c.name.EndsWith("_Loop");
        imp.clipAnimations = clips;
        imp.SaveAndReimport();
    }

    static Material MakeMat(string name, string shader, Action<Material> setup)
    {
        Shader sh = Shader.Find(shader);
        if (sh == null) throw new Exception("Shader bulunamadi: " + shader);
        string path = "Assets/Resources/Materials/" + name + ".mat";
        AssetDatabase.DeleteAsset(path);
        Material m = new Material(sh);
        if (setup != null) setup(m);
        AssetDatabase.CreateAsset(m, path);
        return m;
    }

    static void CreateMaterials()
    {
        MakeMat("Solid", "Istanbul/VertexColorTex", null);
        MakeMat("Facade", "Istanbul/VertexColorTex", null);
        MakeMat("Water", "Istanbul/Water", null);
        MakeMat("Sky", "Skybox/Procedural", m =>
        {
            m.SetFloat("_SunSize", 0.05f);
            m.SetFloat("_AtmosphereThickness", 1.0f);
            m.SetFloat("_Exposure", 1.25f);
            m.SetColor("_SkyTint", new Color(0.55f, 0.65f, 0.80f));
            m.SetColor("_GroundColor", new Color(0.62f, 0.68f, 0.74f));
        });
        AssetDatabase.SaveAssets();
    }

    static AnimationClip FindClip(AnimationClip[] clips, string key)
    {
        AnimationClip best = null;
        foreach (AnimationClip c in clips)
        {
            if (!c.name.EndsWith(key)) continue;
            int pi = c.name.Length - key.Length - 1;
            if (pi >= 0 && (char.IsLetterOrDigit(c.name[pi]) || c.name[pi] == '_')) continue; // Swim_Idle_Loop != Idle_Loop
            if (best == null || c.name.Length < best.name.Length) best = c;
        }
        if (best == null) throw new Exception("Animasyon bulunamadi: " + key);
        return best;
    }

    static AnimatorController CreateAnimator()
    {
        AssetDatabase.DeleteAsset(ControllerPath);
        AnimationClip[] clips = AssetDatabase.LoadAllAssetsAtPath(FbxPath).OfType<AnimationClip>()
            .Where(c => !c.name.StartsWith("__preview__")).ToArray();
        Debug.Log("Bulunan animasyon sayisi: " + clips.Length);

        AnimatorController ctrl = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        ctrl.AddParameter("Speed", AnimatorControllerParameterType.Float);
        ctrl.AddParameter("Grounded", AnimatorControllerParameterType.Bool);
        ctrl.AddParameter("Swim", AnimatorControllerParameterType.Bool);
        AnimatorStateMachine sm = ctrl.layers[0].stateMachine;

        BlendTree loco = new BlendTree();
        loco.name = "Locomotion"; loco.blendType = BlendTreeType.Simple1D; loco.blendParameter = "Speed"; loco.useAutomaticThresholds = false;
        AssetDatabase.AddObjectToAsset(loco, ctrl);
        loco.AddChild(FindClip(clips, "Idle_Loop"), 0f);
        loco.AddChild(FindClip(clips, "Walk_Loop"), 1.8f);
        loco.AddChild(FindClip(clips, "Jog_Fwd_Loop"), 3.9f);
        loco.AddChild(FindClip(clips, "Sprint_Loop"), 6.8f);
        AnimatorState sLoco = sm.AddState("Locomotion");
        sLoco.motion = loco;
        sm.defaultState = sLoco;

        AnimatorState sStart = sm.AddState("JumpStart"); sStart.motion = FindClip(clips, "Jump_Start");
        AnimatorState sLoop = sm.AddState("JumpLoop"); sLoop.motion = FindClip(clips, "Jump_Loop");
        AnimatorState sLand = sm.AddState("JumpLand"); sLand.motion = FindClip(clips, "Jump_Land");

        BlendTree swim = new BlendTree();
        swim.name = "Swim"; swim.blendType = BlendTreeType.Simple1D; swim.blendParameter = "Speed"; swim.useAutomaticThresholds = false;
        AssetDatabase.AddObjectToAsset(swim, ctrl);
        swim.AddChild(FindClip(clips, "Swim_Idle_Loop"), 0f);
        swim.AddChild(FindClip(clips, "Swim_Fwd_Loop"), 1.5f);
        AnimatorState sSwim = sm.AddState("Swim"); sSwim.motion = swim;

        AnimatorStateTransition t;
        t = sLoco.AddTransition(sStart); t.hasExitTime = false; t.duration = 0.08f; t.AddCondition(AnimatorConditionMode.IfNot, 0, "Grounded");
        t = sStart.AddTransition(sLoop); t.hasExitTime = true; t.exitTime = 0.85f; t.duration = 0.1f;
        t = sStart.AddTransition(sLand); t.hasExitTime = false; t.duration = 0.05f; t.AddCondition(AnimatorConditionMode.If, 0, "Grounded");
        t = sLoop.AddTransition(sLand); t.hasExitTime = false; t.duration = 0.05f; t.AddCondition(AnimatorConditionMode.If, 0, "Grounded");
        t = sLand.AddTransition(sLoco); t.hasExitTime = true; t.exitTime = 0.6f; t.duration = 0.12f;
        t = sLoco.AddTransition(sSwim); t.hasExitTime = false; t.duration = 0.2f; t.AddCondition(AnimatorConditionMode.If, 0, "Swim");
        t = sSwim.AddTransition(sLoco); t.hasExitTime = false; t.duration = 0.2f; t.AddCondition(AnimatorConditionMode.IfNot, 0, "Swim");
        t = sStart.AddTransition(sSwim); t.hasExitTime = false; t.duration = 0.1f; t.AddCondition(AnimatorConditionMode.If, 0, "Swim");
        t = sLoop.AddTransition(sSwim); t.hasExitTime = false; t.duration = 0.1f; t.AddCondition(AnimatorConditionMode.If, 0, "Swim");

        EditorUtility.SetDirty(ctrl);
        AssetDatabase.SaveAssets();
        return ctrl;
    }

    static void CreateScene(AnimatorController ctrl)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        GameObject sunGo = new GameObject("Sun");
        Light sun = sunGo.AddComponent<Light>();
        sun.type = LightType.Directional; sun.intensity = 1.15f; sun.color = new Color(1f, 0.96f, 0.88f);
        sun.shadows = LightShadows.Soft; sun.shadowStrength = 0.7f;
        sunGo.transform.rotation = Quaternion.Euler(46f, -32f, 0f);
        RenderSettings.sun = sun;

        GameObject player = new GameObject("Player");
        player.tag = "Player";
        CharacterController cc = player.AddComponent<CharacterController>();
        cc.height = 1.8f; cc.radius = 0.32f; cc.center = new Vector3(0, 0.9f, 0);
        cc.stepOffset = 0.45f; cc.slopeLimit = 55f; cc.skinWidth = 0.04f;
        cc.enabled = false; // dunya hazir olunca acilir

        GameObject fbx = AssetDatabase.LoadAssetAtPath<GameObject>(FbxPath);
        GameObject model = (GameObject)PrefabUtility.InstantiatePrefab(fbx, player.transform);
        model.transform.localPosition = Vector3.zero; model.transform.localRotation = Quaternion.identity;
        Renderer[] rs = model.GetComponentsInChildren<Renderer>();
        if (rs.Length > 0)
        {
            Bounds b = rs[0].bounds;
            foreach (Renderer r in rs) b.Encapsulate(r.bounds);
            if (b.size.y > 0.01f)
            {
                model.transform.localScale = Vector3.one * (1.8f / b.size.y);
                Bounds b2 = rs[0].bounds;
                foreach (Renderer r in rs) b2.Encapsulate(r.bounds);
                model.transform.localPosition = new Vector3(0, -b2.min.y, 0);
            }
        }
        Animator anim = model.GetComponent<Animator>();
        if (anim == null) anim = model.AddComponent<Animator>();
        anim.runtimeAnimatorController = ctrl;
        anim.applyRootMotion = false;
        anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;

        GameObject camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        Camera cam = camGo.AddComponent<Camera>();
        cam.nearClipPlane = 0.3f; cam.farClipPlane = 1300f; cam.fieldOfView = 62f; cam.clearFlags = CameraClearFlags.Skybox;
        camGo.AddComponent<AudioListener>();
        CameraRig rig = camGo.AddComponent<CameraRig>();
        rig.target = player.transform;

        PlayerController pc = player.AddComponent<PlayerController>();
        pc.animator = anim; pc.cameraTransform = camGo.transform;

        GameObject boot = new GameObject("GameBootstrap");
        GameBootstrap gb = boot.AddComponent<GameBootstrap>();
        gb.player = pc; gb.cameraRig = rig; gb.sun = sun;

        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
    }

    static void ApplyPlayerSettings()
    {
        PlayerSettings.companyName = "IstanbulGame";
        PlayerSettings.productName = "Istanbul 3D";
        PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.istanbulgame.city");
        PlayerSettings.bundleVersion = "0.1";
        PlayerSettings.colorSpace = ColorSpace.Gamma;
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel24;
        PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevel33;
        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
        PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { UnityEngine.Rendering.GraphicsDeviceType.OpenGLES3 });
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
        PlayerSettings.allowedAutorotateToPortrait = false;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        PlayerSettings.allowedAutorotateToLandscapeLeft = true;
        PlayerSettings.allowedAutorotateToLandscapeRight = true;
        EditorUserBuildSettings.buildAppBundle = false;
    }
}
