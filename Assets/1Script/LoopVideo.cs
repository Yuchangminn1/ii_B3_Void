using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

public class LoopVideo : MonoBehaviour
{
    VideoPlayer videoPlayer;

    void Awake()
    {
        videoPlayer = GetComponent<VideoPlayer>();
    }

    // Start is called before the first frame update
    private void Start()
    {
        videoPlayer.loopPointReached += OnVideoEnded;
    }

    private void OnVideoEnded(VideoPlayer vp)
    {
        vp.frame = 0;
        vp.Play();
    }

}
