using UnityEngine;

namespace CAU
{
    /**
    * @class           MonoSingleton
    * @deescription    MonoBehaviour를 상속받아 GameObject 형태로 접근하기 위한 Singleton 클래스
    **/
    public abstract class MonoSingleton<T> : MonoBehaviour, ISingleton<T> where T : MonoBehaviour
    {
        private static T _instance = null;
        private static object _lock = null;
        protected static bool _isApplicationQuit = false;
        protected static bool _isInitialized = false;

        [SerializeField]
        private bool _isForceInitialize = false;


        public static bool IsAvailable
        {
            get
            {
                return _instance != null;
            }
        }


        public static T Instance
        {
            get
            {
                /** 고스트 객체 생성 방지용 */
                /** Memory Leak을 방지한다. */
                if (_isApplicationQuit == true)
                {
                    /** null 리턴*/
                    return null;
                }

                if (_lock == null)
                {
                    _lock = new object();
                }

                /** Thread-Safe */
                lock (_lock)
                {
                    if (_instance == null)
                    {
                        /** 현재 씬에 싱글톤 오브젝트가 있는 지 찾아본다. */
                        _instance = FindObjectOfType<T>();

                        /** 없으면 */
                        if (_instance == null)
                        {
                            /** 해당 컴포넌트 이름을 가져온다. */
                            string componentName = typeof(T).ToString();

                            /** 해당 컴포넌트 이름으로 게임 오브젝트 찾기 */
                            GameObject findObject = GameObject.Find(componentName);

                            /** 없으면 새로 생성*/
                            if (findObject == null)
                            {
                                findObject = new GameObject(componentName);
                            }

                            /** 생성된 오브젝트에, 컴포넌트 추가 */
                            _instance = findObject.AddComponent<T>();
                        }
                    }

                    /** 씬이 변경되어도 객체가 유지되도록 설정 */
                    DontDestroyOnLoad(_instance);

                    /** 객체 리턴 */
                    return _instance;
                }
            }
        }


        public virtual void Release()
        {
            if (_instance != null)
            {
                Destroy(_instance.gameObject);
                _instance = null;
            }

            _isInitialized = false;
            _lock = null;
        }


        /** 원칙적으로 싱글톤은 응용 프로그램이 종료될때, 소멸되어야 한다.
            유니티에서 응용 프로그램이 종료되면 임의 순서대로 오브젝트가 파괴된다.
            만약 싱글톤 오브젝트가 파괴된 이후, 싱글톤 오브젝트가 호출된다면
            앱의 재생이 정지된 이후에도, 에디터 씬에서 고스트 객체가 생성된다.
            고스트 객체의 생성을 방지하기 위해서 상태를 관리한다. */


        /// <summary>
        /// 객체가 생성될때 호출
        /// </summary>
        protected virtual void Awake()
        {
            _isApplicationQuit = false;

            if (_isForceInitialize == true)
            {
                if (_lock == null)
                {
                    _lock = new object();
                }

                /** Thread-Safe */
                lock (_lock)
                {
                    if (_instance == null)
                    {
                        /** 현재 씬에 싱글톤 오브젝트가 있는 지 찾아본다. */
                        _instance = FindObjectOfType<T>();

                        /** 없으면 */
                        if (_instance == null)
                        {
                            /** 해당 컴포넌트 이름을 가져온다. */
                            string componentName = typeof(T).ToString();

                            /** 해당 컴포넌트 이름으로 게임 오브젝트 찾기 */
                            GameObject findObject = GameObject.Find(componentName);

                            /** 없으면 새로 생성*/
                            if (findObject == null)
                            {
                                findObject = new GameObject(componentName);
                            }

                            /** 생성된 오브젝트에, 컴포넌트 추가 */
                            _instance = findObject.AddComponent<T>();
                        }
                    }

                    /** 씬이 변경되어도 객체가 유지되도록 설정 */
                    DontDestroyOnLoad(_instance);

                }
            }
        }


        /// <summary>
        /// 앱이 종료될때 호출
        /// </summary>
        protected virtual void OnApplicationQuit()
        {
        }


        /// <summary>
        /// 객체가 파괴될때 호출
        /// </summary>
        protected virtual void OnDestroy()
        {
        }
    }
}//end of namespace