using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class Ball : NetworkBehaviour
{
    public float moveSpeed = 20.0f;

    [Networked] // 네트워크에서 공유 (모든 클라이언트가 알고 있음)
    TickTimer Life { get; set; }    

    public void Init()
    {
        Life = TickTimer.CreateFromSeconds(Runner, 5.0f);   // life는 5초를 카운팅한다.
    }

    public override void FixedUpdateNetwork()
    {
        if(Life.Expired(Runner))            // Life의 시간이 만료되면
        {       
            Runner.Despawn(Object);         // 오브젝트 디스폰
        }
        else
        {
            transform.position += Runner.DeltaTime * moveSpeed * transform.forward; // 계속 앞으로 간다.
        }
    }
}