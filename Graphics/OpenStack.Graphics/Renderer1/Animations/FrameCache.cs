using System;
using System.Numerics;

namespace OpenStack.Graphics.Renderer1.Animations
{
    public class FrameCache
    {
        (int FrameIndex, Frame Frame) PreviousFrame { get; set; }
        (int FrameIndex, Frame Frame) NextFrame { get; set; }
        readonly Frame InterpolatedFrame;
        readonly ISkeleton Skeleton;

        public static Func<ISkeleton, Frame> FrameFactory = skeleton => new Frame(skeleton);

        public FrameCache(ISkeleton skeleton)
        {
            PreviousFrame = (-1, FrameFactory(skeleton));
            NextFrame = (-1, FrameFactory(skeleton));
            InterpolatedFrame = FrameFactory(skeleton);
            Skeleton = skeleton;
            Clear();
        }

        /// <summary>
        /// Get the animation frame at a time.
        /// </summary>
        /// <param name="time">The time to get the frame for.</param>
        public Frame GetFrame(IAnimation anim, float time)
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

        /// <summary>
        /// Clears interpolated frame bones and frame cache.
        /// Should be used on animation change.
        /// </summary>
        public void Clear()
        {
            PreviousFrame = (-1, PreviousFrame.Frame);
            PreviousFrame.Frame.Clear(Skeleton);

            NextFrame = (-1, NextFrame.Frame);
            NextFrame.Frame.Clear(Skeleton);
        }

        /// <summary>
        /// Get the animation frame at a given index.
        /// </summary>
        public Frame GetFrame(IAnimation anim, int frameIndex)
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
    }
}
