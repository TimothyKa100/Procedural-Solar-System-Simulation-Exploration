Shader "Custom/PlanetaryAtmospherePostProcess"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _PlanetRadius ("Planet Radius", Float) = 6371000
        _AtmoThickness ("Atmosphere Thickness", Float) = 100000
        _ScatteringCoeff ("Scattering Coefficients (Rayleigh, Mie, Absorption)", Vector) = (1, 0.8, 0.1, 1)
        _AnisotropyG ("Anisotropy Factor", Range(-1, 1)) = 0.76
        _CloudSpeed ("Cloud Movement Speed (X, Y)", Vector) = (0.001, 0.0005, 0, 0)
        _CloudDensity ("Cloud Density", Range(0, 2)) = 1.0
        _CloudScale ("Cloud Scale", Range(0.1, 10)) = 2.0
        _AtmosphereColor ("Atmosphere Base Color", Color) = (0.5, 0.7, 1.0, 1.0)
        _CloudColor ("Cloud Color", Color) = (1.0, 1.0, 1.0, 1.0)
    }
    
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent+100" }
        LOD 200

        Pass
        {
            Name "AtmospherePass"
            ZWrite Off
            ZTest Always
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 viewDir : TEXCOORD1;
                float3 worldPos : TEXCOORD2;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            
            // Shader parameters
            float _PlanetRadius;
            float _AtmoThickness;
            float4 _ScatteringCoeff;
            float _AnisotropyG;
            float2 _CloudSpeed;
            float _CloudDensity;
            float _CloudScale;
            float4 _AtmosphereColor;
            float4 _CloudColor;

            // Noise generation functions
            // Simplex noise implementation
            float3 mod289(float3 x) { return x - floor(x * (1.0 / 289.0)) * 289.0; }
            float2 mod289(float2 x) { return x - floor(x * (1.0 / 289.0)) * 289.0; }
            float3 permute(float3 x) { return mod289(((x*34.0)+1.0)*x); }

            float snoise(float2 v) {
                // Simplex noise constants
                const float4 C = float4(0.211324865405187, 0.366025403784439, -0.577350269189626, 0.024390243902439);
                
                // First corner
                float2 i  = floor(v + dot(v, C.yy));
                float2 x0 = v - i + dot(i, C.xx);
                
                // Other corners
                float2 i1 = (x0.x > x0.y) ? float2(1.0, 0.0) : float2(0.0, 1.0);
                float4 x12 = x0.xyxy + C.xxzz;
                x12.xy -= i1;
                
                // Permutations
                i = mod289(i);
                float3 p = permute(permute(i.y + float3(0.0, i1.y, 1.0))
                    + i.x + float3(0.0, i1.x, 1.0));
                
                // Gradients
                float3 m = max(0.5 - float3(dot(x0,x0), dot(x12.xy,x12.xy),
                                dot(x12.zw,x12.zw)), 0.0);
                m = m*m;
                m = m*m;
                
                // Gradients from hashed positions
                float3 x = 2.0 * frac(p * C.www) - 1.0;
                float3 h = abs(x) - 0.5;
                float3 ox = floor(x + 0.5);
                float3 a0 = x - ox;
                
                // Normalize gradients
                m *= 1.79284291400159 - 0.85373472095314 * (a0*a0 + h*h);
                
                // Compute final noise value
                float3 g;
                g.x  = a0.x  * x0.x  + h.x  * x0.y;
                g.yz = a0.yz * x12.xz + h.yz * x12.yw;
                return 130.0 * dot(m, g);
            }

            // Generate fractal (FBM) noise
            float fractalNoise(float2 uv, int octaves) {
                float total = 0.0;
                float frequency = 1.0;
                float amplitude = 0.5;
                float maxAmplitude = 0.0;
            
                for(int i = 0; i < octaves; i++) {
                    total += snoise(uv * frequency) * amplitude;
                    maxAmplitude += amplitude;
                    amplitude *= 0.5;   // Each octave contributes half as much as the previous
                    frequency *= 2.0;   // Each octave has twice the frequency of the previous
                }
                return total / maxAmplitude;
            }

            // Calculate atmospheric scattering
            float3 calculateScattering(float3 viewDir, float3 lightDir, float height) {
                float cosTheta = dot(viewDir, lightDir);
                
                // Rayleigh scattering (more effective for shorter wavelengths - blues)
                float rayleigh = 0.5 * (1.0 + cosTheta * cosTheta); 
                
                // Mie scattering (affects all wavelengths more equally - whitish)
                // Uses the Henyey-Greenstein phase function
                float g = _AnisotropyG; // Anisotropy factor
                float mie = (1.0 - g * g) / pow(1.0 + g * g - 2.0 * g * cosTheta, 1.5);
                
                // Apply scattering coefficients to different color channels
                float3 scattering = _ScatteringCoeff.x * rayleigh * _AtmosphereColor.rgb; // Rayleigh contribution
                scattering += _ScatteringCoeff.y * mie * float3(1.0, 0.9, 0.8); // Mie contribution
                
                // Apply atmospheric density falloff with height
                return scattering * exp(-height * _ScatteringCoeff.z);
            }

            // Vertex shader
            Varyings Vert(Attributes input)
            {
                Varyings output;
                
                // Transform vertex from object space to clip space
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                
                // Get world position
                float3 worldPos = TransformObjectToWorld(input.positionOS.xyz);
                output.worldPos = worldPos;
                
                // Calculate view direction
                output.viewDir = normalize(_WorldSpaceCameraPos - worldPos);
                
                return output;
            }

            // Fragment shader
            float4 Frag(Varyings input) : SV_Target
            {
                // Sample the scene color from the main texture
                float4 sceneColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                
                // Calculate relative height in atmosphere
                float distFromCenter = length(input.worldPos);
                float height = (distFromCenter - _PlanetRadius) / _AtmoThickness;
                height = saturate(height); // Clamp between 0 and 1
                
                // Get main directional light for scattering calculations
                Light mainLight = GetMainLight();
                float3 lightDir = mainLight.direction;
                
                // Calculate cloud movement using time and speed parameters
                float2 cloudUV = input.uv * _CloudScale;
                cloudUV += _Time.y * _CloudSpeed;
                
                // Use cloud UV with additional distortion for more natural movement
                float2 distortionUV = cloudUV + float2(0.1, 0.2);
                float distortion = fractalNoise(distortionUV, 3) * 0.2;
                
                // Generate cloud pattern with distorted coordinates
                float clouds = fractalNoise(cloudUV + float2(distortion, distortion * 1.5), 6);
                clouds = saturate(clouds * _CloudDensity);
                
                // Calculate atmospheric scattering
                float3 scattering = calculateScattering(input.viewDir, lightDir, height);
                
                // Blend cloud color with atmosphere color
                float3 atmosphereColor = lerp(_AtmosphereColor.rgb, _CloudColor.rgb, clouds);
                
                // Apply scattering and light color
                atmosphereColor = lerp(atmosphereColor, mainLight.color, scattering);
                
                // Enhance atmospheric effect at horizon
                float horizon = 1.0 - saturate(abs(dot(input.viewDir, normalize(input.worldPos))));
                atmosphereColor += scattering * pow(horizon, 4.0) * 2.0;
                
                // Calculate alpha for blending
                // More opaque near horizon, more transparent when looking directly at the planet
                float alpha = saturate(height * 3.0) * horizon * 0.8;
                
                // Blend atmosphere with scene
                return float4(lerp(sceneColor.rgb, atmosphereColor, alpha), sceneColor.a);
            }
            
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}