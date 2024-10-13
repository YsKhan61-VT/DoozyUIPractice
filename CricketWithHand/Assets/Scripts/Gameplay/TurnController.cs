using UnityEngine;
using UnityEngine.Events;
using CricketWithHand.Utility;
using System.Threading.Tasks;


namespace CricketWithHand.Gameplay
{
    public class TurnController : MonoBehaviour
    {

        #region Events

        /// <summary>
        /// Invoked when first or second half starts
        /// </summary>
        public UnityEvent OnNextHalfStarted;

        /// <summary>
        /// Invoked when the next over started.
        /// </summary>
        public UnityEvent OnNextOverStarted;

        /// <summary>
        /// Called when the next turn is starting. User input panel needs to be activated.
        /// </summary>
        public UnityEvent OnNextTurnCountdownStarted;

        /// <summary>
        /// Called when the next turn duration ends. User input panel needs to be deactivated.
        /// </summary>
        public UnityEvent OnNextTurnCountdownEnded;

        #endregion

        #region Game Data Containers

        [SerializeField]
        private GameDataSO _gameData;

        #endregion

        [SerializeField]
        GameStateManager _gameStateManager;

        int _totalScore
        {
            get
            {
                if (_gameData.IsOwnerBatting)
                    return _gameData.OwnerTotalScoreContainer.Value;
                else
                    return _gameData.OtherTotalScoreContainer.Value;
            }

            set
            {
                if (_gameData.IsOwnerBatting)
                    _gameData.OwnerTotalScoreContainer.UpdateData(value);
                else
                    _gameData.OtherTotalScoreContainer.UpdateData(value);
            }
        }

        /// <summary>
        /// Use this only in either of First half or Second half state, while a player is batting
        /// </summary>
        int _currentBallCount
        {
            get
            {
                if (_gameData.IsOwnerBatting)
                    return _gameData.OwnerBallCountContainer.Value;
                else
                    return _gameData.OtherBallCountContainer.Value;
            }

            set

            {
                if (_gameData.IsOwnerBatting)
                    _gameData.OwnerBallCountContainer.UpdateData(value);
                else
                    _gameData.OtherBallCountContainer.UpdateData(value);
            }
        }

        /// <summary>
        /// Use this only in either of First half or Second half state, while a player is batting
        /// </summary>
        int _currentOverCount
        {
            get
            {
                if (_gameData.IsOwnerBatting)
                    return _gameData.OwnerOverCountContainer.Value;
                else
                    return _gameData.OtherOverCountContainer.Value;
            }

            set
            {
                if (_gameData.IsOwnerBatting)
                    _gameData.OwnerOverCountContainer.UpdateData(value);
                else
                    _gameData.OtherOverCountContainer.UpdateData(value);
            }
        }

        int _turnScore
        {
            get
            {
                if (_gameData.IsOwnerBatting)
                    return _gameData.OwnerTurnScoreContainer.Value;
                else
                    return _gameData.OtherTurnScoreContainer.Value;
            }

            set
            {
                if (_gameData.IsOwnerBatting)
                    _gameData.OwnerTurnScoreContainer.UpdateData(value);
                else
                    _gameData.OtherTurnScoreContainer.UpdateData(value);
            }
        }

        BallData _turnData
        {
            get
            {
                if (_gameData.IsOwnerBatting)
                    return _gameData.OwnerBallDataContainer.Value;
                else
                    return _gameData.OtherBallDataContainer.Value;
            }

            set
            {
                if (_gameData.IsOwnerBatting)
                    _gameData.OwnerBallDataContainer.UpdateData(value);
                else
                    _gameData.OtherBallDataContainer.UpdateData(value);
            }
        }

        int _inputScore
        {
            get
            {
                if (_gameData.IsOwnerBatting)
                    return _gameData.OwnerInputScoreContainer.Value;
                else
                    return _gameData.OtherInputScoreContainer.Value;
            }
            
            set
            {
                if (_gameData.IsOwnerBatting)
                    _gameData.OwnerInputScoreContainer.UpdateData(value);
                else
                    _gameData.OtherInputScoreContainer.UpdateData(value);
            }
        }

        int _totalWicketsLost
        {
            get
            {
                if (_gameData.IsOwnerBatting)
                    return _gameData.OwnerTotalWicketLostContainer.Value;
                else
                    return _gameData.OtherTotalWicketLostContainer.Value;
            }

            set
            {
                if (_gameData.IsOwnerBatting)
                    _gameData.OwnerTotalWicketLostContainer.UpdateData(value);
                else
                    _gameData.OtherTotalWicketLostContainer.UpdateData(value);
            }
        }

        bool _isOutOnThisTurn
        {
            get
            {
                if (_gameData.IsOwnerBatting)
                    return _gameData.OwnerIsOutOnThisTurnContainer.Value;
                else
                    return _gameData.OtherIsOutOnThisTurnContainer.Value;
            }

            set
            {
                if (_gameData.IsOwnerBatting)
                    _gameData.OwnerIsOutOnThisTurnContainer.UpdateData(value);
                else
                    _gameData.OtherIsOutOnThisTurnContainer.UpdateData(value);
            }
        }
       

        float _timeElapsedSinceTurnCountdownStarted = 0;
        bool _pauseCountdown = true;

        void Start()
        {
            _gameData.ResetRuntimeDatas();
        }


        void Update()
        {
            TryExecuteNextTurn();
        }

        public void StartHalf()
        {
            if (!PassPreChecks())
            {
                Debug.LogError("Pre Checks not passed!");
                return;
            }

            ResetForNewHalf();

            OnNextHalfStarted?.Invoke();

            StartNextOver();

            StartNextTurn();
        }

        public void RegisterOwnerInput(int scoreValue) =>
            _gameData.OwnerInputScoreContainer.UpdateData(scoreValue);

        public void RegisterOtherInput(int scoreValue) =>
            _gameData.OtherInputScoreContainer.UpdateData(scoreValue);

        async void TryExecuteNextTurn()
        {
            if (!HasTurnCountdownEnded())
                return;

            OnNextTurnCountdownEnded?.Invoke();

            _currentBallCount++;

            CalculateScoreAndWicketsLost();

            await Task.Delay(_gameData.GameConfig.WaitForSecsBeforeNextTurn * 1000);

            if (IsGameEndConditionsMatching())
            {
                EndGame();
                return;
            }

            if (HasAllOversOfThisHalfEnded() || IsCurrentHalfBattingPlayerAllOut())
            {
                ExecuteHalfTIme();
                return;
            }

            if (IsCurrentOverComplete() && !HasAllOversOfThisHalfEnded())
            {
                StartNextOver();
            }

            StartNextTurn();
        }

        void ResetForNewHalf()
        {
            _currentOverCount = 0;
            _currentBallCount = 0;
        }

        void ExecuteHalfTIme()
        {
            _gameStateManager.ChangeGameState(GameStateCategory.HalfTime);
        }

        bool HasTurnCountdownEnded()
        {
            if (_pauseCountdown) return false;

            _timeElapsedSinceTurnCountdownStarted += Time.deltaTime;
            _gameData.TurnSliderValue.UpdateData(1 - (_timeElapsedSinceTurnCountdownStarted / _gameData.GameConfig.TurnDurationInSecs));

            if (_timeElapsedSinceTurnCountdownStarted < _gameData.GameConfig.TurnDurationInSecs)
                return false;
            else
            {
                _pauseCountdown = true;
                return true;
            }
        }

        void StartNextOver()
        {
            _currentOverCount++;
            OnNextOverStarted?.Invoke();
        }

        void StartNextTurn()
        {
            _timeElapsedSinceTurnCountdownStarted = 0;
            _gameData.TurnSliderValue.UpdateData(1);
            _pauseCountdown = false;
            
            _gameData.OwnerInputScoreContainer.UpdateData(0);
            _gameData.OtherInputScoreContainer.UpdateData(0);

            _isOutOnThisTurn = false;

            OnNextTurnCountdownStarted?.Invoke();
        }

        void EndGame()
        {
            _gameStateManager.ChangeGameState(GameStateCategory.GameEnd);
        }

        void CalculateScoreAndWicketsLost()
        {
            // Check if is out
            _isOutOnThisTurn = _gameData.OwnerInputScoreContainer.Value == _gameData.OtherInputScoreContainer.Value;

            _turnScore = _isOutOnThisTurn ? 0 : _gameData.IsOwnerBatting ? _gameData.OwnerInputScoreContainer.Value : _gameData.OtherInputScoreContainer.Value;
            _totalScore += _turnScore;
            _turnData = new BallData
            {
                OverNumber = _currentOverCount,
                BallNumber = _currentBallCount,
                Score = _turnScore,
                IsWicketLost = _isOutOnThisTurn,
            };

            if (!_isOutOnThisTurn)
                return;

            _totalWicketsLost++;
        }

        /// <summary>
        /// Is the total balls of the current over completed playing.
        /// </summary>
        bool IsCurrentOverComplete() => _currentBallCount == _gameData.GameConfig.BallsPerOver;

        /// <summary>
        /// Is this over the last over of this half
        /// </summary>
        bool IsThisLastOverOfThisHalf() => _currentOverCount == _gameData.TotalOversCountDataContainer.Value;

        /// <summary>
        /// Has the current half batting player lost all wickets.
        /// </summary>
        bool IsCurrentHalfBattingPlayerAllOut() => _totalWicketsLost == _gameData.TotalWicketsCountDataContainer.Value;

        /// <summary>
        /// Is this second half running
        /// </summary>
        bool IsSecondHalf() => _gameData.CurrentGameStateCategory.Value == GameStateCategory.SecondHalf;

        /// <summary>
        /// If both teams have batted AND second half batting team is all out.
        /// </summary>
        bool IsSecondHalfBattingPlayerAllOut() => IsSecondHalf() && IsCurrentHalfBattingPlayerAllOut();

        /// <summary>
        /// has all the overs of this half been played
        /// </summary>
        bool HasAllOversOfThisHalfEnded() => IsCurrentOverComplete() && IsThisLastOverOfThisHalf();

        /// <summary>
        /// Has all the overs of the second half been played
        /// </summary>
        bool HasAllOversOfSecondHalfEnded() => IsSecondHalf() && HasAllOversOfThisHalfEnded();

        /// <summary>
        /// Both teams started batting and Has the owner's score crossed the other's score
        /// </summary>
        bool HasOwnerWon()
        {
            bool won = IsSecondHalf() && 
                _gameData.OwnerTotalScoreContainer.Value > _gameData.OtherTotalScoreContainer.Value;
            if (won)
                _gameData.Winner.UpdateData(PlayerType.OWNER);
            return won;
        }

        /// <summary>
        /// Both teams started batting and Has the other team's score crossed the owner's score
        /// </summary>
        bool HasOtherWon()
        {
            bool won = IsSecondHalf() &&
                _gameData.OtherTotalScoreContainer.Value > _gameData.OwnerTotalScoreContainer.Value;
            if (won)
                _gameData.Winner.UpdateData(PlayerType.OTHER);
            return won;
        }

        /// <summary>
        /// Is the game a draw
        /// </summary>
        bool IsTargetDraw()
        {
            bool isDraw = (IsSecondHalfBattingPlayerAllOut() || HasAllOversOfSecondHalfEnded()) && _gameData.OwnerTotalScoreContainer.Value == _gameData.OtherTotalScoreContainer.Value;
            if (isDraw)
                _gameData.Winner.UpdateData(PlayerType.NONE);
            return isDraw;
        }

        /// <summary>
        /// If this conditions match, we end the game.
        /// </summary>
        bool IsGameEndConditionsMatching() =>
            IsTargetDraw() ||
            HasOwnerWon() ||
            HasOtherWon() ||
            HasAllOversOfSecondHalfEnded() ||
            IsSecondHalfBattingPlayerAllOut();

        bool PassPreChecks()
        {
            if (_gameData.GameConfig.BallsPerOver == 0 || _gameData.TotalOversCountDataContainer.Value == 0)
            {
                gameObject.SetActive(false);
                return false;
            }
            return true;
        }
    }
}
