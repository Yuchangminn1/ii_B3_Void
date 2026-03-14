using System.Collections;
using UnityEngine;


public enum EffectSoundNum
{
    ButtonSound = 0,
    LaserSound,
    FailedSound,
    SuccessSound,
    ScanningSound,
    ClearSound,
    FadeBookSound
}

public class SoundManager : MonoBehaviour
{

    public static SoundManager Instance
    {
        get
        {
            return instance;
        }
    }
    static SoundManager instance;


    public AudioSource Bgm;

    public AudioSource ButtonSound;

    public AudioSource LaserSound;

    public AudioSource FailedSound;

    public AudioSource SuccessSound;

    public AudioSource ScanningSound;

    public AudioSource ClearSound;

    public AudioSource FadeBookSound;


    float _soundVolume = 1f;

    float _delayTime = 0.5f;

    WaitForSeconds _delayWaitForSecond;


    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        _delayWaitForSecond = new WaitForSeconds(_delayTime);

    }

    void Start()
    {
        StartCoroutine(DelayToPlay(Bgm));
    }




    IEnumerator DelayToPlay(AudioSource _tempSource)
    {
        yield return _delayWaitForSecond;
        if (_tempSource == null) yield break;
        _tempSource.Play();
        MuteBGM();
    }


    public void MuteBGM()
    {
        if (Bgm == null) return;
        Bgm.volume = 0f;
        Bgm.Stop();

    }
    public void PlayBGM()
    {
        Bgm.Play();
        Bgm.volume = 0.8f;
    }

    public void PlayEffectSound(EffectSoundNum effectSoundNum, float soundVolume = 1f)
    {
        if (GameManager.Instance.IsStarted == false)
        {
            Debug.Log("Game Not Started Yet");
            return;
        }
        if (soundVolume == 1) soundVolume = _soundVolume;

        switch (effectSoundNum)
        {
            case EffectSoundNum.ButtonSound:
                if (ButtonSound == null) return;
                ButtonSound.PlayOneShot(ButtonSound.clip, soundVolume);
                break;

            case EffectSoundNum.LaserSound:
                if (LaserSound == null) return;
                LaserSound.PlayOneShot(LaserSound.clip, soundVolume);
                break;

            case EffectSoundNum.FailedSound:
                if (FailedSound == null) return;
                FailedSound.PlayOneShot(FailedSound.clip, soundVolume);
                break;

            case EffectSoundNum.SuccessSound:
                if (SuccessSound == null) return;
                SuccessSound.PlayOneShot(SuccessSound.clip, soundVolume);
                break;

            case EffectSoundNum.ScanningSound:
                if (ScanningSound == null) return;
                ScanningSound.PlayOneShot(ScanningSound.clip, soundVolume);
                break;

            case EffectSoundNum.ClearSound:
                if (ClearSound == null) return;
                MuteBGM();
                ClearSound.PlayOneShot(ClearSound.clip, soundVolume);

                break;
            case EffectSoundNum.FadeBookSound:
                if (FadeBookSound == null) return;
                FadeBookSound.PlayOneShot(FadeBookSound.clip, soundVolume); break;

        }

    }
    public void PlayEffectSound(int soundIndex)
    {
        if (soundIndex < 0 || soundIndex > 7) return;
        EffectSoundNum effectSoundNum = (EffectSoundNum)soundIndex;


        switch (effectSoundNum)
        {
            case EffectSoundNum.ButtonSound:
                ButtonSound.PlayOneShot(ButtonSound.clip, 1f);
                break;

            case EffectSoundNum.LaserSound:
                LaserSound.PlayOneShot(LaserSound.clip, 1f);
                break;

            case EffectSoundNum.FailedSound:
                FailedSound.PlayOneShot(FailedSound.clip, 1f);
                break;

            case EffectSoundNum.SuccessSound:
                SuccessSound.PlayOneShot(SuccessSound.clip, 1f);
                break;

            case EffectSoundNum.ScanningSound:
                ScanningSound.PlayOneShot(ScanningSound.clip, 1f);
                break;

            case EffectSoundNum.ClearSound:
                MuteBGM();
                ClearSound.PlayOneShot(ClearSound.clip, 1f);
                break;
            case EffectSoundNum.FadeBookSound:
                FadeBookSound.PlayOneShot(FadeBookSound.clip, 3f);
                break;
        }

    }
}
