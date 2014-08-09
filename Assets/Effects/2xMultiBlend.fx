
sampler TextureSampler : register(s0);
sampler LightSampler : register(s1);

float MixFactor = 0.001;
float4 Portion = 0;
float PortionScale = 1;

float4 MultiBlend(float2 texCoord : TEXCOORD0, float4 color : COLOR0) : COLOR0
{
  float4 colorGround = tex2D(TextureSampler, texCoord);
  float4 colorShadow = 0;

   if (Portion[2] == 0 || Portion[3] == 0)
  {
	colorShadow = tex2D(LightSampler, texCoord);
  }
  else
  {
	float2 newCoord = float2(Portion.x + (Portion.z * texCoord.x),Portion.y + (Portion.w * texCoord.y)) * PortionScale;
	colorShadow = tex2D(LightSampler, newCoord);
  }

  float4 resultColor = colorGround * colorShadow;

  return lerp(colorGround, resultColor, MixFactor);
}

technique Basic
{
	pass Pass0
	{
		PixelShader = compile ps_2_0 MultiBlend();
	}
}