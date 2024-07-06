using System;
using System.Collections.Generic;
using System.Numerics;

namespace OpenStack.Graphics
{
    /// <summary>
    /// Bone
    /// </summary>
    public class Bone
    {
        public int Index;
        public Bone Parent;
        public List<Bone> Children = new List<Bone>();
        public string Name;
        public Vector3 Position;
        public Quaternion Angle;
        public Matrix4x4 BindPose;
        public Matrix4x4 InverseBindPose;

        public Bone(int index, string name, Vector3 position, Quaternion rotation)
        {
            Index = index;
            Name = name;
            Position = position;
            Angle = rotation;
            // Calculate matrices
            BindPose = Matrix4x4.CreateFromQuaternion(rotation) * Matrix4x4.CreateTranslation(position);
            Matrix4x4.Invert(BindPose, out var inverseBindPose);
            InverseBindPose = inverseBindPose;
        }

        public void SetParent(Bone parent)
        {
            if (!Children.Contains(parent))
            {
                Parent = parent;
                parent.Children.Add(this);
            }
        }
    }

    /// <summary>
    /// ISkeleton
    /// </summary>
    public interface ISkeleton
    {
        Bone[] Roots { get; }
        Bone[] Bones { get; }
    }

    /// <summary>
    /// ChannelAttribute
    /// </summary>
    public enum ChannelAttribute
    {
        Position = 0,
        Angle = 1,
        Scale = 2,
        Unknown = 3,
    }

    /// <summary>
    /// Frame
    /// </summary>
    public struct FrameBone
    {
        public Vector3 Position;
        public Quaternion Angle;
        public float Scale;
    }

    /// <summary>
    /// Frame
    /// </summary>
    public class Frame
    {
        public FrameBone[] Bones;

        public Frame(ISkeleton skeleton)
        {
            Bones = new FrameBone[skeleton.Bones.Length];
            Clear(skeleton);
        }

        public void SetAttribute(int bone, ChannelAttribute attribute, Vector3 data)
        {
            switch (attribute)
            {
                case ChannelAttribute.Position: Bones[bone].Position = data; break;
#if DEBUG
                default: Console.WriteLine($"Unknown frame attribute '{attribute}' encountered with Vector3 data"); break;
#endif
            }
        }

        public void SetAttribute(int bone, ChannelAttribute attribute, Quaternion data)
        {
            switch (attribute)
            {
                case ChannelAttribute.Angle: Bones[bone].Angle = data; break;
#if DEBUG
                default: Console.WriteLine($"Unknown frame attribute '{attribute}' encountered with Quaternion data"); break;
#endif
            }
        }

        public void SetAttribute(int bone, ChannelAttribute attribute, float data)
        {
            switch (attribute)
            {
                case ChannelAttribute.Scale: Bones[bone].Scale = data; break;
#if DEBUG
                default: Console.WriteLine($"Unknown frame attribute '{attribute}' encountered with float data"); break;
#endif
            }
        }

        /// <summary>
        /// Resets frame bones to their bind pose. Should be used on animation change.
        /// </summary>
        /// <param name="skeleton">The same skeleton that was passed to the constructor.</param>
        public void Clear(ISkeleton skeleton)
        {
            for (var i = 0; i < Bones.Length; i++)
            {
                Bones[i].Position = skeleton.Bones[i].Position;
                Bones[i].Angle = skeleton.Bones[i].Angle;
                Bones[i].Scale = 1;
            }
        }
    }

    /// <summary>
    /// IAnimation
    /// </summary>
    public interface IAnimation
    {
        string Name { get; }
        float Fps { get; }
        int FrameCount { get; }
        void DecodeFrame(int frameIndex, Frame outFrame);
        Matrix4x4[] GetAnimationMatrices(FrameCache frameCache, object index, ISkeleton skeleton);
    }

    /// <summary>
    /// FrameCache
    /// </summary>
    public class FrameCache
    {
        public static Func<ISkeleton, Frame> FrameFactory = skeleton => new Frame(skeleton);

        (int FrameIndex, Frame Frame) PreviousFrame;
        (int FrameIndex, Frame Frame) NextFrame;
        Frame InterpolatedFrame;
        ISkeleton Skeleton;

        public FrameCache(ISkeleton skeleton)
        {
            PreviousFrame = (-1, FrameFactory(skeleton));
            NextFrame = (-1, FrameFactory(skeleton));
            InterpolatedFrame = FrameFactory(skeleton);
            Skeleton = skeleton;
            Clear();
        }

        /// <summary>
        /// Clears interpolated frame bones and frame cache. Should be used on animation change.
        /// </summary>
        public void Clear()
        {
            PreviousFrame = (-1, PreviousFrame.Frame); PreviousFrame.Frame.Clear(Skeleton);
            NextFrame = (-1, NextFrame.Frame); NextFrame.Frame.Clear(Skeleton);
        }

        /// <summary>
        /// Get the animation frame at a time/index.
        /// </summary>
        /// <param name="time">The time to get the frame for.</param>
        public Frame GetFrame(IAnimation anim, object index)
        {
            switch (index)
            {
                case float time:
                    {
                        // Calculate the index of the current frame
                        var frameIndex = (int)(time * anim.Fps) % anim.FrameCount;
                        var t = (time * anim.Fps - frameIndex) % 1;

                        // Get current and next frame
                        var frame1 = GetFrame(anim, frameIndex);
                        var frame2 = GetFrame(anim, (frameIndex + 1) % anim.FrameCount);

                        // Interpolate bone positions, angles and scale
                        for (var i = 0; i < frame1.Bones.Length; i++)
                        {
                            var frame1Bone = frame1.Bones[i];
                            var frame2Bone = frame2.Bones[i];
                            InterpolatedFrame.Bones[i].Position = Vector3.Lerp(frame1Bone.Position, frame2Bone.Position, t);
                            InterpolatedFrame.Bones[i].Angle = Quaternion.Slerp(frame1Bone.Angle, frame2Bone.Angle, t);
                            InterpolatedFrame.Bones[i].Scale = frame1Bone.Scale + (frame2Bone.Scale - frame1Bone.Scale) * t;
                        }
                        return InterpolatedFrame;
                    }
                case int frameIndex:
                    {
                        // Try to lookup cached (precomputed) frame - happens when GUI Autoplay runs faster than animation FPS
                        if (frameIndex == PreviousFrame.FrameIndex) return PreviousFrame.Frame;
                        else if (frameIndex == NextFrame.FrameIndex) return NextFrame.Frame;

                        // Only two frames are cached at a time to minimize memory usage, especially with Autoplay enabled
                        Frame frame;
                        if (frameIndex > PreviousFrame.FrameIndex)
                        {
                            frame = PreviousFrame.Frame;
                            PreviousFrame = NextFrame;
                            NextFrame = (frameIndex, frame);
                        }
                        else
                        {
                            frame = NextFrame.Frame;
                            NextFrame = PreviousFrame;
                            PreviousFrame = (frameIndex, frame);
                        }
                        // We make an assumption that frames within one animation contain identical bone sets, so we don't clear frame here
                        anim.DecodeFrame(frameIndex, frame);
                        return frame;
                    }
                default: throw new ArgumentOutOfRangeException(nameof(index));
            }
        }
    }

    /// <summary>
    /// AnimationController
    /// </summary>
    public class AnimationController
    {
        FrameCache frameCache;
        Action<IAnimation, int> updateHandler = (a, b) => { };
        IAnimation activeAnimation;
        float Time;
        bool shouldUpdate;
        public IAnimation ActiveAnimation => activeAnimation;
        public bool IsPaused;
        public int Frame
        {
            get => activeAnimation != null && activeAnimation.FrameCount != 0
                ? (int)Math.Round(Time * activeAnimation.Fps) % activeAnimation.FrameCount
                : 0;
            set
            {
                if (activeAnimation != null)
                {
                    Time = activeAnimation.Fps != 0
                        ? value / activeAnimation.Fps
                        : 0f;
                    shouldUpdate = true;
                }
            }
        }

        public AnimationController(ISkeleton skeleton) => frameCache = new FrameCache(skeleton);

        public bool Update(float timeStep)
        {
            if (activeAnimation == null) return false;
            if (IsPaused) { var res = shouldUpdate; shouldUpdate = false; return res; }
            Time += timeStep;
            updateHandler(activeAnimation, Frame);
            shouldUpdate = false;
            return true;
        }

        public void SetAnimation(IAnimation animation)
        {
            frameCache.Clear();
            activeAnimation = animation;
            Time = 0f;
            updateHandler(activeAnimation, -1);
        }

        public void PauseLastFrame()
        {
            IsPaused = true;
            Frame = activeAnimation == null ? 0 : activeAnimation.FrameCount - 1;
        }

        public Matrix4x4[] GetAnimationMatrices(ISkeleton skeleton)
            => IsPaused
            ? activeAnimation.GetAnimationMatrices(frameCache, Frame, skeleton)
            : activeAnimation.GetAnimationMatrices(frameCache, Time, skeleton);

        public void RegisterUpdateHandler(Action<IAnimation, int> handler) => updateHandler = handler;
    }
}