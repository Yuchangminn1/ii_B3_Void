using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StepDataManager : Singleton<StepDataManager>
{
    public event Action<int> OnScoreDataChanged;
    public event Action<int> OnStampDataChanged;


    private int _currentScore = 0;

    private int _currentStamp = 0;


    bool[] leftSuccess = new bool[10];
    bool[] rightSuccess = new bool[10];


    public int CurrentScore
    {
        get { return _currentScore; }
        set
        {
            if (_currentScore != value)
            {
                _currentScore = value;
                //UserDataManager.Instance.GetPlayer().PieceCount = _currentScore;

                OnScoreDataChanged?.Invoke(_currentScore);

                if (value > 0 && (value / 2) > _currentStamp)
                {
                    CurrentStamp = value / 2;
                }
            }
        }
    }

    public int CurrentStamp
    {
        get { return _currentStamp; }
        set
        {
            if (_currentStamp != value)
            {
                _currentStamp = value;
                UserDataManager.Instance.GetPlayer().AddPiece = _currentStamp;
                OnStampDataChanged?.Invoke(_currentStamp);
            }
        }
    }


    public void SetSuccess(Direction direction, int index)
    {
        Debug.Log($"SetSuccess: Direction={direction}, Index={index}");
        if (index >= 0 && index < leftSuccess.Length)
        {
            if (direction == Direction.Left)
            {
                leftSuccess[index] = true;
            }
            else if (direction == Direction.Right)
            {
                rightSuccess[index] = true;
            }
        }
        int score = 0;

        for (int i = 0; i <= index; i++)
        {
            if (leftSuccess[i] && rightSuccess[i])
            {
                score++;
            }
        }

        CurrentScore = score;
        Debug.Log($"CurrentScore updated to {CurrentScore} based on success arrays.");
    }



    void Start()
    {

        PageController.Instance.OnReset += Reset;

    }


    void Reset()
    {
        _currentScore = 0;

        _currentStamp = 0;

        for (int i = 0; i < leftSuccess.Length; i++)
        {
            leftSuccess[i] = false;
        }
        for (int i = 0; i < rightSuccess.Length; i++)
        {
            rightSuccess[i] = false;
        }

    }
}
