using System;
using System.Collections.Generic;
using System.Linq;
using Fusion;
using TMPro;
using UnityEngine;

namespace Asteroids.HostSimple
{
    public class GameStateController : NetworkBehaviour
    {
        enum GameState
        {
            Starting,   // 시작 중 ( 시작 딜레이 카운터되는 시점 )
            Running,    // 플레이 중
            Ending      // 끝났을 때 ( 종료 딜레이 카운팅 되는 사람
        }

        // 게임이 시작되었을 때의 딜레이
        [SerializeField] private float _startDelay = 4.0f;
        // 게임이 종료되었을 때의 딜레이
        [SerializeField] private float _endDelay = 4.0f;
        // 게임의 세션 길이 ( 딱히 사용되고 있지 않음 )
        [SerializeField] private float _gameSessionLength = 180.0f;

        // 시작 / 종료 딜레이 표시용
        [SerializeField] private TextMeshProUGUI _startEndDisplay = null;       
        [SerializeField] private TextMeshProUGUI _ingameTimerDisplay = null; 

        [Networked] private TickTimer _timer { get; set; }      // 타이머 ( 시작 / 종료 / 세션 모두 사용 )

        [Networked] private GameState _gameState { get; set; }  // 개임 상태

        [Networked] private NetworkBehaviourId _winner { get; set; }


        private List<NetworkBehaviourId> _playerDataNetworkedIds = new List<NetworkBehaviourId>();

        // 스폰 이후에 실행되는 함수
        public override void Spawned()
        {
            // --- This section is for all information which has to be locally initialized based on the networked game state
            // --- when a CLIENT joins a game

            // 게임 상태에 따라 로컬로 초기회해야하는 모든 정보에 대한 것 ( 클라이언트가 게임에 접속 했을 때 )

            _startEndDisplay.gameObject.SetActive(true);        // 가운데 텍스트 활성화
            _ingameTimerDisplay.gameObject.SetActive(false);    // 아래쪽 텍스트 활성화

            // 이미 게임이 시작된 상황 ( 다른 사람이 플레이 중인 방에 접속한 상황) 이면
            if (_gameState != GameState.Starting)
            {
                foreach (var player in Runner.ActivePlayers)    // 활성화된 모든 플레이어 순회하면서
                {
                    if (Runner.TryGetPlayerObject(player, out var playerObject) == false) continue;
                    TrackNewPlayer(playerObject.GetComponent<PlayerDataNetworked>().Id);    // 리스트에 플레이어를 추가 ?
                }
            }

            // GameStateController는 클라이언트에서도 FixedUpdate를 실행하게 된다.
            Runner.SetIsSimulated(Object, true);

            // 여기서 부터는 호스트에 의해 초기화 되는 모든 [networked] 변수의 초기화 작업
            if (Object.HasStateAuthority == false) return;

            // Initialize the game state on the host
            // 홋그트가 게임 상태 변경
            _gameState = GameState.Starting;                            // 상태 변경
            _timer = TickTimer.CreateFromSeconds(Runner, _startDelay);  // 시작 딜레이 시작
        }

        public override void FixedUpdateNetwork()
        {
            // 게임 상태에 따른 게임 화면 업데이트
            switch (_gameState)
            {
                case GameState.Starting:
                    UpdateStartingDisplay();
                    break;
                case GameState.Running:
                    UpdateRunningDisplay();
                    // Ends the game if the game session length has been exceeded
                    if (_timer.ExpiredOrNotRunning(Runner))
                    {
                        GameHasEnded(); // _gameSessionLength 시간이 만료되면 게임 종료
                    }
                    break;
                case GameState.Ending:
                    UpdateEndingDisplay();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void UpdateStartingDisplay()
        {
            // --- Host & Client

            // 게임 시작까지 남아있는 시간 출력 ( 소숫점 X )
            _startEndDisplay.text = $"Game Starts In {Mathf.RoundToInt(_timer.RemainingTime(Runner) ?? 0)}";

            // --- Host
            // 호스트가 아니면 이후 진행하지 말 것
            if (Object.HasStateAuthority == false) return;
            // 시작카운트? 타이머가 만료되지않았고 동작 중일 때 이후는 진행 X
            if (_timer.ExpiredOrNotRunning(Runner) == false) return;

            // 여기부터는 호스트이면서 타이머가 만료되거나 실행되고 있지 않을 때 처리

            // 우주선과 운석 스포너를 작동 시킨다 ( 게임 시작하고 딜레이가 만료되면 한번만 실행된다 )
            FindObjectOfType<SpaceshipSpawner>().StartSpaceshipSpawner(this);
            FindObjectOfType<AsteroidSpawner>().StartAsteroidSpawner();

            _gameState = GameState.Running;                                     // 게임 상태 Running으로 변경
            _timer = TickTimer.CreateFromSeconds(Runner, _gameSessionLength);   // 타이머를 세션 길이로 재시작
        }

        private void UpdateRunningDisplay()
        {
            // --- Host & Client
            // Display the remaining time until the game ends in seconds (rounded down to the closest full second)
            _startEndDisplay.gameObject.SetActive(false);       // 가운데 텍스트 비활성화
            _ingameTimerDisplay.gameObject.SetActive(true);     // 아래쪽 텍스트 활성화
            _ingameTimerDisplay.text =
                $"{Mathf.RoundToInt(_timer.RemainingTime(Runner) ?? 0).ToString("000")} seconds left";  // 남은 세션 시간  초단위 출력
        }

        private void UpdateEndingDisplay()
        {
            // --- Host & Client
            // Display the results and
            // the remaining time until the current game session is shutdown
            // 게임결과와 셧다운까지 남은시간 출력

            if (Runner.TryFindBehaviour(_winner, out PlayerDataNetworked playerData) == false) return;  // 승리자가 없으면 종료

            _startEndDisplay.gameObject.SetActive(true);        // 가운데 텍스트 활성화 
            _ingameTimerDisplay.gameObject.SetActive(false);    // 아래쪽 텍스트 비활성화
            _startEndDisplay.text =                             // 승리자와 승리자 점수, 디스커넥트까지 남은시간 출력
                $"{playerData.NickName} won with {playerData.Score} points. Disconnecting in {Mathf.RoundToInt(_timer.RemainingTime(Runner) ?? 0)}";
            _startEndDisplay.color = SpaceshipVisualController.GetColor(playerData.Object.InputAuthority.PlayerId); // 출력할 글의 색상을 플레이어의 색상으로 지정

            // --- Host
            // 게임 세션 셧다운 하기
            // Shutdowns the current game session.
            // The disconnection behaviour is found in the OnServerDisconnect.cs script
            if (_timer.ExpiredOrNotRunning(Runner) == false) return;    //  게임 종료 타이머가 만료될때까지 스킵

            Runner.Shutdown();
        }

        // Called from the ShipController when it hits an asteroid
        // 배가 운석에 맞았을 때 실행되는 함수
        public void CheckIfGameHasEnded()
        {
            if (Object.HasStateAuthority == false) return;  // 호스트가 아니면 리턴

            // 호스트만 처리
            int playersAlive = 0;

            for (int i = 0; i < _playerDataNetworkedIds.Count; i++) // _playerDataNetworkedIds를 순회하면서 러너에 없는 것은 제거
            {
                if (Runner.TryFindBehaviour(_playerDataNetworkedIds[i],
                        out PlayerDataNetworked playerDataNetworkedComponent) == false)
                {
                    _playerDataNetworkedIds.RemoveAt(i);
                    i--;
                    continue;
                }

                if (playerDataNetworkedComponent.Lives > 0) playersAlive++; // 순회하면서 수명이 1 이상인 플레이어의 수 카운팅
            }

            // If more than 1 player is left alive, the game continues.
            // If only 1 player is left, the game ends immediately.

            // 플레이어가 2명이상 남아있거나, 혼자서 플레이하는 경우인데 수명이 남아있으면 게임 계속 진행
            if (playersAlive > 1 || (Runner.ActivePlayers.Count() == 1 && playersAlive == 1)) return;

            foreach (var playerDataNetworkedId in _playerDataNetworkedIds)
            {
                if (Runner.TryFindBehaviour(playerDataNetworkedId,
                        out PlayerDataNetworked playerDataNetworkedComponent) ==
                    false) continue;    // 목록에 없으면 스킵

                if (playerDataNetworkedComponent.Lives > 0 == false) continue;  // 수명이 다 된 사람은 스킵

                _winner = playerDataNetworkedId;    // 승리자 결정
            }

            // _winner가 값이 없으면 혼자서 호스트 모드로 플레이 하고 있다고 가정하고 승리자로 결정
            if (_winner == default) 
            {
                _winner = _playerDataNetworkedIds[0];
            }

            GameHasEnded(); // 실제 게임 종료 처리
        }

        // 게임이 종료될 때 실행될 함수
        private void GameHasEnded()
        {
            _timer = TickTimer.CreateFromSeconds(Runner, _endDelay);    // 종료 딜레이 설정
            _gameState = GameState.Ending;  // 게임 상태를 Ending으로 변경
        }

        public void TrackNewPlayer(NetworkBehaviourId playerDataNetworkedId)
        {
            _playerDataNetworkedIds.Add(playerDataNetworkedId);
        }
    }
}
