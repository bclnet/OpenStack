#version 330

//Includes - resolved by VRF
#include "compression.incl"
//End of includes

//Parameter defines - These are default values and can be overwritten based on material/model parameters
#define param_fulltangent 1
//End of parameter defines

in vec3 vPOSITION;
in vec4 vNORMAL;
in vec2 vTEXCOORD;
in vec4 vTEXCOORD1;
in vec4 vTEXCOORD2;
in vec4 vTEXCOORD3;
in vec4 vTANGENT;
in ivec4 vBLENDINDICES;
in vec4 vBLENDWEIGHT;

out vec3 vFragPosition;

out vec3 vNormalOut;
out vec3 vTangentOut;
out vec3 vBitangentOut;

out vec4 vBlendWeights;
out vec4 vWeightsOut1;
out vec4 vWeightsOut2;

out vec2 vTexCoordOut;

uniform mat4 uProjectionViewMatrix;
uniform mat4 transform;

void main()
{
    vec4 fragPosition = transform * vec4(vPOSITION, 1.0);
    gl_Position = uProjectionViewMatrix * fragPosition;
    vFragPosition = fragPosition.xyz / fragPosition.w;

    //Unpack normals
#if param_fulltangent == 1
    vNormalOut = vNORMAL.xyz;
#else
    vNormalOut = DecompressNormal(vNORMAL);
#endif

    vTexCoordOut = vTEXCOORD;
    vBitangentOut = vTANGENT.xyz;

    //Normalize (?)
    //vTEXCOORD1 - seems empty
    //vTEXCOORD2 - (X,Y,Z) - Ambient occlusion, W - ???
    //vTEXCOORD3 - X - amount of tex1, Y - amount of tex2, Z - amount of tex3, W - ???
    vBlendWeights = vTEXCOORD3/255.0;
    vWeightsOut1 = vTEXCOORD1;
    vWeightsOut2 = vTEXCOORD2/255.0;
}
