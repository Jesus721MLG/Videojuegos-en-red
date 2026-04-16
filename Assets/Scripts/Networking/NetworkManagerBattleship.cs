using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Battleship
{
    /// <summary>
    /// Custom network manager that handles player connections, lobby state,
    /// and transitions between lobby and battle scenes for online multiplayer.
    /// Attach this to a GameObject with a NetworkManager and UnityTransport component.
    /// </summary>
    public class NetworkManagerBattleship : MonoBehaviour
    {
        public static NetworkManagerBattleship Instance { get; private set; }
        public static bool IsOnlineMode { get; set; }

        public event Action OnBothPlayersConnected;
        public event Action<string> OnConnectionStatusChanged;
        public event Action OnClientDisconnected;

        [SerializeField] string _battleSceneName = "Battle";

        int _connectedPlayers;
        bool _gameStarted;

        public int ConnectedPlayers => _connectedPlayers;
        public bool GameStarted => _gameStarted;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>
        /// Start hosting a game. The host acts as both server and client.
        /// </summary>
        public void HostGame()
        {
            IsOnlineMode = true;
            var networkManager = NetworkManager.Singleton;
            networkManager.ConnectionApprovalCallback = ApprovalCallback;
            networkManager.OnClientConnectedCallback += OnClientConnected;
            networkManager.OnClientDisconnectCallback += OnClientDisconnect;

            if (networkManager.StartHost())
            {
                OnConnectionStatusChanged?.Invoke("Hosting... Waiting for opponent.");
                _connectedPlayers = 1;
            }
            else
            {
                OnConnectionStatusChanged?.Invoke("Failed to start host.");
            }
        }

        /// <summary>
        /// Join an existing game as a client.
        /// </summary>
        public void JoinGame()
        {
            IsOnlineMode = true;
            var networkManager = NetworkManager.Singleton;
            networkManager.OnClientConnectedCallback += OnClientConnected;
            networkManager.OnClientDisconnectCallback += OnClientDisconnect;

            if (networkManager.StartClient())
            {
                OnConnectionStatusChanged?.Invoke("Connecting to host...");
            }
            else
            {
                OnConnectionStatusChanged?.Invoke("Failed to connect.");
            }
        }

        /// <summary>
        /// Disconnect and return to the main menu.
        /// </summary>
        public void Disconnect()
        {
            var networkManager = NetworkManager.Singleton;
            if (networkManager != null && networkManager.IsListening)
            {
                networkManager.Shutdown();
            }

            _connectedPlayers = 0;
            _gameStarted = false;
            IsOnlineMode = false;
        }

        void ApprovalCallback(
            NetworkManager.ConnectionApprovalRequest request,
            NetworkManager.ConnectionApprovalResponse response)
        {
            // Only allow 2 players
            if (_connectedPlayers >= 2 || _gameStarted)
            {
                response.Approved = false;
                response.Reason = "Game is full or already started.";
                return;
            }

            response.Approved = true;
            response.CreatePlayerObject = true;
        }

        void OnClientConnected(ulong clientId)
        {
            if (NetworkManager.Singleton.IsServer)
            {
                _connectedPlayers++;
                OnConnectionStatusChanged?.Invoke($"Players connected: {_connectedPlayers}/2");

                if (_connectedPlayers == 2)
                {
                    _gameStarted = true;
                    OnBothPlayersConnected?.Invoke();
                    LoadBattleScene();
                }
            }
            else
            {
                OnConnectionStatusChanged?.Invoke("Connected! Waiting for game to start...");
            }
        }

        void OnClientDisconnect(ulong clientId)
        {
            if (NetworkManager.Singleton.IsServer)
            {
                _connectedPlayers--;

                if (_gameStarted)
                {
                    OnConnectionStatusChanged?.Invoke("Opponent disconnected!");
                    OnClientDisconnected?.Invoke();
                }
                else
                {
                    OnConnectionStatusChanged?.Invoke($"Players connected: {_connectedPlayers}/2");
                }
            }
            else
            {
                OnConnectionStatusChanged?.Invoke("Disconnected from host.");
                OnClientDisconnected?.Invoke();
            }
        }

        void LoadBattleScene()
        {
            if (NetworkManager.Singleton.IsServer)
            {
                NetworkManager.Singleton.SceneManager.LoadScene(_battleSceneName, LoadSceneMode.Single);
            }
        }

        void OnDestroy()
        {
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
                NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnect;
            }
        }
    }
}
