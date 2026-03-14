using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class SetColorA : MonoBehaviour
{
    public bool[] flags;

    public float[] canvasalphas;

    public List<Graphic> graphics = new List<Graphic>();

    public List<CanvasGroup> canvasGroups = new List<CanvasGroup>();


    public bool[] raytargets;

    bool isInitialize = false;

    WaitForFixedUpdate waitForFixedUpdate = new WaitForFixedUpdate();

    Coroutine coroutine;

    Coroutine initializeCoroutine = null;

    public Page pageSequenceController;



    IEnumerator DelayToInitialize()
    {
        // pairObject 전체를 순회하며 Graphic 수집 (비활성 포함)
        graphics.AddRange(GetComponentsInChildren<Graphic>(true));
        canvasGroups.AddRange(GetComponentsInChildren<CanvasGroup>(true));

        // // graphics 리스트 초기화 후 수집본 추가
        // if (graphics == null)
        // {
        //     graphics = new List<Graphic>(collected.Count + 16);
        // }
        // else
        // {
        //     graphics.Clear();
        // }

        // graphics.AddRange(collected);
        yield return new WaitForSeconds(1f);

        //graphics.AddRange(GetComponentsInChildren<Graphic>(true));


        flags = new bool[graphics.Count];
        raytargets = new bool[graphics.Count];
        if (canvasGroups.Count > 0)
        {
            canvasalphas = new float[canvasGroups.Count];

            for (int i = 0; i < canvasGroups.Count; i++)
            {
                canvasalphas[i] = canvasGroups[i].alpha;
            }
        }


        for (int i = 0; i < graphics.Count; i++)
        {
            flags[i] = graphics[i].color.a > 0.5f;
        }
        for (int i = 0; i < graphics.Count; i++)
        {
            raytargets[i] = graphics[i].raycastTarget;
        }
        isInitialize = true;
        initializeCoroutine = null;

    }

    void OnEnable()
    {
        if (FadeManager.Instance == null) return;
        if (coroutine == null) coroutine = StartCoroutine(WaitInitialize());
        if (isInitialize) return;
        if (initializeCoroutine != null)
        {
            StopCoroutine(initializeCoroutine);
            initializeCoroutine = null;
        }
        if (pageSequenceController == null)
        {
            pageSequenceController = GetComponent<Page>();

        }
        initializeCoroutine = StartCoroutine(DelayToInitialize());
    }

    void OnDisable()
    {
        if (coroutine != null) StopCoroutine(coroutine);
        coroutine = null;
    }

    IEnumerator WaitInitialize()
    {
        //yield return new WaitForSeconds(0.2f);

        while (!isInitialize) yield return waitForFixedUpdate;

        for (int i = 0; i < graphics.Count; i++)
        {
            if (flags[i])
            {
                FadeManager.Instance.SetAlphaOne(graphics[i]);
            }
            else
            {
                FadeManager.Instance.SetAlphaZero(graphics[i]);
            }

        }
        if (canvasGroups.Count > 0)
        {
            for (int i = 0; i < canvasGroups.Count; i++)
            {
                canvasGroups[i].alpha = canvasalphas[i];
            }
        }

        coroutine = null;
        yield return null;

    }
}
