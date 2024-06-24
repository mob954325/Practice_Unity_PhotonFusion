using Fusion;
using Fusion.Addons.Physics;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PhysicBall : NetworkBehaviour
{
    public float moveSpeed = 20.0f;

    [Networked] // 네트워크에서 공유 (모든 클라이언트가 알고 있음)
    TickTimer Life { get; set; }

    public void Init(Vector3 forward)
    {
        Life = TickTimer.CreateFromSeconds(Runner, 5.0f);   // life는 5초를 카운팅한다.
        Rigidbody rigid = GetComponent<Rigidbody>();
        rigid.velocity = forward;
    }

    public override void FixedUpdateNetwork()
    {
        if (Life.Expired(Runner))            // Life의 시간이 만료되면
        {
            Runner.Despawn(Object);         // 오브젝트 디스폰
        }
    }
}