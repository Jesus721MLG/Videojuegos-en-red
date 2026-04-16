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
        #endregion

        void Start()
        {
            _preparePanel.SetActive(true);
            _winPanel.SetActive(false);
            _replayPanel.SetActive(false);
            _postReplayPanel.SetActive(false);
            _replayControls.SetActive(false);
            SetPlayerText(0);
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
    }
}
