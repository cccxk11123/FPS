using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ��ɫ������
/// </summary>
public class PlayerController : MonoBehaviour
{
    [Header("�������")]
    private float speed;
    public float walkSpeed = 4f;
    public float aimSpeed = 3f;
    public float runSpeed = 6f;
    public float crouchSpeed = 2f;
    public float startJumpForce = 6f;
    public float fallStartForce = -2f;
    private float jumpForce;
    public float fallForce = 10f;
    public float airMultiplier = 0.5f;

    [Header("��λ����")]
    public KeyCode runKeyName = KeyCode.LeftShift;
    public KeyCode crouchKeyName = KeyCode.LeftControl;
    public KeyCode jumpKeyName = KeyCode.Space;

    [Header("���״̬")]
    public bool isGround;
    public bool topHasObstacle;
    public bool isRun;
    public bool isWalk;
    public bool isAiming;
    public MoveState moveState;
    public PlayerState playerState;
    private bool isJump;
    private bool isCrouch;
    private bool isAir;
    private float standHeight;
    private float crouchHeight;

    [Header("��Ч")]
    public AudioClip walkAudioClip;
    public AudioClip runAudioClip;
    [Tooltip("�������ߺͱ���������С")] public float moveVolume = 1f;
    [Tooltip("�����¶�ʱ��������С")] public float crouchVolume = 0.3f;

    [Header("��ײ���")]
    public LayerMask wallLayerMask;

    [Header("����")]

    private float h;
    private float v;
    private CharacterController characterController;
    private CollisionFlags collisionFlags; //CharacterController������ж���ײ����
    private AudioSource audioSource;
    private Vector3 moveDir;
    private Weapon playerWeapon;

    private void Start()
    {
        characterController = GetComponent<CharacterController>();
        audioSource = GetComponent<AudioSource>();

        standHeight = characterController.height;
        crouchHeight = standHeight * 0.5f;

        moveState = MoveState.idle;
        playerState = PlayerState.play;
    }

    private void Update()
    {
        Jump();
        TopHasObstacle();
        Crouch();
        Moving();
        PlayFootSound();
    }

    #region �����ƶ�
    /// <summary>
    /// �����ƶ�
    /// </summary>
    private void Moving()
    {
        h = Input.GetAxisRaw("Horizontal");
        v = Input.GetAxisRaw("Vertical");

        //��һ����ֹб�Ƿ����ٶȲ�һ��
        moveDir = (transform.right * h + transform.forward * v).normalized;
        //��׼ʱ�����ٶ�
        //�ڵ����ϰ����ܶ����Ҳ�������׼״̬����������Ҳ��Ϊ��,�������¶ף���ֹ���������ܶ�
        isRun = isGround && Input.GetKey(runKeyName) && !isAiming && !isCrouch && v > 0f && moveDir != Vector3.zero;
        isWalk = isGround && moveDir != Vector3.zero && !isRun;

        if(isAiming)
        {
            if(isCrouch)
                SetPlayerMoveState(MoveState.crouching, crouchSpeed);
            else
                SetPlayerMoveState(MoveState.idle, aimSpeed);
        }
        //����״̬
        else if (isGround)
        {
            isAir = false;
            if(isCrouch)
            {
                SetPlayerMoveState(MoveState.crouching, crouchSpeed);
            }
            else if (isRun)
            {
                SetPlayerMoveState(MoveState.runing, runSpeed);
            }
            else if (isWalk)
            {
                SetPlayerMoveState(MoveState.walking, walkSpeed);
            }
            else // idle
            {
                SetPlayerMoveState(MoveState.idle, 0f);
            }
        }
        else if(!isAir)
        {
            SetPlayerMoveState(MoveState.air, speed * airMultiplier);
            isAir = true;
        }
        
        characterController.Move(moveDir * speed * Time.deltaTime);
    }

    private void SetPlayerMoveState(MoveState moveState, float speed)
    {
        this.moveState = moveState;
        this.speed = speed;
    }

    private void Jump()
    {
        //ͷ���ж���������Ծ
        if (topHasObstacle) return;

        isJump = Input.GetKeyDown(jumpKeyName);

        if(isGround && isJump)
        {
            isGround = false;
            jumpForce = startJumpForce;
        }

        //�����ƶ�
        if(!isGround)
        {
            jumpForce -= fallForce * Time.deltaTime;
            Vector3 fallDir = new Vector3(0f, jumpForce * Time.deltaTime, 0f);
            collisionFlags = characterController.Move(fallDir);
        }

        //CollisionFlags.Below�ж��Ƿ��ڵ���
        if (collisionFlags == CollisionFlags.Below)
        {
            isGround = true;
            jumpForce = fallStartForce;
        }

        //�ڵ����ϵ�ʲôҲû��ײ��
        if(isGround && collisionFlags == CollisionFlags.None)
        {
            isGround = false;
        }
    }

    /// <summary>
    /// ���ͷ���Ƿ�����ײ��
    /// </summary>
    private void TopHasObstacle()
    {
        Vector3 checkTop = transform.position + Vector3.up * characterController.height;
        topHasObstacle = Physics.OverlapSphere(checkTop, characterController.radius, wallLayerMask).Length != 0;
    }

    private void Crouch()
    {
        if (topHasObstacle) return;

        isCrouch = Input.GetKey(crouchKeyName);

        //�ı���ײ�������ĺ͸߶�
        characterController.height = isCrouch ? crouchHeight : standHeight;
        characterController.center = characterController.height * 0.5f * Vector3.up;
    }

    /// <summary>
    /// ���ŽŲ�����
    /// </summary>
    private void PlayFootSound()
    {
        if (isGround && moveDir.magnitude > 0)
        {
            if (isCrouch)
            {
                audioSource.volume = crouchVolume;
                audioSource.clip = walkAudioClip;
            }
            else
            {
                audioSource.volume = moveVolume;
                audioSource.clip = isRun ? walkAudioClip : runAudioClip;
            }

            if (!audioSource.isPlaying)
            {
                audioSource.Play();
            }
        }
        else
        {
            if (audioSource.isPlaying)
            {
                audioSource.Pause();
            }
        }
    }

    public enum MoveState
    {
        idle,
        walking,
        runing,
        crouching,
        air
    }
    #endregion

    public void SetPlayerWeapon(Weapon weapon)
    {
        playerWeapon = weapon;
        playerWeapon.OnWeaponFitting += PlayerWeapon_OnWeaponFitting;
    }
    private void PlayerWeapon_OnWeaponFitting()
    {
        playerState = PlayerState.fitting;
    }

    public enum PlayerState
    {
        play,
        fitting
    }
}
