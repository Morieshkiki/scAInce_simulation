using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DUI_MapPlayerMarker : MonoBehaviour
{
    [SerializeField] RectTransform personIcon, vehicleIcon, ArrowIcon;
    DUI_Map _map;
    DUI_Map map { get { if (_map == null) _map = GetComponentInParent<DUI_Map>(); return _map; } }
    RectTransform rectTransform => transform as RectTransform;

    private void OnEnable()
    {
        TUD_PlayerManager.onPlayerChanged += OnPlayerChanged;
        OnPlayerChanged();
    }

    private void OnDisable()
    {
        TUD_PlayerManager.onPlayerChanged -= OnPlayerChanged;
    }

    void Update()
    {
        Vector3 worldPos = TUD_PlayerManager.activePlayer.position;
        Vector2 mapPos = map.WorldToMapPoint(worldPos);
        rectTransform.anchoredPosition = mapPos;
    }

    void OnPlayerChanged()
    {

    }
}
