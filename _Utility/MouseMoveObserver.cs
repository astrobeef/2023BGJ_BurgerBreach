using System;
using System.Collections.Generic;
using System.Linq;
using AxialCS;
using Godot;

namespace Utility
{
    public class MouseMoveObserver
    {
        public main main;

        public delegate void MouseMovementEvent();
        public event MouseMovementEvent OnMouseMovement;

        public Vector2 _pos_last = Vector2.Zero;
        public Vector2 _pos_cur = Vector2.Zero;
        private int _movement_check = 0;
        private static int _MOVEMENT_CHECK_MOD = 10;
        private static float _MOVE_CHECK_THRESH = 1f;

        private bool _isDisplaced => IsDisplaced(_pos_cur, _pos_last, _MOVE_CHECK_THRESH);

        public MouseMoveObserver(main main){
            this.main = main;
            main.OnProcess += DetectMovement;
        }

        /// <summary>
        /// Execute in a <see cref="_Process"/> function
        /// </summary>
        private void DetectMovement()
        {
            // Only execute if there's at least one subscriber
            if (OnMouseMovement != null)
            {
                _movement_check = (_movement_check > 100)
                ? 0
                : _movement_check++;

                if (_isDisplaced)
                {
                    UpdatePositions();

                    OnMovement(_isDisplaced);
                }
                else
                {
                    if (_movement_check % _MOVEMENT_CHECK_MOD == 0)
                    {
                        UpdatePositions();
                        OnMovement(_isDisplaced);
                    }
                }
            }
        }

        private void UpdatePositions()
        {
            _pos_last = _pos_cur;
            _pos_cur =  main.GetViewport().GetMousePosition();
        }

        private static bool IsDisplaced(Vector2 current, Vector2 last, float threshold)
        {
            return (current - last).LengthSquared() > threshold;
        }

        private void OnMovement(bool mouseMovement)
        {
            if (mouseMovement)
            {
                OnMouseMovement?.Invoke();
            }
        }
    }
}