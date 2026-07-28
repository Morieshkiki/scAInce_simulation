using UnityEngine;

// This scene has no baked lighting data, so it loads with a BLACK ambient probe —
// everything in shadow renders pitch black (unseeable ground/trees/props). With
// ambient mode = Skybox, the probe stays zero until someone regenerates it, so do
// that once on scene load. Works in the editor and in builds.
public class AmbientProbeBootstrap : MonoBehaviour
{
    void Start()
    {
        DynamicGI.UpdateEnvironment();
    }
}
