using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Numerics;

namespace OpenStack.Gfx.Animate;

/// <summary>
/// TestBone
/// </summary>
[TestClass]
public class TestBone : Bone
{
    public TestBone() : base(1, "Name", Vector3.One, Quaternion.Zero) { }

    [TestMethod]
    public void Test_Init()
    {
        Assert.AreEqual(1, Index);
        Assert.AreEqual("Name", Name);
        Assert.AreEqual("<1, 1, 1>", Position.ToString());
        Assert.AreEqual(0, Angle.X);
        Assert.AreEqual("{ {M11:1 M12:0 M13:0 M14:0} {M21:0 M22:1 M23:0 M24:0} {M31:0 M32:0 M33:1 M34:0} {M41:1 M42:1 M43:1 M44:1} }", BindPose.ToString());
        Assert.AreEqual("{ {M11:1 M12:0 M13:0 M14:0} {M21:0 M22:1 M23:0 M24:0} {M31:0 M32:0 M33:1 M34:0} {M41:-1 M42:-1 M43:-1 M44:1} }", InverseBindPose.ToString());
    }
    [TestMethod]
    public void Test_SetParent()
    {
        var parent = new Bone(0, "Parent", Vector3.One, Quaternion.Zero);
        // test
        SetParent(parent);
        Assert.AreEqual(1, parent.Children.Count);
    }
}

/// <summary>
/// TestSkeleton
/// </summary>
class TestSkeleton : ISkeleton
{
    public static TestSkeleton Skeleton = new();
    public Bone[] Bones => [new Bone(0, "Bone", Vector3.One, Quaternion.Zero)];
    public Bone[] Roots => [Bones[0]];
}

/// <summary>
/// TestFrame
/// </summary>
[TestClass]
public class TestFrame : Frame
{
    public TestFrame() : base(TestSkeleton.Skeleton) { }

    [TestMethod]
    public void Test_Init()
    {
        Assert.AreEqual(1, Bones.Length);
        Assert.AreEqual(1f, Bones[0].Position.X);
        Assert.AreEqual(0f, Bones[0].Angle.X);
        Assert.AreEqual(1f, Bones[0].Scale);
    }
    [TestMethod]
    public void Test_SetAttribute()
    {
        SetAttribute(0, ChannelAttribute.Position, Vector3.One);
        SetAttribute(0, ChannelAttribute.Angle, Quaternion.Zero);
        SetAttribute(0, ChannelAttribute.Scale, 0f);
        Assert.AreEqual(1f, Bones[0].Position.X);
        Assert.AreEqual(0f, Bones[0].Angle.X);
        Assert.AreEqual(0f, Bones[0].Scale);
    }
    [TestMethod]
    public void Test_Clear()
    {
        Clear(TestSkeleton.Skeleton);
        Assert.AreEqual(1f, Bones[0].Position.X);
        Assert.AreEqual(0f, Bones[0].Angle.X);
        Assert.AreEqual(1f, Bones[0].Scale);
    }
}

/// <summary>
/// TestAnimation
/// </summary>
class TestAnimation : IAnimation
{
    public static TestAnimation Animation = new();
    public string Name => "Animation";
    public float Fps => 15f;
    public int FrameCount => 1;
    public void DecodeFrame(int frameIndex, Frame outFrame) { }
    public Matrix4x4[] GetAnimationMatrices(FrameCache frameCache, object index, ISkeleton skeleton) => null;
}

/// <summary>
/// TestFrameCache
/// </summary>
[TestClass]
public class TestFrameCache : FrameCache
{
    public TestFrameCache() : base(TestSkeleton.Skeleton) { }

    [TestMethod]
    public void Test_Init()
    {
        Assert.AreEqual((-1, 1), (PreviousFrame.frameIndex, PreviousFrame.frame.Bones.Length));
        Assert.AreEqual((-1, 1), (NextFrame.frameIndex, NextFrame.frame.Bones.Length));
        Assert.AreEqual(1, InterpolatedFrame.Bones.Length);
        Assert.AreEqual(TestSkeleton.Skeleton, Skeleton);
    }
    [TestMethod]
    public void Test_Clear()
    {
        Clear();
        Assert.AreEqual((-1, 1), (PreviousFrame.frameIndex, PreviousFrame.frame.Bones.Length));
        Assert.AreEqual((-1, 1), (NextFrame.frameIndex, NextFrame.frame.Bones.Length));
    }
    [TestMethod]
    public void Test_GetFrame()
    {
        var actual1 = GetFrame(TestAnimation.Animation, 1f);
        var actual2 = GetFrame(TestAnimation.Animation, 1);
        Assert.IsTrue(actual1 != null);
        Assert.IsTrue(actual2 != null);
    }
}

/// <summary>
/// TestAnimationController
/// </summary>
[TestClass]
public class TestAnimationController : AnimationController
{
    public TestAnimationController() : base(TestSkeleton.Skeleton) { }

    [TestMethod]
    public void Test_Init()
    {
        Assert.IsTrue(FrameCache != null);
    }
    [TestMethod]
    public void Test_Frame()
    {
    }
    [TestMethod]
    public void Test_Update()
    {
    }
    [TestMethod]
    public void Test_SetAnimation()
    {
    }
    [TestMethod]
    public void Test_PauseLastFrame()
    {
    }
    [TestMethod]
    public void Test_GetAnimationMatrices()
    {
    }
    [TestMethod]
    public void Test_RegisterUpdateHandler()
    {
    }
}
