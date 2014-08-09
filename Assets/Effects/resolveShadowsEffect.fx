// NEW NEW

texture InputTexture; 
sampler inputSampler = sampler_state
{
			Texture   = <InputTexture>;
			MipFilter = Point;
			MinFilter = Point;
			MagFilter = Point;
			AddressU  = Clamp;
			AddressV  = Clamp;
};

texture ShadowMapTexture; 
sampler shadowMapSampler = sampler_state
{
			Texture   = <ShadowMapTexture>;
			MipFilter = Point;
			MinFilter = Point;
			MagFilter = Point;
			AddressU  = Clamp;
			AddressV  = Clamp;
};

float2 renderTargetSize;
float2 lightRelativeZero;
float2 shadowCasterMapPortion;
float2 shadowCasterMapPixelSize;
float renderTargetSizeDesired;
float distanceMod;
float size16bit = 65536;
float range16bit = 65535;
float unit16bit = 0.0000152587890625f;
float range8bit = 255;
float size8bit = 256;
float unit8bit = 0.00390625f;

struct VS_OUTPUT
{
	float4 Position  : POSITION;
	float2 TexCoords  : TEXCOORD0;
};

VS_OUTPUT FullScreenVS( float3 InPos  : POSITION,
						float2 InTex  : TEXCOORD0)
{
	VS_OUTPUT Out = (VS_OUTPUT)0;
	// Offset the position by half a pixel to correctly align texels to pixels
	Out.Position = float4(InPos,1) + 0.475f * float4(-1.0f/renderTargetSize.x, 1.0f/renderTargetSize.y, 0, 0);
	Out.TexCoords = InTex;
	return Out;
}

float4 DistortAndComputeDistancesPS(float2 TexCoord  : TEXCOORD0) : COLOR0
{
	//translate u and v into [-1 , 1] domain
	float u0 = TexCoord.x * 2.0f - 1.0f;
	float v0 = TexCoord.y * 2.0f - 1.0f;
	  
	//then, as u0 approaches 0 (the center), v should also approach 0 
	v0 = v0 * abs(u0);

	//convert back from [-1,1] domain to [0,1] domain
	v0 = (v0 + 1.0f) * 0.5f;

	//we now have the coordinates for reading from the initial image
	float2 newCoords = float2(TexCoord.x, v0);

	//read for both horizontal and vertical direction and store them in separate channels

	float2 shadowCastersMapRelativeCoord = float2((shadowCasterMapPortion.x * newCoords.x) + lightRelativeZero.x, (shadowCasterMapPortion.y * newCoords.y) + lightRelativeZero.y);
	float horizontal = 1.0f - tex2D(inputSampler, shadowCastersMapRelativeCoord).r;
	float distanceH = (horizontal>0.3f?length(newCoords - 0.5f):0.5f) * 2.0f;

	shadowCastersMapRelativeCoord = float2((shadowCasterMapPortion.x * newCoords.y) + lightRelativeZero.x, (shadowCasterMapPortion.y * newCoords.x) + lightRelativeZero.y);
	float vertical = 1.0f - tex2D(inputSampler, shadowCastersMapRelativeCoord).r;
	float distanceV = (vertical>0.3f?length(newCoords - 0.5f):1.0f) * 2.0f;

	float precision8bitH = distanceH % unit8bit;
	float precision8bitV = distanceV % unit8bit;

	return float4(distanceV - precision8bitV,precision8bitV * 100,distanceH - precision8bitH,precision8bitH * 100);
}

float GetShadowDistanceH(float2 TexCoord, float displacementV)
{
	float u = TexCoord.x;
	float v = TexCoord.y;

	u = abs(u-0.5f) * 2;
	v = v * 2 - 1;
	float v0 = v/u;
	v0+=displacementV;
	v0 = (v0 + 1) / 2;
		
	float2 newCoords = float2(TexCoord.x,v0);
	//horizontal info was stored in the Red component
	float4 color = tex2D(shadowMapSampler, newCoords);

	return color.b + (color.a * 0.01f);
}

float GetShadowDistanceV(float2 TexCoord, float displacementV)
{
	float u = TexCoord.y;
	float v = TexCoord.x;
		
	u = abs(u-0.5f) * 2;
	v = v * 2 - 1;
	float v0 = v/u;
	v0+=displacementV;
	v0 = (v0 + 1) / 2;
		
	float2 newCoords = float2(TexCoord.y,v0);
	//vertical info was stored in the Green component
	float4 color= tex2D(shadowMapSampler, newCoords).rgba;

	return color.r + (color.g * 0.01f);
}

float4 DrawShadowsNoAttenuationPS(float2 TexCoord  : TEXCOORD0) : COLOR0
{
	float2 newCoord = TexCoord - 0.5f;

	if (newCoord.x < -distanceMod || newCoord.x > distanceMod || newCoord.y < -distanceMod || newCoord.y > distanceMod)
		return 0;

	// distance of this pixel from the center
	double distance = length(newCoord);

	//distance stored in the shadow map
	float shadowMapDistance;

	//coords in [-1,1]
	float nY = 2.0f*(newCoord.y);
	float nX = 2.0f*(newCoord.x);

	//we use these to determine which quadrant we are in
	if(abs(nY) < abs(nX))
		shadowMapDistance = GetShadowDistanceH(TexCoord,0);
	else
		shadowMapDistance = GetShadowDistanceV(TexCoord,0);

	//if distance to this pixel is lower than distance from shadowMap, 
	//then we are not in shadow
	float light = distance * 2.0f < shadowMapDistance ? 1:0;

	float4 result = light;
	result.a = 1;
	return result;
}

float4 DrawShadowsNoAttenuationPreBlurPS(float2 TexCoord  : TEXCOORD0) : COLOR0
{
	float2 newCoord = TexCoord - 0.5f;

	if (newCoord.x < -distanceMod || newCoord.x > distanceMod || newCoord.y < -distanceMod || newCoord.y > distanceMod)
		return 0;

	// distance of this pixel from the center
	float distance = length(newCoord);

	//distance stored in the shadow map
	float shadowMapDistance;

	//coords in [-1,1]
	float nY = 2.0f*(newCoord.y);
	float nX = 2.0f*(newCoord.x);

	//we use these to determine which quadrant we are in
	if(abs(nY)<abs(nX))
		shadowMapDistance = GetShadowDistanceH(TexCoord,0);
	else
		shadowMapDistance = GetShadowDistanceV(TexCoord,0);

	//if distance to this pixel is lower than distance from shadowMap, 
	//then we are not in shadow
	float light = distance * 2.0f < shadowMapDistance ? 1:0;

	float4 result = light;
	result.b = length(TexCoord - 0.5f) * 2;
	result.a = 1;
	return result;
}

float4 DrawShadowsCurveAttenuationPS(float2 TexCoord  : TEXCOORD0) : COLOR0
{
	if (distanceMod == 0)
		return 0;

	float2 newCoords = TexCoord - 0.5f;

	// distance of this pixel from the center
	float distance = length(newCoords);

	//distance stored in the shadow map
	float shadowMapDistance;

	//coords in [-1,1]
	float nY = 2.0f*(newCoords.y);
	float nX = 2.0f*(newCoords.x);

	//we use these to determine which quadrant we are in
	if(abs(nY)<abs(nX))
	{
		shadowMapDistance = GetShadowDistanceH(TexCoord,0);
	}
	else
	{
		shadowMapDistance = GetShadowDistanceV(TexCoord,0);
	}

	//if distance to this pixel is lower than distance from shadowMap, 
	//then we are not in shadow
	float light = distance * 2.0f< shadowMapDistance ? 1:0;

	//float d = 1.5f * length(TexCoord - 0.5f);
	//float attenuation = pow( saturate(1.0f - d),1.0f);
	 
	 distance /= distanceMod;

	float4 result = light * (1 - (4 * (distance * distance)));
	result.a = 1;
	return result;
}

float4 DrawShadowsLinearAttenuationPS(float2 TexCoord  : TEXCOORD0) : COLOR0
{
	if (distanceMod == 0)
		return 0;

	float2 newCoords = TexCoord - 0.5f;

	// distance of this pixel from the center
	float distance = length(newCoords);

	//distance stored in the shadow map
	float shadowMapDistance;

	//coords in [-1,1]
	float nY = 2.0f*(newCoords.y);
	float nX = 2.0f*(newCoords.x);

	//we use these to determine which quadrant we are in
	if(abs(nY)<abs(nX))
	{
		shadowMapDistance = GetShadowDistanceH(TexCoord,0);
	}
	else
	{
		shadowMapDistance = GetShadowDistanceV(TexCoord,0);
	}

	//if distance to this pixel is lower than distance from shadowMap, 
	//then we are not in shadow
	float light = distance * 2.0f < shadowMapDistance ? 1:0;

	float4 result = light;
	result.a = 1 - ((distance * 2) / distanceMod);
	return result;
}

static const float minBlur = 0.0f;
static const float maxBlur = 5.0f;
static const int g_cKernelSize_High = 13;
static const float2 OffsetAndWeight_High[g_cKernelSize_High] =
{
	{ -6, 0.002216 },
	{ -5, 0.008764 },
	{ -4, 0.026995 },
	{ -3, 0.064759 },
	{ -2, 0.120985 },
	{ -1, 0.176033 },
	{  0, 0.199471 },
	{  1, 0.176033 },
	{  2, 0.120985 },
	{  3, 0.064759 },
	{  4, 0.026995 },
	{  5, 0.008764 },
	{  6, 0.002216 },
};

static const int g_cKernelSize_Mid = 9;
static const float2 OffsetAndWeight_Mid[g_cKernelSize_Mid] =
{
	{ -4, 0.01 },
	{ -3, 0.02 },
	{ -2, 0.05 },
	{ -1, 0.1525 },
	{  0, 0.525 },
	{  1, 0.1525 },
	{  2, 0.05 },
	{  3, 0.02 },
	{  4, 0.01 },
};


static const int g_cKernelSize_Low = 5;
static const float2 OffsetAndWeight_Low[g_cKernelSize_Low] =
{
	{ -2, 0.05 },
	{ -1, 0.1 },
	{  0, 0.7 },
	{  1, 0.1 },
	{  2, 0.05 },
};

float4 BlurHorizontallyLowPS(float2 TexCoord  : TEXCOORD0) : COLOR0
{
	  float sum=0;
	  float distance = tex2D( inputSampler, TexCoord).b * 0.5f;

	  for (int i = 0; i < g_cKernelSize_Low; i++)
	  {    
		sum += tex2D( inputSampler, TexCoord + OffsetAndWeight_Low[i].x * lerp(minBlur, maxBlur , distance)/renderTargetSize.x * float2(1,0) ).r * OffsetAndWeight_Low[i].y;
	  }
	  
	  float4 result = sum;
	  result.b = distance * 2;
	  result.a = 1;
	  return result;
}

float4 BlurHorizontallyMidPS(float2 TexCoord  : TEXCOORD0) : COLOR0
{
	  float sum=0;
	  float distance = tex2D( inputSampler, TexCoord).b * 0.5f;
	  
	  for (int i = 0; i < g_cKernelSize_Mid; i++)
	  {    
		sum += tex2D( inputSampler, TexCoord + OffsetAndWeight_Mid[i].x * lerp(minBlur, maxBlur , distance)/renderTargetSize.x * float2(1,0) ).r * OffsetAndWeight_Mid[i].y;
	  }
	  
	  float4 result = sum;
	  result.b = distance * 2;
	  result.a = 1;
	  return result;
}

float4 BlurHorizontallyHighPS(float2 TexCoord  : TEXCOORD0) : COLOR0
{
	float sum=0;
	float distance = tex2D( inputSampler, TexCoord).b * 0.5f;

	for (int i = 0; i < g_cKernelSize_High; i++)
		sum += tex2D( inputSampler, TexCoord + OffsetAndWeight_High[i].x * lerp(minBlur, maxBlur , distance)/renderTargetSize.x * float2(1,0) ).r * OffsetAndWeight_High[i].y;


	float4 result = sum;
	result.b = distance * 2;
	result.a = 1;
	return result;
}

float4 BlurVerticallyLowNoAttenuationPS(float2 TexCoord  : TEXCOORD0) : COLOR0
{
	float sum=0;
	float distance = tex2D( inputSampler, TexCoord).b * 0.5f;

	for (int i = 0; i < g_cKernelSize_Low; i++)
		sum += tex2D( inputSampler, TexCoord + OffsetAndWeight_Low[i].x * lerp(minBlur, maxBlur , distance)/renderTargetSize.x * float2(0,1) ).r * OffsetAndWeight_Low[i].y;

	float4 result = sum;
	result.a = 1;
	return result;
}

float4 BlurVerticallyMidNoAttenuationPS(float2 TexCoord  : TEXCOORD0) : COLOR0
{
	float sum=0;
	float distance = tex2D( inputSampler, TexCoord).b * 0.5f;

	for (int i = 0; i < g_cKernelSize_Mid; i++)
		sum += tex2D( inputSampler, TexCoord + OffsetAndWeight_Mid[i].x * lerp(minBlur, maxBlur , distance)/renderTargetSize.x * float2(0,1) ).r * OffsetAndWeight_Mid[i].y;

	float4 result = sum;
	result.a = 1;
	return result;
}

float4 BlurVerticallyHighNoAttenuationPS(float2 TexCoord  : TEXCOORD0) : COLOR0
{
	float sum=0;
	float distance = tex2D( inputSampler, TexCoord).b * 0.5f;

	for (int i = 0; i < g_cKernelSize_High; i++)
		sum += tex2D( inputSampler, TexCoord + OffsetAndWeight_High[i].x * lerp(minBlur, maxBlur , distance)/renderTargetSize.x * float2(0,1) ).r * OffsetAndWeight_High[i].y;

	float4 result = sum;
	result.a = 1;
	return result;
}

float4 BlurVerticallyLowLinearAttenuationPS(float2 TexCoord  : TEXCOORD0) : COLOR0
{
	if (distanceMod == 0)
		return 0;

	float sum=0;
	float distance = tex2D( inputSampler, TexCoord).b * 0.5f;

	if (distance >= 0.5f)
		return 0;

	for (int i = 0; i < g_cKernelSize_Low; i++)
		sum += tex2D( inputSampler, TexCoord + OffsetAndWeight_Low[i].x * lerp(minBlur, maxBlur , distance)/renderTargetSize.x * float2(0,1) ).r * OffsetAndWeight_Low[i].y;

	float4 result = sum;
	result.a = 1 - ((distance * 2) / distanceMod);
	return result;
}

float4 BlurVerticallyMidLinearAttenuationPS(float2 TexCoord  : TEXCOORD0) : COLOR0
{
	if (distanceMod == 0)
		return 0;

	float sum=0;
	float distance = tex2D( inputSampler, TexCoord).b * 0.5f;

	if (distance >= 0.5f)
		return 0;

	for (int i = 0; i < g_cKernelSize_Mid; i++)
		sum += tex2D( inputSampler, TexCoord + OffsetAndWeight_Mid[i].x * lerp(minBlur, maxBlur , distance)/renderTargetSize.x * float2(0,1) ).r * OffsetAndWeight_Mid[i].y;

	float4 result = sum;
	result.a = 1 - ((distance * 2) / distanceMod);
	return result;
}

float4 BlurVerticallyHighLinearAttenuationPS(float2 TexCoord  : TEXCOORD0) : COLOR0
{
	if (distanceMod == 0)
		return 0;

	float sum=0;
	float distance = tex2D( inputSampler, TexCoord).b * 0.5f;

	if (distance >= 0.5f)
		return 0;

	for (int i = 0; i < g_cKernelSize_High; i++)
		sum += tex2D( inputSampler, TexCoord + OffsetAndWeight_High[i].x * lerp(minBlur, maxBlur , distance)/renderTargetSize.x * float2(0,1) ).r * OffsetAndWeight_High[i].y;

	float4 result = sum;
	result.a = 1 - ((distance * 2) / distanceMod);
	return result;
}

float4 BlurVerticallyLowCurveAttenuationPS(float2 TexCoord  : TEXCOORD0) : COLOR0
{
	if (distanceMod == 0)
		return 0;

	float sum=0;
	float distance = tex2D( inputSampler, TexCoord).b * 0.5f;

	for (int i = 0; i < g_cKernelSize_Low; i++)
		sum += tex2D( inputSampler, TexCoord + OffsetAndWeight_Low[i].x * lerp(minBlur, maxBlur , distance)/renderTargetSize.x * float2(0,1) ).r * OffsetAndWeight_Low[i].y;

	distance /= distanceMod;

	float4 result = sum * (1 - (4 * (distance * distance)));
	result.a = 1;
	return result;
}

float4 BlurVerticallyMidCurveAttenuationPS(float2 TexCoord  : TEXCOORD0) : COLOR0
{
	if (distanceMod == 0)
		return 0;

	float sum=0;
	float distance = tex2D( inputSampler, TexCoord).b * 0.5f;

	for (int i = 0; i < g_cKernelSize_Mid; i++)
		sum += tex2D( inputSampler, TexCoord + OffsetAndWeight_Mid[i].x * lerp(minBlur, maxBlur , distance)/renderTargetSize.x * float2(0,1) ).r * OffsetAndWeight_Mid[i].y;

	 distance /= distanceMod;

	float4 result = sum * (1 - (4 * (distance * distance)));
	result.a = 1;
	return result;
}

float4 BlurVerticallyHighCurveAttenuationPS(float2 TexCoord  : TEXCOORD0) : COLOR0
{
	if (distanceMod == 0)
		return 0;

	float sum=0;
	float distance = tex2D( inputSampler, TexCoord).b * 0.5f;
	
	for (int i = 0; i < g_cKernelSize_High; i++)
		sum += tex2D( inputSampler, TexCoord + OffsetAndWeight_High[i].x * lerp(minBlur, maxBlur , distance)/renderTargetSize.x * float2(0,1) ).r * OffsetAndWeight_High[i].y;

	distance /= distanceMod;

	float4 result = sum * (1 - (4 * (distance * distance)));
	result.a = 1;
	return result;
}

technique DrawShadowsNoAttenuation
{
	pass P0
	{
		VertexShader = compile vs_2_0 FullScreenVS();
		PixelShader  = compile ps_2_0 DrawShadowsNoAttenuationPS();
	}
}

technique DrawShadowsNoAttenuationPreBlur
{
	pass P0
	{
		VertexShader = compile vs_2_0 FullScreenVS();
		PixelShader  = compile ps_2_0 DrawShadowsNoAttenuationPreBlurPS();
	}
}

technique DrawShadowsLinearAttenuation
{
	pass P0
	{
		VertexShader = compile vs_2_0 FullScreenVS();
		PixelShader  = compile ps_2_0 DrawShadowsLinearAttenuationPS();
	}
}

technique DrawShadowsCurveAttenuation
{
	pass P0
	{
		VertexShader = compile vs_2_0 FullScreenVS();
		PixelShader  = compile ps_2_0 DrawShadowsCurveAttenuationPS();
	}
}

technique BlurHorizontallyLow
{
	pass P0
	{
		VertexShader = compile vs_2_0 FullScreenVS();
		PixelShader  = compile ps_2_0 BlurHorizontallyLowPS();
	}
}

technique BlurHorizontallyMid
{
	pass P0
	{
		VertexShader = compile vs_2_0 FullScreenVS();
		PixelShader  = compile ps_2_0 BlurHorizontallyMidPS();
	}
}

technique BlurHorizontallyHigh
{
	pass P0
	{
		VertexShader = compile vs_2_0 FullScreenVS();
		PixelShader  = compile ps_2_0 BlurHorizontallyHighPS();
	}
}

technique BlurVerticallyLowNoAttenuation
{
	pass P0
	{
		VertexShader = compile vs_2_0 FullScreenVS();
		PixelShader  = compile ps_2_0 BlurVerticallyLowNoAttenuationPS();
	}
}

technique BlurVerticallyMidNoAttenuation
{
	pass P0
	{
		VertexShader = compile vs_2_0 FullScreenVS();
		PixelShader  = compile ps_2_0 BlurVerticallyMidNoAttenuationPS();
	}
}

technique BlurVerticallyHighNoAttenuation
{
	pass P0
	{
		VertexShader = compile vs_2_0 FullScreenVS();
		PixelShader  = compile ps_2_0 BlurVerticallyHighNoAttenuationPS();
	}
}

technique BlurVerticallyLowLinearAttenuation
{
	pass P0
	{
		VertexShader = compile vs_2_0 FullScreenVS();
		PixelShader  = compile ps_2_0 BlurVerticallyLowLinearAttenuationPS();
	}
}

technique BlurVerticallyMidLinearAttenuation
{
	pass P0
	{
		VertexShader = compile vs_2_0 FullScreenVS();
		PixelShader  = compile ps_2_0 BlurVerticallyMidLinearAttenuationPS();
	}
}

technique BlurVerticallyHighLinearAttenuation
{
	pass P0
	{
		VertexShader = compile vs_2_0 FullScreenVS();
		PixelShader  = compile ps_2_0 BlurVerticallyHighLinearAttenuationPS();
	}
}

technique BlurVerticallyLowCurveAttenuation
{
	pass P0
	{
		VertexShader = compile vs_2_0 FullScreenVS();
		PixelShader  = compile ps_2_0 BlurVerticallyLowCurveAttenuationPS();
	}
}

technique BlurVerticallyMidCurveAttenuation
{
	pass P0
	{
		VertexShader = compile vs_2_0 FullScreenVS();
		PixelShader  = compile ps_2_0 BlurVerticallyMidCurveAttenuationPS();
	}
}

technique BlurVerticallyHighCurveAttenuation
{
	pass P0
	{
		VertexShader = compile vs_2_0 FullScreenVS();
		PixelShader  = compile ps_2_0 BlurVerticallyHighCurveAttenuationPS();
	}
}

technique DistortAndComputeDistances
{
	pass P0
	{
		VertexShader = compile vs_2_0 FullScreenVS();
		PixelShader  = compile ps_2_0 DistortAndComputeDistancesPS();
	}
}