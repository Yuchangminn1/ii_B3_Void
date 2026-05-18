using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;




public class QuestionScript : MonoBehaviour
{
    public Direction CurrentDirection;
    const int MAX_QUESTION_COUNT = 15;

    public ShootPieceContainer shootPieceContainer;

    public Timer timer;

    public QuestionShadow questionShadow;

    public Text[] QuestionText;


    public SequenceScript[] endTrigger;


    public PageBase[] pageBase;



    Coroutine _nextQuestionCoroutine = null;

    public GameObject[] ResetCarrier;

    int[] cameraIndex = new int[] { 5, 10, 15 };

    bool _isReturnPiece = false;

    public bool IsReturnPiece
    {
        get { return _isReturnPiece; }
        set { _isReturnPiece = value; }
    }





    bool _shadowCheck = false;

    void Awake()
    {

    }

    void OnEnable()
    {
        if (GameManager.Instance.IsStarted == false)
            return;

        Reset();
        if (CurrentDirection == Direction.Left)
        {
            for (int i = 0; i < QuestionText.Length; i++)
            {
                QuestionText[i].text = QuestionManager.Instance.CurrentQuestionTextLeft;
            }
        }

        else if (CurrentDirection == Direction.Right)
        {
            for (int i = 0; i < QuestionText.Length; i++)
            {
                QuestionText[i].text = QuestionManager.Instance.CurrentQuestionTextRight;
            }
        }

        Debug.Log($"현재 질문 : {QuestionText[0].text}");

        for (int i = 0; i < ResetCarrier.Length; i++)
        {
            ResetCarrier[i]?.SetActive(true);

        }


    }
    void Start()
    {

    }



    public void Reset()
    {
        QuestionManager.Instance.CurrentIndex = 0;
        if (CurrentDirection == Direction.Left)
        {
            for (int i = 0; i < QuestionText.Length; i++)
            {
                QuestionText[i].text = QuestionManager.Instance.CurrentQuestionTextLeft;
            }
        }

        else if (CurrentDirection == Direction.Right)
        {
            for (int i = 0; i < QuestionText.Length; i++)
            {
                QuestionText[i].text = QuestionManager.Instance.CurrentQuestionTextRight;
            }
        }
    }

    public void NextQuestion()
    {
        if (_nextQuestionCoroutine != null)
        {
            StopCoroutine(_nextQuestionCoroutine);
        }
        _nextQuestionCoroutine = StartCoroutine(NextQuestionCoroutine());
    }

    public void ShowShadow()
    {
        _shadowCheck = true;
    }

    public void EndReturnPiece()
    {
        _isReturnPiece = true;
    }

    public IEnumerator NextQuestionCoroutine()
    {

        QuestionManager.Instance.CurrentIndex++;

        yield return CoroutineReturnManager.GetWaitForSeconds(0.1f);


        if (QuestionManager.Instance.CurrentIndex == cameraIndex[0] || QuestionManager.Instance.CurrentIndex == cameraIndex[1] || QuestionManager.Instance.CurrentIndex == cameraIndex[2])
        {
            _shadowCheck = false;

            _isReturnPiece = false;

            shootPieceContainer.ReturnShoot(EndReturnPiece);

            while (_isReturnPiece == false)
            {
                yield return CoroutineReturnManager.GetWaitForSeconds(0.25f);

            }



            while (shootPieceContainer.IsTutorialClear() == false)
            {
                yield return CoroutineReturnManager.GetWaitForSeconds(1f);
            }

        }

        yield return CoroutineReturnManager.GetWaitForSeconds(0.5f);
        int currentIndx = QuestionManager.Instance.CurrentIndex;
        int questionCount = QuestionManager.Instance.QuestionCount;
        if (currentIndx == questionCount)
        {
            Debug.Log($"{currentIndx}번째 질문입니다. Count = {questionCount} 다음 장으로 넘어갑니다.");
            yield return CoroutineReturnManager.GetWaitForSeconds(0.5f);
            for (int i = 0; i < endTrigger.Length; i++)
            {
                endTrigger[i]?.TriggerOn();

            }
            yield break;
        }

        for (int i = 0; i < ResetCarrier.Length; i++)
        {
            ResetCarrier[i]?.SetActive(false);
        }

        yield return CoroutineReturnManager.GetWaitForSeconds(0.5f);

        for (int i = 0; i < ResetCarrier.Length; i++)
        {
            ResetCarrier[i]?.SetActive(true);
        }
        for (int i = 0; i < pageBase.Length; i++)
        {
            pageBase[i]?.ResetValue();
        }
        string nextQuestion;
        if (CurrentDirection == Direction.Left)
        {
            nextQuestion = QuestionManager.Instance.CurrentQuestionTextLeft;
        }
        else
        {
            nextQuestion = QuestionManager.Instance.CurrentQuestionTextRight;
        }


        for (int i = 0; i < QuestionText.Length; i++)
        {
            QuestionText[i].text = nextQuestion;
        }
        Debug.Log($"다음 질문으로 넘어갑니다. 현재 질문 : {nextQuestion}");

        timer?.SetTextInvisible();

        _nextQuestionCoroutine = null;
    }

}
