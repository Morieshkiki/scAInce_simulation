using System.Collections.Generic;
using UnityEngine;

// Runtime mesh deformation ("dents") for vehicle crashes.
//
// Attach to a vehicle root. On the first dent (or an explicit Initialize) it makes a
// per-object INSTANCE of each body mesh (so denting one car never affects others that
// share the same source mesh) and caches its vertices. DentAt() then pushes the
// vertices near an impact point inward, accumulating across multiple hits up to a cap.
//
// Requires the body meshes to be Read/Write enabled in their model import settings;
// any mesh that is not readable is skipped with a warning (the rest still dent).
public class MeshDenter : MonoBehaviour
{
    [Tooltip("Radius (m) of the vertices affected around each impact point.")]
    public float radius = 1.0f;
    [Tooltip("Maximum cumulative inward displacement (m) any single vertex may take.")]
    public float maxDent = 0.4f;
    [Tooltip("Scales the impact strength (e.g. relative speed in m/s) into displacement metres.")]
    public float strengthScale = 0.045f;
    [Tooltip("Meshes whose object name contains any of these are NOT dented (wheels, glass, ...).")]
    public string[] skipNameContains = { "wheel", "rim", "tire", "tyre", "glass", "window", "light" };

    private class Target
    {
        public MeshFilter mf;
        public Mesh mesh;          // the instanced, writable mesh
        public Vector3[] baseVerts; // pristine vertices (for clamping cumulative dent)
        public Vector3[] verts;     // current (deformed) vertices
        public Vector3 localCenter; // body centre in mesh-local space (inward direction)
    }

    private readonly List<Target> _targets = new List<Target>();
    private bool _initialized;

    public int TargetCount => _targets.Count;

    public void Initialize()
    {
        // Re-scan while we have no targets. A vehicle's body mesh can be momentarily
        // non-readable at spawn (right after a Read/Write reimport), which would cache an
        // empty target set forever; retrying on the first real dent (well after spawn)
        // captures the now-readable meshes. Once we have targets we never rescan.
        if (_initialized && _targets.Count > 0) return;
        _initialized = true;
        _targets.Clear();

        foreach (var mf in GetComponentsInChildren<MeshFilter>(true))
        {
            if (mf.sharedMesh == null) continue;

            string nm = mf.name.ToLowerInvariant();
            bool skip = false;
            foreach (var s in skipNameContains)
                if (!string.IsNullOrEmpty(s) && nm.Contains(s)) { skip = true; break; }
            if (skip) continue;

            if (!mf.sharedMesh.isReadable)
            {
                Debug.LogWarning($"[MeshDenter] Mesh '{mf.sharedMesh.name}' on '{mf.name}' is not " +
                                 "Read/Write enabled; it will not dent.");
                continue;
            }

            Mesh inst = mf.mesh; // instantiates a unique copy and assigns it back to the filter
            var t = new Target
            {
                mf = mf,
                mesh = inst,
                baseVerts = inst.vertices,
                verts = inst.vertices,
                localCenter = inst.bounds.center,
            };
            _targets.Add(t);
        }
    }

    // worldPoint  : the contact point of the impact
    // worldNormal : the contact normal (used only as a hint; centre direction dominates
    //               so the dent always goes inward regardless of the normal's sign)
    // strength    : impact magnitude, e.g. relative speed (m/s) or impulse
    //
    // All distances/displacements are computed in WORLD space so that radius and maxDent
    // are meaningful in metres regardless of the model's internal mesh scale (these FBXs
    // import with huge local coordinates scaled down on the transform — doing the maths in
    // local units would make a 0.5 m radius microscopic and dent nothing).
    public void DentAt(Vector3 worldPoint, Vector3 worldNormal, float strength)
    {
        if (!_initialized) Initialize();
        if (_targets.Count == 0 || strength <= 0f) return;

        float amount = Mathf.Min(strength * strengthScale, maxDent); // metres
        if (amount <= 0.0001f) return;

        float r = Mathf.Max(radius, 0.01f);
        Vector3 inwardNormalW = (worldNormal.sqrMagnitude > 1e-6f) ? -worldNormal.normalized : Vector3.zero;

        foreach (var t in _targets)
        {
            Transform tr = t.mf.transform;
            Vector3 worldCenter = tr.TransformPoint(t.localCenter);
            bool changed = false;

            for (int i = 0; i < t.verts.Length; i++)
            {
                Vector3 worldV = tr.TransformPoint(t.verts[i]);
                float dist = Vector3.Distance(worldV, worldPoint);
                if (dist > r) continue;

                float falloff = 1f - dist / r;                  // 1 at centre -> 0 at edge
                Vector3 toCenter = worldCenter - worldV;
                if (toCenter.sqrMagnitude > 1e-6f) toCenter.Normalize();

                // Inward = toward the body centre, nudged by the impact normal when that
                // also points inward (dot > 0). Keeps dents going IN, never out.
                Vector3 dir = toCenter;
                if (Vector3.Dot(inwardNormalW, toCenter) > 0f) dir = (toCenter + inwardNormalW * 0.5f).normalized;

                Vector3 newWorld = worldV + dir * (amount * falloff);

                // Clamp cumulative drift from the pristine position (in world metres).
                Vector3 baseWorld = tr.TransformPoint(t.baseVerts[i]);
                Vector3 fromBase = newWorld - baseWorld;
                if (fromBase.magnitude > maxDent) newWorld = baseWorld + fromBase.normalized * maxDent;

                t.verts[i] = tr.InverseTransformPoint(newWorld);
                changed = true;
            }

            if (changed)
            {
                t.mesh.vertices = t.verts;
                t.mesh.RecalculateNormals();
                t.mesh.RecalculateBounds();
            }
        }
    }
}
