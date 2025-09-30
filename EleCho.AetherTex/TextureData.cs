namespace EleCho.AetherTex
{
    public record struct TextureData(TextureFormat Format, int Width, int Height, nint BaseAddress, nint RowBytes)
    {
        public static int GetRequiredDataSize(TextureFormat format, int width, int height)
        {
            return format switch
            {
                _ => throw new NotImplementedException()
            };
        }
    }
}
