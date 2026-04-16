using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using TMPro;

namespace Battleship
{
    /// <summary>
    /// Manages the lobby UI for online multiplayer.
    /// Allows players to host or join a game, shows connection status,
    /// and handles the transition to the battle scene.
    /// 
    /// Attach this to a Canvas in the Main Menu scene alongside the
    /// NetworkManagerBattleship, NetworkManager, and UnityTransport components.
    /// </summary>
    public class LobbyUI : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] GameObject _mainMenuPanel;
        [SerializeField] GameObject _lobbyPanel;
        [SerializeField] GameObject _connectingPanel;

        [Header("Input")]
        [SerializeField] TMP_InputField _ipAddressInput;
        [SerializeField] TMP_InputField _portInput;

        [Header("Text")]
        [SerializeField] TextMeshProUGUI _statusText;
        [SerializeField] TextMeshProUGUI _connectionInfoText;

        [Header("Buttons")]
        [SerializeField] GameObject _hostButton;
        [SerializeField] GameObject _joinButton;
        [SerializeField] GameObject _disconnectButton;

        const ushort DefaultPort = 7777;

        void Start()
        {
            ShowMainMenu();

            if (NetworkManagerBattleship.Instance != null)
            {
                NetworkManagerBattleship.Instance.OnConnectionStatusChanged += UpdateStatus;
                NetworkManagerBattleship.Instance.OnClientDisconnected += OnDisconnected;
            }
        }

        void OnDestroy()
        {
            if (NetworkManagerBattleship.Instance != null)
            {
                NetworkManagerBattleship.Instance.OnConnectionStatusChanged -= UpdateStatus;
                NetworkManagerBattleship.Instance.OnClientDisconnected -= OnDisconnected;
            }
        }

        /// <summary>
        /// Called by the "Online" button in the main menu.
        /// Shows the lobby panel with host/join options.
        /// </summary>
        public void ShowLobby()
        {
            if (_mainMenuPanel != null)
                _mainMenuPanel.SetActive(false);

            if (_lobbyPanel != null)
                _lobbyPanel.SetActive(true);

            if (_connectingPanel != null)
                _connectingPanel.SetActive(false);

            if (_ipAddressInput != null)
                _ipAddressInput.text = "127.0.0.1";

            if (_portInput != null)
                _portInput.text = DefaultPort.ToString();
        }

        /// <summary>
        /// Returns to the main menu from the lobby.
        /// </summary>
        public void ShowMainMenu()
        {
            if (_mainMenuPanel != null)
                _mainMenuPanel.SetActive(true);

            if (_lobbyPanel != null)
                _lobbyPanel.SetActive(false);

            if (_connectingPanel != null)
                _connectingPanel.SetActive(false);
        }

        /// <summary>
        /// Called by the Host button. Starts hosting a game.
        /// </summary>
        public void OnHostClicked()
        {
            SetTransportPort();
            ShowConnectingPanel();

            if (NetworkManagerBattleship.Instance != null)
            {
                NetworkManagerBattleship.Instance.HostGame();
            }

            UpdateConnectionInfo("Hosting on port " + GetPort());
        }

        /// <summary>
        /// Called by the Join button. Connects to a host.
        /// </summary>
        public void OnJoinClicked()
        {
            SetTransportAddress();
            SetTransportPort();
            ShowConnectingPanel();

            if (NetworkManagerBattleship.Instance != null)
            {
                NetworkManagerBattleship.Instance.JoinGame();
            }

            string address = _ipAddressInput != null ? _ipAddressInput.text : "127.0.0.1";
            UpdateConnectionInfo($"Connecting to {address}:{GetPort()}...");
        }

        /// <summary>
        /// Called by the Disconnect button.
        /// </summary>
        public void OnDisconnectClicked()
        {
            if (NetworkManagerBattleship.Instance != null)
            {
                NetworkManagerBattleship.Instance.Disconnect();
            }

            ShowLobby();
        }

        /// <summary>
        /// Called by the Back button to return to the main menu.
        /// </summary>
        public void OnBackClicked()
        {
            if (NetworkManagerBattleship.Instance != null)
            {
                NetworkManagerBattleship.Instance.Disconnect();
            }

            ShowMainMenu();
        }

        void ShowConnectingPanel()
        {
            if (_lobbyPanel != null)
                _lobbyPanel.SetActive(false);

            if (_connectingPanel != null)
                _connectingPanel.SetActive(true);
        }

        void SetTransportAddress()
        {
            var transport = NetworkManager.Singleton?.GetComponent<UnityTransport>();
            if (transport != null && _ipAddressInput != null)
            {
                string address = _ipAddressInput.text;
                if (string.IsNullOrWhiteSpace(address))
                    address = "127.0.0.1";

                transport.ConnectionData.Address = address;
            }
        }

        void SetTransportPort()
        {
            var transport = NetworkManager.Singleton?.GetComponent<UnityTransport>();
            if (transport != null)
            {
                transport.ConnectionData.Port = GetPort();
            }
        }

        ushort GetPort()
        {
            if (_portInput != null && ushort.TryParse(_portInput.text, out ushort port))
                return port;

            return DefaultPort;
        }

        void UpdateStatus(string status)
        {
            if (_statusText != null)
                _statusText.text = status;
        }

        void UpdateConnectionInfo(string info)
        {
            if (_connectionInfoText != null)
                _connectionInfoText.text = info;
        }

        void OnDisconnected()
        {
            ShowLobby();
            UpdateStatus("Disconnected.");
        }
    }
}
