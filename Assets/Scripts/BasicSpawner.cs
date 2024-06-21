using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Fusion;
using Fusion.Sockets;

public class BasicSpawner : MonoBehaviour, INetworkRunnerCallbacks
{
    private NetworkRunner myRunner = null;    // ��Ʈ��ũ �Ŵ��� ����

    [SerializeField]
    private NetworkPrefabRef playerPrefab;  // �÷��̾� ������

    /// <summary>
    /// �÷��̾� ��ųʸ� ( �÷��̾� ���۷���, ��Ʈ��ũ ������Ʈ )
    /// </summary>
    private Dictionary<PlayerRef, NetworkObject> spawnedCharacters = new Dictionary<PlayerRef, NetworkObject>();

    /// <summary>
    /// ���� ������ ���ų� �����ϴ� �Լ�
    /// </summary>
    /// <param name="gameMode">������ ���� ��� ( Host or Client )</param>
    async void StartGame(GameMode gameMode) // async : �񵿱� �޼��� ( ���ο� await�� ���� )
    {
        myRunner = this.gameObject.AddComponent<NetworkRunner>(); // ��Ʈ��ũ ���� ������Ʈ �߰�
        myRunner.ProvideInput = true;                             // ���� �Է� ������ ���̶�� ����

        // ���� ���� ������� NetWorkSceneInfo ����
        SceneRef scene = SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex);   // ���� �� ���۷��� ��������
        NetworkSceneInfo sceneInfo = new NetworkSceneInfo();
        if(scene.IsValid)
        {
            sceneInfo.AddSceneRef(scene, LoadSceneMode.Additive);
        }

        await myRunner.StartGame(new StartGameArgs()
        {
            // �� ������ ������ ���� ����
            GameMode = gameMode,
            SessionName = "TestRoom",
            Scene = scene,
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>()
        });
    }

    /// <summary>
    /// GUI�� �׸��� ���� �̺�Ʈ �Լ�
    /// </summary>
    private void OnGUI()
    {
        if(myRunner == null)
        {
            if(GUI.Button(new Rect(0,0,200,40), "Host"))
            {
                StartGame(GameMode.Host);
            }

            if(GUI.Button(new Rect(0,40,200,40),"Client"))
            {
                StartGame(GameMode.Client);
            }
        }
    }
    
    /// <summary>
    /// �÷��̾ �������� �� ����Ǵ� �Լ�
    /// </summary>
    /// <param name="runner">�ڱ� �ڽ��� ����(�� ����)</param>
    /// <param name="player">������ �÷��̾�</param>
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if(runner.IsServer) // ���������� ����
        {
            // ���� �� ��ġ�� ���ϱ�
            Vector3 spawnPosition = new Vector3(player.RawEncoded % runner.Config.Simulation.PlayerCount, 0, 0);

            // �÷��̾� ������Ʈ ���� ( 4��° �Ķ���� : �� ������Ʈ�� �Է��� �� �� �ִ� �÷��̾ ���� ���� ( ���� ���� ���� )
            NetworkObject netPlayer = runner.Spawn(playerPrefab, spawnPosition, Quaternion.identity, player);

            // �÷��̾� ��ü Ȯ�� �� ������ ���ϰ� �ϱ� ���� �뵵
            spawnedCharacters.Add(player, netPlayer);
        }
    }

    /// <summary>
    /// �÷��̾ ������ �������� �� ����Ǵ� �Լ�
    /// </summary>
    /// <param name="runner">�ڱ� �ڽ��� ����(�� ����)</param>
    /// <param name="player">������ �÷��̾�</param>
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) 
    { 
        if(spawnedCharacters.TryGetValue(player, out NetworkObject networkObject))  // spawnedCharacters�� player�� ���� ���� ó��
        {
            runner.Despawn(networkObject);      // ���ʿ��� ���� (���� ������Ʈ ������ �Բ� ó��)
            spawnedCharacters.Remove(player);   // ��ųʸ����� ����
        }
    }

    /// <summary>
    /// ������� �Է� �����͸� �����ϴ� �Լ�
    /// </summary>
    /// <param name="runner">��Ʈ��ũ ����</param>
    /// <param name="input">������ �޾ư� ������</param>
    public void OnInput(NetworkRunner runner, NetworkInput input) 
    {
        NetworkInputData data = new NetworkInputData(); // �츮�� ���� ������ Ÿ���� new�ϱ�

        if(Input.GetKey(KeyCode.W))     // �� ���� WŰ�� �������ִ��� Ȯ�� ( ����� true�� WŰ�� �������ִ�. false�� �ȴ������ֵ�.)
        {
            data.direction += Vector3.forward;
        }
        if (Input.GetKey(KeyCode.A))
        {
            data.direction += Vector3.left;

        }
        if (Input.GetKey(KeyCode.S))
        {
            data.direction += Vector3.back;
        }
        if (Input.GetKey(KeyCode.D))
        {
            data.direction += Vector3.right;
        }

        input.Set(data);    // ������ �Է��� ���������� ����
    }


    // ===================================================================================================

    public void OnConnectedToServer(NetworkRunner runner) { }

    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) {}

    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) {}

    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) {}

    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) {}

    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) {}

    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) {}

    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) {}

    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) {}

    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) {}

    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) {}

    public void OnSceneLoadDone(NetworkRunner runner) {}

    public void OnSceneLoadStart(NetworkRunner runner) {}

    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) {}

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) {}

    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) {}
}