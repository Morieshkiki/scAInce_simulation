using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Decorates the SUMO-derived road network (RoadNetworkRoot) with textured asphalt,
/// stone curbs, cobblestone sidewalks, a paved Luisenplatz plaza and street trees.
///
/// Pipeline (Tools/Road Beautifier):
///  1 - Apply Road Materials : converts the imported asset-pack materials to URP, creates
///      road/curb/sidewalk materials and remaps the FBX-embedded materials of RoadNetworkRoot.fbx.
///  2 - Generate Sidewalks + Curbs + Plaza : rasterizes the road meshes top-down, traces the
///      road outline and extrudes curb + sidewalk ribbons along every road edge (narrowing at
///      buildings). Inside the Luisenplatz polygon no ribbons are built - instead the whole
///      non-road surface is paved with the sidewalk material. Output is chunked into ~50 m
///      tiles so individual pieces stay editable. Tree spots are recorded (outside the plaza).
///  3 - Scatter Trees : places Tree9 + Lemon Tree prefabs on the recorded sidewalk spots.
///  4 - Split Building Meshes : splits merged multi-house building meshes into per-house
///      parts (roof islands define footprints; wall triangles follow the nearest roof).
/// </summary>
public static class RoadBeautifier
{
    // ---------------- Tunable parameters ----------------
    const float CellSize = 0.25f;          // raster resolution in meters
    const float GridPadding = 12f;         // meters of empty border around the network
    const float CurbWidth = 0.30f;         // horizontal width of the curb stone
    const float CurbHeight = 0.12f;        // curb / sidewalk elevation above the road
    const float SidewalkWidth = 2.0f;      // desired sidewalk width
    const float MinSidewalkWidth = 0.5f;   // below this the sidewalk plane is skipped (curb only)
    const float SimplifyEpsilon = 0.20f;   // Douglas-Peucker tolerance for the traced outline
    const float ResampleStep = 1.25f;      // max distance between ring vertices
    const int   CloseRadiusCells = 2;      // morphological closing radius (bridges < ~1 m gaps between lanes)
    const float PlazaY = 0.03f;            // plaza pavement height above the road level
    const float TileSize = 50f;            // generated meshes are chunked into tiles of this size
    const float TreeSpacingMin = 8f;       // min distance between trees along a sidewalk
    const float TreeSpacingMax = 14f;      // max distance between trees along a sidewalk
    const float TreeMinWidth = 1.4f;       // sidewalk must be at least this wide to host a tree
    const float TargetTreeHeight = 10f;    // Tree9 prefabs are scaled down to roughly this height
    const float Tree9Chance = 0.30f;       // fraction of tree spots that get a Tree9
    const float LemonChance = 0.30f;       // fraction of tree spots that get a Lemon tree (rest stay empty)
    const int   RandomSeed = 20260709;

    // Luisenplatz square outline in Unity/net coordinates (x, z). Derived from the OSM
    // pedestrian ways named "Luisenplatz" (convex hull, buffered ~6 m outward);
    // UTM32 - netOffset(-474100.46,-5524082.31) puts OSM directly into scene coordinates.
    static readonly Vector2[] PlazaPolygon = new Vector2[]
    {
        new Vector2(754.4f, 467.4f),
        new Vector2(759.8f, 441.4f),
        new Vector2(883.5f, 408.4f),
        new Vector2(897.7f, 513.6f),
        new Vector2(896.9f, 521.8f),
    };

    const string RoadRootName = "RoadNetworkRoot";
    const string FbxPath = "Assets/3d_model/RoadNetworkRoot.fbx";
    const string GenFolder = "Assets/3d_model/Generated";
    const string MatFolder = GenFolder + "/Materials";
    const string TreeFolder = GenFolder + "/TreePrefabs";
    const string MeshAssetPath = GenFolder + "/RoadDecorMeshes.asset";
    const string TreeSpotsPath = GenFolder + "/tree_spots.json";
    const string SidewalkRootName = "GeneratedSidewalks";
    const string TreesRootName = "GeneratedTrees";
    const string BuildingSplitFolder = "Assets/BakedBuildings/Split";

    const string TexAsphaltDiff = "Assets/3d_model/Textures/Asphalt1_Diff.png";
    const string TexAsphaltNorm = "Assets/3d_model/Textures/Asphalt1_Norm.png";
    const string TexCurbDiff = "Assets/Shaded Spectrum/Free Realistic Outdoor Materials/Textures/SmoothStone/SmoothStoneAlbedo.png";
    const string TexCurbNorm = "Assets/Shaded Spectrum/Free Realistic Outdoor Materials/Textures/SmoothStone/SmoothStoneNormalMap.png";
    const string TexWalkDiff = "Assets/3d_model/Textures/Cobblestone2_Diff.png";
    const string TexWalkNorm = "Assets/3d_model/Textures/Cobblestone2_Norm.png";

    // Tree prefabs supplied by the user. Tree9 prefabs are broken Tree-Creator exports and are
    // rebuilt as URP prefabs first (see EnsureTree9Prefabs); Lemon trees are used directly.
    static readonly string[] Tree9Sources =
    {
        "Assets/Tree9/Tree9_2.prefab", "Assets/Tree9/Tree9_3.prefab",
        "Assets/Tree9/Tree9_4.prefab", "Assets/Tree9/Tree9_5.prefab",
    };
    static readonly string[] LemonPrefabs =
    {
        "Assets/Numena/Plants/Lemon/Lemon Tree 3.prefab",
        "Assets/Numena/Plants/Lemon/Lemon Tree 4.prefab",
        "Assets/Numena/Plants/Lemon/Lemon Tree 1.prefab",
        "Assets/Numena/Plants/Lemon/Lemon Tree 2.prefab",
    };
    // Folders whose built-in-pipeline materials get converted to URP/Lit in step 1.
    static readonly string[] LegacyMaterialFolders =
    {
        "Assets/Shaded Spectrum", "Assets/Numena/Plants/Lemon", "Assets/Tree9",
    };

    // ---------------- Menu items ----------------

    [MenuItem("Tools/Road Beautifier/1 - Apply Road Materials")]
    public static void ApplyRoadMaterials()
    {
        EnsureFolders();
        ConvertLegacyMaterialsToURP();

        var roadMat = CreateOrUpdateMaterial(MatFolder + "/Road_Asphalt.mat", TexAsphaltDiff, TexAsphaltNorm,
            new Vector2(2.5f, 2.5f), 0.25f, Color.white);

        var importer = AssetImporter.GetAtPath(FbxPath) as ModelImporter;
        if (importer == null) { Debug.LogError("[RoadBeautifier] FBX importer not found at " + FbxPath); return; }

        var names = new HashSet<string> { "Asphalt", "Lit" };
        foreach (var m in AssetDatabase.LoadAllAssetsAtPath(FbxPath).OfType<Material>())
            names.Add(m.name);
        foreach (var n in names)
            importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), n), roadMat);
        importer.importNormals = ModelImporterNormals.Calculate;
        importer.SaveAndReimport();

        // Flat ground-level meshes must not cast shadows: the network contains coplanar
        // duplicate lane meshes that otherwise blacken each other via self-shadowing.
        var roadRoot = GameObject.Find(RoadRootName);
        if (roadRoot != null)
            foreach (var mr in roadRoot.GetComponentsInChildren<MeshRenderer>(true))
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        Debug.Log("[RoadBeautifier] Remapped FBX materials (" + string.Join(", ", names.ToArray()) + ") to Road_Asphalt.");
    }

    [MenuItem("Tools/Road Beautifier/2 - Generate Sidewalks + Curbs + Plaza")]
    public static void GenerateSidewalks()
    {
        var roadRoot = GameObject.Find(RoadRootName);
        if (roadRoot == null) { Debug.LogError("[RoadBeautifier] '" + RoadRootName + "' not found in scene."); return; }
        EnsureFolders();

        try
        {
            // ---- 1. Collect road triangles in world space (XZ projection) ----
            EditorUtility.DisplayProgressBar("Road Beautifier", "Collecting road geometry...", 0.02f);
            var roadTris = new List<Vector2[]>();
            float baseY = 0f; int ySamples = 0;
            foreach (var mf in roadRoot.GetComponentsInChildren<MeshFilter>())
            {
                var mesh = mf.sharedMesh; if (mesh == null) continue;
                var verts = mesh.vertices; var idx = mesh.triangles;
                var world = new Vector3[verts.Length];
                for (int i = 0; i < verts.Length; i++) { world[i] = mf.transform.TransformPoint(verts[i]); baseY += world[i].y; ySamples++; }
                for (int t = 0; t < idx.Length; t += 3)
                    roadTris.Add(new[] { XZ(world[idx[t]]), XZ(world[idx[t + 1]]), XZ(world[idx[t + 2]]) });
            }
            if (ySamples > 0) baseY /= ySamples;

            // ---- 2. Grid setup ----
            Vector2 min = new Vector2(float.MaxValue, float.MaxValue), max = new Vector2(float.MinValue, float.MinValue);
            foreach (var tri in roadTris) foreach (var p in tri) { min = Vector2.Min(min, p); max = Vector2.Max(max, p); }
            min -= Vector2.one * GridPadding; max += Vector2.one * GridPadding;
            var origin = min;
            int nx = Mathf.CeilToInt((max.x - min.x) / CellSize);
            int ny = Mathf.CeilToInt((max.y - min.y) / CellSize);
            Debug.Log("[RoadBeautifier] Grid " + nx + " x " + ny + " cells, road tris: " + roadTris.Count);

            // ---- 3. Rasterize roads, morphological closing ----
            EditorUtility.DisplayProgressBar("Road Beautifier", "Rasterizing roads...", 0.10f);
            var road = new bool[nx * (long)ny];
            foreach (var tri in roadTris) RasterizeTri(tri[0], tri[1], tri[2], road, nx, ny, origin);
            EditorUtility.DisplayProgressBar("Road Beautifier", "Closing lane gaps...", 0.25f);
            var closed = MorphClose(road, nx, ny, CloseRadiusCells);

            // ---- 4. Rasterize buildings into their own mask ----
            EditorUtility.DisplayProgressBar("Road Beautifier", "Rasterizing buildings...", 0.35f);
            var buildingMask = new bool[nx * (long)ny];
            int buildingRenderers = 0;
            foreach (var mr in UnityEngine.Object.FindObjectsOfType<MeshRenderer>())
            {
                if (!IsBuilding(mr.transform)) continue;
                var mf = mr.GetComponent<MeshFilter>(); if (mf == null || mf.sharedMesh == null) continue;
                buildingRenderers++;
                var mesh = mf.sharedMesh; var verts = mesh.vertices; var idx = mesh.triangles;
                var world = new Vector2[verts.Length];
                for (int i = 0; i < verts.Length; i++) world[i] = XZ(mf.transform.TransformPoint(verts[i]));
                for (int t = 0; t < idx.Length; t += 3)
                    RasterizeTri(world[idx[t]], world[idx[t + 1]], world[idx[t + 2]], buildingMask, nx, ny, origin);
            }
            buildingMask = Dilate(buildingMask, nx, ny, 1); // 0.25 m clearance from walls
            // Sidewalk ribbons may extend neither onto roads nor into buildings:
            var blocked = new bool[nx * (long)ny];
            for (long i = 0; i < blocked.LongLength; i++) blocked[i] = buildingMask[i] || closed[i];

            // ---- 5. Trace road outline contours ----
            EditorUtility.DisplayProgressBar("Road Beautifier", "Tracing road outline...", 0.55f);
            var loops = ExtractContours(closed, nx, ny);
            Debug.Log("[RoadBeautifier] Building renderers: " + buildingRenderers + ", contour loops: " + loops.Count);

            // ---- 6. Build geometry ----
            EditorUtility.DisplayProgressBar("Road Beautifier", "Building curbs and sidewalks...", 0.70f);
            ClearGeneratedObject(SidewalkRootName);
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(MeshAssetPath) != null) AssetDatabase.DeleteAsset(MeshAssetPath);

            EnsureNormalMap(TexCurbNorm);
            var curbMat = CreateOrUpdateMaterial(MatFolder + "/Curb_Stone.mat", TexCurbDiff, TexCurbNorm, new Vector2(0.5f, 0.5f), 0.12f, Color.white);
            var walkMat = CreateOrUpdateMaterial(MatFolder + "/Sidewalk_Cobble.mat", TexWalkDiff, TexWalkNorm, new Vector2(0.4f, 0.4f), 0.2f, Color.white);

            var sidewalkRoot = new GameObject(SidewalkRootName);
            var curbBuilder = new TiledMeshBuilder(sidewalkRoot.transform, "Curb", curbMat, MeshAssetPath);
            var walkBuilder = new TiledMeshBuilder(sidewalkRoot.transform, "Sidewalk", walkMat, MeshAssetPath);
            var plazaBuilder = new TiledMeshBuilder(sidewalkRoot.transform, "Plaza", walkMat, MeshAssetPath);
            // Physics uses its own smooth, welded geometry: the sharp curb step becomes a
            // ramp and the seams of the visual quad soup disappear, so vehicles get a
            // gentle real-world bump instead of jumping and shaking.
            var colBuilder = new TiledMeshBuilder(sidewalkRoot.transform, "Collision", null, MeshAssetPath)
            { addRenderer = false, addCollider = true, weldOnFlush = true };
            curbBuilder.addCollider = false;
            walkBuilder.addCollider = false;
            plazaBuilder.addCollider = false;

            // One flat collider at exact road level is the driving surface for roads AND
            // the plaza (the road meshes have no colliders; the old DriveGround collider
            // sits 2 cm below the visual road, and the plaza lip stays visual-only).
            var drivePlane = new GameObject("RoadCollisionPlane");
            drivePlane.transform.SetParent(sidewalkRoot.transform, false);
            var driveBox = drivePlane.AddComponent<BoxCollider>();
            driveBox.center = new Vector3((min.x + max.x) * 0.5f, baseY - 0.5f, (min.y + max.y) * 0.5f);
            driveBox.size = new Vector3(max.x - min.x, 1f, max.y - min.y);
            drivePlane.isStatic = true;
            var treeSpots = new List<Vector3>();
            var rng = new System.Random(RandomSeed);

            int loopIdx = 0;
            foreach (var rawLoop in loops)
            {
                loopIdx++;
                if (rawLoop.Count < 16) continue; // noise
                var loop = SimplifyClosed(ToWorld(rawLoop, origin), SimplifyEpsilon);
                if (loop.Count < 3) continue;
                loop = ResampleClosed(loop, ResampleStep);
                int n = loop.Count;
                if (n < 3) continue;

                // Outward normals (right of travel direction; road region is on the left).
                var normals = new Vector2[n];
                for (int i = 0; i < n; i++)
                {
                    var d = (loop[(i + 1) % n] - loop[(i - 1 + n) % n]).normalized;
                    normals[i] = new Vector2(d.y, -d.x);
                }
                // Safety: verify orientation empirically (sample a few points toward 'inside').
                int inside = 0, checks = 0;
                for (int i = 0; i < n; i += Mathf.Max(1, n / 24))
                {
                    var probe = loop[i] + normals[i] * 0.45f;
                    if (Sample(closed, nx, ny, origin, probe)) inside++;
                    checks++;
                }
                if (inside > checks / 2) for (int i = 0; i < n; i++) normals[i] = -normals[i];

                // Per-vertex available sidewalk width (limited by buildings / other roads).
                var width = new float[n];
                for (int i = 0; i < n; i++)
                {
                    float ok = 0f;
                    for (float off = CurbWidth + CellSize; off <= CurbWidth + SidewalkWidth + 0.001f; off += CellSize)
                    {
                        if (Sample(blocked, nx, ny, origin, loop[i] + normals[i] * off)) break;
                        ok = off - CurbWidth;
                    }
                    width[i] = ok;
                }
                // Smooth widths to avoid a jagged outer edge.
                for (int pass = 0; pass < 2; pass++)
                {
                    var w2 = new float[n];
                    for (int i = 0; i < n; i++) w2[i] = (width[(i - 1 + n) % n] + width[i] + width[(i + 1) % n]) / 3f;
                    width = w2;
                }
                var inPlaza = new bool[n];
                for (int i = 0; i < n; i++) inPlaza[i] = InPlaza(loop[i]);

                // Emit ring geometry.
                float arc = 0f;
                float nextTree = Mathf.Lerp(TreeSpacingMin, TreeSpacingMax, (float)rng.NextDouble());
                for (int i = 0; i < n; i++)
                {
                    int j = (i + 1) % n;
                    float segLen = (loop[j] - loop[i]).magnitude;
                    float arcJ = arc + segLen;

                    Vector3 Ai = V3(loop[i], baseY), Aj = V3(loop[j], baseY);
                    Vector3 Bi = Ai + Vector3.up * CurbHeight, Bj = Aj + Vector3.up * CurbHeight;
                    Vector3 Ci = Bi + V3(normals[i], 0) * CurbWidth, Cj = Bj + V3(normals[j], 0) * CurbWidth;
                    Vector3 Di = Ci + V3(normals[i], 0) * Mathf.Max(width[i], 0.05f);
                    Vector3 Dj = Cj + V3(normals[j], 0) * Mathf.Max(width[j], 0.05f);

                    if (inPlaza[i] || inPlaza[j])
                    {
                        // No ribbons inside Luisenplatz. Close the profile with an end cap at
                        // the transition so the strip does not end in an open cross-section.
                        if (inPlaza[i] != inPlaza[j])
                        {
                            bool capAtI = inPlaza[j]; // cap on the outside vertex
                            Vector3 a = capAtI ? Ai : Aj, b = capAtI ? Bi : Bj, c = capAtI ? Ci : Cj, d = capAtI ? Di : Dj;
                            var tangent = V3((loop[j] - loop[i]).normalized, 0) * (capAtI ? -1f : 1f);
                            curbBuilder.AddQuad(a, b, c, d,
                                new Vector2(0, 0), new Vector2(0, CurbHeight), new Vector2(CurbWidth, CurbHeight), new Vector2(CurbWidth + 2f, CurbHeight),
                                tangent);
                        }
                        arc = arcJ;
                        continue;
                    }

                    // curb vertical face
                    curbBuilder.AddQuad(Ai, Aj, Bj, Bi,
                        new Vector2(arc, 0), new Vector2(arcJ, 0), new Vector2(arcJ, CurbHeight), new Vector2(arc, CurbHeight),
                        V3(normals[i] + normals[j], 0));
                    // curb top
                    curbBuilder.AddQuad(Bi, Bj, Cj, Ci,
                        new Vector2(arc, CurbHeight), new Vector2(arcJ, CurbHeight), new Vector2(arcJ, CurbHeight + CurbWidth), new Vector2(arc, CurbHeight + CurbWidth),
                        Vector3.up);
                    // collision: ramp from road level straight up to the curb's outer top edge
                    colBuilder.AddQuad(Ai, Aj, Cj, Ci,
                        Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, Vector3.up);
                    // sidewalk plane
                    if (width[i] > MinSidewalkWidth || width[j] > MinSidewalkWidth)
                    {
                        walkBuilder.AddQuad(Ci, Cj, Dj, Di,
                            new Vector2(arc, 0), new Vector2(arcJ, 0), new Vector2(arcJ, width[j]), new Vector2(arc, width[i]),
                            Vector3.up);
                    }
                    // collision walk plane (also where only a narrow curb strip exists)
                    colBuilder.AddQuad(Ci, Cj, Dj, Di,
                        Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, Vector3.up);

                    // tree spots
                    if (arcJ >= nextTree)
                    {
                        if (width[i] >= TreeMinWidth)
                        {
                            var pos = loop[i] + normals[i] * (CurbWidth + width[i] * 0.55f);
                            if (!InPlaza(pos))
                                treeSpots.Add(new Vector3(pos.x, baseY + CurbHeight, pos.y));
                        }
                        nextTree = arcJ + Mathf.Lerp(TreeSpacingMin, TreeSpacingMax, (float)rng.NextDouble());
                    }
                    arc = arcJ;
                }
                if (loopIdx % 10 == 0)
                    EditorUtility.DisplayProgressBar("Road Beautifier", "Building curbs and sidewalks... (" + loopIdx + "/" + loops.Count + ")", 0.70f + 0.15f * loopIdx / loops.Count);
            }

            // ---- 7. Pave the Luisenplatz plaza (everything in the polygon that is not road/building) ----
            EditorUtility.DisplayProgressBar("Road Beautifier", "Paving Luisenplatz...", 0.90f);
            Vector2 pMin = PlazaPolygon[0], pMax = PlazaPolygon[0];
            foreach (var p in PlazaPolygon) { pMin = Vector2.Min(pMin, p); pMax = Vector2.Max(pMax, p); }
            int py0 = Mathf.Max(0, Mathf.FloorToInt((pMin.y - origin.y) / CellSize));
            int py1 = Mathf.Min(ny - 1, Mathf.CeilToInt((pMax.y - origin.y) / CellSize));
            int px0 = Mathf.Max(0, Mathf.FloorToInt((pMin.x - origin.x) / CellSize));
            int px1 = Mathf.Min(nx - 1, Mathf.CeilToInt((pMax.x - origin.x) / CellSize));
            int plazaCells = 0;
            for (int y = py0; y <= py1; y++)
            {
                int runStart = -1;
                for (int x = px0; x <= px1 + 1; x++)
                {
                    bool val = false;
                    if (x <= px1)
                    {
                        long ii = (long)y * nx + x;
                        var wp = new Vector2(origin.x + (x + 0.5f) * CellSize, origin.y + (y + 0.5f) * CellSize);
                        val = !closed[ii] && !buildingMask[ii] && InPlaza(wp);
                    }
                    if (val && runStart < 0) runStart = x;
                    if (!val && runStart >= 0)
                    {
                        float wx0 = origin.x + runStart * CellSize, wx1 = origin.x + x * CellSize;
                        float wz0 = origin.y + y * CellSize, wz1 = origin.y + (y + 1) * CellSize;
                        plazaBuilder.AddQuad(
                            new Vector3(wx0, baseY + PlazaY, wz0), new Vector3(wx1, baseY + PlazaY, wz0),
                            new Vector3(wx1, baseY + PlazaY, wz1), new Vector3(wx0, baseY + PlazaY, wz1),
                            new Vector2(wx0, wz0), new Vector2(wx1, wz0), new Vector2(wx1, wz1), new Vector2(wx0, wz1),
                            Vector3.up);
                        plazaCells += x - runStart;
                        runStart = -1;
                    }
                }
            }

            curbBuilder.Flush();
            walkBuilder.Flush();
            plazaBuilder.Flush();
            colBuilder.Flush();
            AssetDatabase.SaveAssets();

            File.WriteAllText(TreeSpotsPath, JsonUtility.ToJson(new TreeSpotData { spots = treeSpots }));
            AssetDatabase.ImportAsset(TreeSpotsPath);
            Debug.Log("[RoadBeautifier] Done. Curb tiles: " + curbBuilder.ChunkCount + ", sidewalk tiles: " + walkBuilder.ChunkCount
                + ", plaza tiles: " + plazaBuilder.ChunkCount + " (" + (plazaCells * CellSize * CellSize).ToString("F0") + " m2), tree spots: " + treeSpots.Count);
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }

    [MenuItem("Tools/Road Beautifier/3 - Scatter Trees")]
    public static void ScatterTrees()
    {
        if (!File.Exists(TreeSpotsPath)) { Debug.LogError("[RoadBeautifier] No tree spots found - run step 2 first."); return; }
        var data = JsonUtility.FromJson<TreeSpotData>(File.ReadAllText(TreeSpotsPath));
        if (data == null || data.spots == null || data.spots.Count == 0) { Debug.LogError("[RoadBeautifier] Tree spot file is empty."); return; }

        var big = new List<GameObject>();   // preferred street trees (Tree9)
        var small = new List<GameObject>(); // accents (Lemon trees)
        foreach (var p in EnsureTree9Prefabs()) big.Add(p);
        foreach (var p in LemonPrefabs)
        {
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(p);
            if (go != null) small.Add(go);
        }
        if (big.Count == 0 && small.Count == 0) { Debug.LogError("[RoadBeautifier] No tree prefabs available."); return; }

        ClearGeneratedObject(TreesRootName);
        var root = new GameObject(TreesRootName);
        var rng = new System.Random(RandomSeed + 1);
        int placed = 0, skippedPlaza = 0;
        foreach (var spot in data.spots)
        {
            if (InPlaza(new Vector2(spot.x, spot.z))) { skippedPlaza++; continue; }
            GameObject prefab;
            double roll = rng.NextDouble();
            if (roll < Tree9Chance && big.Count > 0)
                prefab = big[rng.Next(big.Count)];
            else if (roll < Tree9Chance + LemonChance && small.Count > 0)
                prefab = small[rng.Next(small.Count)];
            else
                continue; // leave this spot empty to keep the streets from feeling overgrown
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, root.transform);
            go.transform.position = spot;
            go.transform.rotation = Quaternion.Euler(0, (float)rng.NextDouble() * 360f, 0);
            go.transform.localScale = prefab.transform.localScale * Mathf.Lerp(0.85f, 1.2f, (float)rng.NextDouble());
            placed++;
        }
        Debug.Log("[RoadBeautifier] Placed " + placed + " trees (" + big.Count + " Tree9 + " + small.Count + " Lemon variants), skipped in plaza: " + skippedPlaza);
    }

    [MenuItem("Tools/Road Beautifier/4 - Split Building Meshes")]
    public static void SplitBuildings()
    {
        GameObject baked = null;
        foreach (var r in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
            if (r.name == "BakedBuildings") baked = r;
        if (baked == null) { Debug.LogError("[RoadBeautifier] BakedBuildings not found."); return; }
        if (!AssetDatabase.IsValidFolder(BuildingSplitFolder))
            AssetDatabase.CreateFolder("Assets/BakedBuildings", "Split");

        int splitCount = 0, skipped = 0, partsTotal = 0;
        var children = new List<Transform>();
        foreach (Transform b in baked.transform) children.Add(b);

        try
        {
            AssetDatabase.StartAssetEditing();
            for (int bi = 0; bi < children.Count; bi++)
            {
                var b = children[bi];
                if (bi % 25 == 0)
                    EditorUtility.DisplayProgressBar("Road Beautifier", "Splitting buildings... " + bi + "/" + children.Count, (float)bi / children.Count);
                if (b.Find("Part_0") != null) { skipped++; continue; } // already split

                var wallsT = b.Find("Walls");
                var roofT = b.Find("Roof");
                var roofMf = roofT != null ? roofT.GetComponent<MeshFilter>() : null;
                var wallsMf = wallsT != null ? wallsT.GetComponent<MeshFilter>() : null;
                if (roofMf == null || roofMf.sharedMesh == null) { skipped++; continue; }

                int[] triComp;
                int nComp = TriangleIslands(roofMf.sharedMesh, out triComp);
                if (nComp <= 1) { skipped++; continue; } // single building already

                // Roof island bounds in world XZ.
                var roofVerts = roofMf.sharedMesh.vertices;
                var roofTris = roofMf.sharedMesh.triangles;
                var rects = new Rect[nComp];
                var rectInit = new bool[nComp];
                for (int t = 0; t < roofTris.Length; t += 3)
                {
                    int comp = triComp[t / 3];
                    for (int k = 0; k < 3; k++)
                    {
                        var w = roofT.TransformPoint(roofVerts[roofTris[t + k]]);
                        if (!rectInit[comp]) { rects[comp] = new Rect(w.x, w.z, 0, 0); rectInit[comp] = true; }
                        else
                        {
                            var r2 = rects[comp];
                            r2.xMin = Mathf.Min(r2.xMin, w.x); r2.xMax = Mathf.Max(r2.xMax, w.x);
                            r2.yMin = Mathf.Min(r2.yMin, w.z); r2.yMax = Mathf.Max(r2.yMax, w.z);
                            rects[comp] = r2;
                        }
                    }
                }

                // Partition roof triangles by island; wall triangles by nearest roof island rect.
                var roofParts = new List<int>[nComp];
                for (int c = 0; c < nComp; c++) roofParts[c] = new List<int>();
                for (int t = 0; t < roofTris.Length / 3; t++) roofParts[triComp[t]].Add(t);

                List<int>[] wallParts = null;
                if (wallsMf != null && wallsMf.sharedMesh != null)
                {
                    wallParts = new List<int>[nComp];
                    for (int c = 0; c < nComp; c++) wallParts[c] = new List<int>();
                    var wv = wallsMf.sharedMesh.vertices;
                    var wt = wallsMf.sharedMesh.triangles;
                    for (int t = 0; t < wt.Length; t += 3)
                    {
                        var cen = (wallsT.TransformPoint(wv[wt[t]]) + wallsT.TransformPoint(wv[wt[t + 1]]) + wallsT.TransformPoint(wv[wt[t + 2]])) / 3f;
                        int best = 0; float bestD = float.MaxValue;
                        for (int c = 0; c < nComp; c++)
                        {
                            float d = RectDistance(rects[c], cen.x, cen.z);
                            if (d < bestD) { bestD = d; best = c; }
                        }
                        wallParts[best].Add(t / 3);
                    }
                }

                // Build the part GameObjects and meshes.
                string assetPath = BuildingSplitFolder + "/" + b.name + ".asset";
                if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath) != null) AssetDatabase.DeleteAsset(assetPath);
                bool assetCreated = false;
                for (int c = 0; c < nComp; c++)
                {
                    var part = new GameObject("Part_" + c);
                    part.transform.SetParent(b, false);
                    if (roofParts[c].Count > 0)
                        assetCreated = AddPartPiece(part.transform, "Roof", roofMf, roofParts[c], assetPath, assetCreated);
                    if (wallParts != null && wallParts[c].Count > 0)
                        assetCreated = AddPartPiece(part.transform, "Walls", wallsMf, wallParts[c], assetPath, assetCreated);
                    partsTotal++;
                }
                if (wallsT != null) UnityEngine.Object.DestroyImmediate(wallsT.gameObject);
                if (roofT != null) UnityEngine.Object.DestroyImmediate(roofT.gameObject);
                splitCount++;
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
            EditorUtility.ClearProgressBar();
        }
        AssetDatabase.SaveAssets();
        Debug.Log("[RoadBeautifier] Split " + splitCount + " merged buildings into " + partsTotal + " parts (" + skipped + " already fine/skipped). Delete individual houses via BakedBuildings/Building_x_y/Part_n.");
    }

    [MenuItem("Tools/Road Beautifier/Run All (1+2+3)")]
    public static void RunAll()
    {
        ApplyRoadMaterials();
        GenerateSidewalks();
        ScatterTrees();
    }

    [MenuItem("Tools/Road Beautifier/Clear Generated")]
    public static void ClearGenerated()
    {
        ClearGeneratedObject(SidewalkRootName);
        ClearGeneratedObject(TreesRootName);
        Debug.Log("[RoadBeautifier] Cleared generated objects (assets kept in " + GenFolder + ").");
    }

    // ---------------- Building split helpers ----------------

    static bool AddPartPiece(Transform parent, string label, MeshFilter srcMf, List<int> triIndices, string assetPath, bool assetCreated)
    {
        var src = srcMf.sharedMesh;
        var sv = src.vertices; var sn = src.normals; var su = src.uv; var sc = src.colors32; var st = src.triangles;
        var map = new Dictionary<int, int>();
        var nv = new List<Vector3>(); var nn = new List<Vector3>(); var nu = new List<Vector2>(); var nc = new List<Color32>();
        var nt = new List<int>();
        bool hasN = sn != null && sn.Length == sv.Length;
        bool hasU = su != null && su.Length == sv.Length;
        bool hasC = sc != null && sc.Length == sv.Length;
        foreach (var tri in triIndices)
        {
            for (int k = 0; k < 3; k++)
            {
                int oi = st[tri * 3 + k];
                int niIdx;
                if (!map.TryGetValue(oi, out niIdx))
                {
                    niIdx = nv.Count;
                    map[oi] = niIdx;
                    nv.Add(sv[oi]);
                    if (hasN) nn.Add(sn[oi]);
                    if (hasU) nu.Add(su[oi]);
                    if (hasC) nc.Add(sc[oi]);
                }
                nt.Add(niIdx);
            }
        }
        var mesh = new Mesh();
        mesh.name = parent.name + "_" + label;
        mesh.SetVertices(nv);
        if (hasN) mesh.SetNormals(nn);
        if (hasU) mesh.SetUVs(0, nu);
        if (hasC) mesh.SetColors(nc);
        mesh.SetTriangles(nt, 0);
        if (!hasN) mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        if (!assetCreated) AssetDatabase.CreateAsset(mesh, assetPath);
        else AssetDatabase.AddObjectToAsset(mesh, assetPath);

        var go = new GameObject(label);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = srcMf.transform.localPosition;
        go.transform.localRotation = srcMf.transform.localRotation;
        go.transform.localScale = srcMf.transform.localScale;
        var mf = go.AddComponent<MeshFilter>(); mf.sharedMesh = mesh;
        var mr = go.AddComponent<MeshRenderer>();
        var srcMr = srcMf.GetComponent<MeshRenderer>();
        if (srcMr != null)
        {
            mr.sharedMaterials = srcMr.sharedMaterials;
            mr.shadowCastingMode = srcMr.shadowCastingMode;
        }
        GameObjectUtility.SetStaticEditorFlags(go, GameObjectUtility.GetStaticEditorFlags(srcMf.gameObject));
        go.layer = srcMf.gameObject.layer;
        return true;
    }

    static float RectDistance(Rect r, float x, float z)
    {
        float dx = Mathf.Max(0, Mathf.Max(r.xMin - x, x - r.xMax));
        float dz = Mathf.Max(0, Mathf.Max(r.yMin - z, z - r.yMax));
        return dx * dx + dz * dz;
    }

    /// Connected components over triangles (vertices welded by position, 1 cm grid).
    static int TriangleIslands(Mesh m, out int[] triComp)
    {
        var v = m.vertices; int n = v.Length;
        var parent = new int[n];
        for (int i = 0; i < n; i++) parent[i] = i;
        Func<int, int> find = null;
        find = (x) => { while (parent[x] != x) { parent[x] = parent[parent[x]]; x = parent[x]; } return x; };
        var map = new Dictionary<Vector3Int, int>();
        for (int i = 0; i < n; i++)
        {
            var k = new Vector3Int(Mathf.RoundToInt(v[i].x * 100), Mathf.RoundToInt(v[i].y * 100), Mathf.RoundToInt(v[i].z * 100));
            int w;
            if (map.TryGetValue(k, out w)) { int a = find(i), b = find(w); if (a != b) parent[a] = b; }
            else map[k] = i;
        }
        var t = m.triangles;
        for (int i = 0; i < t.Length; i += 3)
        {
            int a = find(t[i]), b = find(t[i + 1]);
            if (a != b) parent[a] = b;
            a = find(t[i]); int c = find(t[i + 2]);
            if (a != c) parent[a] = c;
        }
        var compIds = new Dictionary<int, int>();
        triComp = new int[t.Length / 3];
        for (int i = 0; i < t.Length; i += 3)
        {
            int rootId = find(t[i]);
            int id;
            if (!compIds.TryGetValue(rootId, out id)) { id = compIds.Count; compIds[rootId] = id; }
            triComp[i / 3] = id;
        }
        return compIds.Count;
    }

    // ---------------- Material helpers ----------------

    /// Converts Standard / Legacy / Mobile shader materials of the imported asset packs to URP/Lit.
    static void ConvertLegacyMaterialsToURP()
    {
        var urpLit = Shader.Find("Universal Render Pipeline/Lit");
        int converted = 0;
        foreach (var folder in LegacyMaterialFolders)
        {
            if (!AssetDatabase.IsValidFolder(folder)) continue;
            foreach (var guid in AssetDatabase.FindAssets("t:Material", new[] { folder }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var m = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (m == null || m.shader == null) continue;
                var sName = m.shader.name;
                if (sName.StartsWith("Universal Render Pipeline")) continue;
                bool legacy = sName == "Standard" || sName.StartsWith("Legacy") || sName.StartsWith("Mobile") || sName.StartsWith("Nature");
                if (!legacy) continue;

                bool cutout = sName.Contains("Cutout") || (m.HasProperty("_Mode") && Mathf.Approximately(m.GetFloat("_Mode"), 1f));
                var mainTex = m.HasProperty("_MainTex") ? m.GetTexture("_MainTex") : null;
                var bumpTex = m.HasProperty("_BumpMap") ? m.GetTexture("_BumpMap") : null;
                var color = m.HasProperty("_Color") ? m.GetColor("_Color") : Color.white;
                float cutoff = m.HasProperty("_Cutoff") ? m.GetFloat("_Cutoff") : 0.4f;
                var scale = m.HasProperty("_MainTex") ? m.GetTextureScale("_MainTex") : Vector2.one;

                m.shader = urpLit;
                m.SetTexture("_BaseMap", mainTex);
                m.SetColor("_BaseColor", color);
                m.SetTextureScale("_BaseMap", scale);
                if (bumpTex != null)
                {
                    EnsureNormalMap(AssetDatabase.GetAssetPath(bumpTex));
                    m.SetTexture("_BumpMap", bumpTex);
                    m.EnableKeyword("_NORMALMAP");
                }
                m.SetFloat("_Smoothness", 0.15f);
                if (cutout)
                {
                    m.SetFloat("_AlphaClip", 1f);
                    m.EnableKeyword("_ALPHATEST_ON");
                    m.SetFloat("_Cutoff", cutoff);
                    m.SetFloat("_Cull", 0f); // leaves render double-sided
                    m.renderQueue = 2450;
                }
                EditorUtility.SetDirty(m);
                converted++;
            }
        }
        if (converted > 0) AssetDatabase.SaveAssets();
        Debug.Log("[RoadBeautifier] Converted " + converted + " built-in-pipeline materials to URP/Lit.");
    }

    /// Rebuilds the broken Tree9 Tree-Creator prefabs as plain URP mesh prefabs.
    static List<GameObject> EnsureTree9Prefabs()
    {
        EnsureFolders();
        var result = new List<GameObject>();
        foreach (var srcPath in Tree9Sources)
        {
            var name = Path.GetFileNameWithoutExtension(srcPath);
            var outPath = TreeFolder + "/" + name + "_URP.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(outPath);
            if (existing != null) { result.Add(existing); continue; }

            Mesh mesh = null; MeshRenderer srcMr = null;
            foreach (var a in AssetDatabase.LoadAllAssetsAtPath(srcPath))
            {
                if (a is Mesh) mesh = (Mesh)a;
                if (a is MeshRenderer) srcMr = (MeshRenderer)a;
            }
            if (mesh == null) { Debug.LogWarning("[RoadBeautifier] No baked mesh inside " + srcPath + " - skipped."); continue; }

            // Build URP materials from the Tree-Creator atlas. The optimized materials use
            // hidden Nature shaders, so read their textures via SerializedObject (GetTexture
            // fails on hidden-shader properties); everything maps onto one diffuse atlas.
            var mats = new Material[mesh.subMeshCount];
            var srcMats = srcMr != null ? srcMr.sharedMaterials : new Material[0];
            for (int s = 0; s < mesh.subMeshCount; s++)
            {
                var srcMat = s < srcMats.Length ? srcMats[s] : null;
                bool isLeaf = srcMat != null && srcMat.name.ToLowerInvariant().Contains("leaf");
                Texture tex = FindSerializedTexture(srcMat, "_MainTex");
                if (tex == null) tex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Tree9/Tree9_1_Textures/diffuse.png");
                string matPath = MatFolder + "/" + name + (isLeaf ? "_Leaf" : "_Bark") + ".mat";
                var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                if (mat == null)
                {
                    mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    AssetDatabase.CreateAsset(mat, matPath);
                }
                mat.SetTexture("_BaseMap", tex);
                mat.SetColor("_BaseColor", Color.white);
                mat.SetFloat("_Smoothness", 0.1f);
                if (isLeaf)
                {
                    mat.SetFloat("_AlphaClip", 1f);
                    mat.EnableKeyword("_ALPHATEST_ON");
                    mat.SetFloat("_Cutoff", 0.33f);
                    mat.SetFloat("_Cull", 0f);
                    mat.renderQueue = 2450;
                }
                EditorUtility.SetDirty(mat);
                mats[s] = mat;
            }

            var go = new GameObject(name + "_URP");
            try
            {
                var mf = go.AddComponent<MeshFilter>(); mf.sharedMesh = mesh;
                var mr = go.AddComponent<MeshRenderer>(); mr.sharedMaterials = mats;
                var col = go.AddComponent<CapsuleCollider>();
                float h = Mathf.Max(2f, mesh.bounds.size.y);
                col.center = new Vector3(0, h * 0.25f, 0);
                col.height = h * 0.5f;
                col.radius = 0.25f / Mathf.Max(0.1f, TargetTreeHeight / h);
                // Tree-Creator trees are 20+ m tall; scale them down to street-tree size.
                go.transform.localScale = Vector3.one * (TargetTreeHeight / h);
                var prefab = PrefabUtility.SaveAsPrefabAsset(go, outPath);
                result.Add(prefab);
                Debug.Log("[RoadBeautifier] Rebuilt " + name + " as URP prefab (source height " + h.ToString("F1") + " m, scaled to " + TargetTreeHeight + " m).");
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }
        return result;
    }

    /// Reads a texture reference straight from the serialized material data - works even
    /// when the material's shader is hidden/unsupported and GetTexture would return null.
    static Texture FindSerializedTexture(Material m, string prop)
    {
        if (m == null) return null;
        var so = new SerializedObject(m);
        var texEnvs = so.FindProperty("m_SavedProperties.m_TexEnvs");
        if (texEnvs == null) return null;
        for (int i = 0; i < texEnvs.arraySize; i++)
        {
            var el = texEnvs.GetArrayElementAtIndex(i);
            if (el.FindPropertyRelative("first").stringValue == prop)
                return el.FindPropertyRelative("second.m_Texture").objectReferenceValue as Texture;
        }
        return null;
    }

    static void EnsureFolders()
    {
        if (!AssetDatabase.IsValidFolder("Assets/3d_model")) AssetDatabase.CreateFolder("Assets", "3d_model");
        if (!AssetDatabase.IsValidFolder(GenFolder)) AssetDatabase.CreateFolder("Assets/3d_model", "Generated");
        if (!AssetDatabase.IsValidFolder(MatFolder)) AssetDatabase.CreateFolder(GenFolder, "Materials");
        if (!AssetDatabase.IsValidFolder(TreeFolder)) AssetDatabase.CreateFolder(GenFolder, "TreePrefabs");
    }

    static Material CreateOrUpdateMaterial(string path, string diffPath, string normPath, Vector2 tiling, float smoothness, Color tint)
    {
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, path);
        }
        mat.shader = shader;
        var diff = AssetDatabase.LoadAssetAtPath<Texture2D>(diffPath);
        if (diff == null) Debug.LogWarning("[RoadBeautifier] Missing texture " + diffPath);
        mat.SetTexture("_BaseMap", diff);
        mat.SetColor("_BaseColor", tint);
        mat.SetTextureScale("_BaseMap", tiling);
        if (!string.IsNullOrEmpty(normPath))
        {
            EnsureNormalMap(normPath);
            var norm = AssetDatabase.LoadAssetAtPath<Texture2D>(normPath);
            mat.SetTexture("_BumpMap", norm);
            mat.EnableKeyword("_NORMALMAP");
        }
        mat.SetFloat("_Smoothness", smoothness);
        mat.enableInstancing = true;
        EditorUtility.SetDirty(mat);
        return mat;
    }

    static void EnsureNormalMap(string path)
    {
        var ti = AssetImporter.GetAtPath(path) as TextureImporter;
        if (ti != null && ti.textureType != TextureImporterType.NormalMap)
        {
            ti.textureType = TextureImporterType.NormalMap;
            ti.SaveAndReimport();
        }
    }

    static bool IsBuilding(Transform t)
    {
        for (var c = t; c != null; c = c.parent)
        {
            var n = c.name;
            if (n.StartsWith("Building", StringComparison.OrdinalIgnoreCase) || n.Equals("BakedBuildings", StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    static void ClearGeneratedObject(string name)
    {
        // GameObject.Find misses inactive objects - walk the scene roots instead.
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        foreach (var go in scene.GetRootGameObjects())
            if (go.name == name)
                UnityEngine.Object.DestroyImmediate(go);
    }

    [Serializable]
    class TreeSpotData { public List<Vector3> spots; }

    // ---------------- Geometry / raster helpers ----------------

    static Vector2 XZ(Vector3 v) { return new Vector2(v.x, v.z); }
    static Vector3 V3(Vector2 v, float y) { return new Vector3(v.x, y, v.y); }

    static bool InPlaza(Vector2 p)
    {
        bool inside = false;
        int n = PlazaPolygon.Length;
        for (int i = 0, j = n - 1; i < n; j = i++)
        {
            var a = PlazaPolygon[i]; var b = PlazaPolygon[j];
            if ((a.y > p.y) != (b.y > p.y) && p.x < (b.x - a.x) * (p.y - a.y) / (b.y - a.y) + a.x)
                inside = !inside;
        }
        return inside;
    }

    static bool Sample(bool[] mask, int nx, int ny, Vector2 origin, Vector2 world)
    {
        int x = Mathf.FloorToInt((world.x - origin.x) / CellSize);
        int y = Mathf.FloorToInt((world.y - origin.y) / CellSize);
        if (x < 0 || y < 0 || x >= nx || y >= ny) return true; // outside grid counts as blocked
        return mask[(long)y * nx + x];
    }

    static void RasterizeTri(Vector2 a, Vector2 b, Vector2 c, bool[] mask, int nx, int ny, Vector2 origin)
    {
        float area = Mathf.Abs((b.x - a.x) * (c.y - a.y) - (c.x - a.x) * (b.y - a.y));
        if (area < 0.02f)
        {
            RasterizeSeg(a, b, mask, nx, ny, origin);
            RasterizeSeg(b, c, mask, nx, ny, origin);
            RasterizeSeg(c, a, mask, nx, ny, origin);
            return;
        }
        float minX = Mathf.Min(a.x, Mathf.Min(b.x, c.x)), maxX = Mathf.Max(a.x, Mathf.Max(b.x, c.x));
        float minY = Mathf.Min(a.y, Mathf.Min(b.y, c.y)), maxY = Mathf.Max(a.y, Mathf.Max(b.y, c.y));
        int x0 = Mathf.Max(0, Mathf.FloorToInt((minX - origin.x) / CellSize));
        int x1 = Mathf.Min(nx - 1, Mathf.FloorToInt((maxX - origin.x) / CellSize));
        int y0 = Mathf.Max(0, Mathf.FloorToInt((minY - origin.y) / CellSize));
        int y1 = Mathf.Min(ny - 1, Mathf.FloorToInt((maxY - origin.y) / CellSize));
        const float eps = 0.05f;
        for (int y = y0; y <= y1; y++)
        {
            for (int x = x0; x <= x1; x++)
            {
                var p = new Vector2(origin.x + (x + 0.5f) * CellSize, origin.y + (y + 0.5f) * CellSize);
                float d1 = Cross(a, b, p), d2 = Cross(b, c, p), d3 = Cross(c, a, p);
                bool allNeg = d1 <= eps && d2 <= eps && d3 <= eps;
                bool allPos = d1 >= -eps && d2 >= -eps && d3 >= -eps;
                if (allNeg || allPos) mask[(long)y * nx + x] = true;
            }
        }
    }

    static float Cross(Vector2 a, Vector2 b, Vector2 p)
    {
        return (b.x - a.x) * (p.y - a.y) - (b.y - a.y) * (p.x - a.x);
    }

    static void RasterizeSeg(Vector2 a, Vector2 b, bool[] mask, int nx, int ny, Vector2 origin)
    {
        float len = (b - a).magnitude;
        int steps = Mathf.Max(1, Mathf.CeilToInt(len / (CellSize * 0.5f)));
        for (int i = 0; i <= steps; i++)
        {
            var p = Vector2.Lerp(a, b, (float)i / steps);
            int x = Mathf.FloorToInt((p.x - origin.x) / CellSize);
            int y = Mathf.FloorToInt((p.y - origin.y) / CellSize);
            if (x >= 0 && y >= 0 && x < nx && y < ny) mask[(long)y * nx + x] = true;
        }
    }

    static bool[] Dilate(bool[] src, int nx, int ny, int r)
    {
        var tmp = new bool[src.LongLength];
        var dst = new bool[src.LongLength];
        for (int y = 0; y < ny; y++)
        {
            long row = (long)y * nx;
            for (int x = 0; x < nx; x++)
            {
                bool v = false;
                for (int k = Mathf.Max(0, x - r); k <= Mathf.Min(nx - 1, x + r) && !v; k++) v = src[row + k];
                tmp[row + x] = v;
            }
        }
        for (int x = 0; x < nx; x++)
        {
            for (int y = 0; y < ny; y++)
            {
                bool v = false;
                for (int k = Mathf.Max(0, y - r); k <= Mathf.Min(ny - 1, y + r) && !v; k++) v = tmp[(long)k * nx + x];
                dst[(long)y * nx + x] = v;
            }
        }
        return dst;
    }

    static bool[] Erode(bool[] src, int nx, int ny, int r)
    {
        var inv = new bool[src.LongLength];
        for (long i = 0; i < src.LongLength; i++) inv[i] = !src[i];
        inv = Dilate(inv, nx, ny, r);
        for (long i = 0; i < inv.LongLength; i++) inv[i] = !inv[i];
        return inv;
    }

    static bool[] MorphClose(bool[] src, int nx, int ny, int r)
    {
        return Erode(Dilate(src, nx, ny, r), nx, ny, r);
    }

    // ---------------- Contour tracing (boundary walk on the raster mask) ----------------

    struct Seg { public int sx, sy, ex, ey; }

    static long Key(int x, int y) { return ((long)x << 32) | (uint)y; }

    static List<List<Vector2Int>> ExtractContours(bool[] mask, int nx, int ny)
    {
        var segs = new List<Seg>();
        for (int y = 0; y < ny; y++)
        {
            long row = (long)y * nx;
            for (int x = 0; x < nx; x++)
            {
                if (!mask[row + x]) continue;
                // Directed so the filled region is on the LEFT of travel direction.
                if (x + 1 >= nx || !mask[row + x + 1]) segs.Add(new Seg { sx = x + 1, sy = y, ex = x + 1, ey = y + 1 });
                if (x - 1 < 0 || !mask[row + x - 1]) segs.Add(new Seg { sx = x, sy = y + 1, ex = x, ey = y });
                if (y + 1 >= ny || !mask[row + nx + x]) segs.Add(new Seg { sx = x + 1, sy = y + 1, ex = x, ey = y + 1 });
                if (y - 1 < 0 || !mask[row - nx + x]) segs.Add(new Seg { sx = x, sy = y, ex = x + 1, ey = y });
            }
        }

        var byStart = new Dictionary<long, List<int>>();
        for (int i = 0; i < segs.Count; i++)
        {
            long k = Key(segs[i].sx, segs[i].sy);
            List<int> list;
            if (!byStart.TryGetValue(k, out list)) { list = new List<int>(2); byStart[k] = list; }
            list.Add(i);
        }

        var used = new bool[segs.Count];
        var loops = new List<List<Vector2Int>>();
        for (int s = 0; s < segs.Count; s++)
        {
            if (used[s]) continue;
            var loop = new List<Vector2Int>();
            int cur = s;
            int startX = segs[s].sx, startY = segs[s].sy;
            while (true)
            {
                used[cur] = true;
                loop.Add(new Vector2Int(segs[cur].sx, segs[cur].sy));
                int ex = segs[cur].ex, ey = segs[cur].ey;
                if (ex == startX && ey == startY) break;
                List<int> cands;
                if (!byStart.TryGetValue(Key(ex, ey), out cands)) break;
                int dirX = segs[cur].ex - segs[cur].sx, dirY = segs[cur].ey - segs[cur].sy;
                int best = -1; int bestTurn = int.MinValue;
                foreach (var c in cands)
                {
                    if (used[c]) continue;
                    int ndX = segs[c].ex - segs[c].sx, ndY = segs[c].ey - segs[c].sy;
                    int turn = dirX * ndY - dirY * ndX; // prefer left turns to hug the filled region
                    if (turn > bestTurn) { bestTurn = turn; best = c; }
                }
                if (best < 0) break;
                cur = best;
            }
            if (loop.Count >= 4) loops.Add(loop);
        }
        return loops;
    }

    static List<Vector2> ToWorld(List<Vector2Int> pts, Vector2 origin)
    {
        var res = new List<Vector2>(pts.Count);
        foreach (var p in pts) res.Add(origin + new Vector2(p.x, p.y) * CellSize);
        return res;
    }

    static List<Vector2> SimplifyClosed(List<Vector2> pts, float eps)
    {
        if (pts.Count < 4) return pts;
        var open = new List<Vector2>(pts) { pts[0] };
        var keep = new bool[open.Count];
        keep[0] = keep[open.Count - 1] = true;
        var stack = new Stack<KeyValuePair<int, int>>();
        stack.Push(new KeyValuePair<int, int>(0, open.Count - 1));
        while (stack.Count > 0)
        {
            var range = stack.Pop();
            int a = range.Key, b = range.Value;
            if (b - a < 2) continue;
            float maxD = -1f; int maxI = -1;
            var pa = open[a]; var pb = open[b];
            var ab = pb - pa; float abLen2 = ab.sqrMagnitude;
            for (int i = a + 1; i < b; i++)
            {
                float d;
                if (abLen2 < 1e-9f) d = (open[i] - pa).magnitude;
                else
                {
                    float t = Mathf.Clamp01(Vector2.Dot(open[i] - pa, ab) / abLen2);
                    d = (open[i] - (pa + ab * t)).magnitude;
                }
                if (d > maxD) { maxD = d; maxI = i; }
            }
            if (maxD > eps)
            {
                keep[maxI] = true;
                stack.Push(new KeyValuePair<int, int>(a, maxI));
                stack.Push(new KeyValuePair<int, int>(maxI, b));
            }
        }
        var res = new List<Vector2>();
        for (int i = 0; i < open.Count - 1; i++) if (keep[i]) res.Add(open[i]);
        return res;
    }

    static List<Vector2> ResampleClosed(List<Vector2> pts, float step)
    {
        var res = new List<Vector2>();
        int n = pts.Count;
        for (int i = 0; i < n; i++)
        {
            var a = pts[i]; var b = pts[(i + 1) % n];
            res.Add(a);
            float len = (b - a).magnitude;
            int sub = Mathf.FloorToInt(len / step);
            for (int k = 1; k <= sub; k++)
            {
                float t = (float)k / (sub + 1);
                res.Add(Vector2.Lerp(a, b, t));
            }
        }
        return res;
    }

    // ---------------- Tiled mesh builder (small editable chunks) ----------------

    class TiledMeshBuilder
    {
        class Bucket
        {
            public List<Vector3> verts = new List<Vector3>();
            public List<Vector2> uvs = new List<Vector2>();
            public List<int> tris = new List<int>();
        }

        readonly Transform parent;
        readonly string baseName;
        readonly Material material;
        readonly string assetPath;
        readonly Dictionary<Vector2Int, Bucket> buckets = new Dictionary<Vector2Int, Bucket>();
        public int ChunkCount { get; private set; }
        public bool addRenderer = true;
        public bool addCollider = true;
        public bool weldOnFlush = false; // weld duplicated vertices so physics sees a seamless sheet

        public TiledMeshBuilder(Transform parent, string baseName, Material material, string assetPath)
        {
            this.parent = parent; this.baseName = baseName; this.material = material; this.assetPath = assetPath;
        }

        public void AddQuad(Vector3 a, Vector3 b, Vector3 c, Vector3 d,
                            Vector2 ua, Vector2 ub, Vector2 uc, Vector2 ud, Vector3 expectedNormal)
        {
            var cen = (a + b + c + d) * 0.25f;
            var key = new Vector2Int(Mathf.FloorToInt(cen.x / TileSize), Mathf.FloorToInt(cen.z / TileSize));
            Bucket bu;
            if (!buckets.TryGetValue(key, out bu)) { bu = new Bucket(); buckets[key] = bu; }

            int i0 = bu.verts.Count;
            bu.verts.Add(a); bu.verts.Add(b); bu.verts.Add(c); bu.verts.Add(d);
            bu.uvs.Add(ua); bu.uvs.Add(ub); bu.uvs.Add(uc); bu.uvs.Add(ud);
            var n = Vector3.Cross(b - a, c - a);
            if (Vector3.Dot(n, expectedNormal) >= 0)
            {
                bu.tris.Add(i0); bu.tris.Add(i0 + 1); bu.tris.Add(i0 + 2);
                bu.tris.Add(i0); bu.tris.Add(i0 + 2); bu.tris.Add(i0 + 3);
            }
            else
            {
                bu.tris.Add(i0); bu.tris.Add(i0 + 2); bu.tris.Add(i0 + 1);
                bu.tris.Add(i0); bu.tris.Add(i0 + 3); bu.tris.Add(i0 + 2);
            }
        }

        static void Weld(Bucket bu)
        {
            var map = new Dictionary<Vector3Int, int>();
            var remap = new int[bu.verts.Count];
            var nv = new List<Vector3>(bu.verts.Count);
            var nuv = new List<Vector2>(bu.verts.Count);
            for (int i = 0; i < bu.verts.Count; i++)
            {
                var v = bu.verts[i];
                var k = new Vector3Int(Mathf.RoundToInt(v.x * 1000f), Mathf.RoundToInt(v.y * 1000f), Mathf.RoundToInt(v.z * 1000f));
                int idx;
                if (!map.TryGetValue(k, out idx))
                {
                    idx = nv.Count;
                    map[k] = idx;
                    nv.Add(v);
                    nuv.Add(bu.uvs[i]);
                }
                remap[i] = idx;
            }
            var nt = new List<int>(bu.tris.Count);
            for (int t = 0; t < bu.tris.Count; t += 3)
            {
                int a = remap[bu.tris[t]], b = remap[bu.tris[t + 1]], c = remap[bu.tris[t + 2]];
                if (a == b || b == c || a == c) continue; // degenerate after welding
                nt.Add(a); nt.Add(b); nt.Add(c);
            }
            bu.verts = nv; bu.uvs = nuv; bu.tris = nt;
        }

        public void Flush()
        {
            var groupRoot = new GameObject(baseName + "s");
            groupRoot.transform.SetParent(parent, false);
            foreach (var kv in buckets)
            {
                var bu = kv.Value;
                if (bu.verts.Count == 0) continue;
                if (weldOnFlush) Weld(bu);
                var mesh = new Mesh();
                mesh.name = baseName + "_" + kv.Key.x + "_" + kv.Key.y;
                mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
                mesh.SetVertices(bu.verts);
                mesh.SetUVs(0, bu.uvs);
                mesh.SetTriangles(bu.tris, 0);
                mesh.RecalculateNormals();
                mesh.RecalculateBounds();

                if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath) == null)
                    AssetDatabase.CreateAsset(mesh, assetPath);
                else
                    AssetDatabase.AddObjectToAsset(mesh, assetPath);

                var go = new GameObject(mesh.name);
                go.transform.SetParent(groupRoot.transform, false);
                if (addRenderer)
                {
                    var mf = go.AddComponent<MeshFilter>(); mf.sharedMesh = mesh;
                    var mr = go.AddComponent<MeshRenderer>(); mr.sharedMaterial = material;
                    mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                }
                if (addCollider)
                {
                    var mc = go.AddComponent<MeshCollider>(); mc.sharedMesh = mesh;
                    mc.cookingOptions = MeshColliderCookingOptions.EnableMeshCleaning
                        | MeshColliderCookingOptions.WeldColocatedVertices
                        | MeshColliderCookingOptions.CookForFasterSimulation
                        | MeshColliderCookingOptions.UseFastMidphase;
                }
                go.isStatic = true;
                ChunkCount++;
            }
            buckets.Clear();
        }
    }
}
