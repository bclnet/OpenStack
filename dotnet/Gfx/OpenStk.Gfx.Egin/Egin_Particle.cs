using System;
using System.Collections.Generic;
using System.Numerics;
#pragma warning disable CS9113

namespace OpenStack.Gfx.Egin.Particles;

#region IParticleEmitter

public interface IParticleEmitter {
    void Start(Action particleEmitCallback);
    void Stop();
    void Update(float frameTime);
    bool IsFinished { get; }
}

#endregion

#region IParticleEmitter : ContinuousEmitter

public class ContinuousEmitter : IParticleEmitter {
    public bool IsFinished { get; private set; }

    readonly IDictionary<string, object> _baseProperties;
    readonly INumberProvider _emissionDuration;
    readonly INumberProvider _startTime;
    readonly INumberProvider _emitRate;
    readonly float _emitInterval = 0.01f;
    Action _particleEmitCallback;
    float _time;
    float _lastEmissionTime;

    public ContinuousEmitter(IDictionary<string, object> baseProperties, IDictionary<string, object> keyValues) {
        _baseProperties = baseProperties;
        _emissionDuration = keyValues.GetNumberProvider("m_flEmissionDuration") ?? new LiteralNumberProvider(0);
        _startTime = keyValues.GetNumberProvider("m_flStartTime") ?? new LiteralNumberProvider(0);
        if (keyValues.ContainsKey("m_flEmitRate")) {
            _emitRate = keyValues.GetNumberProvider("m_flEmitRate");
            _emitInterval = 1.0f / (float)_emitRate.NextNumber();
        }
        else _emitRate = new LiteralNumberProvider(100);
    }

    public void Start(Action particleEmitCallback) {
        _particleEmitCallback = particleEmitCallback;
        _time = 0f;
        _lastEmissionTime = 0;
        IsFinished = false;
    }

    public void Stop()
        => IsFinished = true;

    public void Update(float frameTime) {
        if (IsFinished) return;
        _time += frameTime;
        var nextStartTime = _startTime.NextNumber();
        var nextEmissionDuration = _emissionDuration.NextNumber();
        if (_time >= nextStartTime && (nextEmissionDuration == 0f || _time <= nextStartTime + nextEmissionDuration)) {
            var numToEmit = (int)Math.Floor((_time - _lastEmissionTime) / _emitInterval);
            var emitCount = Math.Min(5 * _emitRate.NextNumber(), numToEmit); // Limit the amount of particles to emit at once in case of refocus
            for (var i = 0; i < emitCount; i++) _particleEmitCallback();
            _lastEmissionTime += numToEmit * _emitInterval;
        }
    }
}

#endregion

#region IParticleEmitter : ContinuousEmitter

public class InstantaneousEmitter(IDictionary<string, object> baseProperties, IDictionary<string, object> keyValues) : IParticleEmitter {
    public bool IsFinished { get; private set; }

    readonly IDictionary<string, object> _baseProperties = baseProperties;
    Action _particleEmitCallback;
    INumberProvider _emitCount = keyValues.GetNumberProvider("m_nParticlesToEmit");
    float _startTime = keyValues.GetFloat("m_flStartTime");
    float _time;

    public void Start(Action particleEmitCallback) {
        _particleEmitCallback = particleEmitCallback;
        IsFinished = false;
        _time = 0;
    }

    public void Stop() { }

    public void Update(float frameTime) {
        _time += frameTime;
        if (!IsFinished && _time >= _startTime) {
            var numToEmit = _emitCount.NextInt(); // Get value from number provider
            for (var i = 0; i < numToEmit; i++) _particleEmitCallback();
            IsFinished = true;
        }
    }
}

#endregion

#region IParticleInitializer

public interface IParticleInitializer {
    Particle Initialize(ref Particle particle, ParticleSystemRenderState particleSystemState);
}

#endregion

#region IParticleInitializer : CreateWithinSphere

public class CreateWithinSphere(IDictionary<string, object> keyValues) : IParticleInitializer {
    readonly Random _random = new();
    readonly float _radiusMin = keyValues.GetFloat("m_fRadiusMin");
    readonly float _radiusMax = keyValues.GetFloat("m_fRadiusMax");
    readonly float _speedMin = keyValues.GetFloat("m_fSpeedMin");
    readonly float _speedMax = keyValues.GetFloat("m_fSpeedMax");
    readonly Vector3 _localCoordinateSystemSpeedMin = keyValues.TryGet<double[]>("m_LocalCoordinateSystemSpeedMin", out var z)
            ? new Vector3((float)z[0], (float)z[1], (float)z[2])
            : Vector3.Zero;
    readonly Vector3 _localCoordinateSystemSpeedMax = keyValues.TryGet<double[]>("m_LocalCoordinateSystemSpeedMax", out var z)
            ? new Vector3((float)z[0], (float)z[1], (float)z[2])
            : Vector3.Zero;

    public Particle Initialize(ref Particle particle, ParticleSystemRenderState particleSystemRenderState) {
        var randomVector = new Vector3(
            ((float)_random.NextDouble() * 2) - 1,
            ((float)_random.NextDouble() * 2) - 1,
            ((float)_random.NextDouble() * 2) - 1);

        // Normalize
        var direction = randomVector / randomVector.Length();
        var distance = _radiusMin + ((float)_random.NextDouble() * (_radiusMax - _radiusMin));
        var speed = _speedMin + ((float)_random.NextDouble() * (_speedMax - _speedMin));
        var localCoordinateSystemSpeed = _localCoordinateSystemSpeedMin + ((float)_random.NextDouble() * (_localCoordinateSystemSpeedMax - _localCoordinateSystemSpeedMin));
        particle.Position += direction * distance;
        particle.Velocity = (direction * speed) + localCoordinateSystemSpeed;
        return particle;
    }
}

#endregion

#region IParticleInitializer : InitialVelocityNoise

public class InitialVelocityNoise(IDictionary<string, object> keyValues) : IParticleInitializer {
    readonly IVectorProvider _outputMin = keyValues.GetVectorProvider("m_vecOutputMin") ?? new LiteralVectorProvider(Vector3.Zero);
    readonly IVectorProvider _outputMax = keyValues.GetVectorProvider("m_vecOutputMax") ?? new LiteralVectorProvider(Vector3.One);
    readonly INumberProvider _noiseScale = keyValues.GetNumberProvider("m_flNoiseScale") ?? new LiteralNumberProvider(1f);

    public Particle Initialize(ref Particle particle, ParticleSystemRenderState particleSystemState) {
        var noiseScale = (float)_noiseScale.NextNumber();
        var r = new Vector3(
            Simplex1D(particleSystemState.Lifetime * noiseScale),
            Simplex1D((particleSystemState.Lifetime * noiseScale) + 101723),
            Simplex1D((particleSystemState.Lifetime * noiseScale) + 555557));
        var min = _outputMin.NextVector();
        var max = _outputMax.NextVector();
        particle.Velocity = min + (r * (max - min));
        return particle;
    }

    // Simple perlin noise implementation

    static float Simplex1D(float t) {
        var previous = PseudoRandom((float)Math.Floor(t));
        var next = PseudoRandom((float)Math.Ceiling(t));
        return CosineInterpolate(previous, next, t % 1f);
    }

    static float PseudoRandom(float t)
       => ((1013904223517 * t) % 1664525) / 1664525f;

    static float CosineInterpolate(float start, float end, float mu) {
        var mu2 = (1 - (float)Math.Cos(mu * Math.PI)) / 2f;
        return (start * (1 - mu2)) + (end * mu2);
    }
}

#endregion

#region IParticleInitializer : OffsetVectorToVector

public class OffsetVectorToVector(IDictionary<string, object> keyValues) : IParticleInitializer {
    readonly Random _random = new();
    readonly ParticleField _inputField = (ParticleField)keyValues.GetInt64("m_nFieldInput");
    readonly ParticleField _outputField = (ParticleField)keyValues.GetInt64("m_nFieldOutput");
    readonly Vector3 _offsetMin = keyValues.TryGet<double[]>("m_vecOutputMin", out var z)
            ? new Vector3((float)z[0], (float)z[1], (float)z[2])
            : Vector3.Zero;
    readonly Vector3 _offsetMax = keyValues.TryGet<double[]>("m_vecOutputMax", out var z)
            ? new Vector3((float)z[0], (float)z[1], (float)z[2])
            : Vector3.One;

    public Particle Initialize(ref Particle particle, ParticleSystemRenderState particleSystemState) {
        var input = particle.GetVector(_inputField);

        var offset = new Vector3(
            Lerp(_offsetMin.X, _offsetMax.X, (float)_random.NextDouble()),
            Lerp(_offsetMin.Y, _offsetMax.Y, (float)_random.NextDouble()),
            Lerp(_offsetMin.Z, _offsetMax.Z, (float)_random.NextDouble()));
        if (_outputField == ParticleField.Position) particle.Position += input + offset;
        else if (_outputField == ParticleField.PositionPrevious) particle.PositionPrevious = input + offset;
        return particle;
    }

    static float Lerp(float min, float max, float t)
       => min + (t * (max - min));
}

#endregion

#region IParticleInitializer : PositionOffset

public class PositionOffset(IDictionary<string, object> keyValues) : IParticleInitializer {
    readonly Random _random = new();
    readonly Vector3 _offsetMin = keyValues.TryGet<double[]>("m_OffsetMin", out var z)
            ? new Vector3((float)z[0], (float)z[1], (float)z[2])
            : Vector3.Zero;
    readonly Vector3 _offsetMax = keyValues.TryGet<double[]>("m_OffsetMax", out var z)
            ? new Vector3((float)z[0], (float)z[1], (float)z[2])
            : Vector3.Zero;

    public Particle Initialize(ref Particle particle, ParticleSystemRenderState particleSystemRenderState) {
        var distance = _offsetMax - _offsetMin;
        var offset = _offsetMin + (distance * new Vector3((float)_random.NextDouble(), (float)_random.NextDouble(), (float)_random.NextDouble()));
        particle.Position += offset;
        return particle;
    }
}

#endregion

#region IParticleInitializer : RandomAlpha

public class RandomAlpha : IParticleInitializer {
    readonly Random _random = new();
    readonly int _alphaMin = 255;
    readonly int _alphaMax = 255;

    public RandomAlpha(IDictionary<string, object> keyValue) {
        _alphaMin = (int)keyValue.GetInt64("m_nAlphaMin", 255);
        _alphaMax = (int)keyValue.GetInt64("m_nAlphaMax", 255);
        if (_alphaMin > _alphaMax) (_alphaMax, _alphaMin) = (_alphaMin, _alphaMax);
    }

    public Particle Initialize(ref Particle particle, ParticleSystemRenderState particleSystemRenderState) {
        var alpha = _random.Next(_alphaMin, _alphaMax) / 255f;
        particle.ConstantAlpha = alpha;
        particle.Alpha = alpha;
        return particle;
    }
}

#endregion

#region IParticleInitializer : RandomColor

public class RandomColor : IParticleInitializer {
    readonly Random _random = new();
    readonly Vector3 _colorMin = Vector3.One;
    readonly Vector3 _colorMax = Vector3.One;

    public RandomColor(IDictionary<string, object> keyValues) {
        if (keyValues.ContainsKey("m_ColorMin")) { var z = keyValues.GetInt64Array("m_ColorMin"); _colorMin = new Vector3(z[0], z[1], z[2]) / 255f; }
        if (keyValues.ContainsKey("m_ColorMax")) { var z = keyValues.GetInt64Array("m_ColorMax"); _colorMax = new Vector3(z[0], z[1], z[2]) / 255f; }
    }

    public Particle Initialize(ref Particle particle, ParticleSystemRenderState particleSystemRenderState) {
        var t = (float)_random.NextDouble();
        particle.ConstantColor = _colorMin + (t * (_colorMax - _colorMin));
        particle.Color = particle.ConstantColor;
        return particle;
    }
}

#endregion

#region IParticleInitializer : RandomLifeTime

public class RandomLifeTime(IDictionary<string, object> keyValues) : IParticleInitializer {
    readonly Random _random = new();
    readonly float _lifetimeMin = keyValues.GetFloat("m_fLifetimeMin");
    readonly float _lifetimeMax = keyValues.GetFloat("m_fLifetimeMax");

    public Particle Initialize(ref Particle particle, ParticleSystemRenderState particleSystemRenderState) {
        var lifetime = _lifetimeMin + ((_lifetimeMax - _lifetimeMin) * (float)_random.NextDouble());
        particle.ConstantLifetime = lifetime;
        particle.Lifetime = lifetime;
        return particle;
    }
}

#endregion

#region IParticleInitializer : RandomRadius

public class RandomRadius(IDictionary<string, object> keyValues) : IParticleInitializer {
    readonly Random _random = new();
    readonly float _radiusMin = keyValues.GetFloat("m_flRadiusMin");
    readonly float _radiusMax = keyValues.GetFloat("m_flRadiusMax");

    public Particle Initialize(ref Particle particle, ParticleSystemRenderState particleSystemRenderState) {
        particle.ConstantRadius = _radiusMin + ((float)_random.NextDouble() * (_radiusMax - _radiusMin));
        particle.Radius = particle.ConstantRadius;
        return particle;
    }
}

#endregion

#region IParticleInitializer : RandomRotation

public class RandomRotation(IDictionary<string, object> keyValues) : IParticleInitializer {
    const float PiOver180 = (float)Math.PI / 180f;
    readonly Random _random = new();
    readonly float _degreesMin = keyValues.GetFloat("m_flDegreesMin");
    readonly float _degreesMax = keyValues.GetFloat("m_flDegreesMax", 360f);
    readonly float _degreesOffset = keyValues.GetFloat("m_flDegrees");
    readonly long _fieldOutput = keyValues.GetInt64("m_nFieldOutput", 4);
    readonly bool _randomlyFlipDirection = keyValues.Get<bool>("m_bRandomlyFlipDirection");

    public Particle Initialize(ref Particle particle, ParticleSystemRenderState particleSystemRenderState) {
        var degrees = _degreesOffset + _degreesMin + ((float)_random.NextDouble() * (_degreesMax - _degreesMin));
        if (_randomlyFlipDirection && _random.NextDouble() > 0.5) degrees *= -1;
        if (_fieldOutput == 4) particle.Rotation = new Vector3(particle.Rotation.X, particle.Rotation.Y, degrees * PiOver180); // Roll
        else if (_fieldOutput == 12) particle.Rotation = new Vector3(particle.Rotation.X, degrees * PiOver180, particle.Rotation.Z); // Yaw
        return particle;
    }
}

#endregion

#region IParticleInitializer : RandomRotationSpeed

public class RandomRotationSpeed(IDictionary<string, object> keyValues) : IParticleInitializer {
    const float PiOver180 = (float)Math.PI / 180f;
    readonly Random _random = new();
    readonly ParticleField _fieldOutput = (ParticleField)keyValues.GetInt64("m_nFieldOutput", (int)ParticleField.Roll);
    readonly bool _randomlyFlipDirection = keyValues.Get<bool>("m_bRandomlyFlipDirection", true);
    readonly float _degrees = keyValues.GetFloat("m_flDegrees");
    readonly float _degreesMin = keyValues.GetFloat("m_flDegreesMin");
    readonly float _degreesMax = keyValues.GetFloat("m_flDegreesMax", 360f);

    public Particle Initialize(ref Particle particle, ParticleSystemRenderState particleSystemState) {
        var value = PiOver180 * (_degrees + _degreesMin + ((float)_random.NextDouble() * (_degreesMax - _degreesMin)));
        if (_randomlyFlipDirection && _random.NextDouble() > 0.5) value *= -1;
        if (_fieldOutput == ParticleField.Yaw) particle.RotationSpeed = new Vector3(value, 0, 0);
        else if (_fieldOutput == ParticleField.Roll) particle.RotationSpeed = new Vector3(0, 0, value);
        return particle;
    }
}

#endregion

#region IParticleInitializer : RandomSequence

public class RandomSequence(IDictionary<string, object> keyValues) : IParticleInitializer {
    readonly Random _random = new();
    readonly int _sequenceMin = (int)keyValues.GetInt64("m_nSequenceMin");
    readonly int _sequenceMax = (int)keyValues.GetInt64("m_nSequenceMax");
    readonly bool _shuffle = keyValues.Get<bool>("m_bShuffle");
    int _counter = 0;

    public Particle Initialize(ref Particle particle, ParticleSystemRenderState particleSystemState) {
        if (_shuffle) particle.Sequence = _random.Next(_sequenceMin, _sequenceMax + 1);
        else particle.Sequence = _sequenceMin + (_sequenceMax > _sequenceMin ? (_counter++ % (_sequenceMax - _sequenceMin)) : 0);
        return particle;
    }
}

#endregion

#region IParticleInitializer : RandomTrailLength

public class RandomTrailLength(IDictionary<string, object> keyValues) : IParticleInitializer {
    readonly Random _random = new();
    readonly float _minLength = keyValues.GetFloat("m_flMinLength", 0.1f);
    readonly float _maxLength = keyValues.GetFloat("m_flMaxLength", 0.1f);

    public Particle Initialize(ref Particle particle, ParticleSystemRenderState particleSystemState) {
        particle.TrailLength = _minLength + ((float)_random.NextDouble() * (_maxLength - _minLength));
        return particle;
    }
}

#endregion

#region IParticleInitializer : RemapParticleCountToScalar

public class RemapParticleCountToScalar(IDictionary<string, object> keyValues) : IParticleInitializer {
    readonly long _fieldOutput = keyValues.GetInt64("m_nFieldOutput", 3);
    readonly long _inputMin = keyValues.GetInt64("m_nInputMin");
    readonly long _inputMax = keyValues.GetInt64("m_nInputMax", 10);
    readonly float _outputMin = keyValues.GetFloat("m_flOutputMin");
    readonly float _outputMax = keyValues.GetFloat("m_flOutputMax", 1f);
    readonly bool _scaleInitialRange = keyValues.Get<bool>("m_bScaleInitialRange");

    public Particle Initialize(ref Particle particle, ParticleSystemRenderState particleSystemRenderState) {
        var particleCount = Math.Min(_inputMax, Math.Max(_inputMin, particle.ParticleCount));
        var t = (particleCount - _inputMin) / (float)(_inputMax - _inputMin);
        var output = _outputMin + (t * (_outputMax - _outputMin));
        switch (_fieldOutput) {
            case 3:
                particle.Radius = _scaleInitialRange
                    ? particle.Radius * output
                    : output;
                break;
        }
        return particle;
    }
}

#endregion

#region IParticleInitializer : RingWave

public class RingWave(IDictionary<string, object> keyValues) : IParticleInitializer {
    readonly Random random = new();
    readonly bool _evenDistribution = keyValues.Get<bool>("m_bEvenDistribution");
    readonly float _initialRadius = keyValues.GetFloat("m_flInitialRadius");
    readonly float _thickness = keyValues.GetFloat("m_flThickness");
    readonly float _particlesPerOrbit = keyValues.GetFloat("m_flParticlesPerOrbit", -1f);
    float _orbitCount;

    public Particle Initialize(ref Particle particle, ParticleSystemRenderState particleSystemState) {
        var radius = _initialRadius + ((float)random.NextDouble() * _thickness);
        var angle = GetNextAngle();
        particle.Position += radius * new Vector3((float)Math.Cos(angle), (float)Math.Sin(angle), 0);
        return particle;
    }

    double GetNextAngle() {
        if (_evenDistribution) {
            var offset = _orbitCount / _particlesPerOrbit;
            _orbitCount = (_orbitCount + 1) % _particlesPerOrbit;
            return offset * 2 * Math.PI;
        }
        else return 2 * Math.PI * random.NextDouble(); // Return a random angle between 0 and 2pi
    }
}

#endregion

#region IParticleOperator

public interface IParticleOperator {
    void Update(Span<Particle> particles, float frameTime, ParticleSystemRenderState particleSystemState);
}

#endregion

#region IParticleOperator : BasicMovement

public class BasicMovement(IDictionary<string, object> keyValues) : IParticleOperator {
    readonly Vector3 _gravity = keyValues.TryGet<double[]>("m_Gravity", out var vectorValues)
        ? new Vector3((float)vectorValues[0], (float)vectorValues[1], (float)vectorValues[2])
        : Vector3.Zero;
    readonly float _drag = keyValues.GetFloat("m_fDrag");

    public void Update(Span<Particle> particles, float frameTime, ParticleSystemRenderState particleSystemState) {
        var acceleration = _gravity * frameTime;
        for (var i = 0; i < particles.Length; ++i) {
            // Apply acceleration
            particles[i].Velocity += acceleration;
            // Apply drag
            particles[i].Velocity *= 1 - (_drag * 30f * frameTime);
            particles[i].Position += particles[i].Velocity * frameTime;
        }
    }
}

#endregion

#region IParticleOperator : ColorInterpolate

public class ColorInterpolate : IParticleOperator {
    readonly Vector3 _colorFade;
    readonly float _fadeStartTime;
    readonly float _fadeEndTime;

    public ColorInterpolate(IDictionary<string, object> keyValues) {
        if (keyValues.ContainsKey("m_ColorFade")) {
            var vectorValues = keyValues.GetInt64Array("m_ColorFade");
            _colorFade = new Vector3(vectorValues[0], vectorValues[1], vectorValues[2]) / 255f;
        }
        else _colorFade = Vector3.One;
        _fadeStartTime = keyValues.GetFloat("m_flFadeStartTime");
        _fadeEndTime = keyValues.GetFloat("m_flFadeEndTime", 1f);
    }

    public void Update(Span<Particle> particles, float frameTime, ParticleSystemRenderState particleSystemState) {
        for (var i = 0; i < particles.Length; ++i) {
            var time = 1 - (particles[i].Lifetime / particles[i].ConstantLifetime);
            if (time >= _fadeStartTime && time <= _fadeEndTime) {
                var t = (time - _fadeStartTime) / (_fadeEndTime - _fadeStartTime);
                // Interpolate from constant color to fade color
                particles[i].Color = ((1 - t) * particles[i].ConstantColor) + (t * _colorFade);
            }
        }
    }
}

#endregion

#region IParticleOperator : Decay

public class Decay(IDictionary<string, object> keyValues) : IParticleOperator {
    public void Update(Span<Particle> particles, float frameTime, ParticleSystemRenderState particleSystemState) {
        for (var i = 0; i < particles.Length; ++i) particles[i].Lifetime -= frameTime;
    }
}

#endregion

#region IParticleOperator : FadeAndKill

public class FadeAndKill(IDictionary<string, object> keyValues) : IParticleOperator {
    readonly float _startFadeInTime = keyValues.GetFloat("m_flStartFadeInTime");
    readonly float _endFadeInTime = keyValues.GetFloat("m_flEndFadeInTime", .5f);
    readonly float _startFadeOutTime = keyValues.GetFloat("m_flStartFadeOutTime", .5f);
    readonly float _endFadeOutTime = keyValues.GetFloat("m_flEndFadeOutTime", 1f);
    readonly float _startAlpha = keyValues.GetFloat("m_flStartAlpha", 1f);
    readonly float _endAlpha = keyValues.GetFloat("m_flEndAlpha");

    public void Update(Span<Particle> particles, float frameTime, ParticleSystemRenderState particleSystemState) {
        for (var i = 0; i < particles.Length; ++i) {
            var time = 1 - (particles[i].Lifetime / particles[i].ConstantLifetime);
            // If fading in
            if (time >= _startFadeInTime && time <= _endFadeInTime) {
                var t = (time - _startFadeInTime) / (_endFadeInTime - _startFadeInTime);
                // Interpolate from startAlpha to constantAlpha
                particles[i].Alpha = ((1 - t) * _startAlpha) + (t * particles[i].ConstantAlpha);
            }
            // If fading out
            if (time >= _startFadeOutTime && time <= _endFadeOutTime) {
                var t = (time - _startFadeOutTime) / (_endFadeOutTime - _startFadeOutTime);
                // Interpolate from constantAlpha to end alpha
                particles[i].Alpha = ((1 - t) * particles[i].ConstantAlpha) + (t * _endAlpha);
            }
            particles[i].Lifetime -= frameTime;
        }
    }
}

#endregion

#region IParticleOperator : FadeInSimple

public class FadeInSimple(IDictionary<string, object> keyValues) : IParticleOperator {
    readonly float _fadeInTime = keyValues.GetFloat("m_flFadeInTime", .25f);

    public void Update(Span<Particle> particles, float frameTime, ParticleSystemRenderState particleSystemState) {
        for (var i = 0; i < particles.Length; ++i) {
            var time = 1 - (particles[i].Lifetime / particles[i].ConstantLifetime);
            if (time <= _fadeInTime) particles[i].Alpha = (time / _fadeInTime) * particles[i].ConstantAlpha;
        }
    }
}

#endregion

#region IParticleOperator : FadeOutSimple

public class FadeOutSimple(IDictionary<string, object> keyValues) : IParticleOperator {
    readonly float _fadeOutTime = keyValues.GetFloat("m_flFadeOutTime", .25f);

    public void Update(Span<Particle> particles, float frameTime, ParticleSystemRenderState particleSystemState) {
        for (var i = 0; i < particles.Length; ++i) {
            var timeLeft = particles[i].Lifetime / particles[i].ConstantLifetime;
            if (timeLeft <= _fadeOutTime) { var t = timeLeft / _fadeOutTime; particles[i].Alpha = t * particles[i].ConstantAlpha; }
        }
    }
}

#endregion

#region IParticleOperator : InterpolateRadius

public class InterpolateRadius(IDictionary<string, object> keyValues) : IParticleOperator {
    readonly float _startTime = keyValues.GetFloat("m_flStartTime");
    readonly float _endTime = keyValues.GetFloat("m_flEndTime", 1f);
    readonly float _startScale = keyValues.GetFloat("m_flStartScale", 1f);
    readonly float _endScale = keyValues.GetFloat("m_flEndScale", 1f);

    public void Update(Span<Particle> particles, float frameTime, ParticleSystemRenderState particleSystemState) {
        for (var i = 0; i < particles.Length; ++i) {
            var time = 1 - (particles[i].Lifetime / particles[i].ConstantLifetime);
            if (time >= _startTime && time <= _endTime) {
                var t = (time - _startTime) / (_endTime - _startTime);
                var radiusScale = (_startScale * (1 - t)) + (_endScale * t);
                particles[i].Radius = particles[i].ConstantRadius * radiusScale;
            }
        }
    }
}

#endregion

#region IParticleOperator : OscillateScalar

public class OscillateScalar(IDictionary<string, object> keyValues) : IParticleOperator {
    readonly Random _random = new();
    readonly ParticleField _outputField = (ParticleField)keyValues.GetInt64("m_nField", (int)ParticleField.Alpha);
    readonly float _rateMin = keyValues.GetFloat("m_RateMin");
    readonly float _rateMax = keyValues.GetFloat("m_RateMax");
    readonly float _frequencyMin = keyValues.GetFloat("m_FrequencyMin", 1f);
    readonly float _frequencyMax = keyValues.GetFloat("m_FrequencyMax", 1f);
    readonly float _oscillationMultiplier = keyValues.GetFloat("m_flOscMult", 2f);
    readonly float _oscillationOffset = keyValues.GetFloat("m_flOscAdd", .5f);
    readonly bool _proportional = keyValues.Get<bool>("m_bProportionalOp", true);

    public void Update(Span<Particle> particles, float frameTime, ParticleSystemRenderState particleSystemState) {
        // Remove expired particles
        /*var particlesToRemove = particleRates.Keys.Except(particles[i]).ToList();
        foreach (var p in particlesToRemove)
        {
            particleRates.Remove(p);
            particleFrequencies.Remove(p);
        }*/

        // Update remaining particles
        for (var i = 0; i < particles.Length; ++i) {
            var rate = GetParticleRate(particles[i].ParticleCount);
            var frequency = GetParticleFrequency(particles[i].ParticleCount);
            var t = _proportional
                ? 1 - (particles[i].Lifetime / particles[i].ConstantLifetime)
                : particles[i].Lifetime;
            var delta = (float)Math.Sin(((t * frequency * _oscillationMultiplier) + _oscillationOffset) * Math.PI);
            if (_outputField == ParticleField.Radius) particles[i].Radius += delta * rate * frameTime;
            else if (_outputField == ParticleField.Alpha) particles[i].Alpha += delta * rate * frameTime;
            else if (_outputField == ParticleField.AlphaAlternate) particles[i].AlphaAlternate += delta * rate * frameTime;
        }
    }

    Dictionary<int, float> _particleRates = new();
    Dictionary<int, float> _particleFrequencies = new();

    float GetParticleRate(int particleId) {
        if (_particleRates.TryGetValue(particleId, out var rate)) return rate;
        else { var newRate = _rateMin + ((float)_random.NextDouble() * (_rateMax - _rateMin)); _particleRates[particleId] = newRate; return newRate; }
    }

    float GetParticleFrequency(int particleId) {
        if (_particleFrequencies.TryGetValue(particleId, out var frequency)) return frequency;
        else { var newFrequency = _frequencyMin + ((float)_random.NextDouble() * (_frequencyMax - _frequencyMin)); _particleFrequencies[particleId] = newFrequency; return newFrequency; }
    }
}

#endregion

#region IParticleOperator : SpinUpdate

public class SpinUpdate(IDictionary<string, object> keyValues) : IParticleOperator {
    public void Update(Span<Particle> particles, float frameTime, ParticleSystemRenderState particleSystemState) {
        for (var i = 0; i < particles.Length; ++i) particles[i].Rotation += particles[i].RotationSpeed * frameTime;
    }
}

#endregion

#region INumberProvider

public interface INumberProvider {
    double NextNumber();
}

public class LiteralNumberProvider(double value) : INumberProvider {
    readonly double _value = value;

    public double NextNumber() => _value;
}

public static partial class ParticleExtensions {
    public static INumberProvider GetNumberProvider(this IDictionary<string, object> keyValues, string propertyName, INumberProvider defaultValue = default) {
        if (!keyValues.TryGetValue(propertyName, out var property)) return defaultValue;

        if (property is IDictionary<string, object> numberProviderParameters) {
            var type = numberProviderParameters.Get<string>("m_nType");
            return type switch {
                "PF_TYPE_LITERAL" => new LiteralNumberProvider(numberProviderParameters.GetDouble("m_flLiteralValue")),
                _ => throw new InvalidCastException($"Could not create number provider of type {type}."),
            };
        }
        else return new LiteralNumberProvider(Convert.ToDouble(property));
    }

    public static int NextInt(this INumberProvider numberProvider)
        => (int)numberProvider.NextNumber();
}

#endregion

#region IParticleRenderer

public interface IParticleRenderer {
    void Render(ParticleBag particles, Matrix4x4 viewProjectionMatrix, Matrix4x4 modelViewMatrix);
    void SetRenderMode(string renderMode);
    IEnumerable<string> GetSupportedRenderModes();
}

#endregion

#region IVectorProvider

public interface IVectorProvider {
    Vector3 NextVector();
}

public class LiteralVectorProvider : IVectorProvider {
    readonly Vector3 _value;
    public LiteralVectorProvider(Vector3 value) => _value = value;
    public LiteralVectorProvider(double[] value) => _value = new Vector3((float)value[0], (float)value[1], (float)value[2]);
    public Vector3 NextVector() => _value;
}

public static partial class ParticleExtensions {
    public static IVectorProvider GetVectorProvider(this IDictionary<string, object> keyValues, string propertyName, IVectorProvider defaultValue = default) {
        if (!keyValues.TryGetValue(propertyName, out var property)) return defaultValue;
        if (property is IDictionary<string, object> numberProviderParameters && numberProviderParameters.ContainsKey("m_nType")) {
            var type = numberProviderParameters.Get<string>("m_nType");
            return type switch {
                "PVEC_TYPE_LITERAL" => new LiteralVectorProvider(numberProviderParameters.Get<double[]>("m_vLiteralValue")),
                _ => throw new InvalidCastException($"Could not create vector provider of type {type}."),
            };
        }
        return new LiteralVectorProvider(keyValues.Get<double[]>(propertyName));
    }
}

#endregion

#region Particle

public struct Particle {
    public int ParticleCount { get; set; }

    // Base properties
    public float ConstantAlpha { get; set; }
    public Vector3 ConstantColor { get; set; }
    public float ConstantLifetime { get; set; }
    public float ConstantRadius { get; set; }

    // Variable fields
    public float Alpha { get; set; }
    public float AlphaAlternate { get; set; }

    public Vector3 Color { get; set; }

    public float Lifetime { get; set; }

    public Vector3 Position { get; set; }

    public Vector3 PositionPrevious { get; set; }

    public float Radius { get; set; }

    public float TrailLength { get; set; }

    /// <summary>
    /// Gets or sets (Yaw, Pitch, Roll) Euler angles.
    /// </summary>
    public Vector3 Rotation { get; set; }

    /// <summary>
    /// Gets or sets (Yaw, Pitch, Roll) Euler angles rotation speed.
    /// </summary>
    public Vector3 RotationSpeed { get; set; }

    public int Sequence { get; set; }

    public Vector3 Velocity { get; set; }

    public Particle(IDictionary<string, object> baseProperties) {
        ParticleCount = 0;
        Alpha = 1.0f;
        AlphaAlternate = 1.0f;
        Position = Vector3.Zero;
        PositionPrevious = Vector3.Zero;
        Rotation = Vector3.Zero;
        RotationSpeed = Vector3.Zero;
        Velocity = Vector3.Zero;
        ConstantRadius = baseProperties.GetFloat("m_flConstantRadius", 5.0f);
        ConstantAlpha = 1.0f;
        if (baseProperties.ContainsKey("m_ConstantColor")) {
            var vectorValues = baseProperties.GetInt64Array("m_ConstantColor");
            ConstantColor = new Vector3(vectorValues[0], vectorValues[1], vectorValues[2]) / 255f;
        }
        else ConstantColor = Vector3.One;
        ConstantLifetime = baseProperties.GetFloat("m_flConstantLifespan", 1);
        TrailLength = 1;
        Sequence = 0;

        Color = ConstantColor;
        Lifetime = ConstantLifetime;
        Radius = ConstantRadius;
    }

    public Matrix4x4 GetTransformationMatrix() {
        var scaleMatrix = Matrix4x4.CreateScale(Radius);
        var translationMatrix = Matrix4x4.CreateTranslation(Position.X, Position.Y, Position.Z);
        return Matrix4x4.Multiply(scaleMatrix, translationMatrix);
    }

    public Matrix4x4 GetRotationMatrix() {
        var rotationMatrix = Matrix4x4.Multiply(Matrix4x4.CreateRotationZ(Rotation.Z), Matrix4x4.CreateRotationY(Rotation.Y));
        return rotationMatrix;
    }
}

#endregion

#region ParticleBag

public class ParticleBag(int initialCapacity, bool growable) {
    readonly bool _isGrowable = growable;
    Particle[] _particles = new Particle[initialCapacity];

    public int Count { get; private set; }

    public Span<Particle> LiveParticles
        => new Span<Particle>(_particles, 0, Count);

    public int Add() {
        if (Count < _particles.Length) return Count++;
        else if (_isGrowable) {
            var newSize = _particles.Length < 1024 ? _particles.Length * 2 : _particles.Length + 1024;
            var newArray = new Particle[newSize];
            Array.Copy(_particles, 0, newArray, 0, Count);
            _particles = newArray;
            return Count++;
        }
        return -1;
    }

    public void PruneExpired() {
        // TODO: This alters the order of the particles so they are no longer in creation order after something expires. Fix that.
        for (var i = 0; i < Count;)
            if (_particles[i].Lifetime <= 0) { _particles[i] = _particles[Count - 1]; Count--; }
            else ++i;
    }

    public void Clear()
        => Count = 0;
}

#endregion

#region ParticleField

public enum ParticleField {
    Position = 0,
    PositionPrevious = 2,
    Radius = 3,
    Roll = 4,
    Alpha = 7,
    Yaw = 12,
    AlphaAlternate = 16,
}

public static partial class ParticleExtensions {
    public static float GetScalar(this Particle particle, ParticleField field)
        => field switch {
            ParticleField.Alpha => particle.Alpha,
            ParticleField.AlphaAlternate => particle.AlphaAlternate,
            ParticleField.Radius => particle.Radius,
            _ => 0f,
        };

    public static Vector3 GetVector(this Particle particle, ParticleField field)
        => field switch {
            ParticleField.Position => particle.Position,
            ParticleField.PositionPrevious => particle.PositionPrevious,
            _ => Vector3.Zero,
        };
}

#endregion

#region ParticleSystemRenderState

public class ParticleSystemRenderState {
    public float Lifetime { get; set; } = 0f;

    readonly Dictionary<int, Vector3> _controlPoints = new();

    public Vector3 GetControlPoint(int cp)
        => _controlPoints.TryGetValue(cp, out var value)
        ? value
        : Vector3.Zero;

    public ParticleSystemRenderState SetControlPoint(int cp, Vector3 value) {
        _controlPoints[cp] = value;
        return this;
    }
}

#endregion