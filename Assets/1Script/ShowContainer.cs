using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


[System.Serializable]
public class ShapeData
{
    public TutorialShape tutorialShape;
    public RawImage[] PieceTexture;

    public RawImage[] OutLinePieceTexture;
}



public class ShowContainer : MonoBehaviour
{
    public ShapeData[] LeftShapeData;
    public ShapeData[] RightShapeData;

    public ShootPieceContainer shootPieceContainer;

    public AcCheck[] questionAChecks;

    RawImage currentLeftImage;
    RawImage currentRightImage;

    System.Action _leftClearListener;
    System.Action _rightClearListener;

    void Awake()
    {
        _leftClearListener = HandleLeftClear;
        _rightClearListener = HandleRightClear;
    }

    void Start()
    {
        Reset();
    }



    public void Reset()
    {
        HideSideImages(LeftShapeData);
        HideSideImages(RightShapeData);

        if (questionAChecks != null)
        {
            if (questionAChecks.Length > 0 && questionAChecks[0] != null)
            {
                questionAChecks[0].RemoveOnClearListener(_leftClearListener);
            }

            if (questionAChecks.Length > 1 && questionAChecks[1] != null)
            {
                questionAChecks[1].RemoveOnClearListener(_rightClearListener);
            }
        }
    }

    void HideSideImages(ShapeData[] sideData)
    {
        if (sideData == null)
        {
            return;
        }

        for (int i = 0; i < sideData.Length; i++)
        {
            ShapeData shapeData = sideData[i];
            if (shapeData == null || shapeData.PieceTexture == null || shapeData.OutLinePieceTexture == null)
            {
                continue;
            }

            int imageCount = Mathf.Min(shapeData.PieceTexture.Length, shapeData.OutLinePieceTexture.Length);
            for (int j = 0; j < imageCount; j++)
            {
                RawImage piece = shapeData.PieceTexture[j];
                if (piece != null)
                {
                    piece.gameObject.SetActive(false);
                }

                RawImage outline = shapeData.OutLinePieceTexture[j];
                if (outline != null)
                {
                    outline.gameObject.SetActive(false);
                }
            }
        }
    }

    public void StartCheck(Direction direction)
    {
        if (direction == Direction.Left)
        {
            if (questionAChecks.Length > 0 && questionAChecks[0] != null)
            {
                questionAChecks[0].StartCheck();
            }
        }
        else if (direction == Direction.Right)
        {
            if (questionAChecks.Length > 1 && questionAChecks[1] != null)
            {
                questionAChecks[1].StartCheck();
            }
        }
    }

    public void CurrentImageReset(Direction direction)
    {
        if (direction == Direction.Left)
        {
            if (currentLeftImage != null)
            {
                currentLeftImage.gameObject.SetActive(false);
            }
        }
        else if (direction == Direction.Right)
        {
            if (currentRightImage != null)
            {
                currentRightImage.gameObject.SetActive(false);
            }
        }
    }

    public void ShowSideImages(Direction direction, TutorialShape shape, bool isOutline, int index, bool isClear = false)
    {
        ShapeData[] sideData = null;
        if (direction == Direction.Left)
        {
            if (currentLeftImage != null)
            {
                currentLeftImage.gameObject.SetActive(false);
            }
            sideData = LeftShapeData;
        }
        else if (direction == Direction.Right)
        {
            if (currentRightImage != null)
            {
                currentRightImage.gameObject.SetActive(false);
            }
            sideData = RightShapeData;
        }

        if (sideData == null)
        {
            Debug.LogError("ShapeData not found for direction: " + direction);
            return;
        }

        ShapeData targetData = null;
        for (int i = 0; i < sideData.Length; i++)
        {
            if (sideData[i] != null && sideData[i].tutorialShape == shape)
            {
                targetData = sideData[i];
                break;
            }
        }

        if (targetData == null)
        {
            Debug.LogError("ShapeData not found for shape: " + shape);
            return;
        }

        RawImage[] images = isOutline ? targetData.OutLinePieceTexture : targetData.PieceTexture;
        if (images == null || index < 0 || index >= images.Length || images[index] == null)
        {
            Debug.LogError("Image not found. shape: " + shape + ", index: " + index + ", isOutline: " + isOutline);
            return;
        }

        RawImage targetImage = images[index];
        targetImage.gameObject.SetActive(true);



        if (direction == Direction.Left)
        {
            currentLeftImage = targetImage;
            if (isClear == true)
                return;
            questionAChecks[0].SetTargetRawImage(currentLeftImage);

            questionAChecks[0].RemoveOnClearListener(_leftClearListener);
            questionAChecks[0].AddOnClearListener(_leftClearListener);
        }
        else if (direction == Direction.Right)
        {
            currentRightImage = targetImage;
            if (isClear == true)
                return;
            questionAChecks[1].SetTargetRawImage(currentRightImage);

            questionAChecks[1].RemoveOnClearListener(_rightClearListener);
            questionAChecks[1].AddOnClearListener(_rightClearListener);
        }

    }

    void HandleLeftClear()
    {
        shootPieceContainer.Clear(Direction.Left);
    }

    void HandleRightClear()
    {
        shootPieceContainer.Clear(Direction.Right);
    }

}
