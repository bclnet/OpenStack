using OpenTK.Graphics.OpenGL;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System;
using System.Numerics;

namespace OpenStack.Graphics.OpenGL
{
    /// <summary>
    /// GLCamera
    /// </summary>
    public abstract class GLCamera : Camera
    {
        public bool MouseOverRenderArea;

        public enum EventType
        {
            MouseEnter,
            MouseLeave,
            MouseMove,
            MouseDown,
            MouseUp,
            MouseWheel,
            KeyPress,
            KeyRelease
        };

        public void Event(EventType type, object e, object arg)
        {
            switch (type)
            {
                case EventType.MouseEnter: MouseOverRenderArea = true; break;
                case EventType.MouseLeave: MouseOverRenderArea = false; break;
            }
        }

        public abstract void HandleInput(MouseState mouseState, KeyboardState keyboardState);

        protected override void SetViewport(int x, int y, int width, int height) => GL.Viewport(x, y, width, height);
    }

    /// <summary>
    /// GLDebugCamera
    /// </summary>
    public class GLDebugCamera : GLCamera
    {
        bool MouseDragging;
        Vector2 MouseDelta;
        Vector2 MousePreviousPosition;
        KeyboardState KeyboardState;
        MouseState MouseState;
        int ScrollWheelDelta;

        public override void Tick(float deltaTime)
        {
            if (!MouseOverRenderArea) return;

            // use the keyboard state to update position
            HandleInputTick(deltaTime);

            // full width of the screen is a 1 PI (180deg)
            Yaw -= (float)Math.PI * MouseDelta.X / WindowSize.X;
            Pitch -= (float)Math.PI / AspectRatio * MouseDelta.Y / WindowSize.Y;
            ClampRotation();
            RecalculateMatrices();
        }

        public override void HandleInput(MouseState mouseState, KeyboardState keyboardState)
        {
            //ScrollWheelDelta += mouseState.ScrollWheelValue - MouseState.ScrollWheelValue;
            MouseState = mouseState;
            KeyboardState = keyboardState;
            if (!MouseOverRenderArea || mouseState.IsButtonReleased(MouseButton.Left))
            {
                MouseDragging = false;
                MouseDelta = default;
                if (!MouseOverRenderArea) return;
            }

            // drag
            if (mouseState.IsButtonDown(MouseButton.Left))
            {
                if (!MouseDragging) { MouseDragging = true; MousePreviousPosition = new Vector2(mouseState.X, mouseState.Y); }
                var mouseNewCoords = new Vector2(mouseState.X, mouseState.Y);
                MouseDelta.X = mouseNewCoords.X - MousePreviousPosition.X;
                MouseDelta.Y = mouseNewCoords.Y - MousePreviousPosition.Y;
                MousePreviousPosition = mouseNewCoords;
            }
        }

        internal void HandleInputTick(float deltaTime)
        {
            var speed = CAMERASPEED * deltaTime;

            // double speed if shift is pressed
            if (KeyboardState.IsKeyDown(Keys.LeftShift)) speed *= 2;
            else if (KeyboardState.IsKeyDown(Keys.F)) speed *= 10;

            if (KeyboardState.IsKeyDown(Keys.W)) Location += GetForwardVector() * speed;
            if (KeyboardState.IsKeyDown(Keys.S)) Location -= GetForwardVector() * speed;
            if (KeyboardState.IsKeyDown(Keys.D)) Location += GetRightVector() * speed;
            if (KeyboardState.IsKeyDown(Keys.A)) Location -= GetRightVector() * speed;
            if (KeyboardState.IsKeyDown(Keys.Z)) Location += new Vector3(0, 0, -speed);
            if (KeyboardState.IsKeyDown(Keys.Q)) Location += new Vector3(0, 0, speed);

            // scroll
            if (ScrollWheelDelta != 0) { Location += GetForwardVector() * ScrollWheelDelta * speed; ScrollWheelDelta = 0; }
        }
    }
}