using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public struct MovePosition2D
{
    public float x;
    public float y;
}


public class ShootPiece : MonoBehaviour
{
    Vector3 originPos = Vector3.zero;
    Vector2 originSize = Vector2.zero;
    bool hasOriginSize = false;

    RawImage _rawImage;

    [SerializeField] RawImage targetImage;

    [SerializeField] RawImage returnImage;

    [SerializeField] float moveDuration = 0.5f;
    [SerializeField] float arcHeight = 180f;
    [SerializeField] bool matchTargetSize = true;



    int currentIndex = 0;


    Coroutine moveCoroutine;

    public void ColorWhite()
    {
        if (_rawImage != null)
        {
            _rawImage.color = Color.white;
        }
    }
    public void ColorClear()
    {
        if (_rawImage != null)
        {
            _rawImage.color = Color.clear;
        }
    }

    public void ResetPosition()
    {
        _rawImage.transform.localPosition = originPos;

    }


    public void Reset()
    {



        if (originPos == Vector3.zero)
        {
            originPos = transform.localPosition;
        }

        RectTransform rect = transform as RectTransform;
        if (rect != null && !hasOriginSize)
        {
            originSize = rect.sizeDelta;
            hasOriginSize = true;
        }

        transform.localPosition = originPos;

        if (rect != null && hasOriginSize)
        {
            rect.sizeDelta = originSize;
        }
        if (_rawImage != null)

            _rawImage.color = Color.clear;
    }

    public void OriginSet()
    {
        if (_rawImage != null)
            _rawImage.color = Color.clear;

        if (originPos == Vector3.zero)
        {
            originPos = transform.localPosition;
        }

        RectTransform rect = transform as RectTransform;
        if (rect != null && !hasOriginSize)
        {
            originSize = rect.sizeDelta;
            hasOriginSize = true;
        }
    }

    void OnEnable()
    {
        currentIndex = 0;
    }

    void Start()
    {
        _rawImage = GetComponent<RawImage>();
    }

    public void PieceShot(Action onComplete = null)
    {
        if (targetImage == null)
        {
            return;
        }

        if (originPos == Vector3.zero)
        {
            originPos = transform.localPosition;
        }
        _rawImage.color = Color.white;

        RectTransform selfRect = transform as RectTransform;
        RectTransform targetRect = targetImage.rectTransform;
        Vector2 startSize = Vector2.zero;
        Vector2 endSize = Vector2.zero;
        bool shouldMatchSize = false;

        if (selfRect != null && targetRect != null)
        {
            if (!hasOriginSize)
            {
                originSize = selfRect.sizeDelta;
                hasOriginSize = true;
            }

            if (matchTargetSize)
            {
                startSize = selfRect.sizeDelta;
                endSize = targetRect.sizeDelta;
                shouldMatchSize = true;
            }
        }

        if (moveCoroutine != null)
        {
            StopCoroutine(moveCoroutine);
        }

        Vector3 start = transform.localPosition;
        Vector3 end = GetTargetLocalPosition();

        moveCoroutine = StartCoroutine(MoveBezier(start, end, selfRect, shouldMatchSize, startSize, endSize, onComplete));
    }

    public void ReturnPieceShot()
    {
        StartCoroutine(ReturnCoroutine());
    }

    IEnumerator ReturnCoroutine()
    {
        _rawImage.color = Color.clear;

        yield return CoroutineReturnManager.GetWaitForSeconds(0.5f);

        _rawImage.rectTransform.position = returnImage.rectTransform.position;
        _rawImage.rectTransform.sizeDelta = returnImage.rectTransform.sizeDelta;
        _rawImage.color = Color.white;



    }


    Vector3 GetTargetLocalPosition()
    {
        RectTransform parentRect = transform.parent as RectTransform;
        RectTransform targetRect = targetImage.rectTransform;

        if (parentRect == null)
        {
            return targetRect.position;
        }

        return parentRect.InverseTransformPoint(targetRect.position);
    }

    IEnumerator MoveBezier(
        Vector3 start,
        Vector3 end,
        RectTransform selfRect,
        bool shouldMatchSize,
        Vector2 startSize,
        Vector2 endSize, Action onComplete = null)
    {
        float duration = Mathf.Max(0.01f, moveDuration);
        float elapsed = 0f;

        Vector3 control = (start + end) * 0.5f + Vector3.up * arcHeight;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            float oneMinusT = 1f - t;
            Vector3 point =
                oneMinusT * oneMinusT * start +
                2f * oneMinusT * t * control +
                t * t * end;

            transform.localPosition = point;

            if (shouldMatchSize && selfRect != null)
            {
                selfRect.sizeDelta = Vector2.Lerp(startSize, endSize, t);
            }

            yield return null;
        }

        transform.localPosition = end;

        if (shouldMatchSize && selfRect != null)
        {
            selfRect.sizeDelta = endSize;
        }

        moveCoroutine = null;

        onComplete?.Invoke();
    }

}
