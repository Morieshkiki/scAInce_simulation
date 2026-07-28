using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Generates a pool of triplanar facade materials from the imported texture packs
/// and randomly (but deterministically, seeded per building name) assigns them to
/// every building under the "BakedBuildings" root. Walls and roofs are detected by
/// renderer name. Re-running the menu item always produces the same assignment;
/// change the Seed constant to reshuffle.
/// </summary>
public static class BuildingFacadeRandomizer
{
    const string RootObjectName = "BakedBuildings";
    const string ShaderName = "Custom/TriplanarFacade";
    const string BaseFolder = "Assets/BuildingRandomTextures";
    const string MatFolder = BaseFolder + "/Materials";
    const string TexRoot = "Assets/YughuesFreeArchitecturalMaterials/Textures/";
    const int Seed = 20260709;

    struct Variant
    {
        public string name;      // material asset name
        public string texture;   // texture file name (without folder)
        public Color tint;
        public float worldScale; // meters covered by one texture tile

        public Variant(string name, string texture, Color tint, float worldScale)
        {
            this.name = name;
            this.texture = texture;
            this.tint = tint;
            this.worldScale = worldScale;
        }
    }

    static readonly Variant[] WallVariants =
    {
        new Variant("Wall_BrickWeathered", "T_YFAM_BricksWeathered_d.tga", Color.white, 3f),
        new Variant("Wall_BrickRedRough",  "T_YFAM_BricksRedRough_d.tga",  Color.white, 3f),
        new Variant("Wall_BrickRough",     "T_YFAM_BricksRough_d.tga",     Color.white, 3f),
        new Variant("Wall_BrickGray",      "T_YFAM_BricksGray_d.tga",      Color.white, 3f),
        new Variant("Wall_BrickRedSmooth", "T_YFAM_BricksRedSmooth_d.tga", Color.white, 3f),
        new Variant("Wall_BrickDark",      "T_YFAM_BricksRough_d.tga",     new Color(0.55f, 0.53f, 0.52f), 3f),
        // Plaster diffuse averages ~0.5 brightness, so tints run >1 to land at realistic
        // facade albedo (the shader multiplies, HDR values are fine).
        new Variant("Wall_PlasterWhite",   "T_YFAM_PlasterBoard_d.tga",    new Color(1.30f, 1.30f, 1.28f), 4f),
        new Variant("Wall_PlasterCream",   "T_YFAM_PlasterBoard_d.tga",    new Color(1.28f, 1.22f, 1.05f), 4f),
        new Variant("Wall_PlasterSand",    "T_YFAM_PlasterBoard_d.tga",    new Color(1.19f, 1.08f, 0.86f), 4f),
        new Variant("Wall_PlasterYellow",  "T_YFAM_PlasterBoard_d.tga",    new Color(1.26f, 1.13f, 0.70f), 4f),
        new Variant("Wall_PlasterSalmon",  "T_YFAM_PlasterBoard_d.tga",    new Color(1.24f, 0.97f, 0.81f), 4f),
        new Variant("Wall_PlasterRose",    "T_YFAM_PlasterBoard_d.tga",    new Color(1.22f, 1.03f, 1.03f), 4f),
        new Variant("Wall_PlasterGray",    "T_YFAM_PlasterBoard_d.tga",    new Color(1.05f, 1.05f, 1.05f), 4f),
        new Variant("Wall_PlasterBlue",    "T_YFAM_PlasterBoard_d.tga",    new Color(0.95f, 1.03f, 1.13f), 4f),
        new Variant("Wall_PlasterGreen",   "T_YFAM_PlasterBoard_d.tga",    new Color(1.05f, 1.15f, 0.97f), 4f),
        new Variant("Wall_Pebbledash",     "T_YFAM_LaqueredPebbles_d.tga", Color.white, 1.5f),
        new Variant("Wall_StoneGabion",    "T_YFAM_GabionWall_d.tga",      Color.white, 2f),
        new Variant("Wall_StoneMarble",    "T_YFAM_TilesMarble_d.tga",     Color.white, 1.5f),
        new Variant("Wall_TilesWorn",      "T_YFAM_TilesWorn_d.tga",       Color.white, 1.5f),
        new Variant("Wall_WoodSiding",     "T_YFAM_LaminatedWood_d.tga",   Color.white, 3f),
        new Variant("Wall_WoodPanels",     "T_YFAM_Plywood_d.tga",         new Color(0.85f, 0.75f, 0.60f), 3f),
    };

    static readonly Variant[] RoofVariants =
    {
        // Roof tile diffuse averages ~0.3 brightness, hence the strong tints.
        new Variant("Roof_TilesRed",   "T_YFAM_RoofTiles_d.tga", new Color(1.70f, 0.90f, 0.70f), 3f),
        new Variant("Roof_TilesGray",  "T_YFAM_RoofTiles_d.tga", new Color(1.50f, 1.50f, 1.55f), 3f),
        new Variant("Roof_TilesBrown", "T_YFAM_RoofTiles_d.tga", new Color(1.40f, 1.05f, 0.80f), 3f),
    };

    [MenuItem("Tools/Buildings/Randomize Facades")]
    public static void Randomize()
    {
        Shader shader = Shader.Find(ShaderName);
        if (shader == null)
        {
            Debug.LogError($"[BuildingFacadeRandomizer] Shader '{ShaderName}' not found. Did TriplanarFacade.shader compile?");
            return;
        }

        GameObject root = GameObject.Find(RootObjectName);
        if (root == null)
        {
            Debug.LogError($"[BuildingFacadeRandomizer] Root object '{RootObjectName}' not found in the scene.");
            return;
        }

        List<Material> wallMats = EnsureMaterials(WallVariants, shader);
        List<Material> roofMats = EnsureMaterials(RoofVariants, shader);
        if (wallMats.Count == 0)
        {
            Debug.LogError("[BuildingFacadeRandomizer] No wall materials could be created (textures missing?).");
            return;
        }

        int buildings = 0;
        int renderers = 0;
        foreach (Transform building in root.transform)
        {
            var rng = new System.Random(Fnv1a(building.name) ^ Seed);
            Material wallMat = wallMats[rng.Next(wallMats.Count)];
            Material roofMat = roofMats.Count > 0 ? roofMats[rng.Next(roofMats.Count)] : wallMat;

            foreach (MeshRenderer r in building.GetComponentsInChildren<MeshRenderer>(true))
            {
                bool isRoof = r.gameObject.name.ToLowerInvariant().Contains("roof");
                Material chosen = isRoof ? roofMat : wallMat;

                Undo.RecordObject(r, "Randomize Building Facades");
                Material[] mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                    mats[i] = chosen;
                r.sharedMaterials = mats;
                renderers++;
            }
            buildings++;
        }

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log($"[BuildingFacadeRandomizer] Assigned {wallMats.Count} wall + {roofMats.Count} roof material variants across {buildings} buildings ({renderers} renderers).");
    }

    static List<Material> EnsureMaterials(Variant[] variants, Shader shader)
    {
        if (!AssetDatabase.IsValidFolder(BaseFolder))
            AssetDatabase.CreateFolder("Assets", "BuildingRandomTextures");
        if (!AssetDatabase.IsValidFolder(MatFolder))
            AssetDatabase.CreateFolder(BaseFolder, "Materials");

        var result = new List<Material>();
        foreach (Variant v in variants)
        {
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(TexRoot + v.texture);
            if (tex == null)
            {
                Debug.LogWarning($"[BuildingFacadeRandomizer] Texture not found, skipping variant '{v.name}': {TexRoot + v.texture}");
                continue;
            }

            string path = $"{MatFolder}/{v.name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }

            mat.shader = shader;
            mat.SetTexture("_BaseMap", tex);
            mat.SetColor("_BaseColor", v.tint);
            mat.SetFloat("_WorldScale", v.worldScale);
            mat.SetFloat("_Windows", v.name.StartsWith("Roof_") ? 0f : 1f);
            EditorUtility.SetDirty(mat);
            result.Add(mat);
        }
        AssetDatabase.SaveAssets();
        return result;
    }

    static int Fnv1a(string s)
    {
        unchecked
        {
            int hash = (int)2166136261;
            foreach (char c in s)
                hash = (hash ^ c) * 16777619;
            return hash;
        }
    }
}
