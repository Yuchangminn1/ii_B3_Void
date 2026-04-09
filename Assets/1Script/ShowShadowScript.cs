using System.Collections;
using UnityEngine;
using UnityEngine.UI;


public class ShowShadowScript : MonoBehaviour
{
    public Direction CurrentDirection;

    public RawImage[] ShadowImages;

    public RawImage successImage;

    public RawImage failImage;

    public AcCheck CurrentAcCheck;

    public SaveShadowTextureContainer SaveShadowTextureContainer;


    int currentIndex = 0;

    RawImage CurrentTargetRawImage
    {
        get { return CurrentAcCheck.targetRawImage; }
    }


    public int GetShowImageLength() { return ShadowImages.Length; }


    public void SetACcheck(AcCheck ac)
    {
        CurrentAcCheck = ac;
    }

    private void OnEnable()

    {
        Reset();
    }

    void Start()
    {
        PageController.Instance.OnReset += Reset;


    }

    public void CaptureTexture()
    {
        if (currentIndex > 0)
        {
            SaveShadowTextureContainer.SetTexture(currentIndex);
        }
    }

    public void ShowSuccess()
    {
        Debug.Log($"{name} ShowSuccess");
        SoundManager.Instance.PlayEffectSound(EffectSoundNum.ClearShadowSound);

        failImage.gameObject.SetActive(false);
        successImage.gameObject.SetActive(true);


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
        SoundManager.Instance.PlayEffectSound(EffectSoundNum.FailSound);

        successImage.gameObject.SetActive(false);
        failImage.gameObject.SetActive(true);



        // StartCoroutine(ShowFailDelay());
    }

    IEnumerator ShowFailDelay()
    {
        yield return new WaitForSeconds(1f);
        ClearShadow();

    }

    public void ClearShadow()
    {
        Debug.Log($"{name}ClearShadow");

        if (CurrentAcCheck != null && CurrentAcCheck.targetRawImage != null)
        {
            CurrentTargetRawImage.gameObject.SetActive(false);

            CurrentAcCheck = null;
        }
        for (int i = 0; i < ShadowImages.Length; i++)
        {
            ShadowImages[i].gameObject.SetActive(false);
        }
    }

    public void Reset()
    {
        Debug.Log($"{name}Reset");

        successImage.gameObject.SetActive(false);
        failImage.gameObject.SetActive(false);
        for (int i = 0; i < ShadowImages.Length; i++)
        {
            ShadowImages[i].gameObject.SetActive(false);
        }
    }

    public void ShowShadow(int index)
    {
        currentIndex = index;
        for (int i = 0; i < ShadowImages.Length; i++)
        {
            ShadowImages[i].gameObject.SetActive(i == index);
        }
        SoundManager.Instance.PlayEffectSound(EffectSoundNum.ShowShadowSound);
        Debug.Log($"{name} ShowShadow index: {index}");
        CurrentAcCheck.SetTargetRawImage(ShadowImages[index]);
    }

}
