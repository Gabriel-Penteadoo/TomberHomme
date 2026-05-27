float _BS_Remap(float v, float inMin, float inMax, float outMin, float outMax)
{
    return outMin + (v - inMin) * (outMax - outMin) / (inMax - inMin);
}

// CornerPart  : fraction of the mesh extent occupied by corners [0, 0.5]
//               Must match edge loop placement. e.g. 0.1 = loops at ±0.4
// CornerScale : multiplier on the natural corner size.
//               1.0 = perfect proportion preservation at any scale
//               2.0 = corners twice as large, center shrinks
float NineSliceAxis(float p, float scale, float cornerPart, float cornerScale, float center)
{
    float pc = p - center;

    // Target corner size in object space
    // = natural world size (cornerPart * 1 at scale=1) * CornerScale / current scale
    float b = clamp(cornerPart * cornerScale / max(abs(scale), 0.0001), 0.0, 0.499);
    float cp = clamp(cornerPart, 0.0, 0.499);

    float result;
    if (pc < -0.5 + cp)
        result = _BS_Remap(pc, -0.5, -0.5 + cp, -0.5, -0.5 + b);
    else if (pc > 0.5 - cp)
        result = _BS_Remap(pc, 0.5 - cp, 0.5, 0.5 - b, 0.5);
    else
        result = _BS_Remap(pc, -0.5 + cp, 0.5 - cp, -0.5 + b, 0.5 - b);

    return result + center;
}

void BorderSlicer_float(float3 PositionOS, float3 Scale, float CornerPart, float CornerScale,
                                float3 Center, float3 Filter, out float3 Output)
{
    float3 sliced = float3(
        NineSliceAxis(PositionOS.x, Scale.x, CornerPart, CornerScale, Center.x),
        NineSliceAxis(PositionOS.y, Scale.y, CornerPart, CornerScale, Center.y),
        NineSliceAxis(PositionOS.z, Scale.z, CornerPart, CornerScale, Center.z)
    );

    Output = lerp(PositionOS, sliced, saturate(Filter));
}