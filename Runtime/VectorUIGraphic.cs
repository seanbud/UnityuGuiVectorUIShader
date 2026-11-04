using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace VectorUI
{
    /// <summary>
    /// Design-grade UGUI graphic that renders vector shapes with SDF.
    /// Drop this on any GameObject with a RectTransform to get rounded corners,
    /// gradients, strokes, drop shadows, and inner shadows — all in one draw call.
    /// </summary>
    [AddComponentMenu("UI/VectorUI Graphic")]
    [RequireComponent(typeof(RectTransform))]
    [ExecuteAlways]
    public class VectorUIGraphic : MaskableGraphic
    {
        // ═══════════════════════════════════════════════
        //  ENUMS
        // ═══════════════════════════════════════════════

        public enum ShapeType
        {
            Rectangle,
            Ellipse,
            Star
        }

        public enum FillStyle
        {
            Solid,
            LinearGradient,
            RadialGradient
        }

        public enum ImageScaleMode
        {
            Fill,
            Fit,
            Stretch,
            Tile
        }

        // ═══════════════════════════════════════════════
        //  SERIALIZED FIELDS
        // ═══════════════════════════════════════════════

        [Header("Shape")]
        [SerializeField] private ShapeType m_Shape = ShapeType.Rectangle;
        [SerializeField] private Color m_FillColor = Color.white;
        [SerializeField] private Vector4 m_CornerRadius;
        [SerializeField, Range(0.01f, 4f)] private float m_SdfSmooth = 0.5f;

        [Header("Texture")]
        [SerializeField] private Sprite m_Sprite;
        [SerializeField] private ImageScaleMode m_ImageScaleMode = ImageScaleMode.Fill;

        [Header("Fill")]
        [SerializeField] private FillStyle m_Fill = FillStyle.Solid;
        [SerializeField] private Gradient m_FillGradient;
        [SerializeField] private Vector2[] m_GradientHandlePositions = {
            new(0, 0.5f), new(1, 0.5f), new(0.5f, 0)
        };

        [Header("Stroke")]
        [SerializeField] private Color m_StrokeColor = new Color(0, 0, 0, 1);
        [SerializeField] private float m_StrokeWidth;

        [Header("Drop Shadow")]
        [SerializeField] private bool m_EnableDropShadow;
        [SerializeField] private Color m_ShadowColor = new Color(0, 0, 0, 0.25f);
        [SerializeField] private Vector2 m_ShadowOffset = new Vector2(0, -4);
        [SerializeField, Min(0)] private float m_ShadowBlur = 8;
        [SerializeField, Min(0)] private float m_ShadowSpread;

        [Header("Inner Shadow")]
        [SerializeField] private bool m_EnableInnerShadow;
        [SerializeField] private Color m_InnerShadowColor = new Color(0, 0, 0, 0.25f);
        [SerializeField] private Vector2 m_InnerShadowOffset = new Vector2(0, -2);
        [SerializeField, Min(0)] private float m_InnerShadowBlur = 4;
        [SerializeField, Min(0)] private float m_InnerShadowSpread;

        [Header("Arc (Ellipse Only)")]
        [SerializeField] private float m_EllipseInnerRadius = 1;
        [SerializeField] private Vector2 m_EllipseArcAngleRange = new Vector2(0, Mathf.PI * 2f);

        // ═══════════════════════════════════════════════
        //  SHADER PROPERTY IDS
        // ═══════════════════════════════════════════════

        private static readonly int s_FillColor = Shader.PropertyToID("_FillColor");
        private static readonly int s_CornerRadius = Shader.PropertyToID("_CornerRadius");
        private static readonly int s_SdfSmooth = Shader.PropertyToID("_SdfSmooth");
        private static readonly int s_StrokeColor = Shader.PropertyToID("_StrokeColor");
        private static readonly int s_StrokeWidth = Shader.PropertyToID("_StrokeWidth");
        private static readonly int s_GradientColors = Shader.PropertyToID("_GradientColors");
        private static readonly int s_GradientStops = Shader.PropertyToID("_GradientStops");
        private static readonly int s_GradientHandlePositions = Shader.PropertyToID("_GradientHandlePositions");
        private static readonly int s_GradientNumStops = Shader.PropertyToID("_GradientNumStops");
        private static readonly int s_ArcAngleRange = Shader.PropertyToID("_ArcAngleRangeInnerRadius");
        private static readonly int s_ShadowColor = Shader.PropertyToID("_ShadowColor");
        private static readonly int s_ShadowOffset = Shader.PropertyToID("_ShadowOffset");
        private static readonly int s_ShadowBlur = Shader.PropertyToID("_ShadowBlur");
        private static readonly int s_ShadowSpread = Shader.PropertyToID("_ShadowSpread");
        private static readonly int s_InnerShadowColor = Shader.PropertyToID("_InnerShadowColor");
        private static readonly int s_InnerShadowOffset = Shader.PropertyToID("_InnerShadowOffset");
        private static readonly int s_InnerShadowBlur = Shader.PropertyToID("_InnerShadowBlur");
        private static readonly int s_InnerShadowSpread = Shader.PropertyToID("_InnerShadowSpread");
        private static readonly int s_MainTex = Shader.PropertyToID("_MainTex");

        private const string SHADER_NAME = "VectorUI/Shape";
        private const int MAX_GRADIENT_STOPS = 16;

        private Material m_DynamicMaterial;
        private bool m_CanvasChannelsVerified;

        // ═══════════════════════════════════════════════
        //  PUBLIC API
        // ═══════════════════════════════════════════════

        public ShapeType Shape       { get => m_Shape;     set { m_Shape = value; SetAllDirty(); } }
        public Color FillColor       { get => m_FillColor; set { m_FillColor = value; SetMaterialDirty(); } }
        public Vector4 CornerRadius  { get => m_CornerRadius; set { m_CornerRadius = value; SetMaterialDirty(); } }
        public float SdfSmooth       { get => m_SdfSmooth; set { m_SdfSmooth = value; SetMaterialDirty(); } }
        public FillStyle Fill        { get => m_Fill;      set { m_Fill = value; SetMaterialDirty(); } }
        public Gradient FillGradient { get => m_FillGradient; set { m_FillGradient = value; SetMaterialDirty(); } }
        public Vector2[] GradientHandlePositions { get => m_GradientHandlePositions; set { m_GradientHandlePositions = value; SetMaterialDirty(); } }

        public Color StrokeColor     { get => m_StrokeColor; set { m_StrokeColor = value; SetMaterialDirty(); } }
        public float StrokeWidth     { get => m_StrokeWidth; set { m_StrokeWidth = value; SetMaterialDirty(); } }

        public bool EnableDropShadow { get => m_EnableDropShadow; set { m_EnableDropShadow = value; SetAllDirty(); } }
        public Color ShadowColor     { get => m_ShadowColor; set { m_ShadowColor = value; SetMaterialDirty(); } }
        public Vector2 ShadowOffset  { get => m_ShadowOffset; set { m_ShadowOffset = value; SetMaterialDirty(); } }
        public float ShadowBlur      { get => m_ShadowBlur; set { m_ShadowBlur = value; SetAllDirty(); } }
        public float ShadowSpread    { get => m_ShadowSpread; set { m_ShadowSpread = value; SetAllDirty(); } }

        public bool EnableInnerShadow   { get => m_EnableInnerShadow; set { m_EnableInnerShadow = value; SetAllDirty(); } }
        public Color InnerShadowColor   { get => m_InnerShadowColor; set { m_InnerShadowColor = value; SetMaterialDirty(); } }
        public Vector2 InnerShadowOffset { get => m_InnerShadowOffset; set { m_InnerShadowOffset = value; SetMaterialDirty(); } }
        public float InnerShadowBlur     { get => m_InnerShadowBlur; set { m_InnerShadowBlur = value; SetMaterialDirty(); } }
        public float InnerShadowSpread   { get => m_InnerShadowSpread; set { m_InnerShadowSpread = value; SetMaterialDirty(); } }

        public Sprite Sprite
        {
            get => m_Sprite;
            set { m_Sprite = value; SetAllDirty(); }
        }

        public ImageScaleMode ScaleMode
        {
            get => m_ImageScaleMode;
            set { m_ImageScaleMode = value; SetAllDirty(); }
        }

        public float EllipseInnerRadius
        {
            get => m_EllipseInnerRadius;
            set { m_EllipseInnerRadius = value; SetMaterialDirty(); }
        }

        public Vector2 EllipseArcAngleRange
        {
            get => m_EllipseArcAngleRange;
            set { m_EllipseArcAngleRange = value; SetMaterialDirty(); }
        }

        // ═══════════════════════════════════════════════
        //  TEXTURE
        // ═══════════════════════════════════════════════

        public override Texture mainTexture
        {
            get
            {
                if (m_Sprite != null && m_Sprite.texture != null)
                    return m_Sprite.texture;
                return s_WhiteTexture;
            }
        }

        // ═══════════════════════════════════════════════
        //  LIFECYCLE
        // ═══════════════════════════════════════════════

        protected override void OnEnable()
        {
            EnsureCanvasChannels();
            base.OnEnable();
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            SetAllDirty();
        }
#endif

        protected override void OnDestroy()
        {
            if (m_DynamicMaterial != null)
            {
                #if UNITY_EDITOR
                    DestroyImmediate(m_DynamicMaterial);
                #else
                    Destroy(m_DynamicMaterial);
                #endif
            }
            base.OnDestroy();
        }

        // ═══════════════════════════════════════════════
        //  MATERIAL
        // ═══════════════════════════════════════════════

        public override Material material
        {
            get
            {
                if (m_DynamicMaterial == null)
                    m_DynamicMaterial = new Material(Shader.Find(SHADER_NAME));
                return m_DynamicMaterial;
            }
        }

        public override Material GetModifiedMaterial(Material baseMaterial)
        {
            var mat = base.GetModifiedMaterial(baseMaterial);
            if (mat == null) return mat;

            var shader = mat.shader;

            // ── Fill ──
            mat.SetColor(s_FillColor, m_FillColor);
            mat.SetFloat(s_SdfSmooth, m_SdfSmooth);
            mat.SetVector(s_CornerRadius,
                new Vector4(m_CornerRadius.y, m_CornerRadius.z, m_CornerRadius.x, m_CornerRadius.w));

            // ── Shape keywords ──
            SetKeyword(mat, shader, "SHAPE_RECTANGLE", m_Shape == ShapeType.Rectangle);
            SetKeyword(mat, shader, "SHAPE_ELLIPSE",   m_Shape == ShapeType.Ellipse);
            SetKeyword(mat, shader, "SHAPE_STAR",       m_Shape == ShapeType.Star);

            // ── Stroke ──
            SetKeyword(mat, shader, "STROKE", m_StrokeWidth > 0);
            mat.SetColor(s_StrokeColor, m_StrokeColor);
            mat.SetFloat(s_StrokeWidth, m_StrokeWidth);

            // ── Gradient ──
            SetKeyword(mat, shader, "LINEAR_GRADIENT", m_Fill == FillStyle.LinearGradient);
            SetKeyword(mat, shader, "RADIAL_GRADIENT", m_Fill == FillStyle.RadialGradient);
            if (m_Fill != FillStyle.Solid && m_FillGradient != null)
                SetGradientProperties(mat);

            // ── Arc ──
            SetKeyword(mat, shader, "ARC_ANGLE_RANGE",
                m_EllipseArcAngleRange.y < Mathf.PI * 2f);
            mat.SetVector(s_ArcAngleRange,
                new Vector4(m_EllipseArcAngleRange.x, m_EllipseArcAngleRange.y, m_EllipseInnerRadius, 0));

            // ── Texture clamping ──
            SetKeyword(mat, shader, "CLAMP_TEXTURE", m_ImageScaleMode == ImageScaleMode.Fit);

            // ── Drop Shadow ──
            SetKeyword(mat, shader, "DROP_SHADOW", m_EnableDropShadow);
            if (m_EnableDropShadow)
            {
                mat.SetColor(s_ShadowColor, m_ShadowColor);
                mat.SetVector(s_ShadowOffset, new Vector4(m_ShadowOffset.x, m_ShadowOffset.y, 0, 0));
                mat.SetFloat(s_ShadowBlur, m_ShadowBlur);
                mat.SetFloat(s_ShadowSpread, m_ShadowSpread);
            }

            // ── Inner Shadow ──
            SetKeyword(mat, shader, "INNER_SHADOW", m_EnableInnerShadow);
            if (m_EnableInnerShadow)
            {
                mat.SetColor(s_InnerShadowColor, m_InnerShadowColor);
                mat.SetVector(s_InnerShadowOffset, new Vector4(m_InnerShadowOffset.x, m_InnerShadowOffset.y, 0, 0));
                mat.SetFloat(s_InnerShadowBlur, m_InnerShadowBlur);
                mat.SetFloat(s_InnerShadowSpread, m_InnerShadowSpread);
            }

            // ── Texture ──
            if (m_Sprite != null && m_Sprite.texture != null)
                mat.SetTexture(s_MainTex, m_Sprite.texture);

            return mat;
        }

        // ═══════════════════════════════════════════════
        //  MESH GENERATION
        // ═══════════════════════════════════════════════

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            EnsureCanvasChannels();

            var r = GetPixelAdjustedRect();
            var rectSize = new Vector2(r.width, r.height);

            // Ellipse SDF edge case
            if (m_Shape == ShapeType.Ellipse && Mathf.Abs(r.width - r.height) < 0.001f)
                rectSize.x += 0.001f;

            // Calculate mesh expansion for drop shadow
            float expand = 0;
            if (m_EnableDropShadow)
            {
                expand = m_ShadowBlur + m_ShadowSpread
                       + Mathf.Max(Mathf.Abs(m_ShadowOffset.x), Mathf.Abs(m_ShadowOffset.y));
            }

            var v = new Vector4(
                r.x - expand,
                r.y - expand,
                r.x + r.width + expand,
                r.y + r.height + expand
            );

            // Compute normalised position accounting for expansion
            float totalW = r.width + 2 * expand;
            float totalH = r.height + 2 * expand;
            float normLeft   = -expand / totalW;
            float normBottom = -expand / totalH;
            float normRight  = (r.width + expand) / totalW;
            float normTop    = (r.height + expand) / totalH;

            // Compute tex coords
            Vector2[] uv = ComputeTexCoords(r);

            Color32 c32 = color;
            vh.Clear();

            // 4 verts: BL, TL, TR, BR
            // TEXCOORD1 = (normalised_x, normalised_y, rectWidth, rectHeight)
            vh.AddVert(new Vector3(v.x, v.y), c32, uv[0],
                new Vector4(normLeft, normBottom, rectSize.x, rectSize.y), Vector3.zero, Vector4.zero);
            vh.AddVert(new Vector3(v.x, v.w), c32, uv[1],
                new Vector4(normLeft, normTop, rectSize.x, rectSize.y), Vector3.zero, Vector4.zero);
            vh.AddVert(new Vector3(v.z, v.w), c32, uv[2],
                new Vector4(normRight, normTop, rectSize.x, rectSize.y), Vector3.zero, Vector4.zero);
            vh.AddVert(new Vector3(v.z, v.y), c32, uv[3],
                new Vector4(normRight, normBottom, rectSize.x, rectSize.y), Vector3.zero, Vector4.zero);

            vh.AddTriangle(0, 1, 2);
            vh.AddTriangle(2, 3, 0);
        }

        // ═══════════════════════════════════════════════
        //  HELPERS
        // ═══════════════════════════════════════════════

        private Vector2[] ComputeTexCoords(Rect r)
        {
            if (m_Sprite == null)
            {
                return new[]
                {
                    new Vector2(0, 0), new Vector2(0, 1),
                    new Vector2(1, 1), new Vector2(1, 0)
                };
            }

            switch (m_ImageScaleMode)
            {
                case ImageScaleMode.Tile:
                    var tileFactor = new Vector2(
                        r.width / m_Sprite.rect.width,
                        r.height / m_Sprite.rect.height);
                    return new[]
                    {
                        new Vector2(0, 1 - tileFactor.y), new Vector2(0, 1),
                        new Vector2(tileFactor.x, 1), new Vector2(tileFactor.x, 1 - tileFactor.y)
                    };

                case ImageScaleMode.Fill:
                {
                    float rW = m_Sprite.rect.width / r.width;
                    float rH = m_Sprite.rect.height / r.height;
                    if (rW < rH)
                    {
                        float ideal = r.width * (m_Sprite.rect.height / m_Sprite.rect.width);
                        float vo = 0.5f * (r.height / ideal);
                        return new[]
                        {
                            new Vector2(0, 0.5f - vo), new Vector2(0, 0.5f + vo),
                            new Vector2(1, 0.5f + vo), new Vector2(1, 0.5f - vo)
                        };
                    }
                    else
                    {
                        float ideal = r.height * (m_Sprite.rect.width / m_Sprite.rect.height);
                        float uo = 0.5f * (r.width / ideal);
                        return new[]
                        {
                            new Vector2(0.5f - uo, 0), new Vector2(0.5f - uo, 1),
                            new Vector2(0.5f + uo, 1), new Vector2(0.5f + uo, 0)
                        };
                    }
                }

                case ImageScaleMode.Fit:
                {
                    float rW = m_Sprite.rect.width / r.width;
                    float rH = m_Sprite.rect.height / r.height;
                    if (rW < rH)
                    {
                        float ideal = r.height * (m_Sprite.rect.width / m_Sprite.rect.height);
                        float uo = 0.5f * (r.width / ideal);
                        return new[]
                        {
                            new Vector2(0.5f - uo, 0), new Vector2(0.5f - uo, 1),
                            new Vector2(0.5f + uo, 1), new Vector2(0.5f + uo, 0)
                        };
                    }
                    else
                    {
                        float ideal = r.width * (m_Sprite.rect.height / m_Sprite.rect.width);
                        float vo = 0.5f * (r.height / ideal);
                        return new[]
                        {
                            new Vector2(0, 0.5f - vo), new Vector2(0, 0.5f + vo),
                            new Vector2(1, 0.5f + vo), new Vector2(1, 0.5f - vo)
                        };
                    }
                }

                default: // Stretch
                    return new[]
                    {
                        new Vector2(0, 0), new Vector2(0, 1),
                        new Vector2(1, 1), new Vector2(1, 0)
                    };
            }
        }

        private void SetGradientProperties(Material mat)
        {
            if (m_FillGradient == null) return;
            int count = m_FillGradient.colorKeys.Length;
            var colors = new Color[MAX_GRADIENT_STOPS];
            var stops  = new float[MAX_GRADIENT_STOPS];
            for (int i = 0; i < count; i++)
            {
                colors[i] = m_FillGradient.colorKeys[i].color;
                stops[i]  = m_FillGradient.colorKeys[i].time;
                colors[i].a = m_FillGradient.Evaluate(stops[i]).a;
            }
            mat.SetColorArray(s_GradientColors, colors);
            mat.SetFloatArray(s_GradientStops, stops);
            mat.SetFloat(s_GradientNumStops, count);
            mat.SetFloatArray(s_GradientHandlePositions, FlattenVec2Array(m_GradientHandlePositions));
        }

        private static float[] FlattenVec2Array(Vector2[] arr)
        {
            var flat = new float[arr.Length * 2];
            for (int i = 0; i < arr.Length; i++)
            {
                flat[i * 2]     = arr[i].x;
                flat[i * 2 + 1] = arr[i].y;
            }
            return flat;
        }

        private static void SetKeyword(Material mat, Shader shader, string keyword, bool enabled)
        {
            mat.SetKeyword(new LocalKeyword(shader, keyword), enabled);
        }

        private void EnsureCanvasChannels()
        {
            if (m_CanvasChannelsVerified || canvas == null) return;
            canvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord1;
            m_CanvasChannelsVerified = true;
        }
    }
}
