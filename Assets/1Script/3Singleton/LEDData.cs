using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;






public class LEDData : MonoBehaviour
{

    private static LEDData instance;

    public static LEDData Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<LEDData>();

                if (instance == null)
                {
                    GameObject singletonObject = new GameObject("PlayerDatas");
                    instance = singletonObject.AddComponent<LEDData>();
                }
            }
            return instance;
        }
    }


    public List<int[]> GoalIndexInt = new List<int[]>();

    int[] ledRight = new int[] { 10, 11, 12, 1, 2, 3 };

    int[] ledLeft = new int[] { 4, 5, 6, 7, 8, 9 };


    public Action<int[]> onAddPlayerLEDIndex;



    public void SetLedPair()
    {
        GoalIndexInt.Clear();

        for (int i = 0; i < 50; i++)
        {
            int first = Random.Range(0, 6);
            int second = Random.Range(0, 6);

            if (i < 2)
            {
                first = 0;
                second = 0;
                continue;
            }
            else if (i < 5)
            {
                first = 5;
                second = 5;
                continue;
            }

            first = ledLeft[first];
            second = ledRight[second];
            // if (i > 0 && GoalIndexInt[i - 1][0] == first && GoalIndexInt[i - 1][1] == second)
            // {
            //     i--;
            //     continue;
            // }
            GoalIndexInt.Add(new int[] { first, second });

        }
    }
    public void AddLedPair()
    {
        for (int i = 0; i < 10; i++)
        {
            int first = Random.Range(0, 6);
            int second = Random.Range(0, 6);
            first = ledLeft[first];
            second = ledRight[second];
            GoalIndexInt.Add(new int[] { first, second });
        }
    }
    public int[] GetPlayerLEDPair()
    {

        int currentIndex = UserDataManager.Instance.GetPlayer().LedTagIndex;

        if (GoalIndexInt.Count == 0)
        {
            SetLedPair();
        }

        if (currentIndex + 1 >= GoalIndexInt.Count)
        {
            Debug.Log("사용 다 해서 다시 생성");
            AddLedPair();

        }
        if (currentIndex < 0)
        {
            currentIndex = 0;
            UserDataManager.Instance.GetPlayer().LedTagIndex = 0;
        }

        if (currentIndex >= GoalIndexInt.Count)
        {
            currentIndex = GoalIndexInt.Count - 1;
            UserDataManager.Instance.GetPlayer().LedTagIndex = currentIndex;
        }

        int[] pair = GoalIndexInt[currentIndex];
        return new int[] { pair[0], pair[1] };
    }

    public int GetLEDIndex()
    {
        return UserDataManager.Instance.GetPlayer().LedTagIndex;
    }

    public void AddPlayerLEDIndex()
    {
        UserDataManager.Instance.GetPlayer(Direction.Left).LedTagIndex++;
        UserDataManager.Instance.GetPlayer(Direction.Right).LedTagIndex++;

        onAddPlayerLEDIndex?.Invoke(GetPlayerLEDPair());
    }


}
