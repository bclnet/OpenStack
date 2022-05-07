#version 400

uniform sampler2D uTexture;
uniform float uOverbrightFactor;

in vec2 vTexCoords;
in vec4 vColor;

out vec4 fragColor;

void main(void) {
    vec4 color = texture(uTexture, vTexCoords);

    vec3 finalColor = vColor.xyz * color.xyz;
    float blendingFactor = uOverbrightFactor * (0.212 * finalColor.x + 0.715 * finalColor.y + 0.0722 * finalColor.z);

    fragColor = vec4(finalColor, vColor.w * color.w * blendingFactor);
}
