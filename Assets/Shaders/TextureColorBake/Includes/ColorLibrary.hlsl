#ifndef COLOR_LIBRARY_INCLUDED
#define COLOR_LIBRARY_INCLUDED

#ifndef PI
#define PI 3.14159265359
#endif

// sRGB to Linear conversion
float3 ConvertSrgbToLinear(float3 srgbColor)
{
    bool3 cutoff = srgbColor <= 0.04045;
    float3 lower = srgbColor / 12.92;
    float3 higher = pow(abs(srgbColor + 0.055) / 1.055, 2.4);
    return lerp(higher, lower, cutoff);
}

// Linear to sRGB conversion
float3 ConvertLinearToSrgb(float3 linearColor)
{
    bool3 cutoff = linearColor <= 0.0031308;
    float3 lower = linearColor * 12.92;
    float3 higher = 1.055 * pow(abs(linearColor), 1.0/2.4) - 0.055;
    return lerp(higher, lower, cutoff);
}

// Linear RGB to CIE XYZ D65
float3 ConvertLinearToXyz(float3 linearRgb)
{
    const float3x3 SRGB_TO_XYZ_MATRIX = {
        0.4124, 0.3576, 0.1805,
        0.2126, 0.7152, 0.0722,
        0.0193, 0.1192, 0.9505
    };
    return mul(SRGB_TO_XYZ_MATRIX, linearRgb);
}

// CIE XYZ D65 to CIE LAB
float3 ConvertXyzToLab(float3 xyz)
{
    const float3 D65_WHITE_POINT = float3(0.95047, 1.00000, 1.08883);
    xyz /= D65_WHITE_POINT;

    const float epsilon = 216.0 / 24389.0;
    const float kappa = 24389.0 / 27.0;

    float3 f;
    bool3 isGtEpsilon = xyz > epsilon;
    f.x = isGtEpsilon.x ? pow(xyz.x, 1.0/3.0) : (kappa * xyz.x + 16.0) / 116.0;
    f.y = isGtEpsilon.y ? pow(xyz.y, 1.0/3.0) : (kappa * xyz.y + 16.0) / 116.0;
    f.z = isGtEpsilon.z ? pow(xyz.z, 1.0/3.0) : (kappa * xyz.z + 16.0) / 116.0;

    float L = (116.0 * f.y) - 16.0;
    float a = 500.0 * (f.x - f.y);
    float b = 200.0 * (f.y - f.z);

    return float3(L, a, b);
}

// sRGB to CIE LAB
float3 ConvertSrgbToLab(float3 srgbColor)
{
    float3 linearRgb = ConvertSrgbToLinear(srgbColor);
    float3 xyz = ConvertLinearToXyz(linearRgb);
    return ConvertXyzToLab(xyz);
}

// RGB to HSV
float3 ConvertRgbToHsv(float3 c)
{
    float4 K = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
    float4 p = lerp(float4(c.bg, K.wz), float4(c.gb, K.xy), step(c.b, c.g));
    float4 q = lerp(float4(p.xyw, c.r), float4(c.r, p.yzx), step(p.x, c.r));
    float d = q.x - min(q.w, q.y);
    float e = 1.0e-10;
    return float3(abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);
}

// CIEDE2000 Delta E calculation
float CalculateDeltaE2000(float3 lab1, float3 lab2)
{
    float L1 = lab1.x, a1 = lab1.y, b1 = lab1.z;
    float L2 = lab2.x, a2 = lab2.y, b2 = lab2.z;
    float C1 = sqrt(a1*a1 + b1*b1); float C2 = sqrt(a2*a2 + b2*b2);
    float avgC = (C1 + C2) / 2.0;
    float G = 0.5 * (1.0 - sqrt(pow(avgC, 7.0) / (pow(avgC, 7.0) + pow(25.0, 7.0))));
    float a1_prime = (1.0 + G) * a1; float a2_prime = (1.0 + G) * a2;
    float C1_prime = sqrt(a1_prime*a1_prime + b1*b1); float C2_prime = sqrt(a2_prime*a2_prime + b2*b2);
    float h1_prime_rad = atan2(b1, a1_prime); float h2_prime_rad = atan2(b2, a2_prime);
    if (h1_prime_rad < 0) h1_prime_rad += 2 * PI;
    if (h2_prime_rad < 0) h2_prime_rad += 2 * PI;
    float deltaL_prime = L2 - L1; float deltaC_prime = C2_prime - C1_prime;
    float deltah_prime;
    if (C1_prime * C2_prime == 0) { deltah_prime = 0; }
    else {
        deltah_prime = h2_prime_rad - h1_prime_rad;
        if (abs(deltah_prime) > PI) { deltah_prime -= sign(deltah_prime) * 2 * PI; }
    }
    float deltaH_prime = 2.0 * sqrt(C1_prime * C2_prime) * sin(deltah_prime / 2.0);
    float avgL_prime = (L1 + L2) / 2.0; float avgC_prime = (C1_prime + C2_prime) / 2.0;
    float avgh_prime;
    if (C1_prime * C2_prime == 0) { avgh_prime = h1_prime_rad + h2_prime_rad; }
    else {
        avgh_prime = (h1_prime_rad + h2_prime_rad) / 2.0;
        if(abs(h1_prime_rad - h2_prime_rad) > PI) { avgh_prime -= PI; }
    }
    float T = 1 - 0.17 * cos(avgh_prime - radians(30)) + 0.24 * cos(2 * avgh_prime) + 0.32 * cos(3 * avgh_prime + radians(6)) - 0.20 * cos(4 * avgh_prime - radians(63));
    float deltaTheta_rad = radians(30) * exp(-pow((degrees(avgh_prime) - 275) / 25.0, 2.0));
    float Rc = 2.0 * sqrt(pow(avgC_prime, 7.0) / (pow(avgC_prime, 7.0) + pow(25.0, 7.0)));
    float SL = 1.0 + (0.015 * pow(avgL_prime - 50, 2.0)) / sqrt(20 + pow(avgL_prime - 50, 2.0));
    float SC = 1.0 + 0.045 * avgC_prime; float SH = 1.0 + 0.015 * avgC_prime * T;
    float RT = -sin(2 * deltaTheta_rad) * Rc;
    float termL = deltaL_prime / SL; float termC = deltaC_prime / SC; float termH = deltaH_prime / SH;
    return sqrt(pow(termL, 2.0) + pow(termC, 2.0) + pow(termH, 2.0) + RT * termC * termH);
}

#endif