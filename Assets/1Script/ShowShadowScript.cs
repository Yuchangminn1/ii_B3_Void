using UnityEngine;
using UnityEngine.UI;


public class ShowShadowScript : MonoBehaviour
{

    public RawImage[] ShadowImages;

    public RawImage successImage;

    public RawImage failImage;

    public AcCheck CurrentAcCheck;


    public int GetShowImageLength() { return ShadowImages.Length; }


    public void SetACcheck(AcCheck ac)
    {
        CurrentAcCheck = ac;
    }

    private void OnEnable()

    {
        Reset();
    }

    public void ShowSuccess()
    {
        successImage.gameObject.SetActive(true);
        failImage.gameObject.SetActive(false);

        CurrentAcCheck = null;
    }

    public void ResultImageClear()
    {
        successImage.gameObject.SetActive(false);
        failImage.gameObject.SetActive(false);
    }

    public void ShowFail()
    {
        Debug.Log($"{name} ShowFail");

        successImage.gameObject.SetActive(false);
        failImage.gameObject.SetActive(true);



        CurrentAcCheck = null;
    }

    public void Reset()
    {
        successImage.gameObject.SetActive(false);
        failImage.gameObject.SetActive(false);
        for (int i = 0; i < ShadowImages.Length; i++)
        {
            ShadowImages[i].gameObject.SetActive(false);
        }
    }

    public void ShowShadow(int index)
    {
        for (int i = 0; i < ShadowImages.Length; i++)
        {
            ShadowImages[i].gameObject.SetActive(i == index);
        }
        CurrentAcCheck.SetTargetRawImage(ShadowImages[index]);

        CurrentAcCheck.StartCheck();

    }




}
