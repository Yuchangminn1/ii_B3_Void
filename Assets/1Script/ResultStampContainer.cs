using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class ResultStampContainer : MonoBehaviour
{
    public AnswerStamp[] answerStamps = new AnswerStamp[5];


    public SequenceScript sequenceScript;

    public Texture emptyStampTexture;
    public Texture correctStampTexture;


    Coroutine showStampCoroutine = null;
    void OnEnable()
    {
        Reset();

    }


    virtual protected void Start()
    {
        answerStamps = GetComponentsInChildren<AnswerStamp>();

        foreach (var stamp in answerStamps)
        {
            //Debug.Log($"{stamp.name}");
            stamp.SetTextures(emptyStampTexture, correctStampTexture);
        }
    }
    virtual public void Reset()
    {
        showStampCoroutine = null;
    }


    public void ShowStamp()
    {
        if (showStampCoroutine == null)
            showStampCoroutine = StartCoroutine(ShowStampCoroutine());
    }

    public IEnumerator ShowStampCoroutine()
    {
        int stampCount = UserDataManager.Instance.GetPlayer().AddPiece;

        yield return CoroutineReturnManager.GetWaitForSeconds(1f); //페이지 전환 대기 타임
        UserDataManager.Instance.UserPieceUpdate();

        Debug.Log($"스탬프 개수: {stampCount}");
        for (int i = 0; i < stampCount; i++)
        {
            answerStamps[i].SetCorrectStamp();
            yield return CoroutineReturnManager.GetWaitForSeconds(0.8f);
        }
        while (sequenceScript.TriggerOnBool() == false)
        {
            yield return CoroutineReturnManager.GetWaitForSeconds(0.1f);
        }

        showStampCoroutine = null;
    }
}
