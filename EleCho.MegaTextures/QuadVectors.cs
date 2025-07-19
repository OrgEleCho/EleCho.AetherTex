using System.Numerics;

namespace EleCho.MegaTextures
{
    public record struct QuadVectors(Vector2 LeftTop, Vector2 RightTop, Vector2 RightBottom, Vector2 LeftBottom);

    public record struct TextureData(TextureFormat Format, int Width, int Height, nint BaseAddress, nint RowBytes);
}
