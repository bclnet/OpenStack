using System.Numerics;

namespace OpenStack.Graphics.ParticleSystem
{
    public interface IParticleRenderer
    {
        void Render(ParticleBag particles, Matrix4x4 viewProjectionMatrix, Matrix4x4 modelViewMatrix);
    }
}
