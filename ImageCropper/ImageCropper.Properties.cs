using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ImageCropper
{
    public partial class ImageCropper
    {
        /// <summary>
        /// Gets or sets the minimum cropped length(in pixel).
        /// </summary>
        public double MinCroppedPixelLength { get; set; } = 40;

        /// <summary>
        /// Gets or sets the minimum selectable length.
        /// </summary>
        public double MinSelectedLength { get; set; } = 40;

        /// <summary>
        /// Gets the current cropped region.
        /// </summary>
        public Rect CroppedRegion => _currentCroppedRect;

        private static void OnSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var target = (ImageCropper)d;
            if (e.NewValue is WriteableBitmap bitmap)
            {
                if (bitmap.PixelWidth < target.MinCropSize.Width || bitmap.PixelHeight < target.MinCropSize.Height)
                {
                    target.Source = null;
                    throw new ArgumentException("The resolution of the image is too small!");
                }
            }

            target.InvalidateMeasure();
            target.UpdateCropShape();
            target.InitImageLayout();
            //重置覆盖层
            target.Reset();
            if (target.CropperEnable && target.ThumbMode == ThumbMode.Draw)
            {
                target.UpdateThumbsVisibility(Visibility.Collapsed);
            }
        }

        private static void OnAspectRatioChanged(
            DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var target = (ImageCropper)d;
            target.UpdateAspectRatio(true);
        }

        private static void OnCropShapeChanged(
            DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var target = (ImageCropper)d;
            target.UpdateCropShape();
            target.UpdateThumbsVisibility();
            target.UpdateAspectRatio();
            target.UpdateMaskArea();
        }

        private static void OnThumbPlacementChanged(
            DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var target = (ImageCropper)d;
            target.UpdateThumbsVisibility();
        }

        private static void OnCropperEnableChanged(
            DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var target = (ImageCropper)d;
            if((bool)e.NewValue)
            {
                target.EnableCropper();
                target.ResetDrawThumb();
            }
            else
            {
                target.DisableCropper();
            }
        }

        /// <summary>
        ///  Gets or sets the source of the cropped image.
        /// </summary>
        public WriteableBitmap Source
        {
            get { return (WriteableBitmap)GetValue(SourceProperty); }
            set { SetValue(SourceProperty, value); }
        }

        /// <summary>
        /// Gets or sets the aspect ratio of the cropped image, the default value is null.
        /// Only works when <see cref="CropShape"/> = <see cref="CropShape.Rectangular"/>.
        /// </summary>
        public double? AspectRatio
        {
            get { return (double?)GetValue(AspectRatioProperty); }
            set { SetValue(AspectRatioProperty, value); }
        }

        /// <summary>
        /// Gets or sets the shape to use when cropping.
        /// </summary>
        public CropShape CropShape
        {
            get { return (CropShape)GetValue(CropShapeProperty); }
            set { SetValue(CropShapeProperty, value); }
        }

        /// <summary>
        /// Gets or sets the mask on the cropped image.
        /// </summary>
        public Brush Mask
        {
            get { return (Brush)GetValue(MaskProperty); }
            set { SetValue(MaskProperty, value); }
        }

        /// <summary>
        /// Gets or sets a value for the style to use for the primary thumbs of the ImageCropper.
        /// </summary>
        public Style PrimaryThumbStyle
        {
            get { return (Style)GetValue(PrimaryThumbStyleProperty); }
            set { SetValue(PrimaryThumbStyleProperty, value); }
        }

        /// <summary>
        /// Gets or sets a value for the style to use for the secondary thumbs of the ImageCropper.
        /// </summary>
        public Style SecondaryThumbStyle
        {
            get { return (Style)GetValue(SecondaryThumbStyleProperty); }
            set { SetValue(SecondaryThumbStyleProperty, value); }
        }

        /// <summary>
        /// Gets or sets a value for thumb placement.
        /// </summary>
        public ThumbPlacement ThumbPlacement
        {
            get { return (ThumbPlacement)GetValue(ThumbPlacementProperty); }
            set { SetValue(ThumbPlacementProperty, value); }
        }

        /// <summary>
        /// 是否允许裁剪
        /// false则隐藏裁剪相关显示，单纯显示图片
        /// </summary>
        public bool CropperEnable
        {
            get { return (bool)GetValue(CropperEnableProperty); }
            set { SetValue(CropperEnableProperty, value); }
        }

        /// <summary>
        /// 是否允许拖动图片
        /// </summary>
        public bool DragImgEnable
        {
            get { return (bool)GetValue(DragImgEnableProperty); }
            set { SetValue(DragImgEnableProperty, value); }
        }
        /// <summary>
        /// 调整裁剪区域方式
        /// Move拖拽Thumb
        /// Draw鼠标绘制(绘制完成后会切换到Move的一般模式，如果需要重新通过鼠标绘制，调用ResetDrawThumb)
        /// </summary>
        public ThumbMode ThumbMode
        {
            get { return (ThumbMode)GetValue(ThumbModeProperty); }
            set { SetValue(ThumbModeProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="AspectRatio"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty AspectRatioProperty =
            DependencyProperty.Register(nameof(AspectRatio), typeof(double?), typeof(ImageCropper), new PropertyMetadata(null, OnAspectRatioChanged));

        /// <summary>
        /// Identifies the <see cref="Source"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SourceProperty =
            DependencyProperty.Register(nameof(Source), typeof(WriteableBitmap), typeof(ImageCropper), new PropertyMetadata(null, OnSourceChanged));

        /// <summary>
        /// Identifies the <see cref="CropShape"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CropShapeProperty =
            DependencyProperty.Register(nameof(CropShape), typeof(CropShape), typeof(ImageCropper), new PropertyMetadata(default(CropShape), OnCropShapeChanged));

        /// <summary>
        /// Identifies the <see cref="Mask"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MaskProperty =
            DependencyProperty.Register(nameof(Mask), typeof(Brush), typeof(ImageCropper), new PropertyMetadata(default(Brush)));

        /// <summary>
        /// Identifies the <see cref="PrimaryThumbStyle"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty PrimaryThumbStyleProperty =
            DependencyProperty.Register(nameof(PrimaryThumbStyle), typeof(Style), typeof(ImageCropper), new PropertyMetadata(default(Style)));

        /// <summary>
        /// Identifies the <see cref="SecondaryThumbStyle"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SecondaryThumbStyleProperty =
            DependencyProperty.Register(nameof(SecondaryThumbStyle), typeof(Style), typeof(ImageCropper), new PropertyMetadata(default(Style)));

        /// <summary>
        /// Identifies the <see cref="ThumbPlacement"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ThumbPlacementProperty =
            DependencyProperty.Register(nameof(ThumbPlacement), typeof(ThumbPlacement), typeof(ImageCropper), new PropertyMetadata(default(ThumbPlacement), OnThumbPlacementChanged));

        public static readonly DependencyProperty CropperEnableProperty =
            DependencyProperty.Register(nameof(CropperEnable), typeof(bool), typeof(ImageCropper), new PropertyMetadata(true, OnCropperEnableChanged));

        public static readonly DependencyProperty DragImgEnableProperty =
            DependencyProperty.Register(nameof(DragImgEnable), typeof(bool), typeof(ImageCropper), new PropertyMetadata(true));

        public static readonly DependencyProperty ThumbModeProperty =
            DependencyProperty.Register(nameof(ThumbMode), typeof(ThumbMode), typeof(ImageCropper), new PropertyMetadata(ThumbMode.Move));
    }
}
