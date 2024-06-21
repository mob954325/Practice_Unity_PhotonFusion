using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class Ball : NetworkBehaviour
{
    public float moveSpeed = 20.0f;

    [Networked] // ��Ʈ��ũ���� ���� (��� Ŭ���̾�Ʈ�� �˰� ����)
    TickTimer Life { get; set; }    

    public void Init()
    {
        Life = TickTimer.CreateFromSeconds(Runner, 5.0f);   // life�� 5�ʸ� ī�����Ѵ�.
    }

    public override void FixedUpdateNetwork()
    {
        if(Life.Expired(Runner))            // Life�� �ð��� ����Ǹ�
        {       
            Runner.Despawn(Object);         // ������Ʈ ����
        }
        else
        {
            transform.position += Runner.DeltaTime * moveSpeed * transform.forward; // ��� ������ ����.
        }
    }
}