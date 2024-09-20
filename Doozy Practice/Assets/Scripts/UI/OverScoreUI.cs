using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


namespace DoozyPractice.UI
{
    public class OverScoreUI : MonoBehaviour
    {
        [Serializable]
        class BallScoreUI
        {
            public Image BackgroundImage;
            public TMP_Text ScoreText;
        }

        [SerializeField]
        BallScoreUI[] _ballScoreUIs;

        [SerializeField]
        Color _currentBallColor;

        [SerializeField]
        Color _initialBallColor;

        [SerializeField]
        Color _wicketColor;

        public void ResetBallUIs()
        {
            foreach (var ball in _ballScoreUIs)
            {
                ball.BackgroundImage.color = _initialBallColor;
                ball.ScoreText.text = "";
            }
        }

        public void UpdateScoreOnBallUI(int ballCount, int score)
        {
            int index = ballCount - 1;
            if (index < 0 || index >= _ballScoreUIs.Length)
            {
                Debug.LogError("This should not happen!");
            }

            if (score == 0)
            {
                _ballScoreUIs[index].BackgroundImage.color = _wicketColor;
                _ballScoreUIs[index].ScoreText.text = "W";
            }
            else
            {
                _ballScoreUIs[index].BackgroundImage.color = _currentBallColor;
                _ballScoreUIs[index].ScoreText.text = score.ToString();
            }
        }
    }
}
