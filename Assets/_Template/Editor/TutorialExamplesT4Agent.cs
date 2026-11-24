using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace ThreeDUI.Editor
{
    /// <summary>
    /// Tutorial 4 · the guide agent (lecture: Avatars, Embodiment and AI Agents). Builds the agent prefab from the
    /// Quaternius CC0 character (ported from the graduate Agent Lab) and adds a guide to the T4 example.
    /// Menu: 3DUI > Create > Guide Agent · 3DUI > Create > Guide Thing (on the selected object).
    /// </summary>
    public static partial class TemplateSceneBuilder
    {
        private const string GuideRoot = "Assets/_Template/Agent";
        private const string GuideQ = GuideRoot + "/Quaternius";
        private const string GuideGen = GuideRoot + "/Generated";
        private const string GuideCharacterFbx = GuideQ + "/Character/Superhero_Female_FullBody.fbx";
        private const string GuideHairFbx = GuideQ + "/Hair/Hair_Long.fbx";
        private const string GuideOutfitFbx = GuideQ + "/Outfit/Female_Peasant.fbx";
        private const string GuideAnimFbx = GuideQ + "/Animations/UAL1_Standard.fbx";
        public const string GuidePrefabPath = GuideGen + "/Guide Agent.prefab";

        private static readonly (string source, string state, bool loop)[] GuideClips =
        {
            ("Idle_Loop", "Idle", true), ("Idle_Talking_Loop", "Talk", true), ("Walk_Loop", "Walk", true),
        };

        [MenuItem("3DUI/Create/Guide Agent (Tutorial 4)", priority = 170)]
        private static void MenuGuideAgent()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(GuidePrefabPath) ?? BuildGuidePrefab();
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.transform.position = ScenePivot();
            Undo.RegisterCreatedObjectUndo(go, "Create Guide Agent");
            Selection.activeGameObject = go;
            Debug.Log("[3DUI] Guide Agent added. Put Guide Thing (3DUI > Create > Guide Thing) on the objects it should explain.");
        }

        [MenuItem("3DUI/Create/Guide Thing (on the selected object)", priority = 171)]
        private static void MenuGuideThing()
        {
            var go = Selection.activeGameObject;
            if (go == null) { EditorUtility.DisplayDialog("3DUI", "Select the object the agent should explain first (it needs a collider).", "OK"); return; }
            if (go.GetComponentInChildren<Collider>() == null) Undo.AddComponent<BoxCollider>(go);
            var t = go.GetComponent<GuideThing>() ?? Undo.AddComponent<GuideThing>(go);
            t.id = go.name.ToLowerInvariant().Replace(' ', '_');
            t.DisplayName = go.name;
            t.description = "TODO: one or two short sentences the guide says about it.";
            EditorUtility.SetDirty(t);
        }

        // ================================================================== assets (from the graduate Agent Lab builder)

        private static void PrepareGuideAssets()
        {
            TemplateProjectSetup.EnsureFolder(GuideGen);
            GuideHumanoid(GuideCharacterFbx, null);
            GuideHumanoid(GuideAnimFbx, GuideClips);
            foreach (var path in new[] { GuideHairFbx, GuideOutfitFbx })
            {
                var mi = (ModelImporter)AssetImporter.GetAtPath(path);
                if (mi.animationType != ModelImporterAnimationType.Generic || mi.materialImportMode != ModelImporterMaterialImportMode.None || mi.importAnimation)
                {
                    mi.animationType = ModelImporterAnimationType.Generic;
                    mi.avatarSetup = ModelImporterAvatarSetup.NoAvatar;
                    mi.importAnimation = false;
                    mi.materialImportMode = ModelImporterMaterialImportMode.None;
                    mi.SaveAndReimport();
                }
            }
            foreach (var n in new[] { "T_Superhero_Female_Normal", "T_Hair_2_Normal", "T_Peasant_Normal" })
            {
                var ti = (TextureImporter)AssetImporter.GetAtPath($"{GuideQ}/Textures/{n}.png");
                if (ti != null && ti.textureType != TextureImporterType.NormalMap) { ti.textureType = TextureImporterType.NormalMap; ti.SaveAndReimport(); }
            }
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { GuideQ + "/Textures" }))
            {
                var ti = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(guid));
                if (ti != null && ti.maxTextureSize > 1024) { ti.maxTextureSize = 1024; ti.SaveAndReimport(); }   // the T5 budget
            }
        }

        private static void GuideHumanoid(string path, (string source, string state, bool loop)[] keepClips)
        {
            var mi = (ModelImporter)AssetImporter.GetAtPath(path);
            bool dirty = false;
            if (mi.animationType != ModelImporterAnimationType.Human) { mi.animationType = ModelImporterAnimationType.Human; dirty = true; }
            if (mi.avatarSetup != ModelImporterAvatarSetup.CreateFromThisModel) { mi.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel; dirty = true; }
            if (mi.materialImportMode != ModelImporterMaterialImportMode.None) { mi.materialImportMode = ModelImporterMaterialImportMode.None; dirty = true; }
            if (keepClips == null)
            {
                if (mi.importAnimation) { mi.importAnimation = false; dirty = true; }
            }
            else
            {
                if (dirty) { mi.SaveAndReimport(); dirty = false; }
                var defaults = mi.defaultClipAnimations;
                var wanted = new List<ModelImporterClipAnimation>();
                foreach (var (source, state, loop) in keepClips)
                {
                    var c = defaults.FirstOrDefault(d => d.name.EndsWith("|" + source) || d.name == source);
                    if (c == null) { Debug.LogWarning("[3DUI] guide clip not found: " + source); continue; }
                    c.name = state;
                    c.loopTime = loop;
                    c.lockRootRotation = c.lockRootHeightY = c.lockRootPositionXZ = true;
                    c.keepOriginalOrientation = c.keepOriginalPositionY = c.keepOriginalPositionXZ = true;
                    wanted.Add(c);
                }
                if (mi.clipAnimations.Length != wanted.Count || mi.clipAnimations.Select(c => c.name).Except(wanted.Select(w => w.name)).Any())
                {
                    mi.clipAnimations = wanted.ToArray();
                    dirty = true;
                }
            }
            if (dirty) mi.SaveAndReimport();
        }

        private static Material GuideMat(string name, string baseTex, string normalTex, Color? tint = null)
        {
            var m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            m.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>($"{GuideQ}/Textures/{baseTex}.png"));
            m.SetColor("_BaseColor", tint ?? Color.white);
            m.SetFloat("_Smoothness", 0.25f);
            if (normalTex != null)
            {
                m.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>($"{GuideQ}/Textures/{normalTex}.png"));
                m.EnableKeyword("_NORMALMAP");
            }
            string path = $"{GuideGen}/{name}.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) { existing.CopyPropertiesFromMaterial(m); existing.shaderKeywords = m.shaderKeywords; EditorUtility.SetDirty(existing); return existing; }
            AssetDatabase.CreateAsset(m, path);
            return m;
        }

        private static AnimatorController GuideController()
        {
            string path = GuideGen + "/Guide.controller";
            AssetDatabase.DeleteAsset(path);
            var ctrl = AnimatorController.CreateAnimatorControllerAtPath(path);
            var clips = AssetDatabase.LoadAllAssetsAtPath(GuideAnimFbx).OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview")).ToList();
            var sm = ctrl.layers[0].stateMachine;
            foreach (var (_, state, _) in GuideClips)
            {
                var s = sm.AddState(state);
                s.motion = clips.FirstOrDefault(c => c.name == state);
                s.writeDefaultValues = true;
                if (state == "Idle") sm.defaultState = s;
            }
            var layers = ctrl.layers;
            layers[0].iKPass = true;          // gaze and pointing (GuideBody.ApplyIK)
            ctrl.layers = layers;
            EditorUtility.SetDirty(ctrl);
            return ctrl;
        }

        /// <summary>The guide prefab: the Quaternius character with hair and clothes, animator, body, agent, bubble and tool-call panel.</summary>
        public static GameObject BuildGuidePrefab()
        {
            PrepareGuideAssets();
            var skin = GuideMat("Guide Body", "T_Superhero_Female_Light_BaseColor", "T_Superhero_Female_Normal");
            var eyes = GuideMat("Guide Eyes", "T_Eye_Brown", null);
            var hairMat = GuideMat("Guide Hair", "T_Hair_2_BaseColor", "T_Hair_2_Normal", new Color(0.42f, 0.27f, 0.17f));
            var clothes = GuideMat("Guide Clothes", "T_Peasant_2_BaseColor", "T_Peasant_Normal");
            var ctrl = GuideController();

            var root = new GameObject("Guide Agent (Nova)");
            var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(GuideCharacterFbx));
            PrefabUtility.UnpackPrefabInstance(model, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            model.name = "Model";
            model.transform.SetParent(root.transform, false);
            model.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);   // the FBX faces -Z; the agent's forward is +Z
            foreach (var r in model.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                r.sharedMaterial = r.name.Contains("Eye") && !r.name.Contains("brow") ? eyes : r.name.Contains("brow") ? hairMat : skin;
                r.updateWhenOffscreen = true;
            }
            var bodySmr = model.GetComponentsInChildren<SkinnedMeshRenderer>(true).First(r => r.name == "Superhero_Female");
            bodySmr.sharedMesh = GuideHeadOnly(bodySmr, new[] { "Head", "neck_01" });   // the outfit covers the rest
            var bones = model.GetComponentsInChildren<Transform>(true).GroupBy(t => t.name).ToDictionary(g => g.Key, g => g.First());
            GuideAttach(GuideHairFbx, "Hair", hairMat, bones, model.transform);
            GuideAttach(GuideOutfitFbx, "Clothes", clothes, bones, model.transform);

            var animator = model.GetComponent<Animator>() ?? model.AddComponent<Animator>();
            animator.avatar = AssetDatabase.LoadAllAssetsAtPath(GuideCharacterFbx).OfType<Avatar>().First();
            animator.runtimeAnimatorController = ctrl;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            model.AddComponent<GuideIKRelay>();

            FinishGuide(root, animator, 2.15f, "Nova");
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, GuidePrefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        /// <summary>Collider, speech bubble, tool-call panel, body and agent: the same for every character.</summary>
        private static void FinishGuide(GameObject root, Animator animator, float bubbleHeight, string agentName)
        {
            var col = root.AddComponent<CapsuleCollider>();                    // click the agent: "hello"
            col.center = new Vector3(0f, bubbleHeight * 0.42f, 0f); col.height = bubbleHeight * 0.84f; col.radius = 0.25f;

            var anchor = new GameObject("Bubble anchor").transform;
            anchor.SetParent(root.transform, false);
            anchor.localPosition = new Vector3(0f, bubbleHeight, 0f);
            var bubble = WorldPanel.Create(root.transform, "Speech bubble", new Vector2(1.3f, 0.5f), agentName, " ", new Color(0.97f, 0.97f, 0.95f, 0.95f));
            bubble.transform.localPosition = anchor.localPosition;
            bubble.transform.localScale = Vector3.one * 0.7f / WorldPanel.PixelsPerMeter;
            bubble.TitleText.color = new Color(0.1f, 0.25f, 0.5f);
            bubble.TitleText.fontSize = 52; bubble.TitleText.fontSizeMax = 52;
            bubble.BodyText.color = new Color(0.08f, 0.08f, 0.1f);
            bubble.BodyText.fontSizeMax = 76; bubble.BodyText.fontSizeMin = 44;

            var call = WorldPanel.Create(bubble.transform, "Tool call", new Vector2(1.3f, 0.42f), "Tool call (JSON)", " ");
            call.transform.localPosition = new Vector3(0f, 480f, 0f);          // above the bubble, clear of the head (bubble units: px)
            call.transform.localScale = Vector3.one;
            call.TitleText.fontSize = 34; call.TitleText.fontSizeMax = 34;
            call.BodyText.fontSizeMax = 34; call.BodyText.fontSizeMin = 22;

            var body = root.AddComponent<GuideBody>();
            GuideSet(body, "animator", animator);
            GuideSet(body, "bubbleAnchor", anchor);
            GuideSet(body, "bubble", bubble);
            var agent = root.AddComponent<GuideAgent>();
            agent.agentName = agentName;
            agent.DisplayName = agentName + " (click: hello)";
            GuideSet(agent, "toolCallPanel", call);
        }

        // ================================================================== the robot guide (Quaternius LowPoly Robot, CC0)

        private const string RobotFbx = GuideRoot + "/QuaterniusRobot/Robot.fbx";
        public const string RobotPrefabPath = GuideGen + "/Guide Agent (Robot).prefab";
        private const float RobotYaw = 0f;
        private const float RobotHeight = 1.3f;                              // a small robot: eyes below the visitor's
        private static readonly (string source, string state, bool loop)[] RobotClips =
        {
            ("Robot_Idle", "Idle", true), ("Robot_Idle", "Talk", true), ("Robot_Walking", "Walk", true),
            ("Robot_Wave", "Wave", true), ("Robot_Yes", "Yes", true), ("Robot_No", "No", true),
        };

        [MenuItem("3DUI/Create/Guide Agent - robot (Tutorial 4)", priority = 169)]
        private static void MenuRobotGuide()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RobotPrefabPath) ?? BuildRobotGuidePrefab();
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.transform.position = ScenePivot();
            Undo.RegisterCreatedObjectUndo(go, "Create Guide Agent (robot)");
            Selection.activeGameObject = go;
        }

        /// <summary>
        /// The robot guide. Its rig is not humanoid (the feet hang on IK bones, not on the legs), so it stays Generic:
        /// its own clips play as they are (Idle, Walk, and Wave / Yes / No for the gestures), and GuideBody turns the
        /// head and the right arm directly for gaze and pointing.
        /// </summary>
        public static GameObject BuildRobotGuidePrefab()
        {
            TemplateProjectSetup.EnsureFolder(GuideGen);
            var mi = (ModelImporter)AssetImporter.GetAtPath(RobotFbx);
            if (mi.animationType != ModelImporterAnimationType.Generic || mi.avatarSetup != ModelImporterAvatarSetup.CreateFromThisModel)
            {
                mi.animationType = ModelImporterAnimationType.Generic;
                mi.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                mi.SaveAndReimport();
            }
            {   // scale it to RobotHeight (the FBX comes in at about 4.6 m)
                var probe = (GameObject)Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RobotFbx));
                var prs = probe.GetComponentsInChildren<Renderer>(true);
                var pb = prs[0].bounds; foreach (var r in prs) pb.Encapsulate(r.bounds);
                Object.DestroyImmediate(probe);
                if (Mathf.Abs(pb.size.y - RobotHeight) > 0.05f) { mi.globalScale *= RobotHeight / pb.size.y; mi.SaveAndReimport(); }
            }
            var wanted = new List<ModelImporterClipAnimation>();
            foreach (var (source, state, loop) in RobotClips)
            {
                var c = mi.defaultClipAnimations.FirstOrDefault(d => d.name.EndsWith("|" + source) || d.name == source);   // a fresh copy each time: Talk reuses Idle
                if (c == null) { Debug.LogWarning("[3DUI] robot clip not found: " + source); continue; }
                c.name = state; c.loopTime = loop;
                c.lockRootRotation = c.lockRootHeightY = c.lockRootPositionXZ = true;
                c.keepOriginalOrientation = c.keepOriginalPositionY = c.keepOriginalPositionXZ = true;
                wanted.Add(c);
            }
            if (mi.clipAnimations.Length != wanted.Count || mi.clipAnimations.Select(c => c.name).Except(wanted.Select(w => w.name)).Any())
            {
                mi.clipAnimations = wanted.ToArray();
                mi.SaveAndReimport();
            }
            var avatar = AssetDatabase.LoadAllAssetsAtPath(RobotFbx).OfType<Avatar>().FirstOrDefault();

            string path = GuideGen + "/Robot.controller";
            AssetDatabase.DeleteAsset(path);
            var ctrl = AnimatorController.CreateAnimatorControllerAtPath(path);
            var clips = AssetDatabase.LoadAllAssetsAtPath(RobotFbx).OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview")).ToList();
            var sm = ctrl.layers[0].stateMachine;
            foreach (var state in RobotClips.Select(c => c.state))
            {
                var st = sm.AddState(state);
                st.motion = clips.FirstOrDefault(c => c.name == state);
                st.writeDefaultValues = true;
                if (state == "Idle") sm.defaultState = st;
            }
            var layers = ctrl.layers; layers[0].iKPass = true; ctrl.layers = layers;
            EditorUtility.SetDirty(ctrl);

            var root = new GameObject("Guide Agent (Robot)");
            var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(RobotFbx));
            PrefabUtility.UnpackPrefabInstance(model, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            model.name = "Model";
            model.transform.SetParent(root.transform, false);
            model.transform.localRotation = Quaternion.Euler(0f, RobotYaw, 0f);
            foreach (var r in model.GetComponentsInChildren<SkinnedMeshRenderer>(true)) r.updateWhenOffscreen = true;
            var animator = model.GetComponent<Animator>() ?? model.AddComponent<Animator>();
            animator.avatar = avatar;
            animator.runtimeAnimatorController = ctrl;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            model.AddComponent<GuideIKRelay>();

            float top = 1.4f;
            var rs = model.GetComponentsInChildren<Renderer>(true);
            if (rs.Length > 0) { var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds); top = b.max.y; }
            Debug.Log($"[3DUI] robot height {top:0.00} m");
            FinishGuide(root, animator, top + 0.45f, "Nova");
            var bones = model.GetComponentsInChildren<Transform>(true).GroupBy(t => t.name).ToDictionary(g => g.Key, g => g.First());
            var body = root.GetComponent<GuideBody>();
            GuideSet(body, "genericHead", bones.GetValueOrDefault("Head"));
            GuideSet(body, "genericUpperArm", bones.GetValueOrDefault("UpperArm.R"));
            GuideSet(body, "genericLowerArm", bones.GetValueOrDefault("LowerArm.R"));

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, RobotPrefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static void GuideSet(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(field);
            if (p == null) { Debug.LogError($"[3DUI] {target.GetType().Name}.{field} not found"); return; }
            p.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Mesh GuideHeadOnly(SkinnedMeshRenderer smr, string[] keepBones)
        {
            var src = smr.sharedMesh;
            var keep = new HashSet<int>(smr.bones.Select((b, i) => (b, i)).Where(x => keepBones.Contains(x.b.name)).Select(x => x.i));
            var weights = src.boneWeights;
            var mesh = Object.Instantiate(src);
            mesh.name = src.name + " (head only)";
            for (int sub = 0; sub < src.subMeshCount; sub++)
            {
                var tris = src.GetTriangles(sub);
                var kept = new List<int>();
                for (int t = 0; t < tris.Length; t += 3)
                {
                    int n = 0;
                    for (int k = 0; k < 3; k++) if (keep.Contains(weights[tris[t + k]].boneIndex0)) n++;
                    if (n >= 2) { kept.Add(tris[t]); kept.Add(tris[t + 1]); kept.Add(tris[t + 2]); }
                }
                mesh.SetTriangles(kept, sub);
            }
            string path = GuideGen + "/Guide Head.asset";
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        private static void GuideAttach(string fbx, string name, Material mat, Dictionary<string, Transform> bones, Transform model)
        {
            var part = (GameObject)Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(fbx));
            int i = 0;
            foreach (var smr in part.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                smr.bones = smr.bones.Select(b => b != null && bones.TryGetValue(b.name, out var t) ? t : null).ToArray();
                if (smr.rootBone != null && bones.TryGetValue(smr.rootBone.name, out var rb)) smr.rootBone = rb;
                var mats = smr.sharedMaterials;
                for (int k = 0; k < mats.Length; k++) mats[k] = mat;
                smr.sharedMaterials = mats;
                smr.updateWhenOffscreen = true;
                smr.transform.SetParent(model, false);
                smr.name = i++ == 0 ? name : name + " " + i;
            }
            Object.DestroyImmediate(part);
        }

        // ================================================================== the T4 example gets a guide

        /// <summary>Batch: Unity -batchmode -executeMethod ThreeDUI.Editor.TemplateSceneBuilder.RebuildT4ExampleBatch</summary>
        public static void RebuildT4ExampleBatch()
        {
            try { BuildT4(); AssetDatabase.SaveAssets(); EditorApplication.Exit(0); }
            catch (System.Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }

        /// <summary>Called at the end of BuildT4: Nova stands near the entrance; the statue, a globe and a painting are things.</summary>
        internal static void AddGuideToT4(Transform ideas)
        {
            BuildGuidePrefab();                                               // the human guide stays available (menu)
            var prefab = BuildRobotGuidePrefab();                             // the T4 example uses the robot
            var guide = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            guide.transform.SetParent(ideas, true);
            guide.transform.SetPositionAndRotation(new Vector3(-1.4f, 0f, -2.2f), Quaternion.Euler(0f, 160f, 0f));

            var statue = ideas.Find("Statue (proximity label)/Statue").gameObject;
            GuideThingOn(statue, "statue", "Blue figure", "A blue figure from 2026. Its label appears only when you come close.");

            var globe = new GameObject("Globe").transform;
            globe.SetParent(ideas, false);
            globe.position = new Vector3(-2.6f, 0f, 3.6f);
            Prim(PrimitiveType.Cube, "Pedestal", globe, new Vector3(0f, 0.45f, 0f), new Vector3(0.5f, 0.9f, 0.5f), "Pedestal").isStatic = true;
            var ball = Prim(PrimitiveType.Sphere, "Globe", globe, new Vector3(0f, 1.15f, 0f), Vector3.one * 0.45f, "Blue");
            GuideThingOn(ball, "globe", "Globe", "A globe. I stand beside it, not in front of it, so you can still see it.");

            var frame = new GameObject("Painting").transform;
            frame.SetParent(ideas, false);
            frame.position = new Vector3(2.4f, 1.6f, 5.9f);
            var canvas = Prim(PrimitiveType.Cube, "Painting", frame, Vector3.zero, new Vector3(1.2f, 0.8f, 0.05f), "Pedestal");
            GuideThingOn(canvas, "painting", "Painting", "A painting. When I point at it, I look at it too: that is joint attention.");

            Note(ideas, "5. A guide agent", "Click Nova to say hello. Click the statue, the globe\nor the painting: she walks over, looks and points.\nWalk up to her: she keeps about 1.2 m.",
                 new Vector3(-3.0f, 2.2f, -1.2f), new Vector3(0f, 1.6f, -6f), 1.5f, 0.6f);
        }

        private static void GuideThingOn(GameObject go, string id, string name, string description)
        {
            var t = go.AddComponent<GuideThing>();
            t.id = id;
            t.DisplayName = name;
            t.description = description;
            EditorUtility.SetDirty(t);
        }
    }
}
