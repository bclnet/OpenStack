using System;
using System.Collections.Generic;
using System.Numerics;

namespace OpenStack.Gfx.Particles.Operators;

public class BasicMovement(IDictionary<string, object> keyValues) : IParticleOperator
{
    readonly Vector3 _gravity = keyValues.TryGet<double[]>("m_Gravity", out var vectorValues)
        ? new Vector3((float)vectorValues[0], (float)vectorValues[1], (float)vectorValues[2])
        : Vector3.Zero;
    readonly float _drag = keyValues.GetFloat("m_fDrag");

    public void Update(Span<Particle> particles, float frameTime, ParticleSystemRenderState particleSystemState)
    {
        var acceleration = _gravity * frameTime;
        for (var i = 0; i < particles.Length; ++i)
        {
            // Apply acceleration
            particles[i].Velocity += acceleration;
            // Apply drag
            particles[i].Velocity *= 1 - (_drag * 30f * frameTime);
            particles[i].Position += particles[i].Velocity * frameTime;
        }
    }
}
