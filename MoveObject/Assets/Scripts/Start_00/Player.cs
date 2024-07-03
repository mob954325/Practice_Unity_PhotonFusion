using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Fusion;

public class Player : NetworkBehaviour
{
    /// <summary>
    /// 공 프리팹
    /// </summary>
    public Ball prefabBall;

    /// <summary>
    /// 물리 공 프리팹
    /// </summary>
    public PhysxBall prefabPhysxBall;

    /// <summary>
    /// 네트워크 컨트롤러
    /// </summary>
    private NetworkCharacterController characterController;

    private Vector3 forward = Vector3.forward;

    [Networked] private TickTimer delay { get; set; }

    void Awake()
    {
        characterController = GetComponent<NetworkCharacterController>();
    }

    // 모든 시뮬레이션 틱(Tick)에서 호출된다 fixedUpdate와 같다.
    public override void FixedUpdateNetwork()
    {
        if(GetInput(out NetworkInputData data)) // 데이터 입력 받고 data로 반환
        {
            // 움직임 입력
            data.direction.Normalize();
            characterController.Move(5 * data.direction * Runner.DeltaTime);

            // 정면 벡터 설정
            if(data.direction.sqrMagnitude > 0) // 방향 입력을 했으면 
            {
                forward = data.direction;   // 정면 벡터값 재설정
            }

            // StateAuthority == 호스트
            // 네트워크 객체(Ball)은 홋트만 생성할 수 있기 때문에 체크
            if (HasStateAuthority && delay.ExpiredOrNotRunning(Runner)) // 호스트이고, 타이머가 존재하면 실행
            {
                if (data.buttons.IsSet(NetworkInputData.MOUSEBUTTON0))  // isSet : 해당 버튼이 설정되어있으면 true 아니면 false
                {
                    CreateBall();
                }
                else if(data.buttons.IsSet(NetworkInputData.MOUSEBUTTON1))
                {
                    CreatePhyxsBall();
                }
            }
        }
    }

    /// <summary>
    /// 공을 생성하는 함수
    /// </summary>
    private void CreateBall()
    {
        delay = TickTimer.CreateFromSeconds(Runner, 0.5f); // 딜레이 설정 0.5f

        // 공 생성
        Runner.Spawn(prefabBall,
            transform.position + forward,       // 플레이어 앞 방향 스폰
            Quaternion.LookRotation(forward),
            Object.InputAuthority,
            (runner, o) => // 생성 후 실행할 내용?
            {
                // 동기화전 Ball 초기화
                o.GetComponent<Ball>().Init();
            });             // 인풋 권한 확인
    }

    private void CreatePhyxsBall()
    {
        delay = TickTimer.CreateFromSeconds(Runner, 5.0f);
        Runner.Spawn(prefabPhysxBall,
            transform.position + forward,
            Quaternion.LookRotation(forward),
            Object.InputAuthority,
            (runner, o) =>
            {
                o.GetComponent<PhysxBall>().Init(10 * forward);
            });
    }
}
