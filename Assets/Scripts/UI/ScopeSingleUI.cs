using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ScopeSingleUI : MonoBehaviour, IPointerDownHandler
{
    private int scopeIndex;
    private Image scopeImg;
    private Weapon weapon;

    private void Awake()
    {
        scopeImg = GetComponent<Image>();
        weapon = GetComponentInParent<Weapon>();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        weapon.UpdateScope(scopeIndex);
    }

    public void SetScopeInfo(int index, Sprite sprite)
    {
        scopeIndex = index;
        scopeImg.sprite = sprite;
    }
}
