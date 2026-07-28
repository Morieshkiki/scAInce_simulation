using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class TreadmillPlayer : TUD_Player
{
    [SerializeField] CharacterController charController;
    bool useCustomController = false;
    [SerializeField] Transform _rig;
    [SerializeField] Camera _camera;

    [SerializeField] bool _gravity = true;
    //[SerializeField] bool _collision = true;

    public bool gravity { get => _gravity; set { ToggleGravity(value); } }
    //public bool collision { get => _collision; set { ToggleCollision(value); } }

    [SerializeField] bool debugMode;
    [SerializeField] InputActionReference debugInput;

    [SerializeField] Transform startPos;

    public override Vector3 position => GetPositionOnFloor();
    public override Transform rig => _rig;
    public override Camera camera => _camera;

    void ToggleGravity(bool on)
    {
        _gravity = on;
    }

    /*void ToggleCollision(bool on)
    {
        _collision = on;
        if (on)
            CreateCharacter();
        else
            Destroy(charController.gameObject);
    }*/

    private void Awake()
    {
        if (charController != null)
            useCustomController = true;
    }

    private void OnEnable()
    {
        debugInput.action.Enable();
        if (startPos != null)
        {
            _rig.position = startPos.position;
            _rig.rotation = startPos.rotation;
        }
        CreateCharacter();
    }

    private void OnDisable()
    {
        debugInput.action.Disable();
        if (useCustomController)
            charController.gameObject.SetActive(false);
        else if (charController != null)
            Destroy(charController.gameObject);
    }

    void CreateCharacter()
    {
        Vector3 floorHeadPos = _rig.InverseTransformPoint(_camera.transform.position);
        float headHeight = floorHeadPos.y;
        floorHeadPos.y = 0;
        floorHeadPos = _rig.TransformPoint(floorHeadPos);
        if (useCustomController == false)
        {
            GameObject go = new GameObject();
            go.name = "CharacterController (Treadmill Player)";
            charController = go.AddComponent<CharacterController>();

            if (!debugMode)
                go.hideFlags = HideFlags.HideAndDontSave;
            else
                go.hideFlags = HideFlags.DontSave;
        }
        else
        {
            charController.gameObject.SetActive(true);
        }
        charController.transform.position = floorHeadPos;
        charController.height = headHeight + 0.3f;
        charController.radius = 0.3f;
        charController.center = new Vector3(0,headHeight*0.5f + 0.15f);
        charController.stepOffset = Mathf.Clamp(charController.height,0,0.6f);
        charController.minMoveDistance = 0;
    }

    private void LateUpdate()
    {
        Vector3 floorHeadPos = _rig.InverseTransformPoint(_camera.transform.position);
        float headHeight = floorHeadPos.y;
        floorHeadPos.y = 0;
        floorHeadPos = _rig.TransformPoint(floorHeadPos);

        charController.height = headHeight + 0.3f;
        charController.center = new Vector3(0, headHeight * 0.5f + 0.15f,0);
        charController.stepOffset = Mathf.Clamp(charController.height, 0, 0.6f);

        Vector3 offset = floorHeadPos - charController.transform.position;
        if (debugMode)
        {
            Debug.DrawLine(floorHeadPos, floorHeadPos + Vector3.up);

            Vector2 inputVector = new Vector2();
            inputVector = debugInput.action.ReadValue<Vector2>() * Time.deltaTime * 5;
            offset += new Vector3(inputVector.x, 0, inputVector.y);
            charController.Move(offset + (gravity ? Physics.gravity * Time.deltaTime : Vector3.zero));
            _rig.position += charController.transform.position + Vector3.up * headHeight - _camera.transform.position;
        }
        _rig.position += charController.transform.position + Vector3.up * headHeight - _camera.transform.position;
    }

    Vector3 GetPositionOnFloor()
    {
        Vector3 worldCamPos = _camera.transform.position;
        Vector3 localCamPos = _rig.InverseTransformPoint(worldCamPos);
        localCamPos.y = 0;
        return _rig.TransformPoint(localCamPos);
    }
}
