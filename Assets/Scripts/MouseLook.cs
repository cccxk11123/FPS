using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 玩家视野转动
/// </summary>
public class MouseLook : MonoBehaviour
{
    private Transform playerBody;
    private float yRotation;

    private CharacterController characterController;
    private PlayerController playerController;
    private float playerHeight; //玩家原始高度

    [Header("设置")]
    [Tooltip("高度变化速率")] public float heightChangeStep = 12f;
    public float camerRotateSensitive = 400f;

    private void Start()
    {
        playerBody = GetComponentInParent<PlayerController>().transform;
        characterController = GetComponentInParent<CharacterController>();
        playerController = playerBody.GetComponent<PlayerController>();

        playerHeight = characterController.height;

        //锁定并隐藏鼠标
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void Update()
    {
        if (playerController.playerState == PlayerController.PlayerState.fitting)
        {
            if(Cursor.lockState != CursorLockMode.None)
                Cursor.lockState = CursorLockMode.None;
            return;
        }

        if (Cursor.lockState != CursorLockMode.Locked)
            Cursor.lockState = CursorLockMode.Locked;

        float xInput = Input.GetAxis("Mouse X") * camerRotateSensitive * Time.deltaTime;
        float yInput = Input.GetAxis("Mouse Y") * camerRotateSensitive * Time.deltaTime;
        
        //摄像机上下旋转
        yRotation -= yInput; // += 摄像机反转
        yRotation = Mathf.Clamp(yRotation, -60f, 60f);
        transform.localRotation = Quaternion.Euler(yRotation, 0f, 0f);

        //控制人物左右旋转
        playerBody.Rotate(Vector3.up * xInput); //子物体控制父物体的旋转用Rotate

        //玩家下蹲时高度变化
        float heightTarget = characterController.height;
        playerHeight = Mathf.Lerp(playerHeight, heightTarget, heightChangeStep * Time.deltaTime);
        transform.localPosition = Vector3.up * playerHeight;
    }
}
