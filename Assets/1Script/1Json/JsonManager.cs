using System.Collections.Generic;
using System.Linq;
using UnityEngine;
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

    readonly QuestionLoader[] _questionLoaders = new QuestionLoader[]
     {
        new QuestionLoader("Json/QuestionConfigA1.json"),
        new QuestionLoader("Json/QuestionConfigA2.json"),
        new QuestionLoader("Json/QuestionConfigA3.json"),
        new QuestionLoader("Json/QuestionConfigA4.json"),
        new QuestionLoader("Json/QuestionConfigA5.json"),
        new QuestionLoader("Json/QuestionConfigB1.json"),
        new QuestionLoader("Json/QuestionConfigB2.json"),
        new QuestionLoader("Json/QuestionConfigB3.json"),
        new QuestionLoader("Json/QuestionConfigB4.json"),
        new QuestionLoader("Json/QuestionConfigB5.json"),
        new QuestionLoader("Json/QuestionConfigC1.json"),
        new QuestionLoader("Json/QuestionConfigC2.json"),
        new QuestionLoader("Json/QuestionConfigC3.json"),
        new QuestionLoader("Json/QuestionConfigC4.json"),
        new QuestionLoader("Json/QuestionConfigC5.json"),
        new QuestionLoader("Json/QuestionConfigD1.json"),
        new QuestionLoader("Json/QuestionConfigD2.json"),
        new QuestionLoader("Json/QuestionConfigD3.json"),
        new QuestionLoader("Json/QuestionConfigD4.json"),
        new QuestionLoader("Json/QuestionConfigD5.json")
     };
    public QuestionLoader GetQuestionLoader { get { return _questionLoaders[0]; } }
    readonly List<IQuestionTarget> _questionTargets = new List<IQuestionTarget>();
    int _selectedQuestionCartridge = 1;

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

        _genericLoader.Load();

        foreach (var questionLoader in _questionLoaders)
        {
            questionLoader.Load();
        }

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
        var tempGenericTargets = FindObjectsOfType<MonoBehaviour>().OfType<IJsonGenericTarget>();
        foreach (IJsonGenericTarget t in tempGenericTargets)
        {
            Debug.Log("Register GenericTarget: " + ((Component)t).gameObject.name);
            _genericLoader.Register(((Component)t).gameObject.name, t);
        }
        var allCartridges = new List<QuestionInfo>[_questionLoaders.Length];
        for (int i = 0; i < _questionLoaders.Length; i++)
        {
            allCartridges[i] = _questionLoaders[i].GetQuestions("QuestionManager");
            Debug.Log($"Cartridge {i + 1} loaded: {allCartridges[i]?.Count ?? 0} questions");
        }

        _questionTargets.Clear();

        var tempQuestionTargets = FindObjectsOfType<MonoBehaviour>().OfType<IQuestionTarget>();
        foreach (IQuestionTarget t in tempQuestionTargets)
        {
            _questionTargets.Add(t);
            t.InitializeCartridges(allCartridges);
        }
        SetCartridge(_selectedQuestionCartridge);



    }

    public void SetCartridge(int value)
    {
        _selectedQuestionCartridge = value;
        QuestionManager.Instance.SetRELATION(value);
    }

}

