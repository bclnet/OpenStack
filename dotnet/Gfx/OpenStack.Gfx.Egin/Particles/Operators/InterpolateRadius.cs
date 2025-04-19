using System;
using System.Collections.Generic;

namespace OpenStack.Gfx.Particles.Operators;

public class InterpolateRadius(IDictionary<string, object> keyValues) : IParticleOperator
{
    readonly float _startTime = keyValues.GetFloat("m_flStartTime");
    readonly float _endTime = keyValues.GetFloat("m_flEndTime", 1f);
    readonly float _startScale = keyValues.GetFloat("m_flStartScale", 1f);
    readonly float _endScale = keyValues.GetFloat("m_flEndScale", 1f);

    public void Update(Span<Particle> particles, float frameTime, ParticleSystemRenderState particleSystemState)
    {
        for (var i = 0; i < particles.Length; ++i)
        {
            var time = 1 - (particles[i].Lifetime / particles[i].ConstantLifetime);
            if (time >= _startTime && time <= _endTime)
            {
                var t = (time - _startTime) / (_endTime - _startTime);
                var radiusScale = (_startScale * (1 - t)) + (_endScale * t);
                particles[i].Radius = particles[i].ConstantRadius * radiusScale;
            }
        }
    }
}
