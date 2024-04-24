using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FittingWeaponUI : MonoBehaviour
{
    public WeaponDataSO weaponDataSO;
    public Weapon weapon;
    public GameObject scopeUIPrefab;
    public Transform parentTransform;

    private void Start()
    {
        Initialize();
        Hide();
    }

    private void Initialize()
    {
        for(int i = 0; i < weaponDataSO.weapons.Count; i++)
        {
            GameObject scopeUI = Instantiate(scopeUIPrefab, parentTransform);
            scopeUI.GetComponent<ScopeSingleUI>().SetScopeInfo(weaponDataSO.weapons[i].scopeIndex, weaponDataSO.weapons[i].scopeSprite);
        }
    }

    public void Show()
    {
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}
