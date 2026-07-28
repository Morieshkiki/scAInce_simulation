using Numena.SC;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TUD_DefaultPlayer : TUD_Player
{
    [SerializeField] Transform startPos;
    [SerializeField] Transform _rig;
    [SerializeField] Camera _camera;
    //todo: controllers?

    public override Vector3 position => GetPositionOnFloor();
    public override Transform rig => _rig;
    public override Camera camera => _camera;

    private void OnEnable()
    {
        if (startPos != null)
        {
            rig.position = startPos.position;
            rig.rotation = startPos.rotation;
        }
    }

    Vector3 GetPositionOnFloor()
    {
        Vector3 worldCamPos = _camera.transform.position;
        Vector3 localCamPos = _rig.InverseTransformPoint(worldCamPos);
        localCamPos.y = 0;
        return _rig.TransformPoint(localCamPos);
    }
}
