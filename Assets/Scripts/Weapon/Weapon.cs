using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class SoundClips
{
    [Header("ǹе��Ч")]
    public AudioClip shootClip; //����
    public AudioClip slienceShootClip; //����������
    public AudioClip reloadLeftClip; //����װ�� �ӵ�û��
    public AudioClip reloadOutClip; //����װ�� �ӵ�����
    public AudioClip gunAimClip; //������׼ʱ��Ч
}

public abstract class Weapon : MonoBehaviour
{
    private PlayerController playerController;
    private Animator animator;
    private AudioSource audioSource;

    [Header("ǹе����")]
    [Tooltip("�ӵ������")] public Transform bulletSpwanPoint;
    [Tooltip("�ӵ�Ԥ����")] public GameObject bulletPrefab;
    [Tooltip("�ӵ��ٶ�")] public float shootForce = 100f;
    [Tooltip("�������ɵ�")] public Transform casingSpawnPoint;
    [Tooltip("����Ԥ����")] public GameObject casingPrefab;

    [Header("ǹе����")]
    [Tooltip("���")] public float range;
    [Tooltip("����ٶ�")] public float shootRate;
    [Tooltip("���ģʽ")] public ShootMode shootMode;
    [Tooltip("�Ƿ���������")] public bool isSliencer;
    [Tooltip("���ɢ��")] public float spreadOffset;
    [Tooltip("�����е��ӵ�����")] public int bulletMag;
    private int currentBullet; //��ǰ�ӵ�����
    private int bulletLeft; //����
    private float shootTimer; //���ټ�ʱ��
    [Tooltip("ȫ�Զ�")] private bool isAutoShoot;
    [Tooltip("���Զ�")] private bool isSemiShoot;

    [Header("ǹе��Ч")]
    [Tooltip("ǹ�ڻ���")] public ParticleSystem muzzleflashPartic;
    [Tooltip("����")] public ParticleSystem sparkPartic;
    [Tooltip("��С������������")] public int minSparkNum = 1;
    [Tooltip("��������������")] public int maxSparkNum = 7;
    [Tooltip("ǹ�ڵƹ�")] public Light muzzleflashLight;
    [Tooltip("�ƹ����ʱ��")] public float lightDuration = 0.02f;

    //��Ч
    [Space(10)]
    public SoundClips soundClips;

    [Header("׼��")]
    public Image[] cross_line;
    [Tooltip("�ƶ�ʱ���ϽǶ�")] public float crossAngle = 15f;
    private float currentCrossAngle;
    [Tooltip("��󿪺ϽǶ�")] private float maxCrossAngle = 300f;
    private PlayerController.MoveState state;

    [Header("��λ����")]
    [Tooltip("����װ��")] public KeyCode reloadKeyName = KeyCode.R;
    [Tooltip("�л����ģʽ")] public KeyCode switchShootModeKeyName = KeyCode.B;
    [Tooltip("��������")] public KeyCode inspectWeaponKeyName = KeyCode.N;
    [Tooltip("�������װ��")] public KeyCode fittingWeaponKeyName = KeyCode.T;
    public KeyCode switchScopeKeyName = KeyCode.Tab;

    [Header("UI")]
    public TextMeshProUGUI bulletText; //�ӵ�����UI
    public TextMeshProUGUI shootModeText; //�����ʽUI
    public FittingWeaponUI fittingWeaponUI;

    public enum ShootMode
    {
        auto,
        semi
    }
    //ȫ�Զ� ���Զ�
    private bool shootInput;
    private float originalRate;
    private bool isReloading;

    //������׼
    [Header("��׼ƫ�Ʋ���")]
    [Tooltip("��׼ʱ��׼��")] public float aimSpreadOffset = 0.05f;
    private Vector3 gunPosition; //ǹ��ԭʼλ��
    private float spreadOffsetOriginal; //ԭʼ׼��
    private bool isAiming;

    [Header("�����ζ�")]
    [Tooltip("�����Ƿ�ҡ��")] public bool weaponSway;
    public float swayAmount = 0.02f;
    public float maxSwayAmount = 0.06f;
    public float swaySmoothValue = 4.0f;
    private Vector3 initialSwayPosition; //��ʼ��ҡ��λ��

    [Header("ǹе��׼��")]
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

    //ǹе�����¼�
    public Action OnWeaponFire;
    public Action OnWeaponStopFire;

    //�л�����
    public Action OnWeaponFitting;

    protected virtual void Awake()
    {
        playerController = GetComponentInParent<PlayerController>();
        animator = GetComponentInParent<Animator>();
        audioSource = GetComponent<AudioSource>();
    }

    void Start()
    {
        //���
        muzzleflashLight.enabled = false;

        //λ��
        initialSwayPosition = transform.localPosition;
        gunPosition = transform.localPosition;

        //����
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

        //����װ��
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
    /// ��ʾ��׼��UI��������׼��
    /// </summary>
    private void SwitchWeaponScope()
    {
        //װ��
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
    /// ��������
    /// </summary>
    private void Shoot()
    {
        //����
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
    /// ����׼�Ǳ仯
    /// </summary>
    private void UpdateAimCross()
    {
        //��ȡ���״̬������׼�ǿ��ϽǶ�
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
    /// ������׼��
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
    /// ͨ�������¼�����ʾѡ��UI
    /// </summary>
    public void ShowFittingWeaponUIByAnim()
    {
        fittingWeaponUI.Show();
    }

    /// <summary>
    /// �л����ģʽ
    /// </summary>
    private void SwitchWeaponMode()
    {
        //ģʽ�л�
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
    /// ��������
    /// </summary>
    private void SetWeaponAnim()
    {
        animator.SetBool("isAim", isAiming);
        animator.SetBool("isWalk", playerController.isWalk);
        animator.SetBool("isRun", playerController.isRun);
        animator.SetBool("isFire", shootInput);
        animator.SetBool("filtting", isFiltting);

        //�������� �ƶ� ��������
        if (Input.GetKeyDown(inspectWeaponKeyName))
        {
            animator.SetTrigger("inspect");
        }
    }

    /// <summary>
    /// ͨ���������ж������Ƿ����ڻ���
    /// </summary>
    private void WeaponReloadByAnim()
    {
        //װ�������ж�
        weaponReloadInfo = animator.GetCurrentAnimatorStateInfo(0);
        if (weaponReloadInfo.IsName("reload_ammo_left") || weaponReloadInfo.IsName("reload_out_of_ammo"))
            isReloading = true;
        else
            isReloading = false;
    }

    /// <summary>
    /// ������׼
    /// </summary>
    private void WeaponAim()
    {
        //������׼
        isAiming = Input.GetMouseButton(1); //�Ҽ�
        playerController.isAiming = isAiming; //�ı����״̬
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
    /// ���
    /// </summary>
    public void GunFire()
    {
        if (shootTimer < shootRate || currentBullet <= 0 || isReloading ||
            animator.GetCurrentAnimatorStateInfo(0).IsName("take_out_weapon")) return;

        OnWeaponFire?.Invoke();

        //���߼��
        targetPosition = Vector3.zero;
        Vector3 shootDir = new Vector3(UnityEngine.Random.Range(-spreadOffset, spreadOffset), UnityEngine.Random.Range(-spreadOffset, spreadOffset), 0f);
        Ray ray = Camera.main.ScreenPointToRay(screenCenter + shootDir);

        //�ӵ�ɢ��
        if (Physics.Raycast(ray, out RaycastHit hit, range))
        {
            targetPosition = hit.point;
        }
        else
        {
            targetPosition = Camera.main.transform.forward * 800;
        }

        //�ӵ�����
        GameObject bullet = Instantiate(bulletPrefab, bulletSpwanPoint.position, bulletSpwanPoint.rotation);
        bullet.transform.LookAt(targetPosition);
        bullet.GetComponent<Rigidbody>().velocity = bullet.transform.forward * shootForce;

        //��������
        Instantiate(casingPrefab, casingSpawnPoint.position, casingSpawnPoint.rotation);

        //��Ч
        StartCoroutine(MuzzleflashLight(lightDuration));

        //û���������Ŵ���
        if (!isSliencer)
        {
            muzzleflashPartic.Emit(1);
            sparkPartic.Emit(UnityEngine.Random.Range(minSparkNum, maxSparkNum));
        }

        //����
        audioSource.clip = isSliencer ? soundClips.slienceShootClip : soundClips.shootClip;
        audioSource.Play();

        //׼�Ǳ仯
        StartCoroutine(ShootCross());

        //����  ��׼ʱ ����״̬ʱ
        if (isAiming)
            animator.CrossFadeInFixedTime("aim_fire", 0.1f);
        else
            animator.CrossFadeInFixedTime("fire", 0.1f);

        //���ü�ʱ�� �ӵ������� ����UI
        shootTimer = 0f;
        currentBullet--;
        UpdateGunUI();
    }

    /// <summary>
    /// ���ڿ��ƿ���ʱ�ƹ�
    /// </summary>
    IEnumerator MuzzleflashLight(float duration)
    {
        muzzleflashLight.enabled = true;
        yield return new WaitForSeconds(duration);
        muzzleflashLight.enabled = false;
    }

    /// <summary>
    /// ����׼�ǵ�����ͼ�С
    /// </summary>
    public void ExpandingCrossUpdate(float expandAngle)
    {
        //����Ŀ��Ƕ�
        if (currentCrossAngle < expandAngle - 5f)
        {
            ExpandingCross(150f * Time.deltaTime);
        }
        //��С��Ŀ��Ƕ�
        else if (currentCrossAngle > expandAngle + 5f)
        {
            ExpandingCross(-300f * Time.deltaTime);
        }
    }

    /// <summary>
    /// ׼��
    /// </summary>
    public void ExpandingCross(float add)
    {
        cross_line[0].rectTransform.position += new Vector3(-add, 0, 0); //���
        cross_line[1].rectTransform.position += new Vector3(add, 0, 0); //�ұ�
        cross_line[2].rectTransform.position += new Vector3(0, add, 0); //�ϱ�
        cross_line[3].rectTransform.position += new Vector3(0, -add, 0); //�±�

        currentCrossAngle += add;
        currentCrossAngle = Mathf.Clamp(currentCrossAngle, 0, maxCrossAngle);
    }

    /// <summary>
    /// 1֡����5��ȥ����׼��,���ʱ
    /// </summary>
    public IEnumerator ShootCross()
    {
        yield return null;
        for (int i = 0; i < 5; i++)
            ExpandingCross(500f * Time.deltaTime);
    }

    /// <summary>
    /// ���Ų�ͬ�Ļ�����������������
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
    /// ����װ�� �ڶ����е���
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
    /// �����ӵ�����UI
    /// </summary>
    private void UpdateGunUI()
    {
        bulletText.text = currentBullet + "/" + bulletLeft;
        shootModeText.text = isAutoShoot ? "ȫ�Զ�" : "���Զ�";
    }

    /// <summary>
    /// ����׼��UI ������׼״̬ �����е��� 
    /// ���ԼӾ�ͷ����
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
    /// ��׼�� �˳���׼״̬ �����е���
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
