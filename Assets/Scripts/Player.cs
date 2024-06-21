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
    /// 네트워크 틱별로 계속 실행되는 함수
    /// </summary>
    public override void FixedUpdateNetwork()
    {
        if(GetInput(out NetworkInputData data))    // 서버 쪽에서 입력 정보 받아오기
        {
            data.direction.Normalize();           // 유닛벡터로 이동

            cc.Move(Runner.DeltaTime * speed * data.direction); // 초당 moveSpeed의 속도로 data.direction 방향으로 이동
        }
    }
}