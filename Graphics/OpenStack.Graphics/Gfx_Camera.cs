using System;
using System.Numerics;

namespace OpenStack.Graphics
{
    /// <summary>
    /// Camera
    /// </summary>
    public abstract class Camera
    {
        protected const float CAMERASPEED = 300f; // Per second
        protected const float FOV = MathX.PiOver4;

        public Vector3 Location = new Vector3(1);
        public float Pitch;
        public float Yaw;
        public float Scale = 1.0f;
        public Matrix4x4 ProjectionMatrix;
        public Matrix4x4 CameraViewMatrix;
        public Matrix4x4 ViewProjectionMatrix;
        public Frustum ViewFrustum = new Frustum();
        public IPickingTexture Picker;
        public Vector2 WindowSize;
        public float AspectRatio;

        public Camera()
            => LookAt(new Vector3(0));

        protected void RecalculateMatrices()
        {
            CameraViewMatrix = Matrix4x4.CreateScale(Scale) * Matrix4x4.CreateLookAt(Location, Location + GetForwardVector(), Vector3.UnitZ);
            ViewProjectionMatrix = CameraViewMatrix * ProjectionMatrix;
            ViewFrustum.Update(ViewProjectionMatrix);
        }

        // Calculate forward vector from pitch and yaw
        protected Vector3 GetForwardVector() => new Vector3((float)(Math.Cos(Yaw) * Math.Cos(Pitch)), (float)(Math.Sin(Yaw) * Math.Cos(Pitch)), (float)Math.Sin(Pitch));

        protected Vector3 GetRightVector() => new Vector3((float)Math.Cos(Yaw - MathX.PiOver2), (float)Math.Sin(Yaw - MathX.PiOver2), 0f);

        public void SetViewportSize(int viewportWidth, int viewportHeight)
        {
            // store window size and aspect ratio
            AspectRatio = viewportWidth / (float)viewportHeight;
            WindowSize = new Vector2(viewportWidth, viewportHeight);
            // calculate projection matrix
            ProjectionMatrix = Matrix4x4.CreatePerspectiveFieldOfView(FOV, AspectRatio, 1.0f, 40000.0f);
            RecalculateMatrices();
            // setup viewport
            SetViewport(0, 0, viewportWidth, viewportHeight);
            Picker?.Resize(viewportWidth, viewportHeight);
        }

        protected abstract void SetViewport(int x, int y, int width, int height);

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

        public abstract void Tick(float deltaTime);

        // Prevent camera from going upside-down
        protected void ClampRotation()
        {
            if (Pitch >= MathX.PiOver2) Pitch = MathX.PiOver2 - 0.001f;
            else if (Pitch <= -MathX.PiOver2) Pitch = -MathX.PiOver2 + 0.001f;
        }
    }
}