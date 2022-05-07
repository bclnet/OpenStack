#version 400

uniform vec3 uColor;
uniform sampler2D uTexture;

in vec2 uv;

uniform vec2 uUvOffset;
uniform vec2 uUvScale;

uniform float uAlpha;
uniform float uOverbrightFactor;

out vec4 fragColor;

void main(void) {
    vec4 color = texture(uTexture, uUvOffset + uv * uUvScale);

    vec3 finalColor = uColor * color.xyz;
    float blendingFactor = uOverbrightFactor * (finalColor.x + finalColor.y + finalColor.z) / 3.0;

    fragColor = vec4(finalColor, uAlpha * color.w * blendingFactor);
}
