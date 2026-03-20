using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class WaitCheckManager : MonoBehaviour
{
    static WaitCheckManager instance;

    public static WaitCheckManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<WaitCheckManager>();

                if (instance == null)
                {
                    GameObject singletonObject = new GameObject("WaitCheckManager");
                    instance = singletonObject.AddComponent<WaitCheckManager>();
                }
            }

            return instance;
        }
    }


    public WaitCheck Page1Check;


    public WaitCheck Page3Check;

    SetUpCoroutine[] _pageControllers;





    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }

    }

    void Start()
    {
        _pageControllers = FindObjectsByType<SetUpCoroutine>(FindObjectsSortMode.None);
    }



    public void TagSuccess()
    {
        int page0_Page = _pageControllers[0].CurrentPage;



        for (int i = 1; i < _pageControllers.Length; i++)
        {

            if (_pageControllers[i].CurrentPage != page0_Page)
            {
                break;
            }
            if (i == _pageControllers.Length - 1)
            {
                switch (page0_Page)
                {
                    case 1:
                        Page1Check.OnClear();
                        break;
                    case 3:
                        Page3Check.OnClear();
                        break;
                }

            }
        }

    }
}
