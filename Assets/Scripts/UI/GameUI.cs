using UnityEngine;
using TMPro;

namespace Battleship
{
    public class GameUI : MonoBehaviour
    {
        #region FIELDS
        [Header("Canvas")]
        [SerializeField] ImageFader _imageFader;
        [Header("Panels")]
        [SerializeField] GameObject _preparePanel;
        [SerializeField] GameObject _winPanel;
        [SerializeField] GameObject _replayPanel;
        [SerializeField] GameObject _postReplayPanel;
        [Header("Buttons")]
        [SerializeField] GameObject _replayControls;
        [Header("Text")]
        [SerializeField] TextMeshProUGUI _playerText;
        [SerializeField] TextMeshProUGUI _winnerText;
        [SerializeField] TextMeshProUGUI _warningText;
        [SerializeField] TextMeshProUGUI _loadingText;
        [SerializeField] TMP_InputField _inputField;
        [SerializeField] TextMeshProUGUI[] _boardTexts;

        [Header("Online UI")]
        [SerializeField] TextMeshProUGUI _onlineStatusText;
        [SerializeField] GameObject _onlineTurnIndicator;
        #endregion

        void Start()
        {
            // In online mode, skip showing the prepare panel at start
            if (NetworkManagerBattleship.IsOnlineMode)
            {
                _preparePanel.SetActive(false);
            }
            else
            {
                _preparePanel.SetActive(true);
            }

            _winPanel.SetActive(false);
            _replayPanel.SetActive(false);
            _postReplayPanel.SetActive(false);
            _replayControls.SetActive(false);

            if (!NetworkManagerBattleship.IsOnlineMode)
            {
                SetPlayerText(0);
            }
        }

        void OnEnable()
        {
            if (NetworkManagerBattleship.IsOnlineMode)
            {
                OnlineGameManager.OnOnlineTurnChanged += HandleOnlineTurnChanged;
                NetworkPlayer.OnOpponentDisconnected += HandleOpponentDisconnected;
            }
        }

        void OnDisable()
        {
            OnlineGameManager.OnOnlineTurnChanged -= HandleOnlineTurnChanged;
            NetworkPlayer.OnOpponentDisconnected -= HandleOpponentDisconnected;
        }

        public void TogglePanel()
        {
            _preparePanel.SetActive(!_preparePanel.activeSelf);
        }

        public void SetPlayerText(int index) 
        {
            _playerText.text = $"PLAYER {index + 1}";
        }

        public void SetPlayersTextOpacity(int activePlayer, float playerAlpha, int opponent, float opponentAlpha)
        {
            _boardTexts[activePlayer].color = new Color(1, 1, 1, playerAlpha);
            _boardTexts[opponent].color = new Color(1, 1, 1, opponentAlpha);
        }

        public void FadeImage()
        {
            _imageFader.FadeOut();
        }

        public void SetDisplayWinPanel(int index)
        {
            _winnerText.text = $"PLAYER {index + 1}";
            _winPanel.SetActive(true);
        }

        /// <summary>
        /// Displays a win/lose panel for online mode with a custom message.
        /// </summary>
        public void SetDisplayOnlineWinPanel(string resultText)
        {
            _winnerText.text = resultText;
            _winPanel.SetActive(true);

            // Hide replay option in online mode (replay is for local mode only)
            if (_replayPanel != null)
                _replayPanel.SetActive(false);
        }

        public void DisplayPostReplayPanel()
        {
            _postReplayPanel.SetActive(true);
            _replayControls.SetActive(false);
        }

        public void ChangeLoadingText(string text)
        {
            _loadingText.text = text;
            _imageFader.Enable();
        }

        public void StoreInput()
        {
            string input = _inputField.text;
            int number;

            if (!int.TryParse(input, out number))
            {
                _warningText.text = "Input a whole number!";
                return;
            }

            if (number <= 0 || number > 10)
            {
                _warningText.text = "Enter number 1-10!";
                return;
            }

            _warningText.text = $"Move frequency: {number}";
            FindObjectOfType<ReplaySystem>().MoveInterval = number;
        }

        #region ONLINE UI

        /// <summary>
        /// Sets up the UI for online mode after the game is ready.
        /// Shows the local player's identity and turn status.
        /// </summary>
        public void SetOnlinePlayerInfo()
        {
            if (NetworkPlayer.LocalPlayer == null) return;

            int localIndex = NetworkPlayer.LocalPlayer.PlayerIndex.Value;
            SetPlayerText(localIndex);

            UpdateOnlineStatus("Game started! Waiting for your turn...");

            // Show own ships, hide opponent ships
            if (GameFlowSystem.Instance != null)
            {
                // Show local player's ships
                int opponentIndex = localIndex == 0 ? 1 : 0;
                foreach (var ship in GameFlowSystem.Instance.Players[localIndex].PlacedShipsList)
                {
                    foreach (Transform child in ship.transform)
                        child.gameObject.SetActive(true);
                }

                // Hide opponent's ships
                foreach (var ship in GameFlowSystem.Instance.Players[opponentIndex].PlacedShipsList)
                {
                    foreach (Transform child in ship.transform)
                        child.gameObject.SetActive(false);
                }
            }
        }

        /// <summary>
        /// Updates the online status text (e.g., "Your turn!", "Waiting for opponent...").
        /// </summary>
        public void UpdateOnlineStatus(string status)
        {
            if (_onlineStatusText != null)
                _onlineStatusText.text = status;
        }

        void HandleOnlineTurnChanged(int currentPlayerIndex)
        {
            if (NetworkPlayer.LocalPlayer == null) return;

            int localIndex = NetworkPlayer.LocalPlayer.PlayerIndex.Value;
            bool isMyTurn = currentPlayerIndex == localIndex;

            UpdateOnlineStatus(isMyTurn ? "YOUR TURN - Click a tile!" : "Waiting for opponent...");

            if (_onlineTurnIndicator != null)
                _onlineTurnIndicator.SetActive(isMyTurn);
        }

        void HandleOpponentDisconnected()
        {
            UpdateOnlineStatus("Opponent disconnected!");
        }

        #endregion
    }
}
