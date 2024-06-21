using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : NetworkBehaviour
{

    public float speed = 5f;
    NetworkCharacterController cc;

    void Awake()
    {
        cc = GetComponent<NetworkCharacterController>();
    }
    /// <summary>
    /// ��Ʈ��ũ ƽ���� ��� ����Ǵ� �Լ�
    /// </summary>
    public override void FixedUpdateNetwork()
    {
        if(GetInput(out NetworkInputData data))    // ���� �ʿ��� �Է� ���� �޾ƿ���
        {
            data.direction.Normalize();           // ���ֺ��ͷ� �̵�

            cc.Move(Runner.DeltaTime * speed * data.direction); // �ʴ� moveSpeed�� �ӵ��� data.direction �������� �̵�
        }
    }
}