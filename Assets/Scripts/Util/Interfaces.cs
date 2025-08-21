using System;
using System.Collections.Generic;

namespace CAU
{
    /**
    * @interface    IManager
    * @description  IManager란?
    *               1. 부모 클래스가 같은 객체들을 생성 , 캐싱 , 삭제 ,  게터 , 세터 제공 ex) UIPanelManager 는 BaseUIPanel 들만 관리 함.
    *               2. 캐싱되어 있는 인스턴스들에 호출할 수 있는 공통 메서드 제공 .
    *               3. Singleton 이어야 함.            
    */
    public interface IManager
    {
        /** 해당 매니저 생성 시 초기화한다. 초기화 시 사용하는 메서드 */
        abstract void Initialize();
        /** 재사용 할 수 있게 초기상태로 돌린다. 반복해서 사용하는 메서드 */
        abstract void Refresh();
    }


    /**
    * @interface     IUtility
    * @description   1. 복잡한 로직을 공용화된 편의 기능을 제공하는 메서드 모음
    *                    ex) MathUtility class 
    */
    public interface IUtility
    {

    }


    /**
    * @interface     IController
    * @description   여러 종류 객체들을 쉽게 컨트롤할 수 있도록 캐싱 및 메서드 제공.  
    */
    public interface IController
    {
        abstract void Initialize(Action initCompleteCallback);
        abstract void Release();
    }


    /**
    * @interface	   ISceneController
    * @description     1. UnityScene 의 최상단의 오브젝트(GameObject).
    *                  2. 3D 공간(space)의 오브젝트를 제어하는 Controller 이다.
    *                  3. UnityScene의 하위 child Object들을 캐싱 및 컨트롤한다. 
    */
    public interface ISceneController : IController
    {

    }


    /**
    * @interface      ISingleton
    * @description    1. 한개의 객체만 생성해서 사용하는 Singleton class 들은 상속 받아서 사용.
    */
    public interface ISingleton<T> where T : class
    {
        static T Instance { get; }
        abstract void Release();
    }


    /**
    * @interface      IEventData
    * @description    1. event struct , class 알림을 수행하는 데이터 클래스 
    */
    public interface IEventData
    {
        int EventID { get; }
    }


    /**
    * @interface       INode
    * @description     1. 트리 자료 구조에 부착되는 노드 데이터를 정의합니다.
    */
    public interface INode
    {
        int NodeIndex { get; }
    }


    /**
    * @interface       IBuilder
    * @description     1. 빌더 패턴을 사용하는 클래스
    *                  2. 상황마다 필요한 클래스들을 하나로 조립하여 결과물을 뱉어준다.
    */
    public interface IBuilder
    {

    }


    /**
    * @interface       IBaseUIArgument
    * @description     1. UI에 맵핑되는 데이터를 정의합니다.
    */
    public interface IBaseUIArgument
    {

    }


    /**
    * @interface       IUpdateInvokeObject
    * @description     1. Update 로직을 매니저 클래스에 위임하기 위해 Update 계열 함수를 사용하는 모든 오브젝트에 붙여서 사용합니다.
    */
    public interface IUpdateInvokeObject
    {
        void DoUpdate();

        void DoLateUpdate();
    }


    /**
    * @interface       IBehaviourTreeNode
    * @description     1. AI 비헤이비어 트리 노드에 대한 각각의 진행 상태 및 데이터, 이벤트를 정의합니다.
    */
    public interface IBehaviourTreeNode : INode
    {
        public enum eNodeState
        {
            /// <summary>
            /// 현재 노드가 특정 조건을 만족함
            /// </summary>
            Success = 0,
            /// <summary>
            /// 현재 노드가 특정 조건을 만족하지 않음
            /// </summary>
            Failure,
            /// <summary>
            /// 현재 노드가 특정 조건 검사 진행 중. 조건 검사가 한 프레임 안에 다 끝나지 않을 경우 게임 흐름을 방해하지 않고 다음 프레임에서 다시 조건 검사.
            /// </summary>
            Running,
        }

        eNodeState CurrentNodeState { get; }

        List<IBehaviourTreeNode> ChildNodes { get; }

        eNodeState Evaluate();
    }


    /**
    * @interface       IObjectPool
    * @description     1. 디폴트로 N 입력 시 N개만큼 객체 생성. ex) N=10, List<GameObject> objs = new List<GameObject>(10);
    *                  2. Release() 는 N 개 이하가 될 시 지우지 말고 리턴만 받아야 함.
    *                  3. .NET에서 기본으로 제공하는 컨테이너에 추가 기능까지 래핑 ( 유용성, 편의성 제공 )
    */
    public interface IObjectPool
    {
        /// <summary>
        /// 현재 오브젝트 풀에 존재하는 전체 항목의 개수
        /// </summary>
        int Count { get; }

        /// <summary>
        /// 오브젝트 풀에 들어 있는 모든 항목을 제거
        /// </summary>
        void Clear();

        /// <summary>
        /// 현재 활성화되어 있는 모든 인스턴스를 오브젝트 풀에 전체 반환
        /// </summary>
        void ReleaseAll();

        /// <summary>
        /// 해당 인스턴스를 오브젝트 풀에 다시 반환
        /// </summary>
        /// <param name="index"></param>
        void Release();

    }


    /**
    * @interface       IGenericPool
    * @description     1. Generic Type으로 리턴 타입을 받을 수 있도록 Get 함수 세팅. ( value / object 공통 )
    *                  2. AddObject / Release에 제네릭 타입을 parameter로 받을 수 있도록 세팅.
    */
    public interface IGenericPool<T>
    {
        /// <summary>
        /// 해당 인스턴스를 오브젝트 풀에 추가
        /// </summary>
        /// <param name="obj"></param>
        void AddObject(T obj);

        /// <summary>
        /// 오브젝트 풀에서 인스턴스를 가져오고, 풀이 비어 있으면 새 인스턴스를 생성하여 넘겨 줌
        /// </summary>
        T Get();
    }
}//end of namespace