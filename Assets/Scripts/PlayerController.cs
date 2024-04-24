using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 角色控制器
/// </summary>
public class PlayerController : MonoBehaviour
{
    [Header("玩家数据")]
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

    [Header("键位设置")]
    public KeyCode runKeyName = KeyCode.LeftShift;
    public KeyCode crouchKeyName = KeyCode.LeftControl;
    public KeyCode jumpKeyName = KeyCode.Space;

    [Header("玩家状态")]
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

    [Header("音效")]
    public AudioClip walkAudioClip;
    public AudioClip runAudioClip;
    [Tooltip("控制行走和奔跑声音大小")] public float moveVolume = 1f;
    [Tooltip("控制下蹲时的声音大小")] public float crouchVolume = 0.3f;

    [Header("碰撞检测")]
    public LayerMask wallLayerMask;

    [Header("其他")]

    private float h;
    private float v;
    private CharacterController characterController;
    private CollisionFlags collisionFlags; //CharacterController组件中判断碰撞物体
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

    #region 基础移动
    /// <summary>
    /// 人物移动
    /// </summary>
    private void Moving()
    {
        h = Input.GetAxisRaw("Horizontal");
        v = Input.GetAxisRaw("Vertical");

        //归一化防止斜角方向速度不一样
        moveDir = (transform.right * h + transform.forward * v).normalized;
        //瞄准时控制速度
        //在地面上按下跑动键且不处于瞄准状态，方向输入也不为空,不处于下蹲，禁止往反方向跑动
        isRun = isGround && Input.GetKey(runKeyName) && !isAiming && !isCrouch && v > 0f && moveDir != Vector3.zero;
        isWalk = isGround && moveDir != Vector3.zero && !isRun;

        if(isAiming)
        {
            if(isCrouch)
                SetPlayerMoveState(MoveState.crouching, crouchSpeed);
            else
                SetPlayerMoveState(MoveState.idle, aimSpeed);
        }
        //设置状态
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
        //头顶有东西不能跳跃
        if (topHasObstacle) return;

        isJump = Input.GetKeyDown(jumpKeyName);

        if(isGround && isJump)
        {
            isGround = false;
            jumpForce = startJumpForce;
        }

        //向下移动
        if(!isGround)
        {
            jumpForce -= fallForce * Time.deltaTime;
            Vector3 fallDir = new Vector3(0f, jumpForce * Time.deltaTime, 0f);
            collisionFlags = characterController.Move(fallDir);
        }

        //CollisionFlags.Below判断是否在地面
        if (collisionFlags == CollisionFlags.Below)
        {
            isGround = true;
            jumpForce = fallStartForce;
        }

        //在地面上但什么也没碰撞到
        if(isGround && collisionFlags == CollisionFlags.None)
        {
            isGround = false;
        }
    }

    /// <summary>
    /// 检测头顶是否有碰撞物
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

        //改变碰撞胶囊中心和高度
        characterController.height = isCrouch ? crouchHeight : standHeight;
        characterController.center = characterController.height * 0.5f * Vector3.up;
    }

    /// <summary>
    /// 播放脚步声音
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
