using System;
using System.Collections.Generic;

namespace OpenStack.Gfx.Particles.Operators;

#pragma warning disable CA1801
public class Decay(IDictionary<string, object> keyValues) : IParticleOperator
#pragma warning restore CA1801
{
    public void Update(Span<Particle> particles, float frameTime, ParticleSystemRenderState particleSystemState)
    {
        for (var i = 0; i < particles.Length; ++i) particles[i].Lifetime -= frameTime;
    }
}
