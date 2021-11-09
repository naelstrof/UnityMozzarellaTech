#include "UnityCG.cginc"
Texture2D<float4> _DepthTexture;
float4x4 _WorldToCamera;
float4x4 _CameraToWorld;
float4x4 _CameraInverseProjection;
float _NearClipValue;
float _FarClipValue;

float ComputeWorldSpaceDistance(float2 uv, float nonlin_depth) {
    float screenDist = Linear01Depth(nonlin_depth)*(_FarClipValue-_NearClipValue)+_NearClipValue;
    return screenDist;
}