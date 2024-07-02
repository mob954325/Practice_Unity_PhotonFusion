using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Fusion;

public class Ball : NetworkBehaviour
{
    /// <summary>
    /// 생성 후 존재하는 시간
    /// </summary>
    [Networked] private TickTimer life { get; set; }

    /// <summary>
    /// Ball 초기화 함수
    /// </summary>
    public void Init()
    {
        life = TickTimer.CreateFromSeconds(Runner, 5.0f);
    }

    // 시뮬레이션 내의 Ball 움직임 갱신
    public override void FixedUpdateNetwork()
    {
        if (life.Expired(Runner))   // TickTimer 시간 확인
            Runner.Despawn(Object); // 시간이 초과됬으면 디스폰
        else
            transform.position += 5 * transform.forward * Runner.DeltaTime;
    }
}
