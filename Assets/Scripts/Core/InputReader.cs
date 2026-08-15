using UnityEngine;
using UnityEngine.EventSystems;

namespace SubwaySurfers.Core
{
    /// <summary>
    /// Keyboard + swipe input with UI-passthrough filtering. One Update loop,
    /// everyone else subscribes — no more Input calls scattered across files.
    /// </summary>
    public sealed class InputReader : MonoBehaviour
    {
        public event System.Action<int> OnLane;   // -1 / +1
        public event System.Action OnJump;
        public event System.Action OnRoll;
        public event System.Action OnAnyStart;    // any tap/key in menus
        public event System.Action OnPause;

        private Vector2 swipeStart;
        private float swipeTime;
        private bool tracking;
        private const float MinSwipe = 60f;
        private const float MaxSwipeTime = 0.9f;

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape)) { OnPause?.Invoke(); return; }

            bool playing = Game.I != null && Game.I.Playing;

            if (!playing)
            {
                if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) ||
                    Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
                    OnAnyStart?.Invoke();
                HandleTouchStart();
                return;
            }

            if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow)) OnLane?.Invoke(-1);
            if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow)) OnLane?.Invoke(1);
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow)) OnJump?.Invoke();
            if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow)) OnRoll?.Invoke();

            HandleSwipe();
        }

        private void HandleTouchStart()
        {
            if (Input.touchCount == 0) return;
            var t = Input.GetTouch(0);
            if (t.phase == TouchPhase.Began && !OverUI(t.fingerId))
                OnAnyStart?.Invoke();
        }

        private void HandleSwipe()
        {
            if (Input.touchCount == 0) { tracking = false; return; }
            var t = Input.GetTouch(0);

            if (t.phase == TouchPhase.Began)
            {
                if (OverUI(t.fingerId)) { tracking = false; return; }
                tracking = true;
                swipeStart = t.position;
                swipeTime = Time.unscaledTime;
                return;
            }

            if (!tracking || t.phase != TouchPhase.Ended) return;
            tracking = false;

            if (Time.unscaledTime - swipeTime > MaxSwipeTime) return;
            Vector2 d = t.position - swipeStart;
            if (d.magnitude < MinSwipe) return;

            if (Mathf.Abs(d.x) > Mathf.Abs(d.y))
                OnLane?.Invoke(d.x < 0 ? -1 : 1);
            else if (d.y > 0)
                OnJump?.Invoke();
            else
                OnRoll?.Invoke();
        }

        private static bool OverUI(int fingerId)
        {
            return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(fingerId);
        }
    }
}
