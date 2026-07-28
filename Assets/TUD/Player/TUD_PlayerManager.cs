using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[DefaultExecutionOrder(-9900)]
public class TUD_PlayerManager : MonoBehaviour
{
    static TUD_PlayerManager main;

    [System.Serializable]
    struct PlayerInfo
    {
        public string name;
        public TUD_InputManager.InputMode mode;
        public TUD_Player player;
    }

    [SerializeField] PlayerInfo[] playerInfo;


    PlayerInfo _activePlayer;
    public static TUD_Player activePlayer => main._activePlayer.player;

    public static Action onPlayerChanged;

    private void Awake()
    {
        main = this;
    }

    private void Start()
    {
        foreach(PlayerInfo info in playerInfo)
        {
            if (info.player != null)
                info.player.gameObject.SetActive(false);
        }
        _activePlayer.mode = TUD_InputManager.InputMode.None;
        _activePlayer.player = null;

        SetPlayer(TUD_InputManager.currentInputMode);
    }

    private void OnEnable()
    {
        TUD_InputManager.onInputModeChanged += OnInputModeChanged;
        SetPlayer(TUD_InputManager.currentInputMode);
    }

    private void OnDisable()
    {
        TUD_InputManager.onInputModeChanged -= OnInputModeChanged;
    }

    void SetPlayer(TUD_InputManager.InputMode mode)
    {
        if (_activePlayer.mode == mode)
            return;

        for (int ct = 0; ct < playerInfo.Length; ct++)
        {
            if(playerInfo[ct].mode == mode)
            {
                if (_activePlayer.player != null)
                    _activePlayer.player.gameObject.SetActive(false);
                _activePlayer = playerInfo[ct];
                if (_activePlayer.player != null)
                    _activePlayer.player.gameObject.SetActive(true);

                onPlayerChanged?.Invoke();
                return;
            }
        }
        Debug.LogError("Unable to change player. Mode '" + mode.ToString() + "' not found.", main);
    }

    void OnInputModeChanged(TUD_InputManager.InputMode lastMode)
    {
        SetPlayer(TUD_InputManager.currentInputMode);
    }
}
