using System;
using System.Numerics;

namespace OpenStack.Gfx
{
    /// <summary>
    /// Camera
    /// </summary>
    public abstract class Camera
    {
        protected const float CAMERASPEED = 300f; // Per second
        protected const float FOV = MathX.PiOver4;

        public Vector3 Location = new(1);
        public float Pitch;
        public float Yaw;
        public float Scale = 1.0f;
        public Matrix4x4 ProjectionMatrix;
        public Matrix4x4 CameraViewMatrix;
        public Matrix4x4 ViewProjectionMatrix;
        public Frustum ViewFrustum = new();
        public IPickingTexture Picker;
        public Vector2<int> WindowSize;
        public float AspectRatio;

        public Camera() => LookAt(new Vector3(0));

        protected void RecalculateMatrices()
        {
            CameraViewMatrix = Matrix4x4.CreateScale(Scale) * Matrix4x4.CreateLookAt(Location, Location + GetForwardVector(), Vector3.UnitZ);
            ViewProjectionMatrix = CameraViewMatrix * ProjectionMatrix;
            ViewFrustum.Update(ViewProjectionMatrix);
        }

        // Calculate forward vector from pitch and yaw
        protected Vector3 GetForwardVector() => new((float)(Math.Cos(Yaw) * Math.Cos(Pitch)), (float)(Math.Sin(Yaw) * Math.Cos(Pitch)), (float)Math.Sin(Pitch));

        protected Vector3 GetRightVector() => new((float)Math.Cos(Yaw - MathX.PiOver2), (float)Math.Sin(Yaw - MathX.PiOver2), 0f);

        public void SetViewport(int x, int y, int width, int height)
        {
            // store window size and aspect ratio
            AspectRatio = width / (float)height;
            WindowSize = new Vector2<int>(width, height);
            // calculate projection matrix
            ProjectionMatrix = Matrix4x4.CreatePerspectiveFieldOfView(FOV, AspectRatio, 1.0f, 40000.0f);
            RecalculateMatrices();
            // setup viewport
            GfxViewport(x, y, width, height);
            Picker?.Resize(width, height);
        }

        public abstract void GfxViewport(int x, int y, int width = 0, int height = 0);

        public void CopyFrom(Camera fromOther)
        {
            AspectRatio = fromOther.AspectRatio;
            WindowSize = fromOther.WindowSize;
            Location = fromOther.Location;
            Pitch = fromOther.Pitch;
            Yaw = fromOther.Yaw;
            ProjectionMatrix = fromOther.ProjectionMatrix;
            CameraViewMatrix = fromOther.CameraViewMatrix;
            ViewProjectionMatrix = fromOther.ViewProjectionMatrix;
            ViewFrustum.Update(ViewProjectionMatrix);
        }

        public void SetLocation(Vector3 location)
        {
            Location = location;
            RecalculateMatrices();
        }

        public void SetLocationPitchYaw(Vector3 location, float pitch, float yaw)
        {
            Location = location;
            Pitch = pitch;
            Yaw = yaw;
            RecalculateMatrices();
        }

        public void LookAt(Vector3 target)
        {
            var dir = Vector3.Normalize(target - Location);
            Yaw = (float)Math.Atan2(dir.Y, dir.X);
            Pitch = (float)Math.Asin(dir.Z);
            ClampRotation();
            RecalculateMatrices();
        }

        public void SetFromTransformMatrix(Matrix4x4 matrix)
        {
            Location = matrix.Translation;
            // extract view direction from view matrix and use it to calculate pitch and yaw
            var dir = new Vector3(matrix.M11, matrix.M12, matrix.M13);
            Yaw = (float)Math.Atan2(dir.Y, dir.X);
            Pitch = (float)Math.Asin(dir.Z);
            RecalculateMatrices();
        }

        public void SetScale(float scale)
        {
            Scale = scale;
            RecalculateMatrices();
        }

        public virtual void Tick(int deltaTime) { }

        // Prevent camera from going upside-down
        protected void ClampRotation()
        {
            if (Pitch >= MathX.PiOver2) Pitch = MathX.PiOver2 - 0.001f;
            else if (Pitch <= -MathX.PiOver2) Pitch = -MathX.PiOver2 + 0.001f;
        }
    }
}