using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ThreeDUI.Editor
{
    /// <summary>
    /// One-click fixes for downloaded 3D models (menu: 3DUI > Models). Each works on the objects selected
    /// in the Hierarchy and can be undone with Ctrl+Z.
    /// </summary>
    public static class ModelTools
    {
        private const string Menu = "3DUI/Models/";

        /// <summary>Furniture, walls, props that never move: a collider on every mesh, marked Static.</summary>
        [MenuItem(Menu + "Prepare as static prop (colliders + Static)", priority = 300)]
        public static void PrepareStatic()
        {
            int colliders = 0, objects = 0;
            foreach (var root in Selection.gameObjects)
            {
                foreach (var mf in root.GetComponentsInChildren<MeshFilter>(true))
                {
                    if (mf.sharedMesh == null || mf.GetComponent<Collider>() != null) continue;
                    var mc = Undo.AddComponent<MeshCollider>(mf.gameObject);
                    mc.sharedMesh = mf.sharedMesh;
                    colliders++;
                }
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                {
                    Undo.RecordObject(t.gameObject, "Mark static");
                    t.gameObject.isStatic = true;
                    objects++;
                }
            }
            Report($"Prepared {Selection.gameObjects.Length} model(s): {colliders} collider(s) added, {objects} object(s) marked Static.");
        }

        /// <summary>A floor, stairs or a rug people stand on: needs a collider and a TeleportArea.</summary>
        [MenuItem(Menu + "Make walkable (floor for teleport and walking)", priority = 301)]
        public static void MakeWalkable()
        {
            int n = 0;
            foreach (var go in Selection.gameObjects)
            {
                if (go.GetComponent<Collider>() == null)
                {
                    var mf = go.GetComponent<MeshFilter>();
                    if (mf != null && mf.sharedMesh != null) Undo.AddComponent<MeshCollider>(go).sharedMesh = mf.sharedMesh;
                    else Undo.AddComponent<BoxCollider>(go);
                }
                if (go.GetComponent<TeleportArea>() == null) { Undo.AddComponent<TeleportArea>(go); n++; }
                go.isStatic = true;
            }
            Report($"{n} object(s) are now walkable (collider + TeleportArea).");
        }

        /// <summary>
        /// Pink models: their materials use the old Built-in shaders. Switch them to URP Lit and carry over
        /// the colour, texture and normal map. (Same idea as Window > Rendering > Render Pipeline Converter.)
        /// </summary>
        [MenuItem(Menu + "Fix pink materials (convert to URP)", priority = 302)]
        public static void ConvertMaterials()
        {
            var lit = Shader.Find("Universal Render Pipeline/Lit");
            var mats = new HashSet<Material>();
            foreach (var root in Selection.gameObjects)
                foreach (var r in root.GetComponentsInChildren<Renderer>(true))
                    foreach (var m in r.sharedMaterials)
                        if (m != null && !m.shader.name.StartsWith("Universal Render Pipeline") && !m.shader.name.StartsWith("TextMeshPro")
                            && !m.shader.name.StartsWith("Sprites") && !m.shader.name.StartsWith("UI/"))
                            mats.Add(m);
            foreach (var m in mats)
            {
                Undo.RecordObject(m, "Convert to URP");
                Texture main = m.HasProperty("_MainTex") ? m.GetTexture("_MainTex") : null;
                Color color = m.HasProperty("_Color") ? m.GetColor("_Color") : Color.white;
                Texture normal = m.HasProperty("_BumpMap") ? m.GetTexture("_BumpMap") : null;
                float metallic = m.HasProperty("_Metallic") ? m.GetFloat("_Metallic") : 0f;
                float smooth = m.HasProperty("_Glossiness") ? m.GetFloat("_Glossiness") : 0.3f;
                m.shader = lit;
                m.SetTexture("_BaseMap", main);
                m.SetColor("_BaseColor", color);
                if (normal != null) { m.SetTexture("_BumpMap", normal); m.EnableKeyword("_NORMALMAP"); }
                m.SetFloat("_Metallic", metallic);
                m.SetFloat("_Smoothness", smooth);
                EditorUtility.SetDirty(m);
            }
            Report($"Converted {mats.Count} material(s) to URP Lit." + (mats.Count == 0 ? " (Nothing to convert: select the pink model first.)" : ""));
        }

        [MenuItem(Menu + "Prepare as static prop (colliders + Static)", true)]
        [MenuItem(Menu + "Make walkable (floor for teleport and walking)", true)]
        [MenuItem(Menu + "Fix pink materials (convert to URP)", true)]
        private static bool HasSelection() => Selection.gameObjects.Length > 0;

        private static void Report(string message)
        {
            Debug.Log("[3DUI] " + message);
            EditorUtility.DisplayDialog("3DUI", message, "OK");
        }
    }
}
