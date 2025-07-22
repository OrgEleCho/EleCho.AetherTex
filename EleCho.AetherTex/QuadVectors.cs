using System.Numerics;

namespace EleCho.AetherTex
{
    public record struct QuadVectors(Vector2 LeftTop, Vector2 RightTop, Vector2 RightBottom, Vector2 LeftBottom)
    {
        public static QuadVectors FromRectangle(int left, int top, int width, int height)
        {
            return new QuadVectors(
                new Vector2(left, top),
                new Vector2(left + width, top),
                new Vector2(left + width, top + height),
                new Vector2(left, top + height));
        }
    }

    public record struct TextureData(TextureFormat Format, int Width, int Height, nint BaseAddress, nint RowBytes);
}
