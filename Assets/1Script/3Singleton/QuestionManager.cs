using System.Collections.Generic;
using UnityEngine;


public class QuestionInfo
{
    string _question;
    public string Question
    {
        get { return _question; }
        set { _question = value; }
    }

}
public class QuestionManager : Singleton<QuestionManager>, IQuestionTarget
{
    List<QuestionInfo> questionInfos = new List<QuestionInfo>(16);
    List<QuestionInfo>[] _cachedRELATIONs;
    int _cartridge = 1;


    QuestionInfo morsePass = new QuestionInfo();

    //TODO 퀘스천 선택지 있으니 D1한것처럼 선택지 적용

    int _currentIndex = 1;

    public int CurrentIndex
    {
        get { return _currentIndex; }
        set
        {
            Debug.Log($"퀘스천 인덱스 CurrentIndex  {value}");
            _currentIndex = value;
        }
    }



    public string CurrentQuestionText
    {
        get
        {
            return questionInfos[_currentIndex].Question;
        }
    }

    public QuestionInfo CurrentMorsePass
    {
        get { return morsePass; }
    }




    public List<QuestionInfo> QuestionInfos
    {
        get { return questionInfos; }
    }

    public int QuestionCount
    {
        get { return questionInfos.Count; }
    }

    public void Initialize(List<QuestionInfo> items)
    {
        for (int i = 0; i < items.Count; i++)
        {
            Debug.Log($"{i} : {items[i].Question}");
        }
        questionInfos = items;

        Debug.Log("로드된 질문 수: " + items.Count);
    }

    public List<QuestionInfo> Data()
    {
        return questionInfos;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (_cachedRELATIONs == null || _cachedRELATIONs.Length == 0) return;
            int next = _cartridge + 1;
            if (next > _cachedRELATIONs.Length)
            {
                next = 1;
            }
            SetRELATION(next);
        }
    }
    public void SetRELATION(int value)
    {
        if (_cachedRELATIONs == null || _cachedRELATIONs.Length == 0) return;

        int index = Mathf.Clamp(value - 1, 0, _cachedRELATIONs.Length - 1);
        _cartridge = index + 1;
        Initialize(_cachedRELATIONs[index]);
        Debug.Log($"카트리지 적용: request={value}, selected={_cartridge}, index={index}, questionCount={questionInfos.Count}");
    }

    public void InitializeCartridges(List<QuestionInfo>[] cartridges)
    {
        _cachedRELATIONs = cartridges;
        Debug.Log($"카트리지 초기화 완료: {_cachedRELATIONs.Length} cartridges loaded.");

        foreach (var cartridge in _cachedRELATIONs)
        {
            Debug.Log($"카트리지 질문 수: {cartridge?.Count ?? 0}");
        }
    }
}
