using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Fusion;
using TMPro;
using UnityEngine.SceneManagement;

namespace Asteroids.HostSimple
{
    // 메뉴 처리용 유틸리티 클래스 ( 메뉴씬에서 확인 가능 )
    public class StartMenu : MonoBehaviour
    {
        // 네트워크 러너 프리팹
        [SerializeField] private NetworkRunner _networkRunnerPrefab = null;

        // 플레이어 데이터 프리팹 ( 이름 관련 )
        [SerializeField] private PlayerData _playerDataPrefab = null;

        // 플레이어 이름용 인풋 필드
        [SerializeField] private TMP_InputField _nickName = null;

        // 플레이어 이름용 인풋필드의 플레이스 홀더 ( 랜덤 이름 설정용 )
        [SerializeField] private TextMeshProUGUI _nickNamePlaceholder = null;

        // 방 이름용 인풋필드
        [SerializeField] private TMP_InputField _roomName = null;

        // 게임 씬의 이름
        [SerializeField] private string _gameSceneName = null;

        // 네트워크 러너
        private NetworkRunner _runnerInstance = null;

        // (호스트 or 클라이언트)로 세션을 시작하는 함수
        public void StartHost()
        {
            SetPlayerData();    // 이름 설정
            StartGame(GameMode.AutoHostOrClient, _roomName.text, _gameSceneName);
        }

        // 클라이언트로 세션을 시작하는 함수
        public void StartClient()
        {
            SetPlayerData();
            StartGame(GameMode.Client, _roomName.text, _gameSceneName);
        }

        private void SetPlayerData()
        {
            var playerData = FindObjectOfType<PlayerData>();        // ?
            if (playerData == null)
            {
                playerData = Instantiate(_playerDataPrefab);        // 플레이어 데이터가 없으면 새로 생성
            }

            if (string.IsNullOrWhiteSpace(_nickName.text))          // 플레이스 홀더가 비어있으면, 설정되있지 않으면
            {       
                playerData.SetNickName(_nickNamePlaceholder.text);  // 플레이스 홀더에 있는 이름 사용
            }
            else
            {
                playerData.SetNickName(_nickName.text);             // 설정된 플레이어 이름 사용
            }
        }

        private async void StartGame(GameMode mode, string roomName, string sceneName)
        {
            _runnerInstance = FindObjectOfType<NetworkRunner>();
            if (_runnerInstance == null)
            {
                _runnerInstance = Instantiate(_networkRunnerPrefab);
            }

            // Let the Fusion Runner know that we will be providing user input
            _runnerInstance.ProvideInput = true;

            var startGameArgs = new StartGameArgs()
            {
                GameMode = mode,
                SessionName = roomName,
                ObjectProvider = _runnerInstance.GetComponent<NetworkObjectPoolDefault>(),
            };

            // GameMode.Host = Start a session with a specific name
            // GameMode.Client = Join a session with a specific name
            await _runnerInstance.StartGame(startGameArgs);

            if (_runnerInstance.IsServer)
            {
                _runnerInstance.LoadScene(sceneName);
            }
        }
    }
}