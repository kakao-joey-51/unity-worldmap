using System;

namespace CAU
{
    public abstract class BaseDisposableClass : IDisposable
    {
        /** Protected 멤버 변수 모음 */
        #region PROTECTED MEMBER

        protected bool _isAlreadyDisposed = false;

        #endregion

        ~BaseDisposableClass()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool isDisposing)
        {
            if (_isAlreadyDisposed == true)
            {
                return;
            }

            if (isDisposing == true)
            {
                // 관리 리소스 정리
            }

            // 비관리 리소스 정리

            _isAlreadyDisposed = true;
        }

    }

    /**
    * @class            NormalSingleton
    * @description      GameObject를 생성하지 않고 코드 내부에서만 접근하기 위한 Singleton 클래스
    **/
    public abstract class NormalSingleton<T> : BaseDisposableClass, ISingleton<T> where T : class
    {
        #region SINGLETON

        protected static T _instance = null;
        public static T Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Activator.CreateInstance(typeof(T)) as T;
                }

                return _instance;
            }
        }

        public static bool IsAvailable
        {
            get
            {
                return _instance != null;
            }
        }

        #endregion SINGLETON


        public virtual bool Initialize()
        {
            return true;
        }


        public virtual void Release()
        {
            _instance = null;

            Dispose();
        }
    }
}//end of namespace