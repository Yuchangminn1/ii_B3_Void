using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SaveShadowTextureContainer : MonoBehaviour
{

    public SaveShadowTexture[] SaveShadowTextures;


    public Direction CurrentDirection = Direction.Left;


    public RawImage TargetRawImage;


    public int CurrentIndex = 0;


    void Start()
    {
        PageController.Instance.OnReset += Reset;

        //SaveShadowTextures = GetComponentsInChildren<SaveShadowTexture>();

        if (CurrentDirection == Direction.Left)
        {
            CurrentIndex = 0;

            foreach (SaveShadowTexture saveShadowTextures in SaveShadowTextures)
            {
                saveShadowTextures.CurrentIndex = CurrentIndex;
                CurrentIndex++;
            }
        }
        else
        {
            CurrentIndex = SaveShadowTextures.Length - 1;
            foreach (SaveShadowTexture saveShadowTextures in SaveShadowTextures)
            {
                saveShadowTextures.CurrentIndex = CurrentIndex;
                CurrentIndex--;
            }
        }


    }



    public void SetTexture(int CurrentIndex)
    {
        SaveShadowTextures[CurrentIndex - 1].SetTexture(TargetRawImage);
    }


    void Reset()
    {
        CurrentIndex = 0;
    }


}
