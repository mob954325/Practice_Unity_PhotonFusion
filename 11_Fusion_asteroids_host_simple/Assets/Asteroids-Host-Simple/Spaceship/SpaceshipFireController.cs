using System;
using System.Collections;
using System.Collections.Generic;
using Fusion;
using Unity.VisualScripting;
using UnityEngine;
using NetworkTransform = Fusion.NetworkTransform;

namespace Asteroids.HostSimple
{
    // 우주선의 발사 제어 전용 클래스
    public class SpaceshipFireController : NetworkBehaviour
    {
        // Game Session AGNOSTIC Settings
        // 총알 발사 딜레이
        [SerializeField] private float _delayBetweenShots = 0.2f;
        // 총알 프리팹
        [SerializeField] private NetworkPrefabRef _bullet = NetworkPrefabRef.Empty;

        // Local Runtime references
        // 리지드 바디
        private Rigidbody _rigidbody = null;
        // 우주선 방향 컨트롤러
        private SpaceshipController _spaceshipController = null;

        // Game Session SPECIFIC Settings
        // 입력에서 버튼들의 이전 상태를 저장하는 변수
        [Networked] private NetworkButtons _buttonsPrevious { get; set; }

        // 총알 쿨다운 틱 타이머
        [Networked] private TickTimer _shootCooldown { get; set; }

        public override void Spawned()
        {
            // --- Host & Client
            // Set the local runtime references.
            // 컴포넌트 찾기
            _rigidbody = GetComponent<Rigidbody>();
            _spaceshipController = GetComponent<SpaceshipController>();
        }

        public override void FixedUpdateNetwork()
        {
            if (_spaceshipController.AcceptInput == false) return;  // 리스폰 중이나 게임오버가 된 상황이면 리턴

            if (GetInput<SpaceshipInput>(out var input) == false) return;   // 입력을 못받아오는 상황이면 리턴

            Fire(input);    // 발사 처리
        }

        // 발사 처리
        private void Fire(SpaceshipInput input)
        {
            // 버튼의 이전 상태와 비교해서 방금 눌러진 상황인지 확인
            if (input.Buttons.WasPressed(_buttonsPrevious, SpaceshipButtons.Fire))  // 지금 눌러진것인지 체크 (프레임마다 체크)
            {
                SpawnBullet();  // 총알 생성
            }

            _buttonsPrevious = input.Buttons;   // 버튼 상태 전환
        }

        // 총알 스폰 (총알은 플레이어의 앞쪽으로 날라감)
        private void SpawnBullet()
        {
            // 쿨다운이 다 되지 않았거나 러너가 스폰을 못하는 상황이면 리턴
            if (_shootCooldown.ExpiredOrNotRunning(Runner) == false || !Runner.CanSpawn) return;

            // 스폰 (총알, 내 위치, 내 회전, 내 플레이어 래퍼런스)
            Runner.Spawn(_bullet, _rigidbody.position, _rigidbody.rotation, Object.InputAuthority);

            _shootCooldown = TickTimer.CreateFromSeconds(Runner, _delayBetweenShots);
        }
    }
}