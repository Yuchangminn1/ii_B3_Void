using System.Collections;
using UnityEngine;
using UnityEngine.UI;
public enum TutorialShape
{
    Triangle,
    Moon,
    Heart
}
public class ShootPieceContainer : MonoBehaviour
{
    int _currnetIndex = 0;

    public ShowContainer showContainer;

    public Timer _timer;

    public int CurrentIndex
    {
        get
        {
            return _currnetIndex;
        }
        set
        {
            if (value >= 0 && value < Left_ShootPieces.Length)
            {
                Left_ShootPieces[value].OriginSet();
                Right_ShootPieces[value].OriginSet();
            }

            _currnetIndex = value;
        }
    }

    public ShootPiece[] Left_ShootPieces;

    public ShootPiece[] Right_ShootPieces;

    public CameraVisible cameraVisible;


    bool _isClearLeft = false;
    bool _isClearRight = false;

    float setTime = 10f;
    const int PieceGroupSize = 5;

    TutorialShape[] leftPlayerTutorialShapeIndex = new TutorialShape[] { TutorialShape.Triangle, TutorialShape.Heart, TutorialShape.Moon };
    TutorialShape[] rightPlayerTutorialShapeIndex = new TutorialShape[] { TutorialShape.Moon, TutorialShape.Triangle, TutorialShape.Heart };

    public Texture2D[] TriangleTexture;

    public Texture2D[] MoonTexture;

    public Texture2D[] HeartTexture;

    public Texture2D[] OutlineTriangleTexture;

    public Texture2D[] OutlineMoonTexture;

    public Texture2D[] OutlineHeartTexture;

    int count = 0;


    void OnEnable()
    {
        CurrentIndex = 0;
        Reset();
    }

    public void Reset()
    {
        for (int i = 0; i < Left_ShootPieces.Length; i++)
        {
            Left_ShootPieces[i].Reset();
        }

        for (int i = 0; i < Right_ShootPieces.Length; i++)
        {
            Right_ShootPieces[i].Reset();
        }
        CurrentIndex = 0;
    }


    public bool IsTutorialClear()
    {
        return _isClearLeft && _isClearRight;
    }

    public void Shoot()
    {
        if (CurrentIndex >= Left_ShootPieces.Length)
        {
            return;
        }
        ShootCoroutine();
    }

    public void Clear(Direction direction)
    {
        if (direction == Direction.Left)
        {
            _isClearLeft = true;
            showContainer.ShowSideImages(Direction.Left, leftPlayerTutorialShapeIndex[CurrentIndex / PieceGroupSize - 1], true, count);
            cameraVisible.CameraOffLeft();



        }
        else
        {
            _isClearRight = true;
            showContainer.ShowSideImages(Direction.Right, rightPlayerTutorialShapeIndex[CurrentIndex / PieceGroupSize - 1], true, count);
            cameraVisible.CameraOffRight();
        }
    }

    public void ShootCoroutine()
    {
        Left_ShootPieces[CurrentIndex].Reset();
        Right_ShootPieces[CurrentIndex].Reset();
        Left_ShootPieces[CurrentIndex].PieceShot();
        Right_ShootPieces[CurrentIndex].PieceShot();
        CurrentIndex++;
    }


    public void ShowPiece(Direction direction)
    {
        if (direction == Direction.Left)
        {
            Left_ShootPieces[CurrentIndex].ColorWhite();

        }
        else
        {
            Right_ShootPieces[CurrentIndex].ColorWhite();
        }
    }


    public IEnumerator ReturnShootCoroutine()
    {

        _isClearLeft = false;
        _isClearRight = false;
        for (int i = CurrentIndex - PieceGroupSize; i < CurrentIndex; i++)
        {
            Left_ShootPieces[i].ReturnPieceShot();
            Right_ShootPieces[i].ReturnPieceShot();

            yield return CoroutineReturnManager.GetWaitForSeconds(1f);

        }
        yield return CoroutineReturnManager.GetWaitForSeconds(0.75f);
        count = 0;


        showContainer.ShowSideImages(Direction.Left, leftPlayerTutorialShapeIndex[CurrentIndex / PieceGroupSize - 1], false, count);
        showContainer.ShowSideImages(Direction.Right, rightPlayerTutorialShapeIndex[CurrentIndex / PieceGroupSize - 1], false, count);

        cameraVisible.CameraOn();

        showContainer.StartCheck(Direction.Left);
        showContainer.StartCheck(Direction.Right);

        for (int i = CurrentIndex - PieceGroupSize; i < CurrentIndex; i++)
        {
            Left_ShootPieces[i].ColorClear();
            Left_ShootPieces[i].ResetPosition();
            Right_ShootPieces[i].ColorClear();
            Right_ShootPieces[i].ResetPosition();
        }
        // 틀렸을 경우 1개씩 제거하며 타이머 초기화 및 시작 

        // 통과시 테두리 맞는걸로 교체 이후 다음 문제로 
        _timer.SetDefaultTime(setTime);
        _timer.ResetTimer();
        _timer.StartTimer();



        float timerTime = _timer.DefaultTime;

        while (timerTime > 0 && !IsTutorialClear())
        {
            timerTime -= Time.fixedDeltaTime;
            yield return CoroutineReturnManager.WaitForFixedUpdate;
        }

        count++;

        while (count < 5 && !IsTutorialClear())
        {
            if (_isClearLeft == false)
                showContainer.ShowSideImages(Direction.Left, leftPlayerTutorialShapeIndex[CurrentIndex / PieceGroupSize - 1], false, count);
            if (_isClearRight == false)
                showContainer.ShowSideImages(Direction.Right, rightPlayerTutorialShapeIndex[CurrentIndex / PieceGroupSize - 1], false, count);

            _timer.SetDefaultTime(setTime);
            _timer.ResetTimer();
            _timer.StartTimer();

            timerTime = _timer.DefaultTime;

            while (timerTime > 0 && !IsTutorialClear())
            {
                timerTime -= Time.fixedDeltaTime;
                yield return CoroutineReturnManager.WaitForFixedUpdate;
            }

            count++;
        }
        _timer.ResetTimer();

        yield return CoroutineReturnManager.GetWaitForSeconds(1f);

        showContainer.CurrentImageReset(Direction.Left);
        showContainer.CurrentImageReset(Direction.Right);

        cameraVisible.CameraOff();



        _isClearLeft = true;
        _isClearRight = true;


    }

}

