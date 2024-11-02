#version 400

in vec3 aVertexPosition;
in vec4 aVertexColor;
in vec2 aTexCoords;

uniform mat4 uProjectionViewMatrix;

out vec2 vTexCoords;
out vec4 vColor;

void main(void) {
    vColor = aVertexColor;
    vTexCoords = aTexCoords;
    gl_Position = uProjectionViewMatrix * vec4(aVertexPosition, 1.0);
}
