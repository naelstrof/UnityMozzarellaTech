float BlendDistance( float a, float b, float k ) {
    float h = clamp( 0.5+0.5*(b-a)/k, 0.0, 1.0 );
    float blendDst = lerp( b, a, h ) - k*h*(1.0-h);
    return blendDst;
}
float4 Blend( float a, float b, float4 colA, float4 colB, float k ) {
    float h = clamp( 0.5+0.5*(b-a)/k, 0.0, 1.0 );
    float blendDst = lerp( b, a, h ) - k*h*(1.0-h);
    float4 blendCol = lerp(colB,colA,h);
    return blendCol;
}
float SphereDistance(float3 eye, float3 centre, float radius) {
    return distance(eye, centre) - radius;
}
float SceneDistance(float3 p, float sceneDist, float3 spherePos, float sphereRadius, float blendStrength) {
    float sphereDist = SphereDistance(p, spherePos, sphereRadius);
    return BlendDistance(sphereDist, sceneDist, blendStrength);
}
float3 EstimateNormal(float3 p, float sceneDist, float3 spherePos, float sphereRadius, float blendStrength) {
    float epsilon = 0.02;
    float x = SceneDistance(float3(p.x+epsilon,p.y,p.z), sceneDist, spherePos, sphereRadius, blendStrength) - SceneDistance(float3(p.x-epsilon,p.y,p.z), sceneDist, spherePos, sphereRadius, blendStrength);
    float y = SceneDistance(float3(p.x,p.y+epsilon,p.z), sceneDist, spherePos, sphereRadius, blendStrength) - SceneDistance(float3(p.x,p.y-epsilon,p.z), sceneDist, spherePos, sphereRadius, blendStrength);
    float z = SceneDistance(float3(p.x,p.y,p.z+epsilon), sceneDist, spherePos, sphereRadius, blendStrength) - SceneDistance(float3(p.x,p.y,p.z-epsilon), sceneDist, spherePos, sphereRadius, blendStrength);
    return normalize(float3(x,y,z));
}
float4 GetRaymarchColor(float screenDist, float4 screenColor, inout float3 rayPos, float3 rayDir, float3 spherePos, float sphereRadius, float4 sphereColor, float blendStrength, out float sceneDistFromEye) {
    float maxDst = 50; 
    float rayDst = 0;
    float epsilon = 0.02;
    uint marchSteps = 0;
    sceneDistFromEye = screenDist;
    while ( rayDst < maxDst && rayDst < screenDist && marchSteps < 64) {
        marchSteps++;
        float sphereDist = SphereDistance(rayPos, spherePos, sphereRadius);
        float dist = BlendDistance(sphereDist, sceneDistFromEye, blendStrength);
        if (dist <= epsilon) {
            if (sceneDistFromEye <= dist) {
                return screenColor;
            }
            float4 sceneColor = Blend(sphereDist, sceneDistFromEye, sphereColor, screenColor, blendStrength);
            return sceneColor;
        }
        sceneDistFromEye -= dist;
        rayPos += rayDir*dist;
        rayDst += max(dist,epsilon);
    }
    return screenColor;
}