using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EleCho.AetherTex;
using AetherTex.Viewer.Dialogs;
using SkiaSharp;
using static System.Net.Mime.MediaTypeNames;
using Microsoft.Win32;
using System.IO;
using BitMiracle.LibTiff.Classic;
using ImageMagick;
using System.Windows.Media.Media3D;

namespace AetherTex.Viewer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    [ObservableObject]
    public partial class MainWindow : Window
    {
        private OpenFileDialog? _openImageDialog;
        private SaveFileDialog? _saveImageDialog;

        [ObservableProperty]
        private AetherTexImage? _currentTexture;

        [ObservableProperty]
        private string? _presenterSource;

        [ObservableProperty]
        private string? _outputSourceExpression;

        [ObservableProperty]
        private string? _errorMessage;

        [ObservableProperty]
        private int _outputWidth = 1024;

        [ObservableProperty]
        private int _outputHeight = 1024;

        [ObservableProperty]
        private QuadVectors _quadVectors;

        [ObservableProperty]
        private WriteableBitmap? _currentOutput;

        public string[] PredefinedSources { get; } = new string[]
        {
            "side",
            "top"
        };

        public MainWindow()
        {
            DataContext = this;
            InitializeComponent();
        }

        private static TextureData GetTextureData(SKBitmap bitmap)
        {
            return new TextureData(bitmap.ColorType switch
            {
                SKColorType.Gray8 => TextureFormat.Gray8,
            }, bitmap.Width, bitmap.Height, bitmap.GetPixels(), bitmap.RowBytes);
        }

        private void SetCurrentImage(AetherTexImage image)
        {
            CurrentTexture = image;
            PresenterSource = image.Sources.First();
            OutputSourceExpression = PresenterSource;

            imageViewer.UpdateImage();
        }

        private void NewMegaTexture_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            var newTextureDialog = new NewTextureDialog()
            {
                Owner = this,
            };

            if (newTextureDialog.ShowDialog() != true)
            {
                return;
            }

            SetCurrentImage(new AetherTexImage(
                newTextureDialog.Format,
                newTextureDialog.TileWidth,
                newTextureDialog.TileHeight,
                newTextureDialog.Rows,
                newTextureDialog.Columns,
                newTextureDialog.Sources.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)));
        }

        /// <summary>
        /// 读取 tiled float32 TIFF 文件，并转换为一维 float[]（行主序）。
        /// </summary>
        public static float[] ReadTiledFloat32Tiff(string filePath, out int width, out int height)
        {
            using (Tiff image = Tiff.Open(filePath, "r"))
            {
                if (image == null)
                    throw new Exception("Failed to open TIFF file.");

                width = image.GetField(TiffTag.IMAGEWIDTH)[0].ToInt();
                height = image.GetField(TiffTag.IMAGELENGTH)[0].ToInt();
                int tileWidth = image.GetField(TiffTag.TILEWIDTH)[0].ToInt();
                int tileHeight = image.GetField(TiffTag.TILELENGTH)[0].ToInt();
                int bitsPerSample = image.GetField(TiffTag.BITSPERSAMPLE)[0].ToInt();
                int sampleFormat = image.GetField(TiffTag.SAMPLEFORMAT)[0].ToInt();

                // 校验格式
                if (bitsPerSample != 32 || sampleFormat != (int)SampleFormat.IEEEFP)
                    throw new NotSupportedException("TIFF must be float32 (bitsPerSample=32, sampleFormat=IEEEFP)");

                if (!image.IsTiled())
                    throw new NotSupportedException("TIFF must be tiled storage (IsTiled==true)");

                int totalPixels = width * height;
                float[] result = new float[totalPixels];

                int tilesX = (width + tileWidth - 1) / tileWidth;
                int tilesY = (height + tileHeight - 1) / tileHeight;
                int tileSize = image.TileSize();
                byte[] buffer = new byte[tileSize];

                for (int ty = 0; ty < tilesY; ty++)
                {
                    for (int tx = 0; tx < tilesX; tx++)
                    {
                        int x = tx * tileWidth;
                        int y = ty * tileHeight;
                        int bytesRead = image.ReadTile(buffer, 0, x, y, 0, 0);
                        if (bytesRead == 0)
                            throw new Exception($"Failed to read tile at ({x},{y})");

                        // 遍历 tile 内像素
                        for (int row = 0; row < tileHeight; row++)
                        {
                            int imgY = y + row;
                            if (imgY >= height) break;
                            for (int col = 0; col < tileWidth; col++)
                            {
                                int imgX = x + col;
                                if (imgX >= width) break;
                                int pixelIndex = imgY * width + imgX;
                                int tilePixelIndex = row * tileWidth + col;
                                int byteIndex = tilePixelIndex * 4;

                                // 处理字节序（TIFF 可能为大端）
                                float value = BitConverter.ToSingle(buffer, byteIndex);
                                if (image.IsBigEndian())
                                {
                                    byte[] temp = new byte[4];
                                    Array.Copy(buffer, byteIndex, temp, 0, 4);
                                    Array.Reverse(temp);
                                    value = BitConverter.ToSingle(temp, 0);
                                }
                                result[pixelIndex] = value;
                            }
                        }
                    }
                }
                return result;
            }
        }

        private void ImportImage_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentTexture is null)
            {
                MessageBox.Show(this, "Error", "No MegaTexture created", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var importImageDialog = new ImportImageDialog(CurrentTexture)
            {
                Owner = this,
            };

            if (importImageDialog.ShowDialog() != true)
            {
                return;
            }

            if (!File.Exists(importImageDialog.FilePath))
            {
                MessageBox.Show(this, $"File not exists: {importImageDialog.FilePath}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (CurrentTexture.Format == TextureFormat.Float32)
            {
                var depth = ReadTiledFloat32Tiff(importImageDialog.FilePath!, out var width, out var height);

                unsafe
                {
                    fixed (float* ptr = depth)
                    {
                        CurrentTexture.Write(new TextureData(TextureFormat.Float32, width, height, (nint)ptr, width * sizeof(float)), importImageDialog.TargetSource, importImageDialog.Column, importImageDialog.Row);
                    }
                }
            }
            else if (CurrentTexture.Format == TextureFormat.Gray16)
            {
                using var image = new MagickImage(importImageDialog.FilePath!);
                if (image.ColorSpace != ColorSpace.Gray)
                {
                    MessageBox.Show(this, $"NotSupport {image.ColorSpace}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                if (image.Depth == 16)
                {
                    var pixels = image.GetPixels().ToArray();
                    unsafe
                    {
                        fixed (void* ptr = pixels)
                        {
                            var textureData = new TextureData(TextureFormat.Gray16, (int)image.Width, (int)image.Height, (nint)ptr, (int)(image.Width * sizeof(ushort)));
                            CurrentTexture.Write(textureData, importImageDialog.TargetSource, importImageDialog.Column, importImageDialog.Row);
                        }
                    }
                }
                else if (image.Depth == 8)
                {
                    unsafe
                    {
                        using var bitmap = SKBitmap.Decode(importImageDialog.FilePath);
                        ushort[] gray16Data = new ushort[bitmap.Width * bitmap.Height];
                        var p = (byte*)bitmap.GetPixels();

                        // 3. 遍历每个像素，转换并写入16位数组
                        for (int y = 0; y < bitmap.Width; y++)
                        {
                            for (int x = 0; x < bitmap.Height; x++)
                            {
                                var origin = p[y * bitmap.RowBytes + x];
                                gray16Data[y * bitmap.Width + x] = (ushort)(origin << 8 | origin);
                            }
                        }
                        fixed(void* ptr = gray16Data)
                        {
                            var textureData = new TextureData(TextureFormat.Gray16, bitmap.Width, bitmap.Height, (nint)ptr, bitmap.Width * sizeof(ushort));
                            CurrentTexture.Write(textureData, importImageDialog.TargetSource, importImageDialog.Column, importImageDialog.Row);
                        }
                    }
                }
            }
            else
            {
                using var bitmap = SKBitmap.Decode(importImageDialog.FilePath);
                CurrentTexture.Write(GetTextureData(bitmap), importImageDialog.TargetSource, importImageDialog.Column, importImageDialog.Row);
            }

            megaTexturePresenter.UpdateTileImage(importImageDialog.Column, importImageDialog.Row);
            imageViewer.UpdateImage();
        }

        private void OpenImage_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            _openImageDialog ??= new OpenFileDialog()
            {
                Title = "Open Image",
                Filter = "AetherTex Image|*.atimg",
                CheckFileExists = true,
            };

            if (_openImageDialog.ShowDialog(this) != true)
            {
                return;
            }

            try
            {
                using var file = File.OpenRead(_openImageDialog.FileName);
                var image = AetherTexImage.Deserialize(file);

                SetCurrentImage(image);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Failed to open", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveImage_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentTexture is null)
            {
                return;
            }

            _saveImageDialog ??= new SaveFileDialog()
            {
                Title = "Save Image",
                Filter = "AetherTex Image|*.atimg",
                DefaultExt = ".atimg",
            };

            if (_saveImageDialog.ShowDialog(this) != true)
            {
                return;
            }

            using var file = File.Create(_saveImageDialog.FileName);
            try
            {
                AetherTexImage.Serialize(CurrentTexture, file);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Failed to save", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}