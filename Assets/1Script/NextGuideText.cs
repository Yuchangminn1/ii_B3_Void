using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class NextGuideText : MonoBehaviour
{
    public RawImage EndLineImage;
    Text text;
    string[] _changeTexts = { "체험이 완료되었습니다.\n카드에 표시된 블록으로 이동해 주세요.", "모든 체험이 완료되었습니다.\n결과 출력 공간으로 이동해 주세요." };



    void Start()
    {
        text = GetComponent<Text>();
    }

    public void SetText()
    {
        StartCoroutine(SetTextCoroutine());
    }

    IEnumerator SetTextCoroutine()
    {
        if (UserDataManager.Instance.GetPlayer().IsAllContentPlayed)
        {
            if (EndLineImage != null)
                EndLineImage.color = Color.white;
            Debug.Log("모든 체험이 완료되었습니다.");
            text.text = _changeTexts[1];
        }
        else
        {
            if (EndLineImage != null)

                EndLineImage.color = Color.clear;

            Debug.Log("다음 체험으로 가시오");

            text.text = _changeTexts[0];
        }
        yield return CoroutineReturnManager.GetWaitForSeconds(0.1f);
        FadeManager.Instance.SetAlphaOne(text);
    }

    void OnDisable()
    {
        text.text = "";
    }
}
