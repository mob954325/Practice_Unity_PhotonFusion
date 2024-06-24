using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : NetworkBehaviour
{

    NetworkCharacterController cc;

    Vector3 forward = Vector3.forward;
    public float moveSpeed = 5f;

    [SerializeField] Ball prefabBall;
    [SerializeField] PhysicBall prefabBall_Physx;

    [Networked] TickTimer Delay { get; set; }

    [Networked] public bool spawnedProjectile { get; set; }

    /// <summary>
    /// Networked�� ������ ������ ��ȭ�� �����ϴ� Ŭ����
    /// </summary>
    private ChangeDetector changeDetector;

    /// <summary>
    /// �÷��̾� ���� ���͸���
    /// </summary>
    public Material bodyMaterial;

    void Awake()
    {
        cc = GetComponent<NetworkCharacterController>();
        Transform child = transform.GetChild(0);
        bodyMaterial = child.GetComponent<Renderer>()?.material;
    }
    /// <summary>
    /// ��Ʈ��ũ ƽ���� ��� ����Ǵ� �Լ�
    /// </summary>
    public override void FixedUpdateNetwork()
    {
        if(GetInput(out NetworkInputData data))     // ���� �ʿ��� �Է� ���� �޾ƿ���
        {
            // data.direction.Normalize();          // ���ֺ��ͷ� �̵�

            cc.Move(Runner.DeltaTime * moveSpeed * data.direction); // �ʴ� moveSpeed�� �ӵ��� data.direction �������� �̵�

            if(data.direction.sqrMagnitude > 0)    // �̵� ���̴�
            {
                forward = data.direction;          // ȸ�� ���߿� forward �������� ���� �߻�Ǵ� ���� ����
            }

            if(HasStateAuthority && Delay.ExpiredOrNotRunning(Runner))   // ȣ��Ʈ���� Ȯ�� && delay�� ���� �ȵǾ��ų� 0.5�� �����ϰ� ���� 
            {
                if(data.buttons.IsSet(NetworkInputData.MouseButtonLeft))    // ���콺 ���� ��ư�� ����������
                {
                    Delay = TickTimer.CreateFromSeconds(Runner, 0.5f);
                    Runner.Spawn(
                        prefabBall,                                 // ������ ������ 
                        transform.position + transform.forward,     // ������ ��ġ ( �ڱ� ��ġ + �Է� ���� )
                        Quaternion.LookRotation(forward),           // ������ Ù�� ( �Է� ���� ������ )
                        Object.InputAuthority,                      // ������ �÷��̾ ȣ��Ʈ�� ����
                        (runner, obj) =>                            // ���� ������ ����Ǵ� �����Լ�
                        {
                            obj.GetComponent<Ball>().Init();
                        });
                    spawnedProjectile = !spawnedProjectile;
                }

                if (data.buttons.IsSet(NetworkInputData.MouseButtonRight))
                {
                    Delay = TickTimer.CreateFromSeconds(Runner, 0.5f);
                    Runner.Spawn(
                        prefabBall_Physx,                                       // ������ ������ 
                        transform.position + forward + Vector3.up * 0.5f,       // ������ ��ġ ( �ڱ� ��ġ + �Է� ���� )
                        Quaternion.LookRotation(forward),                       // ������ Ù�� ( �Է� ���� ������ )
                        Object.InputAuthority,                                  // ������ �÷��̾ ȣ��Ʈ�� ����
                        (runner, obj) =>                                        // ���� ������ ����Ǵ� �����Լ�
                        {
                            obj.GetComponent<PhysicBall>().Init(moveSpeed * forward);
                        });
                    spawnedProjectile = !spawnedProjectile;
                }
            }
        }
    }

    /// <summary>
    /// ���� �� ���Ŀ� ����Ǵ� �Լ�
    /// </summary>
    public override void Spawned()
    {
        changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);
    }

    public override void Render()
    {
        // �� ��Ʈ��ũ ������Ʈ���� Networked�� ������ ������ ��ȭ�� �־��� �͵��� ��� ��ȸ
        foreach (var change in changeDetector.DetectChanges(this))
        {
            switch(change)
            {
                case nameof(spawnedProjectile): //spawnedProjectile ������ ����Ǿ��� ��
                    bodyMaterial.color = Color.white;
                    break;
            }
        }

        // Render�� ����Ƽ ���� �����󿡼� �۵� => Update�� ���� ����
        bodyMaterial.color = Color.Lerp(bodyMaterial.color, Color.blue, Time.deltaTime * 2);
    }
}