using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SaveShadowTextureContainer : MonoBehaviour
{
    public struct SaveShadowCapturedFrame
    {
        public Texture Texture;
        public Vector3 LocalPosition;
        public Vector2 SizeDelta;
        public Quaternion LocalRotation;
        public Vector3 LocalScale;
    }


    public SaveShadowTexture[] SaveShadowTextures;


    public Direction CurrentDirection = Direction.Left;


    public RawImage TargetRawImage;

    public RawImage MaskTargetRawImage;

    [Range(0f, 1f)]
    public float MaskMinAlpha = 0.01f;


    public int CurrentIndex = 0;

    readonly List<Texture> _cachedCollectedTextures = new List<Texture>(32);
    readonly List<SaveShadowCapturedFrame> _cachedCollectedFrames = new List<SaveShadowCapturedFrame>(32);


    void Start()
    {
        PageController.Instance.OnReset += Reset;

        if (SaveShadowTextures == null || SaveShadowTextures.Length == 0)
        {
            SaveShadowTextures = GetComponentsInChildren<SaveShadowTexture>(true);
        }

        if (SaveShadowTextures == null || SaveShadowTextures.Length == 0)
        {
            return;
        }

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
        SetTexture(CurrentIndex, null);
    }

    public void SetTexture(int CurrentIndex, RawImage maskTargetRawImage)
    {
        if (SaveShadowTextures == null || SaveShadowTextures.Length == 0)
        {
            return;
        }

        int targetIndex = CurrentIndex - 1;
        if (targetIndex < 0 || targetIndex >= SaveShadowTextures.Length)
        {
            return;
        }

        SaveShadowTexture target = SaveShadowTextures[targetIndex];
        if (target == null)
        {
            return;
        }

        RawImage resolvedMaskTarget = maskTargetRawImage != null ? maskTargetRawImage : MaskTargetRawImage;
        target.SetTexture(TargetRawImage, resolvedMaskTarget, MaskMinAlpha);
    }

    public List<Texture> GetCapturedTextures()
    {
        _cachedCollectedTextures.Clear();

        if (SaveShadowTextures == null)
        {
            return _cachedCollectedTextures;
        }

        for (int i = 0; i < SaveShadowTextures.Length; i++)
        {
            SaveShadowTexture saveShadowTexture = SaveShadowTextures[i];
            if (saveShadowTexture == null)
            {
                continue;
            }

            int count = saveShadowTexture.CapturedCount;
            for (int j = 0; j < count; j++)
            {
                Texture texture = saveShadowTexture.GetCapturedTexture(j);
                if (texture != null)
                {
                    _cachedCollectedTextures.Add(texture);
                }
            }
        }

        return _cachedCollectedTextures;
    }

    public List<SaveShadowCapturedFrame> GetCapturedFrames()
    {
        _cachedCollectedFrames.Clear();

        if (SaveShadowTextures == null)
        {
            return _cachedCollectedFrames;
        }

        for (int i = 0; i < SaveShadowTextures.Length; i++)
        {
            SaveShadowTexture saveShadowTexture = SaveShadowTextures[i];
            if (saveShadowTexture == null)
            {
                continue;
            }

            // 영상 생성 시작 시점의 RectTransform을 기준값으로 스냅샷
            bool hasStartRect = saveShadowTexture.TryGetCurrentRectTransform(out Vector3 startLocalPosition, out Vector2 startSizeDelta, out Quaternion startLocalRotation, out Vector3 startLocalScale);

            int count = saveShadowTexture.CapturedCount;
            for (int j = 0; j < count; j++)
            {
                Texture texture = saveShadowTexture.GetCapturedTexture(j);
                if (texture == null)
                {
                    continue;
                }

                Vector3 localPosition;
                Vector2 sizeDelta;
                Quaternion localRotation;
                Vector3 localScale;

                if (hasStartRect)
                {
                    localPosition = startLocalPosition;
                    sizeDelta = startSizeDelta;
                    localRotation = startLocalRotation;
                    localScale = startLocalScale;
                }
                else
                {
                    saveShadowTexture.TryGetCapturedTransform(j, out localPosition, out sizeDelta, out localRotation, out localScale);
                }

                SaveShadowCapturedFrame frame = new SaveShadowCapturedFrame
                {
                    Texture = texture,
                    LocalPosition = localPosition,
                    SizeDelta = sizeDelta,
                    LocalRotation = localRotation,
                    LocalScale = localScale
                };

                _cachedCollectedFrames.Add(frame);
            }
        }

        return _cachedCollectedFrames;
    }


    void Reset()
    {
        CurrentIndex = 0;
    }

    private void OnDestroy()
    {
        if (PageController.Instance != null)
        {
            PageController.Instance.OnReset -= Reset;
        }
    }


}
