using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class ResultStampContainer : MonoBehaviour
{
    public RawImage[] answerStamps = new RawImage[5];

    protected Color32 defaultColor = new Color32(128, 128, 128, 255);

    protected Color32 getStampColor = new Color32(128, 128, 128, 255);



    public SequenceScript sequenceScript;


    Coroutine showStampCoroutine = null;
    void OnEnable()
    {
        Reset();

    }


    virtual protected void Start()
    {
        answerStamps = GetComponentsInChildren<RawImage>();
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
        if (stampCount == 0)
        {
            Debug.Log("스탬프가 없습니다.");
        }
        else
        {
            for (int i = 0; i < stampCount; i++)
            {
                //answerStamps[i].SetCorrectStamp();
                yield return CoroutineReturnManager.GetWaitForSeconds(0.8f);
            }
        }

        while (sequenceScript.TriggerOnBool() == false)
        {
            yield return CoroutineReturnManager.GetWaitForSeconds(0.1f);
        }
        sequenceScript.TriggerOn();
        showStampCoroutine = null;
    }
}
