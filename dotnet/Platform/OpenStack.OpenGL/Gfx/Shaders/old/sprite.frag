#version 330

//Parameter defines - These are default values and can be overwritten based on material parameters
#define param_F_ALPHA_TEST 0
//End of parameter defines

in vec2 vTexCoordOut;

uniform float g_flAlphaTestReference;
uniform sampler2D g_tColor;

out vec4 outputColor;

void main()
{
    vec4 color = texture(g_tColor, vTexCoordOut);

#if param_F_ALPHA_TEST == 1
    if (color.a < g_flAlphaTestReference)
    {
       discard;
    }
#endif

    outputColor = color;
}  
