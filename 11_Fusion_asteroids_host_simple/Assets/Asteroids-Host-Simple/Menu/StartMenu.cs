using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Fusion;
using TMPro;
using UnityEngine.SceneManagement;

namespace Asteroids.HostSimple
{
    // �޴� ó���� ��ƿ��Ƽ Ŭ���� ( �޴������� Ȯ�� ���� )
    public class StartMenu : MonoBehaviour
    {
        // ��Ʈ��ũ ���� ������
        [SerializeField] private NetworkRunner _networkRunnerPrefab = null;

        // �÷��̾� ������ ������ ( �̸� ���� )
        [SerializeField] private PlayerData _playerDataPrefab = null;

        // �÷��̾� �̸��� ��ǲ �ʵ�
        [SerializeField] private TMP_InputField _nickName = null;

        // �÷��̾� �̸��� ��ǲ�ʵ��� �÷��̽� Ȧ�� ( ���� �̸� ������ )
        [SerializeField] private TextMeshProUGUI _nickNamePlaceholder = null;

        // �� �̸��� ��ǲ�ʵ�
        [SerializeField] private TMP_InputField _roomName = null;

        // ���� ���� �̸�
        [SerializeField] private string _gameSceneName = null;

        // ��Ʈ��ũ ����
        private NetworkRunner _runnerInstance = null;

        // (ȣ��Ʈ or Ŭ���̾�Ʈ)�� ������ �����ϴ� �Լ�
        public void StartHost()
        {
            SetPlayerData();    // �̸� ����
            StartGame(GameMode.AutoHostOrClient, _roomName.text, _gameSceneName);
        }

        // Ŭ���̾�Ʈ�� ������ �����ϴ� �Լ�
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
                playerData = Instantiate(_playerDataPrefab);        // �÷��̾� �����Ͱ� ������ ���� ����
            }

            if (string.IsNullOrWhiteSpace(_nickName.text))          // �÷��̽� Ȧ���� ���������, ���������� ������
            {       
                playerData.SetNickName(_nickNamePlaceholder.text);  // �÷��̽� Ȧ���� �ִ� �̸� ���
            }
            else
            {
                playerData.SetNickName(_nickName.text);             // ������ �÷��̾� �̸� ���
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