﻿using UnityEngine;

namespace ToyBox.Infrastructure.UI;
public static class Div {
    private static Texture2D? m_FillTexture;
    private static GUIStyle? m_DivStyle;
    private static Color m_FillColor = new(1f, 1f, 1f, 0.65f);
    public static void DrawDiv(float indent = 0, float height = 0, float width = 0) {
        m_FillTexture ??= new Texture2D(1, 1);
        m_DivStyle ??= new GUIStyle {
            fixedHeight = 1,
        };
        m_FillTexture.SetPixel(0, 0, m_FillColor);
        m_FillTexture.Apply();
        m_DivStyle.normal.background = m_FillTexture;
        if (m_DivStyle.margin == null) {
            m_DivStyle.margin = new RectOffset((int)indent, 0, 4, 4);
        } else {
            m_DivStyle.margin.left = (int)indent + 3;
        }
        if (width > 0)
            m_DivStyle.fixedWidth = width;
        else
            m_DivStyle.fixedWidth = 0;
        GUILayout.Space((2f * height) / 3f);
        GUILayout.Box(GUIContent.none, m_DivStyle);
        GUILayout.Space(height / 3f);
    }
}
