using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class TUD_Player : MonoBehaviour
{
    public abstract Vector3 position { get; }
    public new abstract Camera camera { get; }
    public abstract Transform rig { get; }
}
