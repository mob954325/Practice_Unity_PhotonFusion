using Fusion;
using Fusion.Addons.Physics;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PhysicBall : NetworkBehaviour
{
    public float moveSpeed = 20.0f;

    [Networked] // ��Ʈ��ũ���� ���� (��� Ŭ���̾�Ʈ�� �˰� ����)
    TickTimer Life { get; set; }

    public void Init(Vector3 forward)
    {
        Life = TickTimer.CreateFromSeconds(Runner, 5.0f);   // life�� 5�ʸ� ī�����Ѵ�.
        Rigidbody rigid = GetComponent<Rigidbody>();
        rigid.velocity = forward;
    }

    public override void FixedUpdateNetwork()
    {
        if (Life.Expired(Runner))            // Life�� �ð��� ����Ǹ�
        {
            Runner.Despawn(Object);         // ������Ʈ ����
        }
    }
}