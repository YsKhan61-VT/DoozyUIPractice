﻿using System;

namespace DoozyPractice.Gameplay
{
    public class GameEndState : GameState
    {
        public override GameStateCategory StateCategory => GameStateCategory.GameEnd;

        private TurnController _turnController;
        private GameplayUIMediator _gameplayUIMediator;

        public GameEndState(
            GameStateManager gameStateManager, 
            TurnController turnController,
            GameplayUIMediator gameplayUIMediator)
        {
            StateManager = gameStateManager;
            _turnController = turnController;
            _gameplayUIMediator = gameplayUIMediator;
        }

        public override void Enter()
        {
            StateManager.OnGameEnds?.Invoke();
            ProcessResult();
        }

        public override void Exit()
        {
            throw new System.NotImplementedException();
        }

        public override void Update()
        {
            throw new System.NotImplementedException();
        }

        void ProcessResult()
        {
            int scoreDiff = _turnController.OtherTotalScore - _turnController.OwnerTotalScore;

            switch (scoreDiff)
            {
                case 0:
                    _gameplayUIMediator.ShowDrawText();
                    break;

                case > 0:
                    _gameplayUIMediator.ShowLossText();
                    break;

                case < 0:
                    _gameplayUIMediator.ShowWinText();
                    break;
            }
        }
    }

}
