using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Fusion;

public class PhysxBall : NetworkBehaviour
{
    [Networked] private TickTimer life {  get; set; }   // 타이머

    /// <summary>
    /// 물리 공 초기화 함수
    /// </summary>
    /// <param name="forward">날라갈 방향 forward</param>
    public void Init(Vector3 forward)
    {
        life = TickTimer.CreateFromSeconds(Runner, 5.0f);
        GetComponent<Rigidbody>().velocity = forward;
    }

    public override void FixedUpdateNetwork()
    {
        if (life.Expired(Runner))
            Runner.Despawn(Object);
    }
}