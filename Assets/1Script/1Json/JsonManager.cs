using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
public interface JsonInitilze
{
    public string JsonAddress { get; set; }

    public void LoadData();
}

public class JsonManager : MonoBehaviour
{
    public static JsonManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<JsonManager>();
            }
            return instance;
        }
    }
    static JsonManager instance;

    TextLoader _textLoader = new TextLoader("Json/TextConfig.JSON");

    public TextLoader GetTextLoader { get { return _textLoader; } }

    AudioLoader _audioLoader = new AudioLoader("Json/AudioConfig.JSON");
    public AudioLoader GetAudioLoader { get { return _audioLoader; } }
    VideoLoader _videoLoader = new VideoLoader("Json/VideoConfig.JSON");
    public VideoLoader GetVideoLoader { get { return _videoLoader; } }
    RawImageLoader _rawImageLoader = new RawImageLoader("Json/RawImageConfig.JSON");
    public RawImageLoader GetRawImageLoader { get { return _rawImageLoader; } }

    GenericLoader _genericLoader = new GenericLoader("Json/GenericConfig.JSON");
    public GenericLoader GetGenericLoader { get { return _genericLoader; } }
    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
    }

    void Update()
    {
        // if (Input.GetKeyDown(KeyCode.S))
        // {
        //     _textLoader.JsonDataUpdate();

        //     _audioLoader.JsonDataUpdate();

        //     _videoLoader.JsonDataUpdate();

        //     _rawImageLoader.JsonDataUpdate();

        //     _genericLoader.JsonDataUpdate();


        // }
        // if (Input.GetKeyDown(KeyCode.G))
        // {
        //     _textLoader.CaptureData();

        //     _audioLoader.CaptureData();

        //     _videoLoader.CaptureData();

        //     _rawImageLoader.CaptureData();

        //     _genericLoader.CaptureData();
        // }

    }

    void Start()
    {
        // _textLoader.Load();

        // _audioLoader.Load();

        // _videoLoader.Load();

        // _rawImageLoader.Load();

        // _genericLoader.Load();



        // Text[] tempText = FindObjectsOfType<Text>();
        // foreach (Text t in tempText)
        // {
        //     _textLoader.Register(t.gameObject.name, t);
        // }


        // AudioSource[] tempaudioSource = FindObjectsOfType<AudioSource>();
        // foreach (AudioSource t in tempaudioSource)
        // {
        //     _audioLoader.Register(t.gameObject.name, t);
        // }

        // VideoPlayer[] tempVideoPlayer = FindObjectsOfType<VideoPlayer>();
        // foreach (VideoPlayer t in tempVideoPlayer)
        // {
        //     _videoLoader.Register(t.gameObject.name, t);
        // }
        // RawImage[] tempRawImage = FindObjectsOfType<RawImage>();
        // foreach (RawImage t in tempRawImage)
        // {


        //     //Debug.Log("Register RawImage: " + t.gameObject.name);
        //     _rawImageLoader.Register(t.gameObject.name, t);
        // }
        // var tempGenericTargets = FindObjectsOfType<MonoBehaviour>().OfType<IJsonGenericTarget>();
        // foreach (IJsonGenericTarget t in tempGenericTargets)
        // {
        //     _genericLoader.Register(((Component)t).gameObject.name, t);
        // }

    }

}

