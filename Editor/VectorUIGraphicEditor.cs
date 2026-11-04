using UnityEditor;
using UnityEngine;

namespace VectorUI.Editor
{
    [CustomEditor(typeof(VectorUIGraphic), true)]
    [CanEditMultipleObjects]
    public class VectorUIGraphicEditor : UnityEditor.Editor
    {
        // ── Shape ──
        private SerializedProperty m_Shape;
        private SerializedProperty m_FillColor;
        private SerializedProperty m_CornerRadius;
        private SerializedProperty m_SdfSmooth;

        // ── Texture ──
        private SerializedProperty m_Sprite;
        private SerializedProperty m_ImageScaleMode;

        // ── Fill ──
        private SerializedProperty m_Fill;
        private SerializedProperty m_FillGradient;
        private SerializedProperty m_GradientHandlePositions;

        // ── Stroke ──
        private SerializedProperty m_StrokeColor;
        private SerializedProperty m_StrokeWidth;

        // ── Drop Shadow ──
        private SerializedProperty m_EnableDropShadow;
        private SerializedProperty m_ShadowColor;
        private SerializedProperty m_ShadowOffset;
        private SerializedProperty m_ShadowBlur;
        private SerializedProperty m_ShadowSpread;

        // ── Inner Shadow ──
        private SerializedProperty m_EnableInnerShadow;
        private SerializedProperty m_InnerShadowColor;
        private SerializedProperty m_InnerShadowOffset;
        private SerializedProperty m_InnerShadowBlur;
        private SerializedProperty m_InnerShadowSpread;

        // ── Arc ──
        private SerializedProperty m_EllipseInnerRadius;
        private SerializedProperty m_EllipseArcAngleRange;

        // ── Foldout state ──
        private static bool s_ShowShape = true;
        private static bool s_ShowTexture;
        private static bool s_ShowFill;
        private static bool s_ShowStroke;
        private static bool s_ShowDropShadow;
        private static bool s_ShowInnerShadow;
        private static bool s_ShowArc;

        private void OnEnable()
        {
            m_Shape        = serializedObject.FindProperty("m_Shape");
            m_FillColor    = serializedObject.FindProperty("m_FillColor");
            m_CornerRadius = serializedObject.FindProperty("m_CornerRadius");
            m_SdfSmooth    = serializedObject.FindProperty("m_SdfSmooth");

            m_Sprite         = serializedObject.FindProperty("m_Sprite");
            m_ImageScaleMode = serializedObject.FindProperty("m_ImageScaleMode");

            m_Fill                    = serializedObject.FindProperty("m_Fill");
            m_FillGradient            = serializedObject.FindProperty("m_FillGradient");
            m_GradientHandlePositions = serializedObject.FindProperty("m_GradientHandlePositions");

            m_StrokeColor = serializedObject.FindProperty("m_StrokeColor");
            m_StrokeWidth = serializedObject.FindProperty("m_StrokeWidth");

            m_EnableDropShadow = serializedObject.FindProperty("m_EnableDropShadow");
            m_ShadowColor      = serializedObject.FindProperty("m_ShadowColor");
            m_ShadowOffset     = serializedObject.FindProperty("m_ShadowOffset");
            m_ShadowBlur       = serializedObject.FindProperty("m_ShadowBlur");
            m_ShadowSpread     = serializedObject.FindProperty("m_ShadowSpread");

            m_EnableInnerShadow = serializedObject.FindProperty("m_EnableInnerShadow");
            m_InnerShadowColor  = serializedObject.FindProperty("m_InnerShadowColor");
            m_InnerShadowOffset = serializedObject.FindProperty("m_InnerShadowOffset");
            m_InnerShadowBlur   = serializedObject.FindProperty("m_InnerShadowBlur");
            m_InnerShadowSpread = serializedObject.FindProperty("m_InnerShadowSpread");

            m_EllipseInnerRadius   = serializedObject.FindProperty("m_EllipseInnerRadius");
            m_EllipseArcAngleRange = serializedObject.FindProperty("m_EllipseArcAngleRange");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // ── Shape ──
            s_ShowShape = EditorGUILayout.Foldout(s_ShowShape, "Shape", true, EditorStyles.foldoutHeader);
            if (s_ShowShape)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(m_Shape);
                EditorGUILayout.PropertyField(m_FillColor);
                if (m_Shape.enumValueIndex == (int)VectorUIGraphic.ShapeType.Rectangle)
                    EditorGUILayout.PropertyField(m_CornerRadius, new GUIContent("Corner Radius (TL, TR, BR, BL)"));
                EditorGUILayout.PropertyField(m_SdfSmooth, new GUIContent("Edge Smoothness"));
                EditorGUI.indentLevel--;
            }

            // ── Texture ──
            s_ShowTexture = EditorGUILayout.Foldout(s_ShowTexture, "Texture", true, EditorStyles.foldoutHeader);
            if (s_ShowTexture)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(m_Sprite);
                EditorGUILayout.PropertyField(m_ImageScaleMode, new GUIContent("Scale Mode"));
                EditorGUI.indentLevel--;
            }

            // ── Fill ──
            s_ShowFill = EditorGUILayout.Foldout(s_ShowFill, "Fill", true, EditorStyles.foldoutHeader);
            if (s_ShowFill)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(m_Fill, new GUIContent("Fill Style"));
                if (m_Fill.enumValueIndex != (int)VectorUIGraphic.FillStyle.Solid)
                {
                    EditorGUILayout.PropertyField(m_FillGradient);
                    EditorGUILayout.PropertyField(m_GradientHandlePositions, new GUIContent("Handle Positions"), true);
                }
                EditorGUI.indentLevel--;
            }

            // ── Stroke ──
            s_ShowStroke = EditorGUILayout.Foldout(s_ShowStroke, "Stroke", true, EditorStyles.foldoutHeader);
            if (s_ShowStroke)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(m_StrokeWidth, new GUIContent("Width"));
                if (m_StrokeWidth.floatValue > 0)
                    EditorGUILayout.PropertyField(m_StrokeColor, new GUIContent("Color"));
                EditorGUI.indentLevel--;
            }

            // ── Drop Shadow ──
            s_ShowDropShadow = EditorGUILayout.Foldout(s_ShowDropShadow, "Drop Shadow", true, EditorStyles.foldoutHeader);
            if (s_ShowDropShadow)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(m_EnableDropShadow, new GUIContent("Enabled"));
                if (m_EnableDropShadow.boolValue)
                {
                    EditorGUILayout.PropertyField(m_ShadowColor, new GUIContent("Color"));
                    EditorGUILayout.PropertyField(m_ShadowOffset, new GUIContent("Offset"));
                    EditorGUILayout.PropertyField(m_ShadowBlur, new GUIContent("Blur"));
                    EditorGUILayout.PropertyField(m_ShadowSpread, new GUIContent("Spread"));
                }
                EditorGUI.indentLevel--;
            }

            // ── Inner Shadow ──
            s_ShowInnerShadow = EditorGUILayout.Foldout(s_ShowInnerShadow, "Inner Shadow", true, EditorStyles.foldoutHeader);
            if (s_ShowInnerShadow)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(m_EnableInnerShadow, new GUIContent("Enabled"));
                if (m_EnableInnerShadow.boolValue)
                {
                    EditorGUILayout.PropertyField(m_InnerShadowColor, new GUIContent("Color"));
                    EditorGUILayout.PropertyField(m_InnerShadowOffset, new GUIContent("Offset"));
                    EditorGUILayout.PropertyField(m_InnerShadowBlur, new GUIContent("Blur"));
                    EditorGUILayout.PropertyField(m_InnerShadowSpread, new GUIContent("Spread"));
                }
                EditorGUI.indentLevel--;
            }

            // ── Arc (only for Ellipse) ──
            if (m_Shape.enumValueIndex == (int)VectorUIGraphic.ShapeType.Ellipse)
            {
                s_ShowArc = EditorGUILayout.Foldout(s_ShowArc, "Arc", true, EditorStyles.foldoutHeader);
                if (s_ShowArc)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(m_EllipseInnerRadius, new GUIContent("Inner Radius"));
                    EditorGUILayout.PropertyField(m_EllipseArcAngleRange, new GUIContent("Angle Range"));
                    EditorGUI.indentLevel--;
                }
            }

            // ── Base Graphic ──
            EditorGUILayout.Space();
            DrawPropertiesExcluding(serializedObject,
                "m_Shape", "m_FillColor", "m_CornerRadius", "m_SdfSmooth",
                "m_Sprite", "m_ImageScaleMode",
                "m_Fill", "m_FillGradient", "m_GradientHandlePositions",
                "m_StrokeColor", "m_StrokeWidth",
                "m_EnableDropShadow", "m_ShadowColor", "m_ShadowOffset", "m_ShadowBlur", "m_ShadowSpread",
                "m_EnableInnerShadow", "m_InnerShadowColor", "m_InnerShadowOffset", "m_InnerShadowBlur", "m_InnerShadowSpread",
                "m_EllipseInnerRadius", "m_EllipseArcAngleRange",
                "m_Script"
            );

            serializedObject.ApplyModifiedProperties();
        }
    }
}
