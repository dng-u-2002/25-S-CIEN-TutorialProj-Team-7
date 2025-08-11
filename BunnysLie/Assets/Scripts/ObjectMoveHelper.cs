using System;
using System.Collections;
using UnityEngine;

namespace Helpers
{
    public enum ePosition
    {
        World = 0,
        Local
    }

    public class ObjectMoveHandler
    {
        Transform Mover;
        string PositionID;
        string RotationID;

        public ObjectMoveHandler(Transform mover)
        {
            Mover = mover;
        }

        public void Move(Vector3 position, Quaternion rotation, float time, ePosition positionType)
        {
            ObjectMoveHelper.TryStop(PositionID);
            PositionID = ObjectMoveHelper.MoveObject(Mover, position, time, positionType);
            ObjectMoveHelper.TryStop(RotationID);
            RotationID = ObjectMoveHelper.RotatebjectSlerp(Mover, rotation, time, positionType);
        }
        public void MoveSlerp(Vector3 position, Quaternion rotation, float time, ePosition positionType)
        {
            ObjectMoveHelper.TryStop(PositionID);
            PositionID = ObjectMoveHelper.MoveObjectSlerp(Mover, position, time, positionType);
            ObjectMoveHelper.TryStop(RotationID);
            RotationID = ObjectMoveHelper.RotatebjectSlerp(Mover, rotation, time, positionType);
        }
        public void MovePosition(Vector3 position, float time, ePosition positionType)
        {
            ObjectMoveHelper.TryStop(PositionID);
            PositionID = ObjectMoveHelper.MoveObject(Mover, position, time, positionType);
        }
    }
    public struct ExtendedEnumerator
        {
            /// <summary> 실제 실행되는 내용을 가지고 있는 Enumerator </summary>
            public IEnumerator Enumerator;
            /// <summary> 고유 ID </summary>
            public string ID { get; private set; }

            public ExtendedEnumerator(IEnumerator enumerator)
            {
                Enumerator = enumerator;
                ID = Guid.NewGuid().ToString();
            }
        }

    public class ObjectMoveHelper
    {

        static ExtendedEnumeratorRunner _InternalRunner;
        static ExtendedEnumeratorRunner InternalRunner
        {
            get
            {
                if (_InternalRunner == null)
                {
                    GameObject go = new GameObject("ObjectMoveRunner");
                    ExtendedEnumeratorRunner runner = go.AddComponent<ExtendedEnumeratorRunner>();
                    _InternalRunner = runner;
                }
                return _InternalRunner;
            }
        }

        /// <summary> ID에 해당하는 코루틴을 중지시킵니다. </summary>
        public static bool TryStop(string ID)
        {
            return InternalRunner.Stop(ID);
        }
        public static string ScaleObject(Transform mover, Vector3 target, float time)
        {
            ExtendedEnumerator info = new ExtendedEnumerator(_ScaleObject(mover, target, time, null));

            InternalRunner.Run(info);
            return info.ID;
        }
        static IEnumerator _ScaleObject(Transform mover, Vector3 target, float time, Action onEnd)
        {
            float t = 0.0f;
            Vector3 startPos;
            startPos = mover.localScale;
            while (t <= time)
            {
                t += Time.deltaTime;
                mover.localScale = Vector3.Lerp(startPos, target, t / time);
                yield return null;
            }
            mover.localScale = target;
            onEnd?.Invoke();
        }


        /// <summary> mover를 현재 위치에서 target까지 time의 시간동안 Lerp로 이동시킵니다. ID를 반환합니다. </summary>
        /// <param name="position">좌표 기준(World, Local)</param>
        public static string MoveObject(Transform mover, Vector3 target, float time, ePosition position)
        {
            ExtendedEnumerator info = new ExtendedEnumerator(_MoveObject(mover, target, time, position, null));

            InternalRunner.Run(info);
            return info.ID;
        }
        public static string MoveObject(Transform mover, Vector3 target, float time, ePosition position, Action onEnd)
        {
            ExtendedEnumerator info = new ExtendedEnumerator(_MoveObject(mover, target, time, position, onEnd));

            InternalRunner.Run(info);
            return info.ID;
        }
        static IEnumerator _MoveObject(Transform mover, Vector3 target, float time, ePosition position, Action onEnd)
        {
            float t = 0.0f;
            Vector3 startPos;
            if (position == ePosition.World)
            {
                startPos = mover.position;
                while (t <= time)
                {
                    t += Time.deltaTime;
                    mover.position = Vector3.Lerp(startPos, target, t / time);
                    yield return null;
                }
                mover.position = target;
            }
            else// if(position == ePosition.Local)
            {
                startPos = mover.localPosition;
                while (t <= time)
                {
                    t += Time.deltaTime;
                    mover.localPosition = Vector3.Lerp(startPos, target, t / time);
                    yield return null;
                }
                mover.localPosition = target;
            }
            onEnd?.Invoke();
        }

        /// <summary> mover를 현재 위치에서 target까지 time의 시간동안 SLerp로 이동시킵니다. ID를 반환합니다. </summary>
        /// <param name="position">좌표 기준(World, Local)</param>
        public static string MoveObjectSlerp(Transform mover, Vector3 target, float time, ePosition position)
        {
            ExtendedEnumerator info = new ExtendedEnumerator(_MoveObjectSlerp(mover, target, time, position, null));

            InternalRunner.Run(info);
            return info.ID;
        }
        public static string MoveObjectSlerp(Transform mover, Vector3 target, float time, ePosition position, Action onEnd)
        {
            ExtendedEnumerator info = new ExtendedEnumerator(_MoveObjectSlerp(mover, target, time, position, onEnd));

            InternalRunner.Run(info);
            return info.ID;
        }
        static IEnumerator _MoveObjectSlerp(Transform mover, Vector3 target, float time, ePosition position, Action onEnd)
        {
            float t = 0.0f;
            Vector3 startPos;
            if (position == ePosition.World)
            {
                startPos = mover.position;
                while (t <= time)
                {
                    t += Time.deltaTime;
                    mover.position = Vector3.Slerp(startPos, target, t / time);
                    yield return null;
                }
                mover.position = target;
            }
            else// if(position == ePosition.Local)
            {
                startPos = mover.localPosition;
                while (t <= time)
                {
                    t += Time.deltaTime;
                    mover.localPosition = Vector3.Slerp(startPos, target, t / time);
                    yield return null;
                }
                mover.localPosition = target;
            }

            onEnd?.Invoke();
        }

        /// <summary> mover를 현재 위치에서 target까지 time의 시간동안 Lerp로 이동시킵니다. ID를 반환합니다. </summary>
        /// <param name="position">좌표 기준(World, Local)</param>
        public static string RotatebjectLerp(Transform mover, Vector3 target, float time, ePosition position)
        {
            ExtendedEnumerator info = new ExtendedEnumerator(_RotateObjectLerp(mover, target, time, position, null));

            InternalRunner.Run(info);
            return info.ID;
        }
        public static string RotatebjectLerp(Transform mover, Vector3 target, float time, ePosition position, Action onEnd)
        {
            ExtendedEnumerator info = new ExtendedEnumerator(_RotateObjectLerp(mover, target, time, position, onEnd));

            InternalRunner.Run(info);
            return info.ID;
        }
        static IEnumerator _RotateObjectLerp(Transform mover, Vector3 target, float time, ePosition position, Action onEnd)
        {
            float t = 0.0f;
            Quaternion startRot, targetRot;
            targetRot = Quaternion.Euler(target);
            if (position == ePosition.World)
            {
                startRot = Quaternion.Euler(mover.eulerAngles);
                while (t <= time)
                {
                    t += Time.deltaTime;
                    mover.rotation = Quaternion.Lerp(startRot, targetRot, t / time);
                    yield return null;
                }
                mover.eulerAngles = target;
            }
            else// if(position == ePosition.Local)
            {
                startRot = Quaternion.Euler(mover.localEulerAngles);
                while (t <= time)
                {
                    t += Time.deltaTime;
                    mover.localRotation = Quaternion.Lerp(startRot, targetRot, t / time);
                    yield return null;
                }
                mover.localEulerAngles = target;
            }

            onEnd?.Invoke();
        }

        public static string ChangeAlpha(UnityEngine.UI.Image img, float targetAlpha, float time)
        {
            ExtendedEnumerator info = new ExtendedEnumerator(_ChangeAlpha(img, targetAlpha, time, null));
            InternalRunner.Run(info);
            return info.ID;
        }
        static IEnumerator _ChangeAlpha(UnityEngine.UI.Image img, float targetAlpha, float time, Action onEnd)
        {
            float t = 0.0f;
            Color startColor = img.color;
            Color targetColor = new Color(startColor.r, startColor.g, startColor.b, targetAlpha);
            while (t <= time)
            {
                t += Time.deltaTime;
                img.color = Color.Lerp(startColor, targetColor, t / time);
                yield return null;
            }
            img.color = targetColor;
            onEnd?.Invoke();
        }

        /// <summary> mover를 현재 위치에서 target까지 time의 시간동안 SLerp로 이동시킵니다. ID를 반환합니다. </summary>
        /// <param name="position">좌표 기준(World, Local)</param>
        public static string RotatebjectSlerp(Transform mover, Quaternion target, float time, ePosition position)
        {
            ExtendedEnumerator info = new ExtendedEnumerator(_RotateObjectSlerp(mover, target, time, position, null));

            InternalRunner.Run(info);
            return info.ID;
        }
        public static string RotatebjectSlerp(Transform mover, Quaternion target, float time, ePosition position, Action onEnd)
        {
            ExtendedEnumerator info = new ExtendedEnumerator(_RotateObjectSlerp(mover, target, time, position, onEnd));

            InternalRunner.Run(info);
            return info.ID;
        }
        static IEnumerator _RotateObjectSlerp(Transform mover, Quaternion target, float time, ePosition position, Action onEnd)
        {
            float t = 0.0f;
            Quaternion startRot, targetRot;
            targetRot = target;
            if (position == ePosition.World)
            {
                startRot = mover.rotation;
                while (t <= time)
                {
                    t += Time.deltaTime;
                    mover.rotation = Quaternion.Slerp(startRot, targetRot, t / time);
                    yield return null;
                }
                mover.rotation = target;
            }
            else// if(position == ePosition.Local)
            {
                startRot = mover.localRotation;
                while (t <= time)
                {
                    t += Time.deltaTime;
                    mover.localRotation = Quaternion.Slerp(startRot, targetRot, t / time);
                    yield return null;
                }
                mover.localRotation = target;
            }

            onEnd?.Invoke();
        }
    }
}