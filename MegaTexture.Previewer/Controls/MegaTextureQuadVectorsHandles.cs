using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using EleCho.AetherTex;

namespace MegaTextures.Previewer.Controls
{
    internal class MegaTextureQuadVectorsHandles : FrameworkElement
    {
        private readonly Vector2[] _quadVectors = new Vector2[4];

        private int? _draggingVectorIndex;
        private Point _draggingStartMousePosition;
        private Vector2 _draggingVectorStartValue;

        public MegaTexture Reference
        {
            get { return (MegaTexture)GetValue(ReferenceProperty); }
            set { SetValue(ReferenceProperty, value); }
        }

        public double HandleSize
        {
            get { return (double)GetValue(HandleSizeProperty); }
            set { SetValue(HandleSizeProperty, value); }
        }

        public Brush HandleFill
        {
            get { return (Brush)GetValue(HandleFillProperty); }
            set { SetValue(HandleFillProperty, value); }
        }

        public Brush Stroke
        {
            get { return (Brush)GetValue(StrokeProperty); }
            set { SetValue(StrokeProperty, value); }
        }

        public double StrokeThickness
        {
            get { return (double)GetValue(StrokeThicknessProperty); }
            set { SetValue(StrokeThicknessProperty, value); }
        }

        public QuadVectors QuadVectors
        {
            get { return (QuadVectors)GetValue(QuadVectorsProperty); }
            set { SetValue(QuadVectorsProperty, value); }
        }

        private Point[] GetPointsToDraw()
        {
            Point[] pointsToDraw = new Point[]
            {
                new (_quadVectors[0].X, _quadVectors[0].Y),
                new (_quadVectors[1].X, _quadVectors[1].Y),
                new (_quadVectors[2].X, _quadVectors[2].Y),
                new (_quadVectors[3].X, _quadVectors[3].Y),
            };

            if (Reference is { } reference)
            {
                for (int i = 0; i < pointsToDraw.Length; i++)
                {
                    var pointToDraw = pointsToDraw[i];
                    pointToDraw = new Point(
                        pointToDraw.X / reference.Width * RenderSize.Width,
                        pointToDraw.Y / reference.Height * RenderSize.Height);

                    pointsToDraw[i] = pointToDraw;
                }
            }

            return pointsToDraw;
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            var handleSize = HandleSize;
            var halfHandleSize = handleSize / 2;

            var pointsToDraw = GetPointsToDraw();
            foreach (Point pointToDraw in pointsToDraw)
            {
                var rect = new Rect(pointToDraw.X - halfHandleSize, pointToDraw.Y - halfHandleSize, handleSize, handleSize);
                drawingContext.DrawRectangle(HandleFill, null, rect);
            }

            base.OnRender(drawingContext);
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            var mousePosition = e.GetPosition(null);
            var localMousePosition = e.GetPosition(this);

            var handleSize = HandleSize;
            var halfHandleSize = handleSize / 2;

            var pointsToDraw = GetPointsToDraw();
            for (int i = 0; i < pointsToDraw.Length; i++)
            {
                Point pointToDraw = pointsToDraw[i];
                var rect = new Rect(pointToDraw.X - halfHandleSize, pointToDraw.Y - halfHandleSize, handleSize, handleSize);
                if (rect.Contains(localMousePosition))
                {
                    _draggingVectorIndex = i;
                    _draggingVectorStartValue = _quadVectors[i];
                    _draggingStartMousePosition = mousePosition;
                    var captured = CaptureMouse();
                    if (!captured)
                    {
                        _draggingVectorIndex = null;
                    }

                    break;
                }
            }

            base.OnMouseDown(e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (_draggingVectorIndex is not null)
            {
                var mousePosition = e.GetPosition(null);
                var vectorOffset = mousePosition - _draggingStartMousePosition;

                if (Reference is { } reference)
                {
                    vectorOffset = new System.Windows.Vector(
                        vectorOffset.X / RenderSize.Width * reference.Width,
                        vectorOffset.Y / RenderSize.Height * reference.Height);
                }

                _quadVectors[_draggingVectorIndex.Value] = _draggingVectorStartValue + new Vector2((float)vectorOffset.X, (float)vectorOffset.Y);
                QuadVectors = new QuadVectors(_quadVectors[0], _quadVectors[1], _quadVectors[2], _quadVectors[3]);
            }

            base.OnMouseMove(e);
        }

        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            ReleaseMouseCapture();
            _draggingVectorIndex = null;
            base.OnMouseUp(e);
        }


        public static readonly DependencyProperty ReferenceProperty =
            DependencyProperty.Register("Reference", typeof(MegaTexture), typeof(MegaTextureQuadVectorsHandles), 
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty HandleSizeProperty =
            DependencyProperty.Register("HandleSize", typeof(double), typeof(MegaTextureQuadVectorsHandles), 
                new FrameworkPropertyMetadata(10.0, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty HandleFillProperty =
            DependencyProperty.Register("HandleFill", typeof(Brush), typeof(MegaTextureQuadVectorsHandles), 
                new FrameworkPropertyMetadata(Brushes.DarkGreen, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty StrokeProperty =
            DependencyProperty.Register("Stroke", typeof(Brush), typeof(MegaTextureQuadVectorsHandles), 
                new FrameworkPropertyMetadata(Brushes.Gray, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty StrokeThicknessProperty =
            DependencyProperty.Register("StrokeThickness", typeof(double), typeof(MegaTextureQuadVectorsHandles), 
                new FrameworkPropertyMetadata(1.0, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty QuadVectorsProperty =
            DependencyProperty.Register("QuadVectors", typeof(QuadVectors), typeof(MegaTextureQuadVectorsHandles), 
                new FrameworkPropertyMetadata(default(QuadVectors), FrameworkPropertyMetadataOptions.AffectsRender, propertyChangedCallback: OnQuadVectorsChanged)
                {
                    BindsTwoWayByDefault = true
                });

        private static void OnQuadVectorsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not MegaTextureQuadVectorsHandles handles ||
                e.NewValue is not QuadVectors newQuadVectors)
            {
                return;
            }

            handles._quadVectors[0] = newQuadVectors.LeftTop;
            handles._quadVectors[1] = newQuadVectors.RightTop;
            handles._quadVectors[2] = newQuadVectors.RightBottom;
            handles._quadVectors[3] = newQuadVectors.LeftBottom;
        }
    }
}
