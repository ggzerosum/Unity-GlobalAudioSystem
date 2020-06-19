using System;
using System.Collections;
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace ProvisGames.Core.Utility
{
    /// <summary>
    /// Error Handlable Coroutine
    /// </summary>
    public class ErrorHandlableCoroutine
    {
        public ErrorHandlableCoroutine(MonoBehaviour owner, IEnumerator method, Action<Exception> onErrorOccured)
        {
            this.owner = owner;
            this.method = method;
            this.onErrorOccured = onErrorOccured;
        }

        private Action<Exception> onErrorOccured = null;
        private IEnumerator method { get; set; } = null;
        public MonoBehaviour owner { get; private set; } = null;

        private Coroutine exceptionHandableCoroutine = null;
        public bool isCoroutineDone { get; private set; } = false;

        /// <summary>
        /// 코루틴을 실행시킵니다. 이미 코루틴이 실행중이라면 함수 실행이 취소됩니다.
        /// </summary>
        public void start()
        {
            if (this.exceptionHandableCoroutine == null && isCoroutineDone == false)
                this.exceptionHandableCoroutine = owner.StartCoroutine(Run());
        }

        /// <summary>
        /// 현재 코루틴이 종료되어야함을 나타냅니다.
        /// </summary>
        /// <returns></returns>
        public bool ShouldBeStopped()
        {
            return this.owner == null || this.isCoroutineDone;
        }

        /// <summary>
        /// 현재 진행중인 코루틴을 강제로 종료합니다.
        /// </summary>
        /// <param name="handler"></param>
        public void Stop()
        {
            if (this.exceptionHandableCoroutine != null)
            {
                if (this.owner != null)
                {
                    this.owner.StopCoroutine(this.exceptionHandableCoroutine);
                }
                else
                {
                    Debug.Log("코루틴을 종료하려하였으나, Owner가 존재하지 않습니다. GC에 의해 수거되도록 내버려둡니다.");
                }
            }

            isCoroutineDone = true;
            this.exceptionHandableCoroutine = null;
            Debug.Log("코루틴 강제 종료 확인");
        }

        private IEnumerator Run()
        {
            while (true)
            {
                try
                {
                    if (isCoroutineDone == true || this.owner == null || !this.method.MoveNext())
                    {
                        break; // couroutine stop
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError(e.Message);
                    onErrorOccured?.Invoke(e);
                    break;
                }

                yield return this.method.Current;
            }

            isCoroutineDone = true;
            //Debug.Log("코루틴 자연 종료 확인");
        }
    }

    public static class Extension
    {
        /// <summary>
        /// Start ErrorHandlableCoroutine. If Error Occured in coroutine, coroutine stop own procees
        /// </summary>
        /// <param name="this"></param>
        /// <param name="method"></param>
        /// <returns></returns>
        public static ErrorHandlableCoroutine StartErrorHandlableCoroutine(this MonoBehaviour @this, IEnumerator method, Action<Exception> onErrorOccured = null)
        {
            var coroutine = new ErrorHandlableCoroutine(@this, method, onErrorOccured);
            coroutine.start();
            return coroutine;
        }
    }
}
