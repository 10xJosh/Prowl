﻿Shader "Default/SSR"

Pass 0
{
	Vertex
	{
		in vec3 vertexPosition;

		void main() 
		{
			gl_Position =vec4(vertexPosition, 1.0);
		}
	}

	Fragment
	{
		layout(location = 0) out vec4 OutputColor;
		
		uniform vec2 Resolution;
		
		uniform sampler2D gColor; // Depth
		uniform sampler2D gNormalMetallic; // Depth
		uniform sampler2D gPositionRoughness; // Depth
		uniform sampler2D gDepth; // Depth
		
		uniform mat4 matProjection;
		uniform mat4 matProjectionInverse;
		uniform mat4 matViewInverse;

		uniform float Time;

		uniform int SSR_STEPS; // [16 20 24 28 32]
		uniform int SSR_BISTEPS; // [0 4 8 16]
		
		#include "Utilities"
		#include "PBR"

		// ----------------------------------------------------------------------------
		
		vec3 calculateSSR(vec3 viewPos, vec3 screenPos, vec3 gBMVNorm, float dither) {
			vec3 reflectedScreenPos = rayTrace(screenPos, viewPos, reflect(normalize(viewPos), gBMVNorm), dither, SSR_STEPS, SSR_BISTEPS, gDepth);
			if(reflectedScreenPos.z < 0.5) return vec3(0);
			return vec3(reflectedScreenPos.xy, 1);
		}

		void main()
		{
			vec2 texCoords = gl_FragCoord.xy / Resolution;

			vec3 color = texture2D(gColor, texCoords).xyz;
			OutputColor = vec4(color, 1.0);

			vec4 viewPosAndRough = texture2D(gPositionRoughness, texCoords);
			float smoothness = 1.0 - viewPosAndRough.w;

			if(smoothness > 0.05)
			{
				vec4 normalAndMetallic = texture2D(gNormalMetallic, texCoords);
				vec3 normal = normalAndMetallic.xyz;
				float metallic = normalAndMetallic.w;
				
				vec3 screenPos = getScreenPos(texCoords, gDepth);
				vec3 viewPos = getViewFromScreenPos(screenPos);

				bool isMetal = metallic > 0.9;

				// Get fresnel
				vec3 fresnel = FresnelSchlick(max(dot(normal, -normalize(viewPos)), 0.0),
					isMetal ? color : vec3(metallic)) * smoothness;
				
				float dither = fract(sin(dot(texCoords + vec2(Time, Time), vec2(12.9898,78.233))) * 43758.5453123);

				vec3 SSRCoord = calculateSSR(viewPos, screenPos, normalize(normal), dither);
				vec3 ssrColor = SSRCoord.z > 0.5 ? texture2D(gColor, SSRCoord.xy).xyz : color;
				//vec3 ssrColor = texture2D(gColor, SSRCoord.xy).xyz;
				
				OutputColor.rgb *= isMetal ? vec3(1.0 - smoothness) : 1.0 - fresnel;
				OutputColor.rgb += ssrColor * fresnel;
			}
		}

	}
}