using System.Collections;
using System.Collections.Generic;
using Fusion;
using Fusion.Addons.Physics;
using UnityEngine;

namespace Asteroids.HostSimple
{
    // 운석에 대한 정보를 가지는 스크립트
    public class AsteroidBehaviour : NetworkBehaviour
    {
        // 운석을 파괴 시켰을 때 플레이어가 얻을 점수 ( 로컬 )
        [SerializeField] private int _points = 1;

        // The IsBig variable is Networked as it can be used to evaluate and derive visual information for an asteroid locally.
        // 큰 운석인지 여부 (로컬에서 평가하는데 필요하기 때문에 Networked로 설정)
        [HideInInspector] [Networked] public NetworkBool IsBig { get; set; }

        // Used to delay the despawn after the hit and play the destruction animation.
        // 적중 후 소멸을 지연시키고 파괴 애니메이션을 재생하는데 사용
        [Networked] private NetworkBool _wasHit { get; set; }

        // 디스폰 타이머
        [Networked] private TickTimer _despawnTimer { get; set; }

        // 네트워크 리지드바디
        private NetworkRigidbody3D _networkRigidbody;

        // 안맞았으면 무조건 살아있음
        public bool IsAlive => !_wasHit;

        public override void Spawned()
        {
            _networkRigidbody = GetComponent<NetworkRigidbody3D>();         // 네트워크 리지드 바디 찾아놓기
            _networkRigidbody.InterpolationTarget.localScale = Vector3.one; // 물리 보간용 오브젝트의 크기를 1,1,1로 세팅
        }

        // 운석이 다른 물체와 부딛쳤을 대 어떤 행동을 할지 결정하는 함수
        public void HitAsteroid(PlayerRef player)
        {
            // 운석의 충돌은 아직 부딪치지 않은 상태에서 호스트에서만 트리거 된다.
            if (Object == null) return;                     // 존재하는 오브젝드여야한다.
            if (Object.HasStateAuthority == false) return;  // 호스트가 아니면 스킵
            if (_wasHit) return;                            // 맞았으면 스킵

            // If this hit was triggered by a projectile, the player who shot it gets points
            // The player object is retrieved via the Runner.
            // 총알에 의해 맞은 경우에는 총알을 발사한 플레이어의 점수가 증가한 플레이어의 점수가 올라감
            // 플레이어의 오브젝트는 Runner을 통해 찾는다.
            if (Runner.TryGetPlayerObject(player, out var playerNetworkObject))
            {
                playerNetworkObject.GetComponent<PlayerDataNetworked>().AddToScore(_points);    // 찾은 플레이어의 점수 추가
            }

            _wasHit = true;     // 맞았다고 표시
            _despawnTimer = TickTimer.CreateFromSeconds(Runner, .2f);   // 디스폰 타이머 돌리기
        }

        public override void FixedUpdateNetwork()
        {
            // 호스트이고 맞았고, 디스폰 타이머도 만료되었으면
            if (Object.HasStateAuthority && _wasHit && _despawnTimer.Expired(Runner))
            {
                _wasHit = false;                    // 명중 안했다고 리셋
                _despawnTimer = TickTimer.None;     // 디스폰 타이머도 제거

                // Big asteroids tell the AsteroidSpawner to spawn multiple small asteroids as it breaks up.
                if (IsBig)
                {
                  // 큰 운석이 파괴되면 여러 작은 운석이 생성된다.
                    FindObjectOfType<AsteroidSpawner>().BreakUpBigAsteroid(transform.position);
                }

                Runner.Despawn(Object); // 디스폰 하기
            }
        }

        public override void Render()
        {
            if (_wasHit && _despawnTimer.IsRunning) // 맞았는데 디스폰이 아직 안된상황
            {
                _networkRigidbody.InterpolationTarget.localScale *= .95f; // 이 오브젝트 크기를 계속 줄인다.
            }
        }
    }
}
