namespace OpenStack.Gfx.Particles.Initializers;

public interface IParticleInitializer
{
    Particle Initialize(ref Particle particle, ParticleSystemRenderState particleSystemState);
}
