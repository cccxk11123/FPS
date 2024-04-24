using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// �л������ű�
/// </summary>
public class Inventory : MonoBehaviour
{
    [SerializeField] private List<GameObject> weapons = new List<GameObject>();
    public int currentWeaponIndex;
    private float mouseScrollWheelInput;
    private PlayerController playerController;

    private void Awake()
    {
        playerController = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerController>();
    }

    private void Start()
    {
        Initialize(currentWeaponIndex);
    }

    private void Update()
    {
        // -0.1 0 0.1
        mouseScrollWheelInput = Input.GetAxisRaw("Mouse ScrollWheel");
        if(mouseScrollWheelInput == 0.1f)
        {
            SwitchWeaponByScroll(true);
        }
        else if(mouseScrollWheelInput == -0.1f)
        {
            SwitchWeaponByScroll(false);
        }

        //�������͸�����
        if (Input.GetKeyDown(KeyCode.Alpha1))
            SwitchWeaponByNum(1);
        else if(Input.GetKeyDown(KeyCode.Alpha2))
            SwitchWeaponByNum(2);
    }

    private void Initialize(int index)
    {
        for (int i = 0; i < weapons.Count; i++)
        {
            if (i == index)
                SetWeapon(i);
            else
                weapons[i].SetActive(false);
        }
    }

    /// <summary>
    /// ����С�����л�����
    /// </summary>
    private void SwitchWeaponByNum(int value)
    {
        value--;
        for(int i = 0; i < weapons.Count; i++)
        {
            if (i == value)
                SetWeapon(i);
            else 
                weapons[i].SetActive(false);
        }
    }

    /// <summary>
    /// �����л�����
    /// </summary>
    private void SwitchWeaponByScroll(bool isUP)
    {
        if (isUP) currentWeaponIndex += 1;
        else currentWeaponIndex -= 1;
        currentWeaponIndex %= weapons.Count;

        if(currentWeaponIndex < 0) currentWeaponIndex = weapons.Count - 1;

        for (int i = 0; i < weapons.Count; i++)
        {
            if (i == currentWeaponIndex)
                SetWeapon(i);
            else
                weapons[i].SetActive(false);
        }
    }

    /// <summary>
    /// ʰȡ����
    /// </summary>
    private void AddWeapon(GameObject weapon)
    {
        if(weapons.Contains(weapon))
        {
            Debug.LogError("�Ѿ�ӵ�д�������");
            return;
        }

        weapons.Add(weapon);
        weapons[currentWeaponIndex].SetActive(false);
        weapon.SetActive(true);
    }

    /// <summary>
    /// ��������
    /// </summary>
    private void ThrowWeapon()
    {
        if(!weapons.Contains(weapons[currentWeaponIndex]))
        {
            Debug.LogError("û���������");
            return;
        }

        weapons.Remove(weapons[currentWeaponIndex]);
        weapons[currentWeaponIndex].SetActive(false);
        SwitchWeaponByScroll(true);
    }

    private void SetWeapon(int index)
    {
        weapons[index].SetActive(true);
        playerController.SetPlayerWeapon(weapons[index].GetComponentInChildren<Weapon>());
    }
}
