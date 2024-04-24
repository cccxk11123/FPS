using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "WeaponSO")]
[SerializeField]
public class WeaponDataSO : ScriptableObject
{
    public List<WeaponScopeData> weapons;
}

[System.Serializable]
public struct WeaponScopeData
{
    public int scopeIndex;
    public Vector3 gunCameraPosition; //��ͬ��׼��ǹ��λ�ò�ͬ
    public float gunFieldOfView;
    public Sprite scopeSprite;
}