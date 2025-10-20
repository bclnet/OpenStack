#version 410

in vec2 vTexCoordOut;
out vec4 outputColor;
uniform sampler2D g_tColor;

void main(void) {
    outputColor = texture(g_tColor, vTexCoordOut);
}
