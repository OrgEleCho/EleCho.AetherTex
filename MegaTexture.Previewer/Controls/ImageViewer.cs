using EleCho.AetherTex;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace AetherTex.Viewer.Controls
{
    public class ImageViewer : Control
    {
        static ImageViewer()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ImageViewer), new FrameworkPropertyMetadata(typeof(ImageViewer)));
        }

        private WriteableBitmap? _currentOutput;
        private readonly Dictionary<string, AetherTexImage.ExprSource> _cachedExpressions = new();

        public AetherTexImage Input
        {
            get { return (AetherTexImage)GetValue(InputProperty); }
            set { SetValue(InputProperty, value); }
        }

        public QuadVectors QuadVectors
        {
            get { return (QuadVectors)GetValue(QuadVectorsProperty); }
            set { SetValue(QuadVectorsProperty, value); }
        }

        public string SourceExpression
        {
            get { return (string)GetValue(SourceExpressionProperty); }
            set { SetValue(SourceExpressionProperty, value); }
        }

        public int OutputWidth
        {
            get { return (int)GetValue(OutputWidthProperty); }
            set { SetValue(OutputWidthProperty, value); }
        }

        public int OutputHeight
        {
            get { return (int)GetValue(OutputHeightProperty); }
            set { SetValue(OutputHeightProperty, value); }
        }

        public ImageSource? Output
        {
            get { return (ImageSource?)GetValue(OutputProperty); }
            private set { SetValue(OutputPropertyKey, value); }
        }

        public string? ErrorMessage
        {
            get { return (string?)GetValue(ErrorMessageProperty); }
            private set { SetValue(ErrorMessagePropertyKey, value); }
        }

        public void UpdateImage()
        {
            ErrorMessage = null;

            if (Input is null)
            {
                _currentOutput = null;
                ErrorMessage = "No Input";
            }
            else if (
                OutputWidth < 1 ||
                OutputHeight < 1)
            {
                _currentOutput = null;
                ErrorMessage = "Invalid Output Size";
            }
            else if (
                string.IsNullOrWhiteSpace(SourceExpression))
            {
                _currentOutput = null;
                ErrorMessage = "Empty Source Expression";
            }
            else
            {
                if (_currentOutput is null ||
                    _currentOutput.PixelWidth != OutputWidth ||
                    _currentOutput.PixelHeight != OutputHeight)
                {
                    _currentOutput = new WriteableBitmap(OutputWidth, OutputHeight, 96, 96, PixelFormats.Bgra32, null);
                }

                try
                {
                    if (!_cachedExpressions.TryGetValue(SourceExpression, out var source))
                    {
                        source = _cachedExpressions[SourceExpression] = Input.CreateSource(SourceExpression);
                    }

                    _currentOutput.Lock();

                    var buffer = new TextureData(
                        TextureFormat.Bgra8888,
                        OutputWidth,
                        OutputHeight,
                        _currentOutput.BackBuffer,
                        _currentOutput.BackBufferStride);

                    Input.Read(source, QuadVectors, buffer);
                    _currentOutput.AddDirtyRect(new Int32Rect(0, 0, OutputWidth, OutputHeight));

                    _currentOutput.Unlock();
                }
                catch (Exception ex)
                {
                    ErrorMessage = $"Invalid Source Expression, {ex.Message}";
                }
            }

            Output = _currentOutput;
        }

        public static readonly DependencyProperty InputProperty =
            DependencyProperty.Register("Input", typeof(AetherTexImage), typeof(ImageViewer),
                new PropertyMetadata(null, propertyChangedCallback: OnInputChanged));

        public static readonly DependencyProperty QuadVectorsProperty =
            DependencyProperty.Register("QuadVectors", typeof(QuadVectors), typeof(ImageViewer),
                new PropertyMetadata(default(QuadVectors), propertyChangedCallback: OnRenderOptionsChanged));

        public static readonly DependencyProperty SourceExpressionProperty =
            DependencyProperty.Register("SourceExpression", typeof(string), typeof(ImageViewer),
                new PropertyMetadata(null, propertyChangedCallback: OnRenderOptionsChanged));

        public static readonly DependencyProperty OutputWidthProperty =
            DependencyProperty.Register("OutputWidth", typeof(int), typeof(ImageViewer),
                new PropertyMetadata(1024, propertyChangedCallback: OnRenderOptionsChanged));

        public static readonly DependencyProperty OutputHeightProperty =
            DependencyProperty.Register("OutputHeight", typeof(int), typeof(ImageViewer),
                new PropertyMetadata(1024, propertyChangedCallback: OnRenderOptionsChanged));

        private static readonly DependencyPropertyKey OutputPropertyKey =
            DependencyProperty.RegisterReadOnly("Output", typeof(ImageSource), typeof(ImageViewer),
                new PropertyMetadata(null));

        private static readonly DependencyPropertyKey ErrorMessagePropertyKey =
            DependencyProperty.RegisterReadOnly("ErrorMessage", typeof(string), typeof(ImageViewer),
                new PropertyMetadata(null));

        public static readonly DependencyProperty OutputProperty
            = OutputPropertyKey.DependencyProperty;

        public static readonly DependencyProperty ErrorMessageProperty
            = ErrorMessagePropertyKey.DependencyProperty;

        private static void OnInputChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not ImageViewer viewer)
            {
                return;
            }

            viewer._cachedExpressions.Clear();
            OnRenderOptionsChanged(d, e);
        }

        private static void OnRenderOptionsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not ImageViewer viewer)
            {
                return;
            }

            viewer.UpdateImage();
        }

    }
}
