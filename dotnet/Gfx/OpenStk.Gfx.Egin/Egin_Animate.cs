using System;
using System.Collections.Generic;
using System.Numerics;

namespace OpenStack.Gfx.Egin;

#region Animate

/// <summary>
/// Bone
/// </summary>
public class Bone {
    public int Index;
    public Bone Parent;
    public List<Bone> Children = [];
    public string Name;
    public Vector3 Position;
    public Quaternion Angle;
    public Matrix4x4 BindPose;
    public Matrix4x4 InverseBindPose;

    public Bone(int index, string name, Vector3 position, Quaternion rotation) {
        Index = index;
        Name = name;
        Position = position;
        Angle = rotation;
        // calculate matrices
        BindPose = Matrix4x4.CreateFromQuaternion(rotation) * Matrix4x4.CreateTranslation(position);
        Matrix4x4.Invert(BindPose, out InverseBindPose);
    }

    public void SetParent(Bone parent) {
        if (!Children.Contains(parent)) {
            Parent = parent;
            parent.Children.Add(this);
        }
    }
}

/// <summary>
/// ISkeleton
/// </summary>
public interface ISkeleton {
    Bone[] Roots { get; }
    Bone[] Bones { get; }
}

/// <summary>
/// ChannelAttribute
/// </summary>
public enum ChannelAttribute {
    Position = 0,
    Angle = 1,
    Scale = 2,
    Unknown = 3,
}

/// <summary>
/// Frame
/// </summary>
public struct FrameBone {
    public Vector3 Position;
    public Quaternion Angle;
    public float Scale;
}

/// <summary>
/// Frame
/// </summary>
public class Frame {
    public FrameBone[] Bones;

    public Frame(ISkeleton skeleton) {
        Bones = new FrameBone[skeleton.Bones.Length];
        Clear(skeleton);
    }

    public void SetAttribute(int bone, ChannelAttribute attribute, Vector3 data) {
        switch (attribute) {
            case ChannelAttribute.Position: Bones[bone].Position = data; break;
#if DEBUG
            default: Console.WriteLine($"Unknown frame attribute '{attribute}' encountered with Vector3 data"); break;
#endif
        }
    }

    public void SetAttribute(int bone, ChannelAttribute attribute, Quaternion data) {
        switch (attribute) {
            case ChannelAttribute.Angle: Bones[bone].Angle = data; break;
#if DEBUG
            default: Console.WriteLine($"Unknown frame attribute '{attribute}' encountered with Quaternion data"); break;
#endif
        }
    }

    public void SetAttribute(int bone, ChannelAttribute attribute, float data) {
        switch (attribute) {
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
    public void Clear(ISkeleton skeleton) {
        for (var i = 0; i < Bones.Length; i++) {
            Bones[i].Position = skeleton.Bones[i].Position;
            Bones[i].Angle = skeleton.Bones[i].Angle;
            Bones[i].Scale = 1;
        }
    }
}

/// <summary>
/// IAnimation
/// </summary>
public interface IAnimation {
    string Name { get; }
    float Fps { get; }
    int FrameCount { get; }
    void DecodeFrame(int frameIndex, Frame outFrame);
    Matrix4x4[] GetAnimationMatrices(FrameCache frameCache, object index, ISkeleton skeleton);
}

/// <summary>
/// FrameCache
/// </summary>
public class FrameCache {
    public static Func<ISkeleton, Frame> FrameFactory = skeleton => new Frame(skeleton);

    internal (int frameIndex, Frame frame) PreviousFrame;
    internal (int frameIndex, Frame frame) NextFrame;
    internal Frame InterpolatedFrame;
    internal ISkeleton Skeleton;

    public FrameCache(ISkeleton skeleton) {
        PreviousFrame = (-1, FrameFactory(skeleton));
        NextFrame = (-1, FrameFactory(skeleton));
        InterpolatedFrame = FrameFactory(skeleton);
        Skeleton = skeleton;
        Clear();
    }

    /// <summary>
    /// Clears interpolated frame bones and frame cache. Should be used on animation change.
    /// </summary>
    public void Clear() {
        PreviousFrame = (-1, PreviousFrame.frame); PreviousFrame.frame.Clear(Skeleton);
        NextFrame = (-1, NextFrame.frame); NextFrame.frame.Clear(Skeleton);
    }

    /// <summary>
    /// Get the animation frame at a time/index.
    /// </summary>
    /// <param name="time">The time to get the frame for.</param>
    public Frame GetFrame(IAnimation anim, object index) {
        switch (index) {
            case float time: {
                    // calculate the index of the current frame
                    var frameIndex = (int)(time * anim.Fps) % anim.FrameCount;
                    var t = (time * anim.Fps - frameIndex) % 1;
                    // get current and next frame
                    var frame1 = GetFrame(anim, frameIndex);
                    var frame2 = GetFrame(anim, (frameIndex + 1) % anim.FrameCount);
                    // interpolate bone positions, angles and scale
                    for (var i = 0; i < frame1.Bones.Length; i++) {
                        var frame1Bone = frame1.Bones[i];
                        var frame2Bone = frame2.Bones[i];
                        InterpolatedFrame.Bones[i].Position = Vector3.Lerp(frame1Bone.Position, frame2Bone.Position, t);
                        InterpolatedFrame.Bones[i].Angle = Quaternion.Slerp(frame1Bone.Angle, frame2Bone.Angle, t);
                        InterpolatedFrame.Bones[i].Scale = frame1Bone.Scale + (frame2Bone.Scale - frame1Bone.Scale) * t;
                    }
                    return InterpolatedFrame;
                }
            case int frameIndex: {
                    // try to lookup cached (precomputed) frame - happens when GUI Autoplay runs faster than animation FPS
                    if (frameIndex == PreviousFrame.frameIndex) return PreviousFrame.frame;
                    else if (frameIndex == NextFrame.frameIndex) return NextFrame.frame;
                    // only two frames are cached at a time to minimize memory usage, especially with Autoplay enabled
                    Frame frame;
                    if (frameIndex > PreviousFrame.frameIndex) { frame = PreviousFrame.frame; PreviousFrame = NextFrame; NextFrame = (frameIndex, frame); }
                    else { frame = NextFrame.frame; NextFrame = PreviousFrame; PreviousFrame = (frameIndex, frame); }
                    // we make an assumption that frames within one animation contain identical bone sets, so we don't clear frame here
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
public class AnimationController {
    internal FrameCache FrameCache;
    Action<IAnimation, int> UpdateHandler = (a, b) => { };
    public IAnimation ActiveAnimation;
    float Time;
    bool ShouldUpdate;
    public bool IsPaused;

    public int Frame {
        get => ActiveAnimation != null && ActiveAnimation.FrameCount != 0
            ? (int)Math.Round(Time * ActiveAnimation.Fps) % ActiveAnimation.FrameCount
            : 0;
        set {
            if (ActiveAnimation != null) {
                Time = ActiveAnimation.Fps != 0
                    ? value / ActiveAnimation.Fps
                    : 0f;
                ShouldUpdate = true;
            }
        }
    }

    public AnimationController(ISkeleton skeleton) => FrameCache = new FrameCache(skeleton);

    public bool Update(float timeStep) {
        if (ActiveAnimation == null) return false;
        if (IsPaused) { var res = ShouldUpdate; ShouldUpdate = false; return res; }
        Time += timeStep;
        UpdateHandler(ActiveAnimation, Frame);
        ShouldUpdate = false;
        return true;
    }

    public void SetAnimation(IAnimation animation) {
        FrameCache.Clear();
        ActiveAnimation = animation;
        Time = 0f;
        UpdateHandler(ActiveAnimation, -1);
    }

    public void PauseLastFrame() {
        IsPaused = true;
        Frame = ActiveAnimation == null ? 0 : ActiveAnimation.FrameCount - 1;
    }

    public Matrix4x4[] GetAnimationMatrices(ISkeleton skeleton)
        => IsPaused
        ? ActiveAnimation.GetAnimationMatrices(FrameCache, Frame, skeleton)
        : ActiveAnimation.GetAnimationMatrices(FrameCache, Time, skeleton);

    public void RegisterUpdateHandler(Action<IAnimation, int> handler) => UpdateHandler = handler;
}

#endregion