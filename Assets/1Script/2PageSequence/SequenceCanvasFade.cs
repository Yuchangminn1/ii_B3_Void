using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class SequenceCanvasFade : MonoBehaviour
{
    public Page page;

    [Tooltip("페이드 효과를 적용할 CanvasGroup")] public CanvasGroup canvasGroup;

    [Tooltip("페이드 효과를 적용할 페어CanvasGroup")] public CanvasGroup PairCanvasGroup;


    [Tooltip("페이드 효과를 적용할 CanvasGroup")] public Graphic[] graphics;


    [Tooltip("페이드 효과의 지속 시간 (초)")] public float fadeDuration = 1f;

    public bool isPlaying = false;
    public bool IsPlaying
    {
        get
        {
            return isPlaying;
        }
        set
        {
            if (value == false)
            {
                StopAllCoroutines();
            }
            isPlaying = value;

        }

    }


    Coroutine fadeCoroutine;

    Color color = new Color(1, 1, 1, 0);

    void OnEnable()
    {
        canvasGroup.alpha = 0f;
        if (PairCanvasGroup != null)
        {
            PairCanvasGroup.alpha = 0f;
        }

    }

    void OnDisable()
    {
        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);
        isPlaying = false;
    }


    private void Awake()
    {
        // CanvasGroup이 할당되지 않았다면 같은 GameObject에서 가져옵니다.
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }
        canvasGroup.alpha = 0f;
        if (PairCanvasGroup != null)
        {
            PairCanvasGroup.alpha = 0f;
        }


        page = GetComponent<Page>();
        if (page != null)
        {
            page.onStartPage.AddListener(FadeIn);
            //page.onEndPage.AddListener(FadeOut);
        }
        else
        {
            Debug.Log("pageSequenceController is null");
        }
        graphics = GetComponentsInChildren<Graphic>();

    }


    /// <summary>
    /// CanvasGroup을 완전히 보이게 하는 페이드 인 효과 실행
    /// </summary>
    public void FadeIn()
    {
        if (!isPlaying)
        {
            if (gameObject.activeInHierarchy)
            {

                fadeCoroutine = StartCoroutine(FadeRoutine(canvasGroup.alpha, 1f));

                isPlaying = true;

            }
            else
            {
                Debug.LogWarning("오브젝트가 비활성화 상태입니다: " + gameObject.name);
            }
        }

    }

    /// <summary>
    /// CanvasGroup을 완전히 숨기는 페이드 아웃 효과 실행
    /// </summary>
    public void FadeOut()
    {
        if (!isPlaying)
        {
            if (gameObject.activeInHierarchy)
            {
                fadeCoroutine = StartCoroutine(FadeRoutine(canvasGroup.alpha, 0f));

                isPlaying = true;

            }
            else
            {
                Debug.LogWarning("오브젝트가 비활성화 상태입니다: " + gameObject.name);
            }
        }

    }


    /// <summary>
    /// 시작 값에서 목표 값까지 알파값을 선형 보간하여 변경하는 코루틴
    /// </summary>
    /// <param name="startAlpha">시작 알파값</param>
    /// <param name="targetAlpha">목표 알파값</param>
    private IEnumerator FadeRoutine(float startAlpha, float targetAlpha)
    {

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            //Debug.Log($"elapsed{elapsed} /  fadeDuration {fadeDuration}");
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / fadeDuration);
            PairCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / fadeDuration);
            yield return null;
        }
        //Debug.Log($"targetAlpha = {targetAlpha}");
        canvasGroup.alpha = targetAlpha;
        PairCanvasGroup.alpha = targetAlpha;

        fadeCoroutine = null;
        IsPlaying = false;

        // if (targetAlpha < 0.1f)
        // {
        //     page.gameObject.SetActive(false);
        // }

    }
}