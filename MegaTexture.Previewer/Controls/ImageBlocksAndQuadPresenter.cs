using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using EleCho.AetherTex;

namespace AetherTex.Viewer.Controls
{
    public partial class ImageBlocksAndQuadPresenter : Control
    {
        static ImageBlocksAndQuadPresenter()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ImageBlocksAndQuadPresenter), new FrameworkPropertyMetadata(typeof(ImageBlocksAndQuadPresenter)));
        }

        public AetherTexImage Texture
        {
            get { return (AetherTexImage)GetValue(TextureProperty); }
            set { SetValue(TextureProperty, value); }
        }

        public string Source
        {
            get { return (string)GetValue(SourceProperty); }
            set { SetValue(SourceProperty, value); }
        }
        public QuadVectors QuadVectors
        {
            get { return (QuadVectors)GetValue(QuadVectorsProperty); }
            set { SetValue(QuadVectorsProperty, value); }
        }

        public IReadOnlyList<Tile> TextureTiles
        {
            get { return (IReadOnlyList<Tile>)GetValue(TextureTilesProperty); }
            private set { SetValue(TextureTilesPropertyKey, value); }
        }

        public void UpdateTileImage(int column, int row)
        {
            if (TextureTiles.FirstOrDefault(t => t.Column == column && t.Row == row) is not { } tile)
            {
                return;
            }

            tile.UpdateImage();
        }


        public static readonly DependencyProperty TextureProperty =
            DependencyProperty.Register(nameof(Texture), typeof(AetherTexImage), typeof(ImageBlocksAndQuadPresenter), new PropertyMetadata(null, propertyChangedCallback: OnTextureChanged));

        public static readonly DependencyProperty SourceProperty =
            DependencyProperty.Register(nameof(Source), typeof(string), typeof(ImageBlocksAndQuadPresenter), new PropertyMetadata("color", propertyChangedCallback: OnSourceChanged));

        public static readonly DependencyProperty QuadVectorsProperty =
            DependencyProperty.Register(nameof(QuadVectors), typeof(QuadVectors), typeof(ImageBlocksAndQuadPresenter), new FrameworkPropertyMetadata(default(QuadVectors))
            {
                BindsTwoWayByDefault = true,
            });

        private static readonly DependencyPropertyKey TextureTilesPropertyKey =
            DependencyProperty.RegisterReadOnly(nameof(TextureTiles), typeof(IReadOnlyList<Tile>), typeof(ImageBlocksAndQuadPresenter), new PropertyMetadata(Array.Empty<Tile>()));

        public static readonly DependencyProperty TextureTilesProperty = TextureTilesPropertyKey.DependencyProperty;

        private static void OnSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not ImageBlocksAndQuadPresenter presenter ||
                presenter.Texture is not { } texture)
            {
                return;
            }

            for (int y = 0; y < texture.Rows; y++)
            {
                for (int x = 0; x < texture.Columns; x++)
                {
                    presenter.UpdateTileImage(x, y);
                }
            }
        }

        private static void OnTextureChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not ImageBlocksAndQuadPresenter presenter)
            {
                return;
            }

            if (e.NewValue is AetherTexImage newTexture)
            {
                var tiles = new List<Tile>();
                for (int y = 0; y < newTexture.Rows; y++)
                {
                    for (int x = 0; x < newTexture.Columns; x++)
                    {
                        tiles.Add(new Tile(presenter, newTexture, y, x));
                    }
                }

                presenter._cachedSources.Clear();
                presenter.TextureTiles = tiles.AsReadOnly();
                presenter.QuadVectors = new QuadVectors(
                    new Vector2(0, 0),
                    new Vector2(newTexture.Width, 0),
                    new Vector2(newTexture.Width, newTexture.Height),
                    new Vector2(0, newTexture.Height));
            }
            else
            {
                presenter.TextureTiles = Array.Empty<Tile>();
            }
        }

    }
}
