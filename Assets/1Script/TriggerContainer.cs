using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TriggerContainer : MonoBehaviour
{
    List<SequenceScript> _triggers;

    void Start()
    {
        _triggers = new List<SequenceScript>(GetComponentsInChildren<SequenceScript>());

        for (int i = 0; i < _triggers.Count; i++)
        {
            _triggers[i].CurrentIndex = i;
        }

    }
}
