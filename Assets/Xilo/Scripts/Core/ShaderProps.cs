using UnityEngine;

namespace Xilo.Core
{
    // Original property names of the existing shaders (read-only contract, some are in Portuguese).
    public static class ShaderProps
    {
        public const string SketchTexture = "SketchTexture";
        public const string SketchMask = "SketchMask";
        public const string SculptMask = "SculptMask";
        public const string SandpaperMask = "SandpaperMask";
        public const string PaintMask = "PaintMask";
        public const string PrintMask = "PrintMask";
        public const string InkMask = "InkMask";
        public const string Result = "Resultado";
        public const string Authorial = "autoral";
        public const string AuthorialArtMask = "mascaraArteAutoral";

        public const string RollerHasInk = "isTinta";
        public const string RollerInkLevel = "inkLevel";

        public const string OutlineColor = "_Color";
        public const string OutlineSize = "_Size";

        public const string BrushSprite = "_SpriteTex";
        public const string BrushPosition = "_Pos";
        public const string BrushScale = "_Scale";
        public const string BrushRotation = "_Rotation";

        public static readonly int SketchTextureId = Shader.PropertyToID(SketchTexture);
        public static readonly int ResultId = Shader.PropertyToID(Result);
        public static readonly int AuthorialId = Shader.PropertyToID(Authorial);
        public static readonly int RollerHasInkId = Shader.PropertyToID(RollerHasInk);
        public static readonly int RollerInkLevelId = Shader.PropertyToID(RollerInkLevel);
        public static readonly int BrushSpriteId = Shader.PropertyToID(BrushSprite);
        public static readonly int BrushPositionId = Shader.PropertyToID(BrushPosition);
        public static readonly int BrushScaleId = Shader.PropertyToID(BrushScale);
        public static readonly int BrushRotationId = Shader.PropertyToID(BrushRotation);
    }
}
