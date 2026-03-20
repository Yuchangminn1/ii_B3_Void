using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class FadeManager : MonoBehaviour
{
    private static FadeManager instance;
    public static FadeManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<FadeManager>();
                if (instance == null)
                {
                    GameObject singletonObject = new GameObject("FadeManager");
                    instance = singletonObject.AddComponent<FadeManager>();
                }
            }
            return instance;
        }
    }

    float _fadeDuration = 0.05f;

    public float FadeDuration
    {
        get { return _fadeDuration; }
        set { _fadeDuration = value; }
    }



    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        _fadeDuration = 0.05f;
    }

    private readonly WaitForSeconds _delayWait = new WaitForSeconds(1f);

    // -------------------------------------------------------
    //  공통 진입부
    // -------------------------------------------------------
    public void ToggleFade(Graphic graphic, float fadeTime = 1f)
    {
        if (graphic == null) return;
        if (fadeTime == 1f)
        {
            fadeTime = _fadeDuration;
        }
        StartCoroutine(FadeCoroutine(new[] { graphic }, fadeTime, toggleMode: true));
    }

    public void ToggleFade(Graphic[] graphics, float fadeTime = 1f)
    {
        if (graphics == null || graphics.Length == 0) return;
        if (fadeTime == 1f)
        {
            fadeTime = _fadeDuration;
        }
        StartCoroutine(FadeCoroutine(graphics, fadeTime, toggleMode: true));
    }

    public void ToggleFade(CanvasGroup canvas, float fadeTime = 1f)
    {
        if (canvas == null) return;
        if (fadeTime == 1f)
        {
            fadeTime = _fadeDuration;
        }
        StartCoroutine(FadeCoroutine(new List<CanvasGroup> { canvas }, fadeTime, toggleMode: true));
    }

    public void ToggleFade(List<CanvasGroup> canvases, float fadeTime = 1f)
    {
        if (canvases == null || canvases.Count == 0) return;
        if (fadeTime == 1f)
        {
            fadeTime = _fadeDuration;
        }
        StartCoroutine(FadeCoroutine(canvases, fadeTime, toggleMode: true));
    }

    public void TargetFade(Graphic graphic, float targetAlpha, float fadeTime = 1f)
    {

        if (graphic == null) return;
        if (fadeTime == 1f)
        {
            fadeTime = _fadeDuration;
        }
        StartCoroutine(FadeCoroutine(new[] { graphic }, fadeTime, targetAlpha));
    }

    public void TargetFade(Graphic[] graphics, float targetAlpha, float fadeTime = 1f)
    {
        if (graphics == null || graphics.Length == 0) return;
        if (fadeTime == 1f)
        {
            fadeTime = _fadeDuration;
        }
        StartCoroutine(FadeCoroutine(graphics, fadeTime, targetAlpha));
    }

    public void TargetFade(CanvasGroup canvas, float targetAlpha, float fadeTime = 1f)
    {
        if (canvas == null) return;
        if (fadeTime == 1f)
        {
            fadeTime = _fadeDuration;
        }
        StartCoroutine(FadeCoroutine(new List<CanvasGroup> { canvas }, fadeTime, targetAlpha));
    }

    public void TargetFade(List<CanvasGroup> canvases, float targetAlpha, float fadeTime = 1f)
    {
        if (canvases == null || canvases.Count == 0) return;
        if (fadeTime == 1f)
        {
            fadeTime = _fadeDuration;
        }
        StartCoroutine(FadeCoroutine(canvases, fadeTime, targetAlpha));
    }

    // -------------------------------------------------------
    //  공통 코루틴 (Graphic용)
    // -------------------------------------------------------
    private IEnumerator FadeCoroutine(Graphic[] graphics, float fadeTime, float? targetAlpha = null, bool toggleMode = false)
    {
        int count = graphics.Length;
        float[] start = new float[count];
        float[] end = new float[count];

        for (int i = 0; i < count; i++)
        {
            if (graphics[i] == null) continue;
            start[i] = graphics[i].color.a;
            end[i] = toggleMode ? (start[i] >= 0.5f ? 0f : 1f) : targetAlpha.Value;
        }

        float elapsed = 0f;
        while (elapsed < fadeTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeTime;

            for (int i = 0; i < count; i++)
            {
                if (graphics[i] == null) continue;
                Color c = graphics[i].color;
                c.a = Mathf.Lerp(start[i], end[i], t);
                graphics[i].color = c;
            }
            yield return null;
        }

        for (int i = 0; i < count; i++)
        {
            if (graphics[i] == null) continue;
            Color c = graphics[i].color;
            c.a = end[i];
            graphics[i].color = c;
        }
    }

    // -------------------------------------------------------
    //  공통 코루틴 (CanvasGroup용)
    // -------------------------------------------------------
    private IEnumerator FadeCoroutine(List<CanvasGroup> groups, float fadeTime, float? targetAlpha = null, bool toggleMode = false)
    {
        int count = groups.Count;
        float[] start = new float[count];
        float[] end = new float[count];

        for (int i = 0; i < count; i++)
        {
            if (groups[i] == null) continue;
            start[i] = groups[i].alpha;
            end[i] = toggleMode ? (start[i] >= 0.5f ? 0f : 1f) : targetAlpha.Value;
        }

        float elapsed = 0f;
        while (elapsed < fadeTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeTime;

            for (int i = 0; i < count; i++)
            {
                if (groups[i] == null) continue;
                groups[i].alpha = Mathf.Lerp(start[i], end[i], t);
            }
            yield return null;
        }

        for (int i = 0; i < count; i++)
        {
            if (groups[i] == null) continue;
            groups[i].alpha = end[i];
            if (groups[i].alpha == 0f)
            {
                groups[i].blocksRaycasts = false;
            }
            else if (groups[i].alpha == 1f)
            {
                groups[i].blocksRaycasts = true;
            }

        }
    }

    // -------------------------------------------------------
    //  간단한 알파 제어
    // -------------------------------------------------------
    public void SetAlphaZero(Graphic graphic)
    {
        if (graphic == null) return;
        var c = graphic.color;
        graphic.color = new Color(c.r, c.g, c.b, 0f);
    }
    public void SetAlphaZero(CanvasGroup graphic)
    {
        if (graphic == null) return;
        graphic.alpha = 0f;
    }
    public void SetAlphaOne(CanvasGroup graphic)
    {
        if (graphic == null) return;
        graphic.alpha = 1f;
    }
    public void SetAlphaZero(SpriteRenderer renderer)
    {
        if (renderer == null) return;
        var c = renderer.color;
        renderer.color = new Color(c.r, c.g, c.b, 0f);
    }
    public void SetAlphaOne(Graphic graphic)
    {
        if (graphic == null) return;
        var c = graphic.color;
        graphic.color = new Color(c.r, c.g, c.b, 1f);
    }

    public void SetAlphaOne(SpriteRenderer renderer)
    {

        if (renderer == null) return;
        var c = renderer.color;
        renderer.color = new Color(c.r, c.g, c.b, 1f);
    }

    public void SetAlphaZero(Graphic[] graphics)
    {
        foreach (var g in graphics)
            SetAlphaZero(g);
    }

    public void SetAlphaOne(Graphic[] graphics)
    {
        foreach (var g in graphics)
            SetAlphaOne(g);
    }

    // -------------------------------------------------------
    //  단순 토글 컷
    // -------------------------------------------------------
    public void ToggleCut(Graphic graphic)
    {
        if (graphic == null) return;
        if (graphic.color.a > 0.1f) SetAlphaZero(graphic);
        else SetAlphaOne(graphic);
    }

    public void ToggleCut(Graphic[] graphics)
    {
        foreach (var g in graphics)
            ToggleCut(g);
    }

    public void ToggleCut(CanvasGroup canvasGroup)
    {
        if (canvasGroup == null) return;
        if (canvasGroup.alpha > 0.1f) canvasGroup.alpha = 0f;
        else canvasGroup.alpha = 1f;
    }

    public void ToggleCut(CanvasGroup[] canvasGroups)
    {
        foreach (var canvasGroup in canvasGroups)
            ToggleCut(canvasGroup);
    }

    void OnDestroy()
    {
        StopAllCoroutines();
    }

}
