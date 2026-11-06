# VectorUI

Design-grade procedural SDF shape renderer for Unity UGUI. Renders vector-sharp rounded rectangles, ellipses, stars, gradients, strokes, drop shadows, and inner shadows directly in a single shader with zero sprite texture dependencies.

A standalone, drop-in UI graphic component for any Unity project.

---

## ✨ Features

- **Procedural SDF Shapes:**
  - **Rounded Rectangle:** Individual per-corner radii (Top-Left, Top-Right, Bottom-Right, Bottom-Left) with smooth aspect ratio clamping so narrow shapes (e.g. progress bars, pills) don't distort.
  - **Ellipse:** Fast fill mode and precise Inigo Quilez distance-based outline mode; supports partial arc/pie angles & inner radius (doughnut shapes).
  - **Star:** 5-pointed procedural star.
- **Fills & Gradients:**
  - Solid color fill.
  - Linear gradient with handle positions and multi-stop color blending.
  - Radial gradient with handle points.
  - Optional texture/sprite fill with Fill, Fit, Stretch, and Tile modes.
- **Outlines / Strokes:**
  - Clean SDF-based outline with pixel-perfect anti-aliasing.
- **Shadows (Zero extra GameObjects):**
  - **Drop Shadow:** Configurable color, offset, blur, and spread. Vertices automatically expand so the shadow isn't clipped by the element's bounding rect!
  - **Inner Shadow:** Inverted SDF inner glow / inset shadow.
- **Customizable Anti-Aliasing:**
  - Exposes `SdfSmooth` control to switch between soft/diffused edges and razor-sharp UI borders.
- **Seamless UGUI Integration:**
  - Inherits from `MaskableGraphic`.
  - Works with Unity Canvas grouping, masking, and Stencil buffers.
  - Handles Linear and Gamma color spaces correctly.

---

## 🚀 Installation

### Via Unity Package Manager (Git URL)

1. Open your Unity Project.
2. Go to **Window** > **Package Manager**.
3. Click the `+` button in the top left and choose **Add package from git URL...**.
4. Enter:
   ```
   https://github.com/seanbud/UnityuGuiVectorUIShader.git
   ```

Alternatively, clone or place this folder into your project's `Packages/` or `Assets/` directory.

---

## 🛠️ Usage

1. Create a UI element in your canvas (**GameObject** > **UI** > **VectorUI Graphic** or add the `VectorUIGraphic` component to any `RectTransform`).
2. Tweak shapes, fills, strokes, corner radius, drop shadow, and inner shadow in the Inspector.
3. Every property is accessible at runtime via C# script:

```csharp
using UnityEngine;
using VectorUI;

public class UIController : MonoBehaviour
{
    [SerializeField] private VectorUIGraphic vectorGraphic;

    void Start()
    {
        // Adjust corner radii (TL, TR, BR, BL)
        vectorGraphic.CornerRadius = new Vector4(16, 16, 16, 16);

        // Turn on drop shadow
        vectorGraphic.EnableDropShadow = true;
        vectorGraphic.ShadowColor = new Color(0, 0, 0, 0.35f);
        vectorGraphic.ShadowOffset = new Vector2(0, -6);
        vectorGraphic.ShadowBlur = 12f;

        // Add inner shadow
        vectorGraphic.EnableInnerShadow = true;
        vectorGraphic.InnerShadowColor = new Color(0, 0, 0, 0.2f);
        vectorGraphic.InnerShadowOffset = new Vector2(0, -2);
        vectorGraphic.InnerShadowBlur = 4f;

        // Change stroke
        vectorGraphic.StrokeWidth = 2f;
        vectorGraphic.StrokeColor = Color.white;
    }
}
```

---

## 📄 License

MIT License. See [LICENSE](LICENSE) for details.
