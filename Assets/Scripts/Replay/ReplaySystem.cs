using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Battleship
{
    public class ReplaySystem : MonoBehaviour
    {
        public int MoveInterval = 1;
        List<Tile> _playedTiles = new List<Tile>();
        Coroutine _replayCoroutine;
        bool _replayStarted;
        bool _autoReplay;
        int _currentReplayIndex;

        public List<Tile> PlayedTiles => _playedTiles;

        public void AddToTileList(Tile tile)
        {
            if (!_replayStarted)
                _playedTiles.Add(tile);
        }

        public void StartReplay() 
        {
            _replayStarted = true;
            _currentReplayIndex = 0;
            GameFlowSystem.Instance.SetState(new Replay(GameFlowSystem.Instance));
        }

        public void ReplayTileActions()
        {
            _autoReplay = true;
            _replayCoroutine = StartCoroutine(CO_ReplayTileActions());
        }

        public void AutoPlay()
        {
            if (CheckForReplayFinished())
                return;

            _replayCoroutine = StartCoroutine(CO_ReplayTileActions());
        }

        public void StopAutoPlay()
        {
            CheckForCoroutine();
        }

        public void GoForward()
        {
            if (CheckForReplayFinished())
                return;

            CheckForCoroutine();
            _playedTiles[_currentReplayIndex].ReplayTile();
            _currentReplayIndex++;
        }

        public void GoBack()
        {
            if (CheckForReplayFinished() || _currentReplayIndex <= 0)
                return;

            CheckForCoroutine();
            _currentReplayIndex--;
            _playedTiles[_currentReplayIndex].UndoTile();
        }

        IEnumerator CO_ReplayTileActions()
        {
            _autoReplay = true;

            for (int i = _currentReplayIndex; i < _playedTiles.Count; i++)
            {
                _playedTiles[i].ReplayTile();
                _currentReplayIndex++;
                yield return new WaitForSeconds(MoveInterval);
            }
        }

        void CheckForCoroutine()
        {
            if (_autoReplay)
            {
                StopCoroutine(_replayCoroutine);
                _autoReplay = false;
            }
        }

        public bool CheckForReplayFinished()
        {
            return _currentReplayIndex == _playedTiles.Count;
        }
    }
}
