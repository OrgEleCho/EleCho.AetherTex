using System.Numerics;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using EleCho.MegaTextures;

namespace MegaTextures.Previewer.Controls
{
    public partial class MegaTexturePresenter
    {
        private readonly Dictionary<string, MegaTexture.ExprSource> _cachedSources = new();

        private MegaTexture.ExprSource? GetCachedSource(string text)
        {
            if (Texture is null)
            {
                return null;
            }

            try
            {
                if (!_cachedSources.TryGetValue(text, out var source))
                {
                    source = _cachedSources[text] = Texture.CreateSource(text);
                }

                return source;
            }
            catch
            {
                return null;
            }
        }

        public partial class MegaTextureTile : ObservableObject
        {
            private WriteableBitmap? _tileImage;

            public MegaTextureTile(MegaTexturePresenter owner, MegaTexture texture, int row, int column)
            {
                Owner = owner;
                Texture = texture;
                Row = row;
                Column = column;
            }

            public MegaTexturePresenter Owner { get; }
            public MegaTexture Texture { get; }
            public int Row { get; }
            public int Column { get; }

            public ImageSource? TileImage
            {
                get => _tileImage;
            }

            public void UpdateImage()
            {
                if (Owner.Texture is null ||
                    Owner.GetCachedSource(Owner.Source) is not { } source)
                {
                    SetProperty(ref _tileImage, null, nameof(TileImage));
                    return;
                }

                var tileImage = _tileImage ?? new WriteableBitmap(Texture.TileWidth, Texture.TileHeight, 96, 96, PixelFormats.Bgra32, null);
                tileImage.Lock();

                var quadVectors = new QuadVectors(
                    new Vector2(Texture.TileWidth * Column, Texture.TileHeight * Row),
                    new Vector2(Texture.TileWidth * (Column + 1), Texture.TileHeight * Row),
                    new Vector2(Texture.TileWidth * (Column + 1), Texture.TileHeight * (Row + 1)),
                    new Vector2(Texture.TileWidth * (Column), Texture.TileHeight * (Row + 1)));

                var buffer = new TextureData(TextureFormat.Bgra8888, tileImage.PixelWidth, tileImage.PixelHeight, tileImage.BackBuffer, tileImage.BackBufferStride);
                Texture.Read(source, quadVectors, buffer);
                tileImage.AddDirtyRect(new System.Windows.Int32Rect(0, 0, tileImage.PixelWidth, tileImage.PixelHeight));
                tileImage.Unlock();

                SetProperty(ref _tileImage, tileImage, nameof(TileImage));
            }
        }

    }
}
