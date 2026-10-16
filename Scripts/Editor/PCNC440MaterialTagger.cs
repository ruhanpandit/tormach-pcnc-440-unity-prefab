using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// PCNC 440 Material Tagger — Unity Editor Tool
/// 
/// HOW TO USE:
/// 1. Place this file in Assets/Editor/ (create the folder if needed)
/// 2. Select your PCNC 440 prefab instance in the Hierarchy
/// 3. Go to Tools → PCNC 440 Material Tagger
/// 4. Click through each body, tagging it with the right category
/// 5. When done, click "Apply All Materials" and you're set!
/// </summary>
public class PCNC440MaterialTagger : EditorWindow
{
    // --- Category definitions ---
    private enum BodyCategory
    {
        Untagged,
        GrayPanel,        // Light gray matte body panels & cabinet
        GlassDoor,        // Transparent polycarbonate enclosure
        DarkInterior,     // Dark interior machine area
        DecalLogo,        // Side panels with lettering/branding
        DarkTrim,         // Dark gray trim, handles, step stool
        MachineParts,     // Spindle, vise, internal mechanical parts
        Skip              // Ignore / not visible
    }

    private static readonly Dictionary<BodyCategory, Color> categoryColors = new Dictionary<BodyCategory, Color>
    {
        { BodyCategory.Untagged,      Color.white },
        { BodyCategory.GrayPanel,     new Color(0.69f, 0.69f, 0.69f) },
        { BodyCategory.GlassDoor,     new Color(0.85f, 0.92f, 1.0f, 0.3f) },
        { BodyCategory.DarkInterior,  new Color(0.25f, 0.25f, 0.28f) },
        { BodyCategory.DecalLogo,     new Color(0.55f, 0.55f, 0.58f) },
        { BodyCategory.DarkTrim,      new Color(0.15f, 0.15f, 0.15f) },
        { BodyCategory.MachineParts,  new Color(0.5f, 0.5f, 0.52f) },
        { BodyCategory.Skip,          Color.magenta },
    };

    // --- State ---
    private GameObject rootObject;
    private List<Transform> allBodies = new List<Transform>();
    private Dictionary<int, BodyCategory> tags = new Dictionary<int, BodyCategory>();
    private int currentIndex = 0;
    private Vector2 scrollPos;
    private bool materialsCreated = false;
    private string materialFolder = "Assets/PCNC440_Materials";

    [MenuItem("Tools/PCNC 440 Material Tagger")]
    public static void ShowWindow()
    {
        var win = GetWindow<PCNC440MaterialTagger>("PCNC 440 Tagger");
        win.minSize = new Vector2(340, 500);
    }

    private void OnGUI()
    {
        // --- Header ---
        GUILayout.Space(8);
        GUILayout.Label("PCNC 440 Material Tagger", EditorStyles.boldLabel);
        GUILayout.Label("Select your PCNC 440 root object, scan, then tag each body.", EditorStyles.wordWrappedLabel);
        GUILayout.Space(8);

        // --- Root object selection ---
        EditorGUI.BeginChangeCheck();
        rootObject = (GameObject)EditorGUILayout.ObjectField("Root Object", rootObject, typeof(GameObject), true);
        if (EditorGUI.EndChangeCheck())
        {
            allBodies.Clear();
            tags.Clear();
            currentIndex = 0;
        }

        GUILayout.Space(4);

        if (rootObject == null)
        {
            EditorGUILayout.HelpBox("Drag your PCNC 440 prefab (or instance) into the field above.", MessageType.Info);
            return;
        }

        // --- Scan button ---
        if (GUILayout.Button("Scan for Bodies", GUILayout.Height(30)))
        {
            ScanBodies();
        }

        if (allBodies.Count == 0)
        {
            EditorGUILayout.HelpBox("Click 'Scan for Bodies' to find all mesh children.", MessageType.Info);
            return;
        }

        GUILayout.Space(4);
        GUILayout.Label($"Found {allBodies.Count} bodies  |  Tagged: {tags.Count}  |  Remaining: {allBodies.Count - tags.Count}", EditorStyles.miniLabel);

        // --- Progress bar ---
        float progress = (float)tags.Count / allBodies.Count;
        EditorGUI.ProgressBar(EditorGUILayout.GetControlRect(false, 20), progress, $"{(progress * 100):F0}% tagged");
        GUILayout.Space(8);

        // --- Current body navigation ---
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("◀ Prev", GUILayout.Width(70)))
            NavigateBody(-1);

        GUILayout.FlexibleSpace();
        GUILayout.Label($"Body {currentIndex + 1} / {allBodies.Count}", EditorStyles.boldLabel);
        GUILayout.FlexibleSpace();

        if (GUILayout.Button("Next ▶", GUILayout.Width(70)))
            NavigateBody(1);
        EditorGUILayout.EndHorizontal();

        // --- Current body info ---
        if (currentIndex >= 0 && currentIndex < allBodies.Count)
        {
            var current = allBodies[currentIndex];
            GUILayout.Space(4);

            EditorGUILayout.BeginVertical("box");
            GUILayout.Label($"Name: {current.name}", EditorStyles.label);

            BodyCategory currentTag = BodyCategory.Untagged;
            tags.TryGetValue(currentIndex, out currentTag);
            GUILayout.Label($"Current tag: {currentTag}", EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();

            // --- Focus button ---
            if (GUILayout.Button("Focus in Scene View", GUILayout.Height(25)))
            {
                FocusBody(currentIndex);
            }

            GUILayout.Space(8);

            // --- Category buttons ---
            GUILayout.Label("Tag this body as:", EditorStyles.boldLabel);
            GUILayout.Space(4);

            // Two-column layout for buttons
            int col = 0;
            EditorGUILayout.BeginHorizontal();
            foreach (BodyCategory cat in System.Enum.GetValues(typeof(BodyCategory)))
            {
                if (cat == BodyCategory.Untagged) continue;

                var prevColor = GUI.backgroundColor;
                if (tags.ContainsKey(currentIndex) && tags[currentIndex] == cat)
                    GUI.backgroundColor = Color.green;
                else
                    GUI.backgroundColor = categoryColors[cat];

                string label = FormatCategoryName(cat);
                if (GUILayout.Button(label, GUILayout.Height(35), GUILayout.MinWidth(140)))
                {
                    tags[currentIndex] = cat;
                    // Auto-advance to next untagged
                    AdvanceToNextUntagged();
                }

                GUI.backgroundColor = prevColor;
                col++;
                if (col % 2 == 0)
                {
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        GUILayout.Space(12);

        // --- Jump to next untagged ---
        if (GUILayout.Button("Jump to Next Untagged", GUILayout.Height(25)))
        {
            AdvanceToNextUntagged();
        }

        // --- Quick actions ---
        GUILayout.Space(8);
        GUILayout.Label("Quick Actions", EditorStyles.boldLabel);

        if (GUILayout.Button("Tag ALL remaining as 'Gray Panel'", GUILayout.Height(25)))
        {
            for (int i = 0; i < allBodies.Count; i++)
            {
                if (!tags.ContainsKey(i))
                    tags[i] = BodyCategory.GrayPanel;
            }
        }

        if (GUILayout.Button("Clear All Tags", GUILayout.Height(25)))
        {
            if (EditorUtility.DisplayDialog("Clear Tags", "Are you sure you want to clear all tags?", "Yes", "Cancel"))
            {
                tags.Clear();
                currentIndex = 0;
            }
        }

        GUILayout.Space(12);

        // --- Apply materials ---
        GUI.backgroundColor = new Color(0.3f, 0.9f, 0.4f);
        if (GUILayout.Button("✓  Apply All Materials", GUILayout.Height(40)))
        {
            CreateAndApplyMaterials();
        }
        GUI.backgroundColor = Color.white;

        GUILayout.Space(4);
        EditorGUILayout.HelpBox(
            "Tip: You can tag the obvious glass/dark parts first, then use 'Tag ALL remaining as Gray Panel' for the rest. Most of the bodies are probably gray panels.",
            MessageType.Info);
    }

    // --- Helpers ---

    private void ScanBodies()
    {
        allBodies.Clear();
        tags.Clear();
        currentIndex = 0;

        var allTransforms = rootObject.GetComponentsInChildren<Transform>(true);
        foreach (var t in allTransforms)
        {
            if (t == rootObject.transform) continue;
            // Include anything with a MeshRenderer or MeshFilter
            if (t.GetComponent<MeshRenderer>() != null || t.GetComponent<MeshFilter>() != null)
            {
                allBodies.Add(t);
            }
        }

        if (allBodies.Count == 0)
        {
            // Fallback: just grab all children
            foreach (Transform child in rootObject.GetComponentsInChildren<Transform>(true))
            {
                if (child != rootObject.transform)
                    allBodies.Add(child);
            }
        }

        if (allBodies.Count > 0)
            FocusBody(0);

        Debug.Log($"[PCNC 440 Tagger] Found {allBodies.Count} bodies.");
    }

    private void FocusBody(int index)
    {
        if (index < 0 || index >= allBodies.Count) return;

        // Select and frame in scene
        Selection.activeGameObject = allBodies[index].gameObject;
        SceneView.lastActiveSceneView?.FrameSelected();

        // Ping in hierarchy
        EditorGUIUtility.PingObject(allBodies[index].gameObject);
    }

    private void NavigateBody(int direction)
    {
        currentIndex = Mathf.Clamp(currentIndex + direction, 0, allBodies.Count - 1);
        FocusBody(currentIndex);
    }

    private void AdvanceToNextUntagged()
    {
        // Search forward from current
        for (int i = currentIndex + 1; i < allBodies.Count; i++)
        {
            if (!tags.ContainsKey(i))
            {
                currentIndex = i;
                FocusBody(currentIndex);
                return;
            }
        }
        // Wrap around
        for (int i = 0; i < currentIndex; i++)
        {
            if (!tags.ContainsKey(i))
            {
                currentIndex = i;
                FocusBody(currentIndex);
                return;
            }
        }
        // All tagged
        EditorUtility.DisplayDialog("All Tagged!", "Every body has been tagged. Click 'Apply All Materials' to finish.", "OK");
    }

    private string FormatCategoryName(BodyCategory cat)
    {
        switch (cat)
        {
            case BodyCategory.GrayPanel:     return "Gray Panel";
            case BodyCategory.GlassDoor:     return "Glass Door";
            case BodyCategory.DarkInterior:  return "Dark Interior";
            case BodyCategory.DecalLogo:     return "Decal / Logo";
            case BodyCategory.DarkTrim:      return "Dark Trim";
            case BodyCategory.MachineParts:  return "Machine Parts";
            case BodyCategory.Skip:          return "Skip";
            default:                         return cat.ToString();
        }
    }

    private void CreateAndApplyMaterials()
    {
        // Create material folder
        if (!AssetDatabase.IsValidFolder(materialFolder))
        {
            AssetDatabase.CreateFolder("Assets", "PCNC440_Materials");
        }

        // Detect render pipeline
        bool isURP = UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline != null;
        string shaderName = isURP ? "Universal Render Pipeline/Lit" : "Standard";
        string transparentShader = isURP ? "Universal Render Pipeline/Lit" : "Standard";

        Dictionary<BodyCategory, Material> materials = new Dictionary<BodyCategory, Material>();

        // --- Gray Panel ---
        var grayPanel = CreateOrLoadMaterial("PCNC440_GrayPanel", shaderName);
        grayPanel.color = new Color(0.69f, 0.69f, 0.69f, 1f);
        SetMetallicSmoothness(grayPanel, 0.45f, 0.3f, isURP);
        materials[BodyCategory.GrayPanel] = grayPanel;

        // --- Glass Door (Transparent) ---
        var glass = CreateOrLoadMaterial("PCNC440_GlassDoor", transparentShader);
        if (isURP)
        {
            glass.SetFloat("_Surface", 1); // Transparent
            glass.SetFloat("_Blend", 0);   // Alpha
            glass.SetOverrideTag("RenderType", "Transparent");
            glass.renderQueue = 3000;
            glass.SetFloat("_DstBlend", 10);
            glass.SetFloat("_SrcBlend", 5);
            glass.SetFloat("_ZWrite", 0);
            glass.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        }
        else
        {
            glass.SetFloat("_Mode", 3); // Transparent
            glass.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            glass.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            glass.SetInt("_ZWrite", 0);
            glass.DisableKeyword("_ALPHATEST_ON");
            glass.EnableKeyword("_ALPHABLEND_ON");
            glass.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            glass.renderQueue = 3000;
        }
        glass.color = new Color(0.85f, 0.92f, 1.0f, 0.12f);
        SetMetallicSmoothness(glass, 0.1f, 0.88f, isURP);
        materials[BodyCategory.GlassDoor] = glass;

        // --- Dark Interior ---
        var darkInterior = CreateOrLoadMaterial("PCNC440_DarkInterior", shaderName);
        darkInterior.color = new Color(0.22f, 0.22f, 0.25f, 1f);
        SetMetallicSmoothness(darkInterior, 0.5f, 0.35f, isURP);
        materials[BodyCategory.DarkInterior] = darkInterior;

        // --- Decal / Logo panels ---
        var decal = CreateOrLoadMaterial("PCNC440_DecalPanel", shaderName);
        decal.color = new Color(0.52f, 0.52f, 0.55f, 1f);
        SetMetallicSmoothness(decal, 0.4f, 0.25f, isURP);
        materials[BodyCategory.DecalLogo] = decal;

        // --- Dark Trim ---
        var darkTrim = CreateOrLoadMaterial("PCNC440_DarkTrim", shaderName);
        darkTrim.color = new Color(0.12f, 0.12f, 0.12f, 1f);
        SetMetallicSmoothness(darkTrim, 0.3f, 0.2f, isURP);
        materials[BodyCategory.DarkTrim] = darkTrim;

        // --- Machine Parts ---
        var machineParts = CreateOrLoadMaterial("PCNC440_MachineParts", shaderName);
        machineParts.color = new Color(0.48f, 0.48f, 0.50f, 1f);
        SetMetallicSmoothness(machineParts, 0.7f, 0.5f, isURP);
        materials[BodyCategory.MachineParts] = machineParts;

        AssetDatabase.SaveAssets();

        // --- Apply to bodies ---
        int applied = 0;
        int skipped = 0;

        // Record undo
        Undo.RecordObject(rootObject, "Apply PCNC 440 Materials");

        for (int i = 0; i < allBodies.Count; i++)
        {
            if (!tags.ContainsKey(i)) continue;
            var cat = tags[i];
            if (cat == BodyCategory.Skip || cat == BodyCategory.Untagged) { skipped++; continue; }

            var renderer = allBodies[i].GetComponent<MeshRenderer>();
            if (renderer != null && materials.ContainsKey(cat))
            {
                Undo.RecordObject(renderer, "Assign PCNC material");
                // Apply to all material slots
                var mats = new Material[renderer.sharedMaterials.Length];
                for (int m = 0; m < mats.Length; m++)
                    mats[m] = materials[cat];
                renderer.sharedMaterials = mats;
                applied++;
            }
        }

        // Mark prefab dirty
        EditorUtility.SetDirty(rootObject);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Done!",
            $"Materials applied!\n\n" +
            $"• Applied: {applied} bodies\n" +
            $"• Skipped: {skipped} bodies\n" +
            $"• Untagged: {allBodies.Count - tags.Count} bodies\n\n" +
            $"Materials saved to:\n{materialFolder}/",
            "Nice!");

        Debug.Log($"[PCNC 440 Tagger] Applied materials to {applied} bodies. Materials in {materialFolder}/");
    }

    private Material CreateOrLoadMaterial(string name, string shaderName)
    {
        string path = $"{materialFolder}/{name}.mat";
        var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null) return existing;

        var mat = new Material(Shader.Find(shaderName));
        mat.name = name;
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    private void SetMetallicSmoothness(Material mat, float metallic, float smoothness, bool isURP)
    {
        if (isURP)
        {
            mat.SetFloat("_Metallic", metallic);
            mat.SetFloat("_Smoothness", smoothness);
        }
        else
        {
            mat.SetFloat("_Metallic", metallic);
            mat.SetFloat("_Glossiness", smoothness);
        }
    }
}
