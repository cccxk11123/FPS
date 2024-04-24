using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct AmmoPosData
{
    public float mirrorRate; //�����ӵ�����
    public Data[] datas; // �ӵ�������Ӧ������

    [System.Serializable]
    public struct Data
    {
        public int ammo; // �ӵ�����
        public float left; // ��ƫ�Ƹ���
        public float right; // ��ƫ�Ƹ���
    }

    // �����ӵ�������ȡ�ӵ�ƫ�Ʒ���
    public int GetDir(int ammoCount, bool isMirror)
    {
        int maxId = datas.Length - 1;
        int nextId = 0;
        Data dt;
        
        // �������б��в���ƥ�������
        do
        {
            dt = datas[nextId];
            nextId++;
        } while (nextId <= maxId && ammoCount > dt.ammo);

        // ���������ȷ��ƫ�Ʒ���
        float random = Random.Range(0, 1f);
        if (random < dt.left)
        {
            return isMirror ? 1 : - 1; // ��ƫ��
        }
        else if (random < dt.left + dt.right)
        {
            return isMirror ? -1 : 1; // ��ƫ��
        }

        return 0; // ��ƫ��
    }

    //�������ж��Ƿ���
    public bool GetMirror()
    {
        if (Random.Range(0, 1f) < mirrorRate)
            return true;
        return false;
    }
}

public class GunRecoil : MonoBehaviour
{
    private Weapon_AutoGun weapon;

    [SerializeField]
    private AmmoPosData ammoData;

    [Header("����������")]
    private Vector2 recoil; // �������������洢X��Y�����ϵ�ƫ����
    public Vector2 addSpeed = new Vector2(0.1f, 0.75f); // �����������ٶ�
    public Vector2 subSpeed = new Vector2(3f, 5f); // �����������ٶ�
    public Vector2 maxRecoil = new Vector2(1, 5); // �����������ֵ

    private int shootAmmo;
    private bool isMirror;

    private void Awake()
    {
        weapon = GetComponent<Weapon_AutoGun>();
    }

    private void Start()
    {
        weapon.OnWeaponFire += Weapon_OnWeaponFire;
        weapon.OnWeaponStopFire += Weapon_OnWeaponStopFire;
    }

    void Update()
    {
        // ʹ��Mathf.MoveTowards�����𽥽�recoil.x��recoil.y���ٵ�0
        // subSpeed.x��subSpeed.y�ֱ��ʾ��ÿ���ڼ��ٵ���������Time.deltaTime����ʹ�ٶ���֡���޹�
        recoil.x = Mathf.MoveTowards(recoil.x, 0, subSpeed.x * Time.deltaTime);
        recoil.y = Mathf.MoveTowards(recoil.y, 0, subSpeed.y * Time.deltaTime);

        // ��recoil.yӦ�õ������X����ת�Ƕȣ���recoil.xӦ�õ������Y����ת�Ƕ�
        transform.localEulerAngles = new Vector3(-recoil.y, recoil.x, 0);
    }

    /// <summary>
    /// ֹͣ������õ���
    /// </summary>
    private void Weapon_OnWeaponStopFire()
    {
        shootAmmo = 0;
    }

    private void Weapon_OnWeaponFire()
    {
        shootAmmo++;
        if (shootAmmo == 1)
        {
            //��һ���ӵ���������
            isMirror = ammoData.GetMirror();
        }
        AddRecoil(ammoData.GetDir(shootAmmo, isMirror));
        
        //Debug.Log("shootAmmo:" + shootAmmo);
        //Debug.Log(ammoData.GetDir(shootAmmo));
    }

    public void AddRecoil(int offset)
    {
        // // ͨ�����ȡֵ��Χ[-1, 1]������recoil.x��ֵ����������-maxRecoil.x��maxRecoil.x֮��
        recoil.x = Mathf.Clamp(recoil.x + offset * addSpeed.x, -maxRecoil.x, maxRecoil.x);
        // // ����recoil.y��ֵ����������0��maxRecoil.y֮��
        recoil.y = Mathf.Clamp(recoil.y + addSpeed.y, 0, maxRecoil.y);
    }
}
