using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace AetherTex.Viewer.Panels
{
    public class FixedRatioPanel : Panel
    {
        public double Ratio
        {
            get { return (double)GetValue(RatioProperty); }
            set { SetValue(RatioProperty, value); }
        }

        public double ReferenceWidth
        {
            get { return (double)GetValue(ReferenceWidthProperty); }
            set { SetValue(ReferenceWidthProperty, value); }
        }

        public double ReferenceHeight
        {
            get { return (double)GetValue(ReferenceHeightProperty); }
            set { SetValue(ReferenceHeightProperty, value); }
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            var ratio = ReferenceWidth / ReferenceHeight;
            if (double.IsNaN(ratio))
            {
                ratio = Ratio;
            }
            if (double.IsNaN(ratio))
            {
                ratio = 1;
            }

            var finalWidth = availableSize.Width;
            var finalHeight = availableSize.Width / ratio;
            if (finalHeight > availableSize.Height)
            {
                finalHeight = availableSize.Height;
                finalWidth = availableSize.Height * ratio;
            }

            availableSize = new Size(
                Math.Min(availableSize.Width, finalWidth),
                Math.Min(availableSize.Height, finalHeight));

            Size finalSize = new Size(0, 0);
            foreach (UIElement child in InternalChildren)
            {
                child.Measure(availableSize);
                finalSize.Width = Math.Max(finalSize.Width, child.DesiredSize.Width);
                finalSize.Height = Math.Max(finalSize.Height, child.DesiredSize.Height);
            }

            return finalSize;
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            var ratio = ReferenceWidth / ReferenceHeight;
            if (double.IsNaN(ratio))
            {
                ratio = Ratio;
            }
            if (double.IsNaN(ratio))
            {
                ratio = 1;
            }

            var finalWidth = ActualWidth;
            var finalHeight = ActualWidth / ratio;
            if (finalHeight > ActualHeight)
            {
                finalHeight = ActualHeight;
                finalWidth = ActualHeight * ratio;
            }

            var finalChildRect = new Rect(
                (ActualWidth - finalWidth) / 2,
                (ActualHeight - finalHeight) / 2,
                finalWidth,
                finalHeight);

            foreach (UIElement child in InternalChildren)
            {
                child.Arrange(finalChildRect);
            }

            return finalSize;
        }


        // Using a DependencyProperty as the backing store for Ratio.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty RatioProperty =
            DependencyProperty.Register("Ratio", typeof(double), typeof(FixedRatioPanel), new FrameworkPropertyMetadata(double.NaN, FrameworkPropertyMetadataOptions.AffectsArrange));

        // Using a DependencyProperty as the backing store for ReferenceWidth.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ReferenceWidthProperty =
            DependencyProperty.Register("ReferenceWidth", typeof(double), typeof(FixedRatioPanel), new FrameworkPropertyMetadata(double.NaN, FrameworkPropertyMetadataOptions.AffectsArrange));

        // Using a DependencyProperty as the backing store for ReferenceHeight.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ReferenceHeightProperty =
            DependencyProperty.Register("ReferenceHeight", typeof(double), typeof(FixedRatioPanel), new FrameworkPropertyMetadata(double.NaN, FrameworkPropertyMetadataOptions.AffectsArrange));
    }
}
