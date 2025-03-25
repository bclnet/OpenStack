using System;
using System.Collections.Generic;

namespace OpenStack.Gfx.Particles.Operators;

#pragma warning disable CS9113
public class SpinUpdate(IDictionary<string, object> keyValues) : IParticleOperator
#pragma warning restore CS9113
{
    public void Update(Span<Particle> particles, float frameTime, ParticleSystemRenderState particleSystemState)
    {
        for (var i = 0; i < particles.Length; ++i) particles[i].Rotation += particles[i].RotationSpeed * frameTime;
    }
}
