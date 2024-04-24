using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct AmmoPosData
{
    public float mirrorRate; //镜像子弹概率
    public Data[] datas; // 子弹数量对应的数据

    [System.Serializable]
    public struct Data
    {
        public int ammo; // 子弹数量
        public float left; // 左偏移概率
        public float right; // 右偏移概率
    }

    // 根据子弹数量获取子弹偏移方向
    public int GetDir(int ammoCount, bool isMirror)
    {
        int maxId = datas.Length - 1;
        int nextId = 0;
        Data dt;
        
        // 在数据列表中查找匹配的数据
        do
        {
            dt = datas[nextId];
            nextId++;
        } while (nextId <= maxId && ammoCount > dt.ammo);

        // 根据随机数确定偏移方向
        float random = Random.Range(0, 1f);
        if (random < dt.left)
        {
            return isMirror ? 1 : - 1; // 左偏移
        }
        else if (random < dt.left + dt.right)
        {
            return isMirror ? -1 : 1; // 右偏移
        }

        return 0; // 不偏移
    }

    //按概率判断是否镜像
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

    [Header("后坐力参数")]
    private Vector2 recoil; // 后坐力向量，存储X和Y方向上的偏移量
    public Vector2 addSpeed = new Vector2(0.1f, 0.75f); // 后坐力增加速度
    public Vector2 subSpeed = new Vector2(3f, 5f); // 后坐力减少速度
    public Vector2 maxRecoil = new Vector2(1, 5); // 后坐力的最大值

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
        // 使用Mathf.MoveTowards方法逐渐将recoil.x和recoil.y减少到0
        // subSpeed.x和subSpeed.y分别表示在每秒内减少的量，乘以Time.deltaTime可以使速度与帧率无关
        recoil.x = Mathf.MoveTowards(recoil.x, 0, subSpeed.x * Time.deltaTime);
        recoil.y = Mathf.MoveTowards(recoil.y, 0, subSpeed.y * Time.deltaTime);

        // 将recoil.y应用到物体的X轴旋转角度，将recoil.x应用到物体的Y轴旋转角度
        transform.localEulerAngles = new Vector3(-recoil.y, recoil.x, 0);
    }

    /// <summary>
    /// 停止射击重置弹道
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
            //第一发子弹决定镜像
            isMirror = ammoData.GetMirror();
        }
        AddRecoil(ammoData.GetDir(shootAmmo, isMirror));
        
        //Debug.Log("shootAmmo:" + shootAmmo);
        //Debug.Log(ammoData.GetDir(shootAmmo));
    }

    public void AddRecoil(int offset)
    {
        // // 通过随机取值范围[-1, 1]来增加recoil.x的值，并限制在-maxRecoil.x和maxRecoil.x之间
        recoil.x = Mathf.Clamp(recoil.x + offset * addSpeed.x, -maxRecoil.x, maxRecoil.x);
        // // 增加recoil.y的值，并限制在0和maxRecoil.y之间
        recoil.y = Mathf.Clamp(recoil.y + addSpeed.y, 0, maxRecoil.y);
    }
}
