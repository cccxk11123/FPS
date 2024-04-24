using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class SoundClips
{
    [Header("枪械音效")]
    public AudioClip shootClip; //开火
    public AudioClip slienceShootClip; //消音器开火
    public AudioClip reloadLeftClip; //重新装弹 子弹没完
    public AudioClip reloadOutClip; //重新装弹 子弹已完
    public AudioClip gunAimClip; //武器瞄准时音效
}

public abstract class Weapon : MonoBehaviour
{
    private PlayerController playerController;
    private Animator animator;
    private AudioSource audioSource;

    [Header("枪械设置")]
    [Tooltip("子弹发射点")] public Transform bulletSpwanPoint;
    [Tooltip("子弹预制体")] public GameObject bulletPrefab;
    [Tooltip("子弹速度")] public float shootForce = 100f;
    [Tooltip("弹壳生成点")] public Transform casingSpawnPoint;
    [Tooltip("弹壳预制体")] public GameObject casingPrefab;

    [Header("枪械参数")]
    [Tooltip("射程")] public float range;
    [Tooltip("射击速度")] public float shootRate;
    [Tooltip("射击模式")] public ShootMode shootMode;
    [Tooltip("是否有消音器")] public bool isSliencer;
    [Tooltip("射击散度")] public float spreadOffset;
    [Tooltip("弹夹中的子弹数量")] public int bulletMag;
    private int currentBullet; //当前子弹数量
    private int bulletLeft; //备弹
    private float shootTimer; //射速计时器
    [Tooltip("全自动")] private bool isAutoShoot;
    [Tooltip("半自动")] private bool isSemiShoot;

    [Header("枪械特效")]
    [Tooltip("枪口火焰")] public ParticleSystem muzzleflashPartic;
    [Tooltip("火星")] public ParticleSystem sparkPartic;
    [Tooltip("最小火星生成数量")] public int minSparkNum = 1;
    [Tooltip("最大火星生成数量")] public int maxSparkNum = 7;
    [Tooltip("枪口灯光")] public Light muzzleflashLight;
    [Tooltip("灯光持续时间")] public float lightDuration = 0.02f;

    //音效
    [Space(10)]
    public SoundClips soundClips;

    [Header("准星")]
    public Image[] cross_line;
    [Tooltip("移动时开合角度")] public float crossAngle = 15f;
    private float currentCrossAngle;
    [Tooltip("最大开合角度")] private float maxCrossAngle = 300f;
    private PlayerController.MoveState state;

    [Header("键位设置")]
    [Tooltip("重新装弹")] public KeyCode reloadKeyName = KeyCode.R;
    [Tooltip("切换射击模式")] public KeyCode switchShootModeKeyName = KeyCode.B;
    [Tooltip("武器检视")] public KeyCode inspectWeaponKeyName = KeyCode.N;
    [Tooltip("武器配件装配")] public KeyCode fittingWeaponKeyName = KeyCode.T;
    public KeyCode switchScopeKeyName = KeyCode.Tab;

    [Header("UI")]
    public TextMeshProUGUI bulletText; //子弹数量UI
    public TextMeshProUGUI shootModeText; //射击方式UI
    public FittingWeaponUI fittingWeaponUI;

    public enum ShootMode
    {
        auto,
        semi
    }
    //全自动 半自动
    private bool shootInput;
    private float originalRate;
    private bool isReloading;

    //武器瞄准
    [Header("瞄准偏移参数")]
    [Tooltip("瞄准时的准度")] public float aimSpreadOffset = 0.05f;
    private Vector3 gunPosition; //枪的原始位置
    private float spreadOffsetOriginal; //原始准度
    private bool isAiming;

    [Header("武器晃动")]
    [Tooltip("武器是否摇摆")] public bool weaponSway;
    public float swayAmount = 0.02f;
    public float maxSwayAmount = 0.06f;
    public float swaySmoothValue = 4.0f;
    private Vector3 initialSwayPosition; //初始化摇摆位置

    [Header("枪械瞄准镜")]
    public Camera gunCamera;
    public float defaultFov = 40f;
    public float fovSpeed = 15.0f;
    public List<GameObject> scopeMeshRenderList;
    public List<GameObject> scopeGoList;
    public WeaponDataSO scopeSO;
    private int currentScopeIndex;
    private Coroutine changeGunCameraViewCorotinue;
    private bool isFiltting;

    private AnimatorStateInfo weaponReloadInfo;
    private Vector3 targetPosition;
    private Vector3 screenCenter = new Vector3(Screen.width / 2, Screen.height / 2, 0);

    private bool isOnWeaponFitting;

    //枪械开火事件
    public Action OnWeaponFire;
    public Action OnWeaponStopFire;

    //切换镜子
    public Action OnWeaponFitting;

    protected virtual void Awake()
    {
        playerController = GetComponentInParent<PlayerController>();
        animator = GetComponentInParent<Animator>();
        audioSource = GetComponent<AudioSource>();
    }

    void Start()
    {
        //组件
        muzzleflashLight.enabled = false;

        //位置
        initialSwayPosition = transform.localPosition;
        gunPosition = transform.localPosition;

        //参数
        spreadOffsetOriginal = spreadOffset;
        currentBullet = bulletMag;
        bulletLeft = bulletMag * 5;
        originalRate = shootRate;
        isAutoShoot = shootMode == ShootMode.auto ? true : false;
        isSemiShoot = shootMode == ShootMode.semi ? true : false;

        UpdateScope(currentScopeIndex);
        UpdateGunUI();
    }

    private void LateUpdate()
    {
        //Weapon sway
        if (weaponSway)
        {
            float movementX = -Input.GetAxis("Mouse X") * swayAmount;
            float movementY = -Input.GetAxis("Mouse Y") * swayAmount;
            //Clamp movement to min and max amount
            movementX = Mathf.Clamp
                (movementX, -maxSwayAmount, maxSwayAmount);
            movementY = Mathf.Clamp
                (movementY, -maxSwayAmount, maxSwayAmount);

            Vector3 finalSwayPosition = new Vector3
                (movementX, movementY, 0);

            //Lerp local pos 
            transform.localPosition = Vector3.Lerp
                (transform.localPosition, finalSwayPosition +
                    initialSwayPosition, Time.deltaTime * swaySmoothValue);
        }
    }

    void Update()
    {
        SetWeaponAnim();
        SwitchWeaponScope();

        if (playerController.playerState == PlayerController.PlayerState.fitting) return;

        WeaponAim();
        WeaponReloadByAnim();
        SwitchWeaponMode();
        Shoot();
        UpdateAimCross();

        //重新装弹
        if (Input.GetKeyDown(reloadKeyName) && bulletLeft > 0 && currentBullet < bulletMag && !isReloading && !isAiming)
        {
            DoReloadAnim();
        }

        //TEST
        /*
        if(Input.GetKeyDown(switchScopeKeyName))
        {
            currentScopeIndex = (currentScopeIndex + 1) % scopeGoList.Count;
            UpdateScope(currentScopeIndex);
        }*/
    }

    /// <summary>
    /// 显示瞄准镜UI并更换瞄准镜
    /// </summary>
    private void SwitchWeaponScope()
    {
        //装配
        if (Input.GetKey(fittingWeaponKeyName))
        {
            isFiltting = true;
            if (!isOnWeaponFitting)
            {
                OnWeaponFitting?.Invoke();
                isOnWeaponFitting = true;
            }
        }
        else
        {
            isOnWeaponFitting = false;
            isFiltting = false;
            if (playerController.playerState == PlayerController.PlayerState.fitting)
            {
                playerController.playerState = PlayerController.PlayerState.play;
                fittingWeaponUI.Hide();
            }
        }
    }

    /// <summary>
    /// 开火输入
    /// </summary>
    private void Shoot()
    {
        //开火
        shootInput = isSemiShoot ? Input.GetMouseButtonDown(0) : Input.GetMouseButton(0);

        if (shootTimer < shootRate)
            shootTimer += Time.deltaTime;
        if (shootInput)
            GunFire();
        else
        {
            OnWeaponStopFire?.Invoke();
        }
    }

    /// <summary>
    /// 更新准星变化
    /// </summary>
    private void UpdateAimCross()
    {
        //获取玩家状态来更改准星开合角度
        state = playerController.moveState;

        if (state == PlayerController.MoveState.crouching)
        {
            ExpandingCrossUpdate(crossAngle * 0.5f);
        }
        else if (state == PlayerController.MoveState.walking)
        {
            ExpandingCrossUpdate(crossAngle);
        }
        else if (state == PlayerController.MoveState.runing)
        {
            ExpandingCrossUpdate(crossAngle * 2f);
        }
        else
        {
            ExpandingCrossUpdate(0f);
        }
    }

    /// <summary>
    /// 更新瞄准镜
    /// </summary>
    /// <param name="index"></param>
    public void UpdateScope(int index)
    {
        int len = scopeGoList.Count - 1;
        for (int i = 0; i <= len; i++)
        {
            if (i == index)
            {
                currentScopeIndex = i;
                scopeGoList[i].SetActive(true);
                scopeMeshRenderList[i].SetActive(true);
                gunCamera.transform.localPosition = scopeSO.weapons[index].gunCameraPosition;
            }
            else
            {
                scopeGoList[i].SetActive(false);
                scopeMeshRenderList[i].SetActive(false);
            }
        }
    }

    /// <summary>
    /// 通过动画事件来显示选配UI
    /// </summary>
    public void ShowFittingWeaponUIByAnim()
    {
        fittingWeaponUI.Show();
    }

    /// <summary>
    /// 切换射击模式
    /// </summary>
    private void SwitchWeaponMode()
    {
        //模式切换
        if (Input.GetKeyDown(switchShootModeKeyName))
        {
            if (isAutoShoot)
                shootMode = ShootMode.semi;
            else
                shootMode = ShootMode.auto;

            isAutoShoot = shootMode == ShootMode.auto ? true : false;
            isSemiShoot = shootMode == ShootMode.semi ? true : false;
            shootRate = isAutoShoot ? originalRate : 0.2f;

            UpdateGunUI();
        }
    }

    /// <summary>
    /// 动画设置
    /// </summary>
    private void SetWeaponAnim()
    {
        animator.SetBool("isAim", isAiming);
        animator.SetBool("isWalk", playerController.isWalk);
        animator.SetBool("isRun", playerController.isRun);
        animator.SetBool("isFire", shootInput);
        animator.SetBool("filtting", isFiltting);

        //动画控制 移动 武器检视
        if (Input.GetKeyDown(inspectWeaponKeyName))
        {
            animator.SetTrigger("inspect");
        }
    }

    /// <summary>
    /// 通过动画来判断武器是否正在换弹
    /// </summary>
    private void WeaponReloadByAnim()
    {
        //装弹过程判断
        weaponReloadInfo = animator.GetCurrentAnimatorStateInfo(0);
        if (weaponReloadInfo.IsName("reload_ammo_left") || weaponReloadInfo.IsName("reload_out_of_ammo"))
            isReloading = true;
        else
            isReloading = false;
    }

    /// <summary>
    /// 武器瞄准
    /// </summary>
    private void WeaponAim()
    {
        //武器瞄准
        isAiming = Input.GetMouseButton(1); //右键
        playerController.isAiming = isAiming; //改变玩家状态
    }

    IEnumerator ChangeGunCameraViewCorotinue(float targetAimFOV)
    {
        while (true)
        {
            gunCamera.fieldOfView = Mathf.Lerp(gunCamera.fieldOfView, targetAimFOV, fovSpeed * Time.deltaTime);
            if (gunCamera.fieldOfView == targetAimFOV)
                yield break;
            yield return null;
        }
    }

    /// <summary>
    /// 射击
    /// </summary>
    public void GunFire()
    {
        if (shootTimer < shootRate || currentBullet <= 0 || isReloading ||
            animator.GetCurrentAnimatorStateInfo(0).IsName("take_out_weapon")) return;

        OnWeaponFire?.Invoke();

        //射线检测
        targetPosition = Vector3.zero;
        Vector3 shootDir = new Vector3(UnityEngine.Random.Range(-spreadOffset, spreadOffset), UnityEngine.Random.Range(-spreadOffset, spreadOffset), 0f);
        Ray ray = Camera.main.ScreenPointToRay(screenCenter + shootDir);

        //子弹散射
        if (Physics.Raycast(ray, out RaycastHit hit, range))
        {
            targetPosition = hit.point;
        }
        else
        {
            targetPosition = Camera.main.transform.forward * 800;
        }

        //子弹生成
        GameObject bullet = Instantiate(bulletPrefab, bulletSpwanPoint.position, bulletSpwanPoint.rotation);
        bullet.transform.LookAt(targetPosition);
        bullet.GetComponent<Rigidbody>().velocity = bullet.transform.forward * shootForce;

        //弹壳生成
        Instantiate(casingPrefab, casingSpawnPoint.position, casingSpawnPoint.rotation);

        //特效
        StartCoroutine(MuzzleflashLight(lightDuration));

        //没加消音器才触发
        if (!isSliencer)
        {
            muzzleflashPartic.Emit(1);
            sparkPartic.Emit(UnityEngine.Random.Range(minSparkNum, maxSparkNum));
        }

        //声音
        audioSource.clip = isSliencer ? soundClips.slienceShootClip : soundClips.shootClip;
        audioSource.Play();

        //准星变化
        StartCoroutine(ShootCross());

        //动画  瞄准时 正常状态时
        if (isAiming)
            animator.CrossFadeInFixedTime("aim_fire", 0.1f);
        else
            animator.CrossFadeInFixedTime("fire", 0.1f);

        //重置计时器 子弹数减少 更新UI
        shootTimer = 0f;
        currentBullet--;
        UpdateGunUI();
    }

    /// <summary>
    /// 用于控制开火时灯光
    /// </summary>
    IEnumerator MuzzleflashLight(float duration)
    {
        muzzleflashLight.enabled = true;
        yield return new WaitForSeconds(duration);
        muzzleflashLight.enabled = false;
    }

    /// <summary>
    /// 控制准星的增大和减小
    /// </summary>
    public void ExpandingCrossUpdate(float expandAngle)
    {
        //扩大到目标角度
        if (currentCrossAngle < expandAngle - 5f)
        {
            ExpandingCross(150f * Time.deltaTime);
        }
        //缩小到目标角度
        else if (currentCrossAngle > expandAngle + 5f)
        {
            ExpandingCross(-300f * Time.deltaTime);
        }
    }

    /// <summary>
    /// 准星
    /// </summary>
    public void ExpandingCross(float add)
    {
        cross_line[0].rectTransform.position += new Vector3(-add, 0, 0); //左边
        cross_line[1].rectTransform.position += new Vector3(add, 0, 0); //右边
        cross_line[2].rectTransform.position += new Vector3(0, add, 0); //上边
        cross_line[3].rectTransform.position += new Vector3(0, -add, 0); //下边

        currentCrossAngle += add;
        currentCrossAngle = Mathf.Clamp(currentCrossAngle, 0, maxCrossAngle);
    }

    /// <summary>
    /// 1帧调用5次去增大准星,射击时
    /// </summary>
    public IEnumerator ShootCross()
    {
        yield return null;
        for (int i = 0; i < 5; i++)
            ExpandingCross(500f * Time.deltaTime);
    }

    /// <summary>
    /// 播放不同的换弹动画并设置声音
    /// </summary>
    public void DoReloadAnim()
    {
        if (currentBullet > 0 && bulletLeft > 0)
        {
            animator.CrossFadeInFixedTime("reload_ammo_left", 0.1f);
            audioSource.clip = soundClips.reloadLeftClip;
        }
        if (currentBullet == 0 && bulletLeft > 0)
        {
            animator.CrossFadeInFixedTime("reload_out_of_ammo", 0.1f);
            audioSource.clip = soundClips.reloadOutClip;
        }

        audioSource.Play();
    }

    /// <summary>
    /// 重新装弹 在动画中调用
    /// </summary>
    public void Reload()
    {
        if (bulletLeft <= 0) return;

        int bulletToReload = bulletMag - currentBullet;
        bulletToReload = bulletLeft >= bulletToReload ? bulletToReload : bulletLeft;
        bulletLeft -= bulletToReload;
        currentBullet += bulletToReload;

        UpdateGunUI();
    }

    /// <summary>
    /// 更新子弹数量UI
    /// </summary>
    private void UpdateGunUI()
    {
        bulletText.text = currentBullet + "/" + bulletLeft;
        shootModeText.text = isAutoShoot ? "全自动" : "半自动";
    }

    /// <summary>
    /// 隐藏准星UI 进入瞄准状态 动画中调用 
    /// 可以加镜头缩放
    /// </summary>
    public void AimIn()
    {
        for (int i = 0; i < cross_line.Length; i++)
        {
            cross_line[i].enabled = false;
        }

        if (changeGunCameraViewCorotinue != null)
            StopCoroutine(changeGunCameraViewCorotinue);
        changeGunCameraViewCorotinue = StartCoroutine(ChangeGunCameraViewCorotinue(scopeSO.weapons[currentScopeIndex].gunFieldOfView));

        spreadOffset = aimSpreadOffset;
        audioSource.clip = soundClips.gunAimClip;
        audioSource.Play();
    }

    /// <summary>
    /// 打开准星 退出瞄准状态 动画中调用
    /// </summary>
    public void AimOut()
    {
        for (int i = 0; i < cross_line.Length; i++)
        {
            cross_line[i].enabled = true;
        }

        if (changeGunCameraViewCorotinue != null)
            StopCoroutine(changeGunCameraViewCorotinue);
        changeGunCameraViewCorotinue = StartCoroutine(ChangeGunCameraViewCorotinue(defaultFov));

        spreadOffset = spreadOffsetOriginal;
        transform.localPosition = gunPosition;
    }

    public void HideAimUI()
    {
        for (int i = 0; i < cross_line.Length; i++)
        {
            cross_line[i].enabled = false;
        }
    }

    public void ShowAimUI()
    {
        for (int i = 0; i < cross_line.Length; i++)
        {
            cross_line[i].enabled = true;
        }
    }
}
