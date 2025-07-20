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

namespace MegaTextures.Previewer.Controls
{
    public partial class MegaTexturePresenter : Control
    {
        static MegaTexturePresenter()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(MegaTexturePresenter), new FrameworkPropertyMetadata(typeof(MegaTexturePresenter)));
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

        public IReadOnlyList<MegaTextureTile> TextureTiles
        {
            get { return (IReadOnlyList<MegaTextureTile>)GetValue(TextureTilesProperty); }
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
            DependencyProperty.Register(nameof(Texture), typeof(AetherTexImage), typeof(MegaTexturePresenter), new PropertyMetadata(null, propertyChangedCallback: OnTextureChanged));

        public static readonly DependencyProperty SourceProperty =
            DependencyProperty.Register(nameof(Source), typeof(string), typeof(MegaTexturePresenter), new PropertyMetadata("color", propertyChangedCallback: OnSourceChanged));

        public static readonly DependencyProperty QuadVectorsProperty =
            DependencyProperty.Register(nameof(QuadVectors), typeof(QuadVectors), typeof(MegaTexturePresenter), new FrameworkPropertyMetadata(default(QuadVectors))
            {
                BindsTwoWayByDefault = true,
            });

        private static readonly DependencyPropertyKey TextureTilesPropertyKey =
            DependencyProperty.RegisterReadOnly(nameof(TextureTiles), typeof(IReadOnlyList<MegaTextureTile>), typeof(MegaTexturePresenter), new PropertyMetadata(Array.Empty<MegaTextureTile>()));

        public static readonly DependencyProperty TextureTilesProperty = TextureTilesPropertyKey.DependencyProperty;

        private static void OnSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not MegaTexturePresenter presenter ||
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
            if (d is not MegaTexturePresenter presenter)
            {
                return;
            }

            if (e.NewValue is AetherTexImage newTexture)
            {
                var tiles = new List<MegaTextureTile>();
                for (int y = 0; y < newTexture.Rows; y++)
                {
                    for (int x = 0; x < newTexture.Columns; x++)
                    {
                        tiles.Add(new MegaTextureTile(presenter, newTexture, y, x));
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
                presenter.TextureTiles = Array.Empty<MegaTextureTile>();
            }
        }

    }
}
