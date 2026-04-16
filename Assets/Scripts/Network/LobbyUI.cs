using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Mirror;

namespace Battleship
{
    /// <summary>
    /// Lobby / in-game HUD for the networked Battleship game.
    ///
    /// EDITOR SETUP:
    /// 1. In the Battle scene, create a Canvas named "NetworkCanvas".
    /// 2. Inside it, create a Panel named "LobbyPanel" with:
    ///       • A Button "HostButton"  (label: "Host Game")
    ///       • A Button "JoinButton"  (label: "Join Game")
    ///       • A TMP_InputField "IpInput" (placeholder: "Enter Host IP…")
    ///       • A TextMeshProUGUI "StatusText"
    /// 3. Inside the Canvas, also create:
    ///       • A TextMeshProUGUI "TurnText" (will display turn info during battle).
    /// 4. Create an empty GameObject "LobbyUI", add this script,
    ///    and drag all the references into the Inspector fields.
    /// </summary>
    public class LobbyUI : MonoBehaviour
    {
        [Header("Lobby Panel (visible before game starts)")]
        [SerializeField] GameObject       _lobbyPanel;
        [SerializeField] Button           _hostButton;
        [SerializeField] Button           _joinButton;
        [SerializeField] TMP_InputField   _ipAddressInput;
        [SerializeField] TextMeshProUGUI  _statusText;

        [Header("In-Game HUD (visible during game)")]
        [SerializeField] TextMeshProUGUI  _turnText;

        [Header("Disconnect / Back (optional)")]
        [Tooltip("Button to disconnect and return to the main menu. " +
                 "Leave empty if you don't need one.")]
        [SerializeField] Button           _disconnectButton;

        void OnEnable()
        {
            _hostButton.onClick.AddListener(OnHostClicked);
            _joinButton.onClick.AddListener(OnJoinClicked);
            if (_disconnectButton != null)
                _disconnectButton.onClick.AddListener(OnDisconnectClicked);

            BattleshipPlayer.OnLobbyStatus        += HandleLobbyStatus;
            BattleshipPlayer.OnGameStarted         += HandleGameStarted;
            BattleshipPlayer.OnTurnChanged         += HandleTurnChanged;
            BattleshipPlayer.OnGameOver            += HandleGameOver;
            BattleshipPlayer.OnOpponentDisconnected += HandleOpponentDisconnected;
        }

        void OnDisable()
        {
            _hostButton.onClick.RemoveListener(OnHostClicked);
            _joinButton.onClick.RemoveListener(OnJoinClicked);
            if (_disconnectButton != null)
                _disconnectButton.onClick.RemoveListener(OnDisconnectClicked);

            BattleshipPlayer.OnLobbyStatus        -= HandleLobbyStatus;
            BattleshipPlayer.OnGameStarted         -= HandleGameStarted;
            BattleshipPlayer.OnTurnChanged         -= HandleTurnChanged;
            BattleshipPlayer.OnGameOver            -= HandleGameOver;
            BattleshipPlayer.OnOpponentDisconnected -= HandleOpponentDisconnected;
        }

        void Start()
        {
            _lobbyPanel.SetActive(true);
            if (_turnText != null) _turnText.gameObject.SetActive(false);
            if (_disconnectButton != null) _disconnectButton.gameObject.SetActive(false);
        }

        // ────────────────────── Button callbacks ──────────────────────

        void OnHostClicked()
        {
            NetworkManager.singleton.StartHost();
            SetStatus("Hosting… waiting for opponent (1/2)");
            _hostButton.interactable = false;
            _joinButton.interactable = false;
        }

        void OnJoinClicked()
        {
            string ip = _ipAddressInput != null ? _ipAddressInput.text : "";
            if (string.IsNullOrWhiteSpace(ip)) ip = "localhost";

            NetworkManager.singleton.networkAddress = ip;
            NetworkManager.singleton.StartClient();
            SetStatus($"Connecting to {ip}…");
            _hostButton.interactable = false;
            _joinButton.interactable = false;
        }

        // ────────────────────── Event handlers ──────────────────────

        void HandleLobbyStatus(string msg)
        {
            SetStatus(msg);
        }

        void HandleGameStarted(int myIndex)
        {
            _lobbyPanel.SetActive(false);
            if (_turnText != null) _turnText.gameObject.SetActive(true);
            if (_disconnectButton != null) _disconnectButton.gameObject.SetActive(true);
        }

        void HandleTurnChanged(int currentTurn)
        {
            if (_turnText == null) return;

            bool mine = BattleshipPlayer.LocalInstance != null
                     && BattleshipPlayer.LocalInstance.GetMyPlayerIndex() == currentTurn;

            _turnText.text = mine
                ? "YOUR TURN – click a tile on the opponent's board!"
                : "Opponent's turn… please wait.";
        }

        void HandleGameOver(int winnerIndex)
        {
            if (_turnText == null) return;

            bool iWon = BattleshipPlayer.LocalInstance != null
                     && BattleshipPlayer.LocalInstance.GetMyPlayerIndex() == winnerIndex;

            _turnText.text = iWon ? "🎉 YOU WIN!" : "💀 YOU LOSE!";
        }

        void HandleOpponentDisconnected()
        {
            SetStatus("Opponent disconnected.");
            if (_turnText != null) _turnText.text = "Opponent disconnected.";
        }

        /// <summary>Disconnect from the current session and return to the lobby.</summary>
        void OnDisconnectClicked()
        {
            if (NetworkServer.active && NetworkClient.isConnected)
                NetworkManager.singleton.StopHost();
            else if (NetworkClient.isConnected)
                NetworkManager.singleton.StopClient();

            // Reload the scene to reset everything cleanly
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
        }

        void SetStatus(string msg)
        {
            if (_statusText != null) _statusText.text = msg;
        }
    }
}
