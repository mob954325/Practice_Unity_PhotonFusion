using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Fusion;
using TMPro;

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

    /// <summary>
    /// 메세지 텍스트 UI
    /// </summary>
    private TMP_Text messageText;

    private Vector3 forward = Vector3.forward;

    [Networked] private TickTimer delay { get; set; }

    void Awake()
    {
        characterController = GetComponent<NetworkCharacterController>();
    }

    private void Update()
    {
        if(Object.HasInputAuthority && Input.GetKeyDown(KeyCode.R))
        {
            RPC_SendMessage("Hey Mate!");
        }
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

    // RpcSources.InputAuthority : 인풋 권한이 있는 클라이언트만 Rpc를 실행해 메세지를 보낸다 
    // RpcTargets.StateAuthority : 호스트로 SendMessage RPC를 보낸다
    // RpcHostMode.SourceIsHostPlayer : 호스트는 서버이자 클라이언트이므로 둘 중 하나를 지정해야한다(서버 or 클라이언트)
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority, HostMode = RpcHostMode.SourceIsHostPlayer)]
    public void RPC_SendMessage(string message, RpcInfo info = default)
    {
        RPC_RelayMessage(message, info.Source);
    }


    // RpcSources.StateAuthority : 서버또는 호스트는 해당 RPC를 보낸다.
    // RpcTargets.All = 모든 클라이언트가 이 RPC를 받는다
    // HostMode = RpcHostMode.SourceIsServer : 호스트 어플리케이션 중 서버 부분이 해당 ROC를 보낸다.

    /// <summary>
    /// 서버로 보낼 텍스트 메세지
    /// </summary>
    /// <param name="message">Rpc로 보낼 메세지</param>
    /// <param name="messageSource">메세지 보내는 플레이어 레퍼런스</param>
    [Rpc(RpcSources.StateAuthority, RpcTargets.All, HostMode = RpcHostMode.SourceIsServer)] // 서버로 Rpc 보내기
    public void RPC_RelayMessage(string message, PlayerRef messageSource)
    {
        if(messageText == null) // 컴포넌트 찾기
        {
            messageText = FindObjectOfType<TMP_Text>();
        }

        if(messageSource == Runner.LocalPlayer)
        {
            // 보낸 메세지가 자신이면 
            message = $"You said : {message}\n";
        }
        else
        {
            // 다른 플레이어가 보냈으면
            message = $"Some other player said : {message}\n";
        }

        messageText.text += message; // 메세지 텍스트 추가
    }
}
