using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

// 플레이어의 사망, 리스폰, 충돌 처리
public class PlayerBehaviour : NetworkBehaviour
{
    // 스프라이트 랜더러가 있는 곳 (=카메라가 따라다닐 위치)
    public Transform CameraTransform;

    // 플레이어 이름
    [Networked]
    public NetworkString<_16> Nickname { get; set; }

    // 플레이어 색상
    [Networked]
    public Color PlayerColor { get; set; }

    // 이 플레이어의 ID (PlayerRef)
    [Networked]
    public int PlayerID { get; private set; }

    // 네트워크 리지드바디2D(리스폰중 안움직이기, 일반 동작중 회전 안하기, 리스폰 지역으로 순간이동 시키기 용)
    private Fusion.Addons.Physics.NetworkRigidbody2D _rb;

    // 입력받는 처리용 클래스(리스폰 버튼이 누른 순간에만 동작하도록 확인하기 위한 용도)
    private InputController _inputController;

    // 이 플레이어의 콜라이더
    private Collider2D _collider;

    // 이 플레이어와 부딪친 콜라이더
    private Collider2D _hitCollider;

    // 리스폰 타이머(리스폰 과정의 시간 측정용)
    [Networked]
    private TickTimer RespawnTimer { get; set; }

    // 리스폰 중인지 아닌지 체크하는 변수
    [Networked]
    private NetworkBool Respawning { get; set; }

    // 골인지점에 들어갔는지 확인하는 변수
    [Networked]
    private NetworkBool Finished { get; set; }

    // 입력을 허용할지 여부
    [Networked]
    public NetworkBool InputsAllowed { get; set; }

    // 파티클 매니저 (오브젝트 풀 구현되어있음)
    [SerializeField] private ParticleManager _particleManager;

    [Space()]
    [Header("Sound")]
    // 사운드 재생을 위한 채널?
    [SerializeField] private SoundChannelSO _sfxChannel;

    // 사망시 사운드 처리용 (스크립터블 오브젝트를 이용해서 파일에 재생할 사운드를 저장해 놓는다.)
    [SerializeField] private SoundSO _deathSound;

    // 플레이어가 가지고있는 오디오소스
    [SerializeField] private AudioSource _playerSource;

    // 네트워크 변수 변경 확인
    private ChangeDetector _changeDetector;

    private void Awake()
    {
        // 컴포넌트 찾기
        _inputController = GetBehaviour<InputController>();
        _rb = GetBehaviour<Fusion.Addons.Physics.NetworkRigidbody2D>();
        _collider = GetComponentInChildren<Collider2D>();
    }

    public override void Spawned()
    {
        // 네트워크 변수 변경 감지 할 수 있도록 설정
        _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState, false);
        
        // 입력 권한을 가진 플레이어의 아이디를 저장 (= 자기 아이디 저장)
        PlayerID = Object.InputAuthority.PlayerId;
            
        if (Object.HasInputAuthority)   // 입력 권한이 있는 경우 (이 플레이어의 캐릭터일 경우)
        {
            CameraManager camera = FindObjectOfType<CameraManager>();   // 카메라 찾고
            camera.CameraTarget = CameraTransform;                      // 카메라가 내 중심점을 따라 움직이게 설정

            if (Nickname == string.Empty)
            {
                RPC_SetNickname(PlayerPrefs.GetString("Nick"));         // 이름 없으면 Nick으로 설정
            }
            GetComponentInChildren<SpriteRenderer>().sortingOrder += 1; // 내 플레이어를 항상 위쪽에 그리기
        }
        GetComponentInChildren<NicknameText>().SetupNick(Nickname.ToString());  // 플레이어 이름 설정
        GetComponentInChildren<SpriteRenderer>().color = PlayerColor;           // 플레이어 색상 성ㄹ정
        _particleManager.ClearParticles();  // 파티클 오브젝트 풀 초기화
    }

    // 오너(오브젝트를 실제로 조종하는 플레이어)가 호스트에게 보냄
    [Rpc(sources: RpcSources.InputAuthority, targets: RpcTargets.StateAuthority)]
    public void RPC_SetNickname(string nick)
    {
        Nickname = nick;
    }

    // 입력 허가 상태를 설정
    public void SetInputsAllowed(bool value)
    {
        InputsAllowed = value;
    }

    // 리스폰 상태로 만들기
    private void SetRespawning()
    {
        if (Runner.IsServer)    // 러너가 서버면
        {
            RPC_DeathEffects(); // 사망 이팩트 재생
            _rb.Rigidbody.constraints = RigidbodyConstraints2D.FreezeAll; // 못움직이게 만들기
        }
    }

    // 호스트가 모두에게 보내는 RPC
    [Rpc(sources: RpcSources.StateAuthority, targets: RpcTargets.All)]
    private void RPC_DeathEffects()
    {
        // 사망 파티클 내 위치에 만들기
        _particleManager.Get(ParticleManager.ParticleID.Death).transform.position = transform.position;
        PlayDeathSound();   // 사망 사운드 틀기
    }

    // 캐릭터 비주얼 부분을 끄거나 켜는 함수. Respawning이 변경 될 때 실행됨
    private void SetGFXActive(bool value)
    {
        gameObject.transform.GetChild(0).gameObject.SetActive(value);
    }
 
    // 이름이 변경되었을 때 실행하는 함수
    private void OnNickChanged()
    {
        GetComponentInChildren<NicknameText>().SetupNick(Nickname.ToString());  // 이름 변경
    }


    public override void FixedUpdateNetwork()
    {

        DetectCollisions(); // 매 물리 프레임마다 충돌 검출

        if (GetInput<InputData>(out var input) && InputsAllowed)    // 입력이 허용되었는 상황이면 입력받기
        {
            if (input.GetButtonPressed(_inputController.PrevButtons).IsSet(InputButton.RESPAWN) && !Respawning)
            {
                // 리스폰 버튼을 지금 눌렀고 리스폰 중이 아니라면 (= 한번만 처리하기 위한 용도)
                RequestRespawn();   // 리스폰 요청
            }
        }

        if (Respawning) // 리스폰 중이라면
        {
            if (RespawnTimer.Expired(Runner)) // 타임아웃이 되었는지 확인하고
            {
                // 타이머가 끝나면
                _rb.Rigidbody.constraints = RigidbodyConstraints2D.FreezeRotation;  // 회전만 안되게 만들기(=이동은 풀기)
                StartCoroutine(Respawn()); // 리스폰 코루틴 실행
            }
        }

    }

    public override void Render()
    {
        foreach (var change in _changeDetector.DetectChanges(this)) // 네트워크 변경 감지
        {
            switch (change)
            {
                case nameof(Respawning):
                    SetGFXActive(!Respawning);  // Respawning에 따라 플레이어 비주얼 끄고 켜기
                    break;
                case nameof(Nickname):
                    OnNickChanged();            // 이름 변경 처리
                    break;
            }
        }
    }

    private void PlayDeathSound()
    {
        // 플레이어가 죽는 소리 재생 (내 것은 잘 들리고 다른 플레이어 것은 거리에 따라 들리게하기?)
        _sfxChannel.CallSoundEvent(_deathSound, Object.HasInputAuthority ? null : _playerSource);
    }

    // 리스폰 코루틴(상황에 따라 한번에 어려)
    private IEnumerator Respawn()
    {
        _rb.Teleport(PlayerSpawner.PlayerSpawnPos); // 플레이어를 스폰위치로 순간이동
        yield return new WaitForSeconds(.1f);       // 0.1f 대기
        Respawning = false;                         // Respawning를 false로 대기
        SetInputsAllowed(true);
    }

    // 경주 마무리
    private void FinishRace()
    {
        if (Finished) { return; }   // 이미 종료 처리가 되었으면 더 이상 안함

        if (Object.HasInputAuthority)   // 입력권한이 있다면
        {
            GameManager.Instance.SetPlayerSpectating(this); // 나를 관전 모드로 설정
        }

        if (Runner.IsServer)    // 러너가 서버라면
        {
            FindObjectOfType<LevelBehaviour>().PlayerOnFinishLine(Object.InputAuthority, this); // 골인한 플레이어가 3등안에 들어갔으면 승리자로 설정
            Finished = true;
        }
    }

    // 리스폰 요청
    public void RequestRespawn()
    {
        Respawning = true;          // 리스폰 중이면
        SetInputsAllowed(false);    // 입력 막기
        RespawnTimer = TickTimer.CreateFromSeconds(Runner, 1f); // 리스폰 타이머 활성화  
        SetRespawning();            // 리스폰 시작
    }

    private void DetectCollisions()
    {
        // 박스와 겹치는지 체크
        _hitCollider = Runner.GetPhysicsScene2D().OverlapBox(
            transform.position,                 // 박스의 위치
            _collider.bounds.size * .9f,        // 원래 컬라이더보다 작게 확인 (= 충분히 닿았을 때)
            0,                                  // 회젅 없음
            LayerMask.GetMask("Interact"));     // Interact외만 충돌 확인

        if (_hitCollider != default)            // 뭔가가 충돌했으면
        {
            if (_hitCollider.tag.Equals("Kill") && !Respawning) // 충돌한 대상이 KILL태그를 가지고 있고 리스폰 중이 아니라면
            {
                RequestRespawn();    // 리스폰 요청
            }
            else if (_hitCollider.tag.Equals("Finish") && !Finished)    // 충돌한 대상이 Finish 태그를 가지고있으면
            {
                FinishRace();   // 경주 종료 ( 여러명이 플레이하고 있을 경우 다른 사람의 시점으로 보러가기)
            }
        }
    }
}
