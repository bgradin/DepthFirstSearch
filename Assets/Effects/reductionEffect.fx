texture SourceTexture; 
          
sampler inputSampler = sampler_state      
{
    Texture   = <SourceTexture>;
            
    MipFilter = Point;
    MinFilter = Point;
    MagFilter = Point;
            
    AddressU  = Clamp;
    AddressV  = Clamp;
};
   
float2 SourcePixelDimensions;
int ReductionPower = 8;

struct VS_OUTPUT
{
    float4 Pos  : POSITION;
    float2 Tex  : TEXCOORD0;
};

VS_OUTPUT VSNew(
    float3 InPos  : POSITION,
    float2 InTex  : TEXCOORD0)
{
    VS_OUTPUT Out = (VS_OUTPUT)0;

    // transform the position to the screen
	Out.Pos = float4(InPos,1) + float4(-SourcePixelDimensions.x * 3, SourcePixelDimensions.y, 0, 0);
    Out.Tex = InTex;

    return Out;
}


float4 HorizontalReductionPS(float2 TexCoord  : TEXCOORD0) : COLOR0
{
	float2 pickupTexture = float2(TexCoord.x - SourcePixelDimensions.x, TexCoord.y);
	float2 TextureXOffsetAdd = float2(SourcePixelDimensions.x, 0);

	float2 vertical = float2(1,1);
	float2 horizontal = float2(1,1);
	int hardLimit = ReductionPower;

	if (hardLimit > 8)
		hardLimit = 8;

	for (int i = 0; i < hardLimit; i++)
	{
		float4 tex = tex2D(inputSampler, pickupTexture + (TextureXOffsetAdd * i));
		
		if (tex.r < vertical[0])
		{
			vertical[0] = tex.r;
			vertical[1] = tex.g;
		}
		if (tex.b < horizontal[0])
		{
			horizontal[0] = tex.b;
			horizontal[1] = tex.a;
		}
	}

	return float4(vertical, horizontal);
}

technique HorizontalReduction
{
    pass P0
    {          
        VertexShader = compile vs_2_0 VSNew();
        PixelShader  = compile ps_2_0 HorizontalReductionPS();
    }
}