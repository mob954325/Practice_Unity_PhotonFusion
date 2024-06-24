using UnityEngine;
using UnityEngine.EventSystems;

namespace Asteroids.HostSimple
{
    /// <summary>
    /// Event system spawner. Will add an EventSystem GameObject with an EventSystem component and a StandaloneInputModule component.
    /// Use this in additive scene loading context where you would otherwise get a "Multiple EventSystem in scene... this is not supported" error
    /// from Unity.
    /// 
    /// 이벤트 시스템 스포너
    /// EventSystem 게임 오브젝트 추가 (EventSystem, StandaloneInputModule 컴포넌트 포함)
    /// Addive로 씬을 로딩할 때 "EventSystem"이 여러개인 씬은 지원하지 않습니다"라는 애러가 발생하면 사용
    /// </summary>
    public class EventSystemSpawner : MonoBehaviour
    {
        void OnEnable()
        {
            EventSystem sceneEventSystem = FindObjectOfType<EventSystem>();
            if (sceneEventSystem == null)   // 찾아서 없을 때만 만들기
            {
                GameObject eventSystem = new GameObject("EventSystem"); // 빈 오브젝트 생성    

                eventSystem.AddComponent<EventSystem>();                // EventSystem 컴포넌트 추가
                eventSystem.AddComponent<StandaloneInputModule>();      // StandaloneInputModule 컴포넌트 추가
            }
        }
    }
}
