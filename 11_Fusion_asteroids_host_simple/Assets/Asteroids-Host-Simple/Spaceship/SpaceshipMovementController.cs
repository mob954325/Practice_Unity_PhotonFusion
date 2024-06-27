using System.Collections;
using System.Collections.Generic;
using Fusion;
using Fusion.Addons.Physics;
using UnityEngine;

namespace Asteroids.HostSimple
{
    // 우주선의 이동 전용 클래스
    public class SpaceshipMovementController : NetworkBehaviour
    {
        // Game Session AGNOSTIC Settings
        // 회전속도
        [SerializeField] private float _rotationSpeed = 90.0f;
        // 이동용 힘의 크기 계산용(입력에 따라 한번에 적용되는 힘의 크기)
        [SerializeField] private float _movementSpeed = 2000.0f;
        // 최고 속도
        [SerializeField] private float _maxSpeed = 200.0f;

        // Local Runtime references
        // The Unity Rigidbody (RB) is automatically synchronised across the network thanks to the NetworkRigidbody (NRB) component.
        private Rigidbody _rigidbody = null;    // NetworkRigidbody를 통해 자동으로 동기화 된다.

        private SpaceshipController _spaceshipController = null;    // 우주선 생명주기 컨트롤러(살아있는지 체크용)

        // Game Session SPECIFIC Settings
        // 화면 바운더리 (나갔을 때 반대쪽에서 등장시키기 위한 용도)
        [Networked] private float _screenBoundaryX { get; set; }
        [Networked] private float _screenBoundaryY { get; set; }

        public override void Spawned()
        {
            // --- Host & Client
            // Set the local runtime references.
            _rigidbody = GetComponent<Rigidbody>();
            _spaceshipController = GetComponent<SpaceshipController>();

            // --- Host
            // The Game Session SPECIFIC settings are initialized
            if (Object.HasStateAuthority == false) return;  // 호스트가 아니면 종료

            _screenBoundaryX = Camera.main.orthographicSize * Camera.main.aspect;   // 스크린 바운더리 계산
            _screenBoundaryY = Camera.main.orthographicSize;
        }

        public override void FixedUpdateNetwork()
        {
            // Bail out of FUN() if this spaceship does not currently accept input
            if (_spaceshipController.AcceptInput == false) return;  // 우주선이 입력을 받지 못하는 상황(리스폰, 게임오버)이면 종료

            // GetInput() can only be called from NetworkBehaviours.
            // In SimulationBehaviours, either TryGetInputForPlayer<T>() or GetInputForPlayer<T>() has to be called.
            // This will only return true on the Client with InputAuthority for this Object and the Host.
            if (Runner.TryGetInputForPlayer<SpaceshipInput>(Object.InputAuthority, out var input))
            {
                Move(input);    // 입력을 받아왔으면 Move함수 실행
            }

            CheckExitScreen();  // 화면 밖을 벗어났으면 반대쪽으로 순간이동
        }

        // Moves the spaceship RB using the input for the client with InputAuthority over the object
        // 입력권한이 있는 클라이언트 입력을 바탕으로 움직인다.
        private void Move(SpaceshipInput input)
        {
            // 원래 회전에서 입력받은 만큼 추가로 회전시키기
            // 회전구하기 = 원래 회전 * AD입력을 기반으로 만든 회전
            Quaternion rot = _rigidbody.rotation *
                             Quaternion.Euler(0, input.HorizontalInput * _rotationSpeed * Runner.DeltaTime, 0); // AD입력을 기반으로 회전 구하기
            _rigidbody.MoveRotation(rot);   // 계산이 끝난 회전을 적용

            // (rot * Vector3.forward) : 현재 비행기가 바라보는 앞쪽 방향
            // VerticalInput : 전진 or 후진
            // _movementSpeed : 힘의 크기
            // Runner.DeltaTime : 이번 프레임에 걸릴 시간
            //Vector3 force = (rot * Vector3.forward) * input.VerticalInput * _movementSpeed * Runner.DeltaTime;
            Vector3 force = rot * (input.VerticalInput * _movementSpeed * Runner.DeltaTime * Vector3.forward); // 최적화를 위해 순서 조정
            _rigidbody.AddForce(force);     // add

            //if (_rigidbody.velocity.magnitude > _maxSpeed)
            if (_rigidbody.velocity.sqrMagnitude > _maxSpeed * _maxSpeed)
            {
                _rigidbody.velocity = _rigidbody.velocity.normalized * _maxSpeed;
            }
        }

        // 우주선이 화면 바운더리를 벗어나면 화면 반대쪽으로 보내는 코드
        private void CheckExitScreen()
        {
            var position = _rigidbody.position;

            if (Mathf.Abs(position.x) < _screenBoundaryX && Mathf.Abs(position.z) < _screenBoundaryY) return;   // x, y 둘다 안이면 종료

            if (Mathf.Abs(position.x) > _screenBoundaryX)
            {
                // x가 벗어나면 반대쪽 위치 설정 (x부호만 반대로 설정)
                position = new Vector3(-Mathf.Sign(position.x) * _screenBoundaryX, 0, position.z); 
            }

            if (Mathf.Abs(position.z) > _screenBoundaryY)
            {
                // y 바운더리를 벗어나면 z부호만 반대로 설정
                position = new Vector3(position.x, 0, -Mathf.Sign(position.z) * _screenBoundaryY);
            }

            // 두개 면에서 계속 순간이동 하는 것을 방지하기 위해 약간 안쪽으로 이동시킴
            position -= position.normalized * 0.1f;

            GetComponent<NetworkRigidbody3D>().Teleport(position);  // 최종 위치로 순간이동
        }
    }
}