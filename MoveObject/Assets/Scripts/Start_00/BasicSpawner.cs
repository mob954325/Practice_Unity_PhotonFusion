using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

using Fusion;
using Fusion.Sockets;
using System;
using Fusion.Addons.Physics;

// INetworkRunnerCallbacks : Fusion에서 네트워크 시뮬레이션을 실행하기 위한 인터페이스
public class BasicSpawner : MonoBehaviour, INetworkRunnerCallbacks
{
    /// <summary>
    /// 네트워크 러너
    /// </summary>
    private NetworkRunner runner;

    /// <summary>
    /// 플레이어 프리팹 레퍼런스
    /// </summary>
    [SerializeField] private NetworkPrefabRef playerPrefab;

    /// <summary>
    /// 접속한 플레이어 추적을 위한 리스트
    /// </summary>
    private Dictionary<PlayerRef, NetworkObject> spawnedCharacters = new Dictionary<PlayerRef, NetworkObject>();

    /// <summary>
    /// 왼쪽클릭 입력 확인용 변수
    /// </summary>
    private bool mouseButton0;

    /// <summary>
    /// 오른쪽클릭 입력 확인용 변수
    /// </summary>
    private bool mouseButton1;

    // 생명 함수 ========================================================================================================

    private void Update()
    {
        // 마우스 버튼 입력 비트 확인, 마우스를 입력하거나 입력이 되어있으면 1 아니면 0
        mouseButton0 = mouseButton0 | Input.GetMouseButton(0);
        mouseButton1 = mouseButton1 | Input.GetMouseButton(1);
    }


    // 기능 함수 ========================================================================================================

    async void StartGame(GameMode mode)
    {
        runner = gameObject.AddComponent<NetworkRunner>();
        runner.ProvideInput = true; // 입력을 제공할 것을 알림

        // 현재씬의 네트워크 씬인포(NetworkSceneInfo) 생성
        var scene = SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex);
        var sceneInfo = new NetworkSceneInfo();
        if(scene.IsValid)
        {
            sceneInfo.AddSceneRef(scene, LoadSceneMode.Additive); // sceneInfo의 씬 리스트에 씬 추가
        }

        // 특정 세션 이름으로 게임모드에 따라서 시작 또는 참가
        await runner.StartGame(new StartGameArgs()
        {
            GameMode = mode,
            SessionName = "TestRoom",
            Scene = scene,
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>()            
        });

        gameObject.AddComponent<RunnerSimulatePhysics3D>();
    }

    // 게임 시작될 때 생성될 GUI(게임 모드에 따른 참가)
    private void OnGUI()
    {
        if(runner == null)
        {
            if(GUI.Button(new Rect(0,0,200,40), "Host"))
            {
                StartGame(GameMode.Host);
            }
            if(GUI.Button(new Rect(0, 40, 200, 40), "Join"))
            {
                StartGame(GameMode.Client);
            }
        }
    }

    // NetWorkRunnerCallBack ===================================================================================================

    // 플레이어가 들어왔을 때 호출되는 함수
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if(runner.IsServer)
        {
            // 플레이어 고유 포지션 생성
            Vector3 spawnPosition = new Vector3((player.RawEncoded % runner.Config.Simulation.PlayerCount) * 3, 1, 0);
            NetworkObject networkPlayerObject = runner.Spawn(playerPrefab, spawnPosition, Quaternion.identity, player);

            // 플레이어를 쉽게 접근하기 위해 리스트 추가
            spawnedCharacters.Add(player, networkPlayerObject);
        }
    }
    
    // 플레이어가 나갔을 때 호출되는 함수
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        // 플레이어 찾기
        if(spawnedCharacters.TryGetValue(player, out NetworkObject networkObject))
        {
            runner.Despawn(networkObject);      // 디스폰
            spawnedCharacters.Remove(player);   // 리스트 제거
        }
    }

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        var data = new NetworkInputData();

        if (Input.GetKey(KeyCode.W))
            data.direction += Vector3.forward;

        if (Input.GetKey(KeyCode.S))
            data.direction += Vector3.back;

        if (Input.GetKey(KeyCode.A))
            data.direction += Vector3.left;

        if (Input.GetKey(KeyCode.D))
            data.direction += Vector3.right;

        data.buttons.Set(NetworkInputData.MOUSEBUTTON0, mouseButton0);
        mouseButton0 = false; // 마우스 입력 해제

        data.buttons.Set(NetworkInputData.MOUSEBUTTON1, mouseButton1);
        mouseButton1 = false; // 마우스 입력 해제

        input.Set(data);
    }


    // 사용 안함 ===================================================================================================================================
    public void OnConnectedToServer(NetworkRunner runner)
    {
    }

    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
    {
    }

    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)
    {
    }

    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data)
    {
    }

    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
    }

    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken)
    {
    }

    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input)
    {
    }

    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {
    }

    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {
    }

    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress)
    {
    }

    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data)
    {
    }

    public void OnSceneLoadDone(NetworkRunner runner)
    {
    }

    public void OnSceneLoadStart(NetworkRunner runner)
    {
    }

    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    {
    }

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
    }

    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message)
    {
    }
}