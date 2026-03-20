using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class QuestionScript : MonoBehaviour
{
    const int MAX_QUESTION_COUNT = 15;

    int currentIndex = 0;

    // string[] questions = {
    //     "Q. 나는 상대방의 기분을 헤아리기 위해 많은 에너지를 쏟는다.",
    //     "Q1. 대화 중 상대의 시선이 어디를 향하고 있는지 살피게 된다.",
    //      "Q2. 상대의 작은 미소나 고개 끄덕임에서도 긍정적인 반응을 느낀다.",
    //       "Q3. 상대가 나와의 거리나 자세를 바꿀 때 그 변화를 느낀다.",
    //        "Q4. 상대의 표정이 평소와 조금만 달라져도 바로 눈에 들어온다.",
    //         "Q5. 말하지 않아도 상대방의 컨디션 변화를 잘 알아차린다.",
    //         "Q6. 상대의 행동 중 큰 의미 없이 반복되는 습관을 구분할 수 있다.",
    //          "Q7. 상대가 웃고 있어도 어딘가 어색해 보이면 그 이유를 생각하게 된다.",
    //          "Q8. 상대의 작은 반응 하나하나가 나에 대한 신호처럼 느껴질 때가 있다.",
    //           "Q9. 상대의 반응이 미묘하게 달라졌을 때, 그 변화를 그냥 넘기지 않는 편이다.",
    //            "Q10. 상대의 표정을 보고 지금 대화를 이어갈지 판단한다. ",
    //            "Q11. 나는 무의식적인 반응이 더 솔직하다고 느낄 때가 있다.",
    //             "Q12. 상대의 반응이 자연스러울 때, 나도 그 분위기를 편안하게 느낀다.",
    //             "Q13. 상대가 기분이 좋을 때 나타나는 작은 변화들을 알고 있다.",
    //             "Q14. 대화를 할 때 말의 내용보다는 표정이나 자세를 통해 분위기를 읽는 편이다." ,
    //             "Q15. 상대가 나를 어떻게 바라보고 있는지에 따라 내 태도가 달라질 때가 있다."
    //             };

    public Text QuestionText;


    public SequenceScript endTrigger;

    public PageBase pageBase;



    WaitForSeconds delayWait = new WaitForSeconds(1f);

    Coroutine _nextQuestionCoroutine = null;

    public GameObject ResetCarrier;

    Direction _currentDirection;




    void Awake()
    {
        pageBase = GetComponent<PageBase>();
    }

    void OnEnable()
    {
        if (GameManager.Instance.IsStarted == false)
            return;

        Reset();

        QuestionText.text = QuestionManager.Instance.CurrentQuestionText;

        ResetCarrier?.SetActive(true);

    }
    void Start()
    {
        _currentDirection = GetComponentInParent<TouchChecker>().currentDirection;
    }



    public void Reset()
    {
        if (_currentDirection == Direction.Left)
            QuestionManager.Instance.CurrentIndex = 0;
        QuestionText.text = QuestionManager.Instance.CurrentQuestionText;
    }

    public void NextQuestion()
    {
        if (_nextQuestionCoroutine != null)
        {
            StopCoroutine(_nextQuestionCoroutine);
        }
        _nextQuestionCoroutine = StartCoroutine(NextQuestionCoroutine());
    }

    public IEnumerator NextQuestionCoroutine()
    {
        //마지막 질문 넘어서 다음장으로


        if (_currentDirection == Direction.Left)
            QuestionManager.Instance.CurrentIndex++;

        yield return CoroutineReturnManager.GetWaitForSeconds(0.5f);
        int currentIndx = QuestionManager.Instance.CurrentIndex;
        int questionCount = QuestionManager.Instance.QuestionCount;
        if (currentIndx == questionCount)
        {
            Debug.Log($"{currentIndx}번째 질문입니다. Count = {questionCount} 다음 장으로 넘어갑니다.");
            yield return CoroutineReturnManager.GetWaitForSeconds(0.5f);
            endTrigger?.TriggerOn();
            yield break;
        }

        ResetCarrier?.SetActive(false);

        yield return CoroutineReturnManager.GetWaitForSeconds(0.5f);

        ResetCarrier?.SetActive(true);
        pageBase.ResetValue();
        string nextQuestion = QuestionManager.Instance.CurrentQuestionText;
        QuestionText.text = nextQuestion;
        Debug.Log($"다음 질문으로 넘어갑니다. 현재 질문 : {nextQuestion}");

        _nextQuestionCoroutine = null;
    }

}
