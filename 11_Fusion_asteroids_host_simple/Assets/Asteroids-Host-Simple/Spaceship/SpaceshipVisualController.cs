using System.Collections;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

namespace Asteroids.HostSimple
{
    // Class controlling the visual representation of the spaceship (turning the 3D model on / off)
    // and visual feedback for the player (engine & destruction VFX)

    // 우주선의 비주얼적인 면을 컨트롤하는 클래스 ( 엔진 불길과 파괴용 VFX로 파견 )
    public class SpaceshipVisualController : MonoBehaviour
    {
        // 배 3D 모델를 그리는 메시 랜더러
        [SerializeField] private MeshRenderer _spaceshipModel = null;

        // 배가 터지는 이펙트
        [SerializeField] private ParticleSystem _destructionVFX = null;

        // 엔진 트레일 이펙트
        [SerializeField] private ParticleSystem _engineTrailVFX = null;

        // PlayerRef를 이용해 배의 색상을 지정
        public void SetColorFromPlayerID(int playerID)
        {
            foreach (Renderer r in GetComponentsInChildren<Renderer>())
            {
                r.material.color = GetColor(playerID);  // 모든 랜더러의 머터리얼의 색상을 플레이어 i
            }
        }

        // 스폰되었을 때 실행
        public void TriggerSpawn()
        {
            _spaceshipModel.enabled = true; // 모델 보여주기
            _engineTrailVFX.Play();         // 엔진 트레일 실행
            _destructionVFX.Stop();         // 폭발 이펙트 끄기
        }

        // 파괴되었을 때 실행
        public void TriggerDestruction()
        {
            _spaceshipModel.enabled = false;    // 모델 안보여주기
            _engineTrailVFX.Stop();             // 엔진 트레일 제거
            _destructionVFX.Play();             // 폭발 이펙트 켜기
        }

        // 플레이어를 구별하기위한 색상셋을 정의 ( 기본적으로 최대 4인 플레이지만 현재 ,2;)
        public static Color GetColor(int player)
        {
            switch (player%8)
            {
                case 0: return Color.red;
                case 1: return Color.green;
                case 2: return Color.blue;
                case 3: return Color.yellow;
                case 4: return Color.cyan;
                case 5: return Color.grey;
                case 6: return Color.magenta;
                case 7: return Color.white;
            }
            return Color.black;
        }
    }
}