using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// �����Ұת��
/// </summary>
public class MouseLook : MonoBehaviour
{
    private Transform playerBody;
    private float yRotation;

    private CharacterController characterController;
    private PlayerController playerController;
    private float playerHeight; //���ԭʼ�߶�

    [Header("����")]
    [Tooltip("�߶ȱ仯����")] public float heightChangeStep = 12f;
    public float camerRotateSensitive = 400f;

    private void Start()
    {
        playerBody = GetComponentInParent<PlayerController>().transform;
        characterController = GetComponentInParent<CharacterController>();
        playerController = playerBody.GetComponent<PlayerController>();

        playerHeight = characterController.height;

        //�������������
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
        
        //�����������ת
        yRotation -= yInput; // += �������ת
        yRotation = Mathf.Clamp(yRotation, -60f, 60f);
        transform.localRotation = Quaternion.Euler(yRotation, 0f, 0f);

        //��������������ת
        playerBody.Rotate(Vector3.up * xInput); //��������Ƹ��������ת��Rotate

        //����¶�ʱ�߶ȱ仯
        float heightTarget = characterController.height;
        playerHeight = Mathf.Lerp(playerHeight, heightTarget, heightChangeStep * Time.deltaTime);
        transform.localPosition = Vector3.up * playerHeight;
    }
}
