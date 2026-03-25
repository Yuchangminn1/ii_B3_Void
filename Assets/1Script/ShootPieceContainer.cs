using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShootPieceContainer : MonoBehaviour
{
    int _currnetIndex = 0;

    public int CurrnetIndex
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


    int[] breakingIndex = new int[] { 0, 5, 10 };


    void OnEnable()
    {
        CurrnetIndex = 0;
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
        CurrnetIndex = 0;

    }


    public void Shoot()
    {
        if (CurrnetIndex >= Left_ShootPieces.Length)
        {
            return;
        }
        StartCoroutine(ShootCoroutine());
    }

    public IEnumerator ShootCoroutine()
    {
        Left_ShootPieces[CurrnetIndex].ColorWhite();
        Right_ShootPieces[CurrnetIndex].ColorWhite();
        yield return new WaitForSeconds(1f);

        Left_ShootPieces[CurrnetIndex].Reset();
        Right_ShootPieces[CurrnetIndex].Reset();
        Left_ShootPieces[CurrnetIndex].PieceShot();
        Right_ShootPieces[CurrnetIndex].PieceShot();
        CurrnetIndex++;
    }

    public void ReturnShoot()
    {
        StartCoroutine(ReturnShootCoroutine());
    }

    public IEnumerator ReturnShootCoroutine()
    {
        for (int i = CurrnetIndex - 1; i > -1; i--)
        {
            Left_ShootPieces[i].ReturnPieceShot();
            Right_ShootPieces[i].ReturnPieceShot();
            if (i == breakingIndex[0] || i == breakingIndex[1] || i == breakingIndex[2])
            {
                break;
            }
            yield return new WaitForSeconds(0.25f);

        }
    }

}
