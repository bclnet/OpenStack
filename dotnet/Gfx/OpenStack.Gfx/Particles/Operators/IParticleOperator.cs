using System;

namespace OpenStack.Gfx.Particles.Operators
{
    public interface IParticleOperator
    {
        void Update(Span<Particle> particles, float frameTime, ParticleSystemRenderState particleSystemState);
    }
}
