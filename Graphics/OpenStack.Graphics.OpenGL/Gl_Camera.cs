using OpenTK.Graphics.OpenGL;
using OpenTK.Input;
using System;
using System.Numerics;

namespace OpenStack.Graphics.OpenGL
{
    /// <summary>
    /// GLCamera
    /// </summary>
    public abstract class GLCamera : Camera
    {
        protected override void SetViewport(int x, int y, int width, int height)
            => GL.Viewport(0, 0, width, height);
    }

    /// <summary>
    /// GLDebugCamera
    /// </summary>
    public class GLDebugCamera : GLCamera
    {
        public bool MouseOverRenderArea; // Set from outside this class by forms code
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

        public void HandleInput(MouseState mouseState, KeyboardState keyboardState)
        {
            ScrollWheelDelta += mouseState.ScrollWheelValue - MouseState.ScrollWheelValue;
            MouseState = mouseState;
            KeyboardState = keyboardState;
            if (!MouseOverRenderArea || mouseState.LeftButton == ButtonState.Released)
            {
                MouseDragging = false;
                MouseDelta = default;
                if (!MouseOverRenderArea) return;
            }

            // drag
            if (mouseState.LeftButton == ButtonState.Pressed)
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
            if (KeyboardState.IsKeyDown(Key.ShiftLeft)) speed *= 2;
            else if (KeyboardState.IsKeyDown(Key.F)) speed *= 10;

            if (KeyboardState.IsKeyDown(Key.W)) Location += GetForwardVector() * speed;
            if (KeyboardState.IsKeyDown(Key.S)) Location -= GetForwardVector() * speed;
            if (KeyboardState.IsKeyDown(Key.D)) Location += GetRightVector() * speed;
            if (KeyboardState.IsKeyDown(Key.A)) Location -= GetRightVector() * speed;
            if (KeyboardState.IsKeyDown(Key.Z)) Location += new Vector3(0, 0, -speed);
            if (KeyboardState.IsKeyDown(Key.Q)) Location += new Vector3(0, 0, speed);

            // scroll
            if (ScrollWheelDelta != 0) { Location += GetForwardVector() * ScrollWheelDelta * speed; ScrollWheelDelta = 0; }
        }
    }
}