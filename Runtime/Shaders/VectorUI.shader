// VectorUI — Design-grade SDF UI shader for Unity UGUI
// Renders rounded rectangles, ellipses, stars with:
//   - Per-corner radii (smooth clamping for any aspect ratio)
//   - Linear & radial gradients (multi-stop, handle-positioned)
//   - Stroke (configurable width, inside/center/outside)
//   - Drop shadow (offset, blur, spread, color) via SDF
//   - Inner shadow (offset, blur, spread, color) via SDF
//   - Arc/pie shapes
//   - Image fill with Fill/Fit/Stretch/Tile modes
//   - Configurable SDF anti-aliasing smoothness
//
// Based on Unity's Default UI Shader and 2D SDF functions from Inigo Quilez
// https://iquilezles.org/articles/distfunctions2d/

Shader "VectorUI/Shape"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15

        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0

        // ── Shape ──
        _CornerRadius ("Corner Radius", Vector) = (0, 0, 0, 0)
        _FillColor ("Fill Color", Color) = (1,1,1,1)
        _SdfSmooth ("SDF Smoothing", Float) = 0.5

        // ── Stroke ──
        _StrokeWidth ("Stroke Width", Float) = 0
        _StrokeColor ("Stroke Color", Color) = (0,0,0,1)

        // ── Drop Shadow ──
        _ShadowColor ("Shadow Color", Color) = (0,0,0,0.25)
        _ShadowOffset ("Shadow Offset", Vector) = (0, -4, 0, 0)
        _ShadowBlur ("Shadow Blur", Float) = 8
        _ShadowSpread ("Shadow Spread", Float) = 0

        // ── Inner Shadow ──
        _InnerShadowColor ("Inner Shadow Color", Color) = (0,0,0,0.25)
        _InnerShadowOffset ("Inner Shadow Offset", Vector) = (0, -2, 0, 0)
        _InnerShadowBlur ("Inner Shadow Blur", Float) = 4
        _InnerShadowSpread ("Inner Shadow Spread", Float) = 0

        // ── Arc ──
        _ArcAngleRangeInnerRadius ("Arc Angle Range/Inner Radius", Vector) = (0, 6.28318, 1, 0)
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "Default"
        CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
            #pragma multi_compile_local _ LINEAR_GRADIENT RADIAL_GRADIENT
            #pragma multi_compile_local _ STROKE
            #pragma multi_compile_local _ SHAPE_RECTANGLE SHAPE_ELLIPSE SHAPE_STAR
            #pragma multi_compile_local _ ARC_ANGLE_RANGE
            #pragma multi_compile_local _ CLAMP_TEXTURE
            #pragma multi_compile_local _ DROP_SHADOW
            #pragma multi_compile_local _ INNER_SHADOW

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                // xy = normalised pos within rect (0..1), zw = rect size in pixels
                float4 normalised_pos_size : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex                : SV_POSITION;
                fixed4 color                 : COLOR;
                float2 texcoord              : TEXCOORD0;
                float4 worldPosition         : TEXCOORD1;
                float4 relative_rt_position  : TEXCOORD2;
                float2 normalised_position   : TEXCOORD3;
                float4 clamped_corner_radius : TEXCOORD4;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            // ── GLSL-style mod ──
            #define mod(x, y) (x - y * floor(x / y))

            // ── Uniforms ──
            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _FillColor;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float4 _MainTex_ST;

            float4 _CornerRadius;
            float4 _StrokeColor;
            float _StrokeWidth;
            float _SdfSmooth;

            // Gradient
            float4 _GradientColors[16];
            float _GradientStops[16];
            float _GradientNumStops;
            float _GradientHandlePositions[6];

            // Arc
            float4 _ArcAngleRangeInnerRadius;

            // Drop shadow
            float4 _ShadowColor;
            float4 _ShadowOffset;
            float _ShadowBlur;
            float _ShadowSpread;

            // Inner shadow
            float4 _InnerShadowColor;
            float4 _InnerShadowOffset;
            float _InnerShadowBlur;
            float _InnerShadowSpread;

            // ═══════════════════════════════════════════════
            //  VERTEX SHADER
            // ═══════════════════════════════════════════════

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);
                OUT.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                OUT.color = v.color;

                #if SHAPE_RECTANGLE
                    // Smooth corner radius clamping for narrow aspect ratios (e.g. pills/bars)
                    float width  = max(1.0, v.normalised_pos_size.z);
                    float height = max(1.0, v.normalised_pos_size.w);
                    float halfH  = height * 0.5;

                    // LEFT side corners
                    float unclampedLT = _CornerRadius.z / max(1.0, (_CornerRadius.x + _CornerRadius.z) / width);
                    float unclampedLB = _CornerRadius.w / max(1.0, (_CornerRadius.y + _CornerRadius.w) / width);
                    float clampedLT   = min(unclampedLT, halfH);
                    float clampedLB   = min(unclampedLB, halfH);

                    // Freeze left corners to half-height when width < height
                    float freezeLerp = saturate((width - 0.5 * halfH) / (0.5 * halfH));
                    float leftTop = lerp(halfH, clampedLT, freezeLerp);
                    float leftBot = lerp(halfH, clampedLB, freezeLerp);

                    // RIGHT side corners
                    float unclampedRT = _CornerRadius.x / max(1.0, (_CornerRadius.x + _CornerRadius.y) / width);
                    float unclampedRB = _CornerRadius.y / max(1.0, (_CornerRadius.x + _CornerRadius.y) / width);
                    float clampedRT   = min(unclampedRT, halfH);
                    float clampedRB   = min(unclampedRB, halfH);

                    // Smoothstep blend right → left as width narrows
                    float blendT = saturate((width - halfH) / halfH);
                    float ease   = blendT * blendT * (3.0 - 2.0 * blendT);
                    float preRT  = lerp(leftTop, clampedRT, ease);
                    float preRB  = lerp(leftBot, clampedRB, ease);

                    // Fully collapse right to left when ultra-narrow
                    float collapseT = saturate(1.0 - (width / halfH));
                    float rightTop  = lerp(preRT, leftTop, collapseT);
                    float rightBot  = lerp(preRB, leftBot, collapseT);

                    OUT.clamped_corner_radius = float4(rightTop, rightBot, leftTop, leftBot);
                #else
                    OUT.clamped_corner_radius = float4(0,0,0,0);
                #endif

                OUT.normalised_position = float2(v.normalised_pos_size.x, v.normalised_pos_size.y);
                OUT.relative_rt_position = float4(
                    v.normalised_pos_size.x * v.normalised_pos_size.z,
                    v.normalised_pos_size.y * v.normalised_pos_size.w,
                    v.normalised_pos_size.z,
                    v.normalised_pos_size.w
                );

                return OUT;
            }

            // ═══════════════════════════════════════════════
            //  SDF FUNCTIONS (Inigo Quilez)
            // ═══════════════════════════════════════════════

            // Rounded rectangle SDF — https://www.shadertoy.com/view/4llXD7
            float sdf_rounded_rect(float4 rel_pos, float4 corner_radius)
            {
                float2 b = float2(rel_pos.z * 0.5, rel_pos.w * 0.5);
                float2 p = rel_pos.xy - b;
                float4 r = corner_radius;
                r.xy = (p.x > 0.0) ? r.xy : r.zw;
                r.x  = (p.y > 0.0) ? r.x  : r.y;
                float2 q = abs(p) - b + r.x;
                return min(max(q.x, q.y), 0.0) + length(max(q, 0.0)) - r.x;
            }

            // Ellipse SDF (fast, slight inaccuracy at edges — fine for fill)
            float sdf_ellipse_fast(float2 norm_pos, float4 rel_pos)
            {
                float2 rectSize = float2(rel_pos.z, rel_pos.w);
                float2 fromCenter = 2.0 * (norm_pos - 0.5);
                float dist = -(1.0 - length(fromCenter));
                return dist * length(rectSize * 0.5);
            }

            // Ellipse SDF (accurate — needed for strokes) — https://www.shadertoy.com/view/4sS3zz
            float sdf_ellipse(float4 rel_pos)
            {
                float2 ab = float2(rel_pos.z * 0.5, rel_pos.w * 0.5);
                float2 p  = rel_pos.xy - ab;
                p = abs(p);
                if (p.x > p.y) { p = p.yx; ab = ab.yx; }

                float l = ab.y*ab.y - ab.x*ab.x;
                float m = ab.x*p.x/l;      float m2 = m*m;
                float n = ab.y*p.y/l;      float n2 = n*n;
                float c = (m2+n2-1.0)/3.0; float c3 = c*c*c;
                float q = c3 + m2*n2*2.0;
                float d = c3 + m2*n2;
                float g = m + m*n2;
                float co;

                if (d < 0.0)
                {
                    float h = acos(q/c3) / 3.0;
                    float s = cos(h);
                    float t = sin(h) * sqrt(3.0);
                    float rx = sqrt(-c*(s + t + 2.0) + m2);
                    float ry = sqrt(-c*(s - t + 2.0) + m2);
                    co = (ry + sign(l)*rx + abs(g)/(rx*ry) - m) / 2.0;
                }
                else
                {
                    float h = 2.0*m*n*sqrt(d);
                    float s = sign(q+h) * pow(abs(q+h), 1.0/3.0);
                    float u = sign(q-h) * pow(abs(q-h), 1.0/3.0);
                    float rx = -s - u - c*4.0 + 2.0*m2;
                    float ry = (s - u) * sqrt(3.0);
                    float rm = sqrt(rx*rx + ry*ry);
                    co = (ry/sqrt(rm-rx) + 2.0*g/rm - m) / 2.0;
                }
                float2 r = ab * float2(co, sqrt(1.0 - co*co));
                return length(r - p) * sign(p.y - r.y);
            }

            // Five-point star SDF — https://iquilezles.org/articles/distfunctions2d/
            float sdf_star(float4 rel_pos)
            {
                float2 ab = float2(rel_pos.z * 0.5, rel_pos.w * 0.5);
                float2 p  = rel_pos.xy - ab;
                float r   = min(ab.x, ab.y);
                float rf  = 0.38;

                const float2 k1 = float2(0.809016994375, -0.587785252292);
                const float2 k2 = float2(-k1.x, k1.y);
                p.x = abs(p.x);
                p -= 2.0 * max(dot(k1, p), 0.0) * k1;
                p -= 2.0 * max(dot(k2, p), 0.0) * k2;
                p.x = abs(p.x);
                p.y -= r;
                float2 ba = rf * float2(-k1.y, k1.x) - float2(0, 1);
                float h = clamp(dot(p, ba) / dot(ba, ba), 0.0, r);
                return length(p - ba * h) * sign(p.y * ba.x - p.x * ba.y);
            }

            // ═══════════════════════════════════════════════
            //  HELPERS
            // ═══════════════════════════════════════════════

            float4 GammaToLinearIfNeeded(float4 c)
            {
                #if UNITY_COLORSPACE_GAMMA
                    return c;
                #else
                    return float4(GammaToLinearSpace(c.rgb), c.a);
                #endif
            }

            float4 GetGradientColor(float t)
            {
                float4 gc = lerp(_GradientColors[0], _GradientColors[1],
                    smoothstep(_GradientStops[0], _GradientStops[1], t));
                for (int i = 1; i < _GradientNumStops - 1; ++i)
                {
                    gc = lerp(gc, _GradientColors[i+1],
                        smoothstep(_GradientStops[i], _GradientStops[i+1], t));
                }
                return GammaToLinearIfNeeded(gc);
            }

            float get_shape_sdf(float4 rel_pos, float4 corner_radius, float2 norm_pos)
            {
                #if SHAPE_ELLIPSE
                    #if STROKE
                        return sdf_ellipse(rel_pos);
                    #else
                        return sdf_ellipse_fast(norm_pos, rel_pos);
                    #endif
                #elif SHAPE_STAR
                    return sdf_star(rel_pos);
                #else
                    return sdf_rounded_rect(rel_pos, corner_radius);
                #endif
            }

            // Soft shadow ramp mapping
            float sdf_shadow_alpha(float sdf_dist, float blur)
            {
                float sigma = max(blur * 0.5, 0.001);
                return saturate(0.5 - sdf_dist / (sigma * 2.5));
            }

            // ═══════════════════════════════════════════════
            //  FRAGMENT SHADER
            // ═══════════════════════════════════════════════

            fixed4 frag(v2f IN) : SV_Target
            {
                const float half_pi = 1.57079632679;

                // ── Compute shape SDF ──
                float sdf = get_shape_sdf(IN.relative_rt_position, IN.clamped_corner_radius, IN.normalised_position);

                // ── Fill color ──
                half4 shapeColor = _FillColor;

                // ── Linear Gradient ──
                #if LINEAR_GRADIENT
                    float2 gp0 = float2(_GradientHandlePositions[0], 1.0 - _GradientHandlePositions[1]);
                    float2 gp1 = float2(_GradientHandlePositions[2], 1.0 - _GradientHandlePositions[3]);
                    float2 gp2 = float2(_GradientHandlePositions[4], 1.0 - _GradientHandlePositions[5]);
                    float2 gDir    = gp1 - gp0;
                    float2 gNormal = gp2 - gp0;

                    float gAngle = half_pi - atan2(-gNormal.y, gNormal.x);
                    float cs = cos(gAngle);
                    float sn = sin(gAngle);
                    float2x2 rotMat = float2x2(cs, -sn, sn, cs);
                    float2 relPos = IN.normalised_position - gp0;
                    float2 rotated = mul(relPos, rotMat);
                    float gradientT = saturate(rotated.x / length(gDir));

                    float4 gradCol = GetGradientColor(gradientT);
                    shapeColor.rgb *= gradCol.rgb;
                    shapeColor.a   *= gradCol.a;
                #endif

                // ── Radial Gradient ──
                #if RADIAL_GRADIENT
                    float2 gp0 = float2(_GradientHandlePositions[0], 1.0 - _GradientHandlePositions[1]);
                    float2 gp1 = float2(_GradientHandlePositions[2], 1.0 - _GradientHandlePositions[3]);
                    float2 gp2 = float2(_GradientHandlePositions[4], 1.0 - _GradientHandlePositions[5]);
                    float2 relPos = IN.normalised_position - gp0;

                    float2 gVec1 = gp1 - gp0;
                    float2 gVec2 = gp2 - gp0;

                    float gAngle1 = half_pi - atan2(-gVec1.y, gVec1.x);
                    float2x2 rotMat1 = float2x2(cos(gAngle1), -sin(gAngle1), sin(gAngle1), cos(gAngle1));
                    float d2 = mul(relPos, rotMat1).x / length(gVec2);

                    float gAngle2 = half_pi - atan2(-gVec2.y, gVec2.x);
                    float2x2 rotMat2 = float2x2(cos(gAngle2), -sin(gAngle2), sin(gAngle2), cos(gAngle2));
                    float d1 = mul(relPos, rotMat2).x / length(gVec1);

                    float gradientT = saturate(length(float2(abs(d1), abs(d2))));
                    float4 gradCol = GetGradientColor(gradientT);
                    shapeColor.rgb *= gradCol.rgb;
                    shapeColor.a   *= gradCol.a;
                #endif

                // ── Texture sampling ──
                half4 texColor = (tex2D(_MainTex, IN.texcoord) + _TextureSampleAdd) * shapeColor;

                #if CLAMP_TEXTURE
                    float withinBounds = 1.0
                        - step(IN.texcoord.x, 0.0) - step(1.0, IN.texcoord.x)
                        - step(IN.texcoord.y, 0.0) - step(1.0, IN.texcoord.y);
                    texColor = lerp(half4(0,0,0,0), texColor, withinBounds);
                #endif

                // ── Shape edge alpha ──
                float shapeAlpha = saturate(-sdf / _SdfSmooth);

                // ── Arc angle range ──
                #if ARC_ANGLE_RANGE
                    float2 fromCenter = IN.normalised_position - 0.5;
                    float angle = atan2(fromCenter.y, fromCenter.x);
                    float withinArc = angle > _ArcAngleRangeInnerRadius.x
                                   && angle < (_ArcAngleRangeInnerRadius.x + _ArcAngleRangeInnerRadius.y);
                    shapeAlpha *= withinArc;
                #endif

                // ── Stroke ──
                #if STROKE
                    float strokeEdge  = sdf + _StrokeWidth;
                    float strokeBlend = saturate(-strokeEdge / _SdfSmooth);
                    texColor = half4(
                        lerp(_StrokeColor.rgb, texColor.rgb, strokeBlend),
                        lerp(_StrokeColor.a,   texColor.a,   strokeBlend)
                    );
                #endif

                texColor.a *= shapeAlpha;

                // ── Inner Shadow ──
                #if INNER_SHADOW
                {
                    float4 offsetPos = IN.relative_rt_position;
                    offsetPos.x -= _InnerShadowOffset.x;
                    offsetPos.y += _InnerShadowOffset.y;

                    float innerSdf = get_shape_sdf(offsetPos, IN.clamped_corner_radius, IN.normalised_position);
                    float innerDist = innerSdf + _InnerShadowSpread;
                    float innerAlpha = sdf_shadow_alpha(-innerDist, _InnerShadowBlur);
                    innerAlpha *= shapeAlpha;
                    texColor.rgb = lerp(texColor.rgb, _InnerShadowColor.rgb, innerAlpha * _InnerShadowColor.a);
                }
                #endif

                // ── Drop Shadow ──
                #if DROP_SHADOW
                {
                    float4 shadowPos = IN.relative_rt_position;
                    shadowPos.x -= _ShadowOffset.x;
                    shadowPos.y += _ShadowOffset.y;

                    float shadowSdf = get_shape_sdf(shadowPos, IN.clamped_corner_radius, IN.normalised_position);
                    float shadowDist = shadowSdf - _ShadowSpread;
                    float shadowAlpha = sdf_shadow_alpha(shadowDist, _ShadowBlur) * _ShadowColor.a;

                    float4 shadowLayer = float4(_ShadowColor.rgb, shadowAlpha);
                    texColor.rgb = texColor.rgb * texColor.a + shadowLayer.rgb * shadowLayer.a * (1.0 - texColor.a);
                    texColor.a   = texColor.a + shadowLayer.a * (1.0 - texColor.a);
                }
                #endif

                // ── Vertex color (CanvasGroup, alpha groups) ──
                texColor *= IN.color;

                // ── UI clipping ──
                #ifdef UNITY_UI_CLIP_RECT
                    texColor.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                    clip(texColor.a - 0.001);
                #endif

                // ── Color space correction ──
                #if UNITY_COLORSPACE_GAMMA
                    return texColor;
                #else
                    return float4(texColor.rgb, LinearToGammaSpace(texColor.a).r);
                #endif
            }

        ENDCG
        }
    }
}
