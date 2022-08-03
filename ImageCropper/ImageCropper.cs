using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ImageCropper
{
    [TemplatePart(Name = LayoutGridName, Type = typeof(Grid))]
    [TemplatePart(Name = ImageCanvasPartName, Type = typeof(Canvas))]
    [TemplatePart(Name = SourceImagePartName, Type = typeof(Image))]
    [TemplatePart(Name = MaskAreaPathPartName, Type = typeof(System.Windows.Shapes.Path))]
    [TemplatePart(Name = TopThumbPartName, Type = typeof(ImageCropperThumb))]
    [TemplatePart(Name = BottomThumbPartName, Type = typeof(ImageCropperThumb))]
    [TemplatePart(Name = LeftThumbPartName, Type = typeof(ImageCropperThumb))]
    [TemplatePart(Name = RightThumbPartName, Type = typeof(ImageCropperThumb))]
    [TemplatePart(Name = UpperLeftThumbPartName, Type = typeof(ImageCropperThumb))]
    [TemplatePart(Name = UpperRightThumbPartName, Type = typeof(ImageCropperThumb))]
    [TemplatePart(Name = LowerLeftThumbPartName, Type = typeof(ImageCropperThumb))]
    [TemplatePart(Name = LowerRightThumbPartName, Type = typeof(ImageCropperThumb))]
    public partial class ImageCropper : UserControl
    {
        /// <summary>
        /// 用于将实际图片大小rect缩放成控件上显示的rect
        /// </summary>
        private readonly CompositeTransform _imageTransform = new CompositeTransform();
        /// <summary>
        /// 用于将控件上显示的rect缩放成实际图片大小rect
        /// </summary>
        private readonly CompositeTransform _inverseImageTransform = new CompositeTransform();
        private readonly GeometryGroup _maskAreaGeometryGroup = new GeometryGroup { FillRule = FillRule.EvenOdd };

        private Grid _layoutGrid;
        private Canvas _imageCanvas;
        private Image _sourceImage;
        private Path _maskAreaPath;
        private ImageCropperThumb _topThumb;
        private ImageCropperThumb _bottomThumb;
        private ImageCropperThumb _leftThumb;
        private ImageCropperThumb _rightThumb;
        private ImageCropperThumb _upperLeftThumb;
        private ImageCropperThumb _upperRightThumb;
        private ImageCropperThumb _lowerLeftThumb;
        private ImageCropperThumb _lowerRigthThumb;
        private double _startX;
        private double _startY;
        private double _endX;
        private double _endY;
        /// <summary>
        /// 实际图片截取区域
        /// 最后取截取结果使用此rect
        /// </summary>
        private Rect _currentCroppedRect = Rect.Empty;
        private Rect _restrictedCropRect = Rect.Empty;
        private Rect _restrictedSelectRect = Rect.Empty;
        private RectangleGeometry _outerGeometry;
        private Geometry _innerGeometry;
        private TimeSpan _animationDuration = TimeSpan.FromSeconds(0.3);

        /// <summary>
        /// Initializes a new instance of the <see cref="ImageCropper"/> class.
        /// </summary>
        public ImageCropper()
        {
            DefaultStyleKey = typeof(ImageCropper);
        }

        private Rect CanvasRect => new Rect(0, 0, _imageCanvas?.ActualWidth ?? 0, _imageCanvas?.ActualHeight ?? 0);

        private bool KeepAspectRatio => UsedAspectRatio > 0;

        private double UsedAspectRatio
        {
            get
            {
                var aspectRatio = AspectRatio;
                switch (CropShape)
                {
                    case CropShape.Rectangular:
                        break;
                    case CropShape.Circular:
                        aspectRatio = 1;
                        break;
                }

                return aspectRatio != null && aspectRatio > 0 ? aspectRatio.Value : -1;
            }
        }

        /// <summary>
        /// Gets the minimum cropped size.
        /// </summary>
        private Size MinCropSize
        {
            get
            {
                var aspectRatio = KeepAspectRatio ? UsedAspectRatio : 1;
                var size = new Size(MinCroppedPixelLength, MinCroppedPixelLength);
                if (aspectRatio >= 1)
                {
                    size.Width = size.Height * aspectRatio;
                }
                else
                {
                    size.Height = size.Width / aspectRatio;
                }

                return size;
            }
        }

        /// <summary>
        /// Gets the minimum selectable size.
        /// </summary>
        private Size MinSelectSize
        {
            get
            {
                var realMinSelectSize = _imageTransform.TransformBounds(new Rect(0, 0, MinCropSize.Width, MinCropSize.Height));
                var minLength = Math.Min(realMinSelectSize.Width, realMinSelectSize.Height);
                if (minLength < MinSelectedLength)
                {
                    var aspectRatio = KeepAspectRatio ? UsedAspectRatio : 1;
                    var minSelectSize = new Size(MinSelectedLength, MinSelectedLength);
                    if (aspectRatio >= 1)
                    {
                        minSelectSize.Width = minSelectSize.Height * aspectRatio;
                    }
                    else
                    {
                        minSelectSize.Height = minSelectSize.Width / aspectRatio;
                    }

                    return minSelectSize;
                }

                return new Size(realMinSelectSize.Width, realMinSelectSize.Height);
            }
        }

        /// <inheritdoc/>
        public override void OnApplyTemplate()
        {
            UnhookEvents();
            _layoutGrid = GetTemplateChild(LayoutGridName) as Grid;
            _imageCanvas = GetTemplateChild(ImageCanvasPartName) as Canvas;
            _sourceImage = GetTemplateChild(SourceImagePartName) as Image;
            _maskAreaPath = GetTemplateChild(MaskAreaPathPartName) as Path;
            _topThumb = GetTemplateChild(TopThumbPartName) as ImageCropperThumb;
            _bottomThumb = GetTemplateChild(BottomThumbPartName) as ImageCropperThumb;
            _leftThumb = GetTemplateChild(LeftThumbPartName) as ImageCropperThumb;
            _rightThumb = GetTemplateChild(RightThumbPartName) as ImageCropperThumb;
            _upperLeftThumb = GetTemplateChild(UpperLeftThumbPartName) as ImageCropperThumb;
            _upperRightThumb = GetTemplateChild(UpperRightThumbPartName) as ImageCropperThumb;
            _lowerLeftThumb = GetTemplateChild(LowerLeftThumbPartName) as ImageCropperThumb;
            _lowerRigthThumb = GetTemplateChild(LowerRightThumbPartName) as ImageCropperThumb;
            HookUpEvents();
            UpdateThumbsVisibility();
        }

        private void HookUpEvents()
        {
            if (_layoutGrid != null)
            {
                _layoutGrid.MouseMove += LayoutGrid_MouseMove;
                _layoutGrid.MouseUp += LayoutGrid_MouseUp;
            }
            if (_imageCanvas != null)
            {
                _imageCanvas.SizeChanged += ImageCanvas_SizeChanged;
                _imageCanvas.MouseDown += SourceImage_MouseDown;
                _imageCanvas.MouseWheel += ImageCanvas_MouseWheel;
            }

            if (_maskAreaPath != null)
            {
                _maskAreaPath.Data = _maskAreaGeometryGroup;
            }

            if (_topThumb != null)
            {
                _topThumb.Position = ThumbPosition.Top;
                _topThumb.MouseDown += ImageCropperThumb_MouseDown;
            }

            if (_bottomThumb != null)
            {
                _bottomThumb.Position = ThumbPosition.Bottom;
                _bottomThumb.MouseDown += ImageCropperThumb_MouseDown;
            }

            if (_leftThumb != null)
            {
                _leftThumb.Position = ThumbPosition.Left;
                _leftThumb.MouseDown += ImageCropperThumb_MouseDown;
            }

            if (_rightThumb != null)
            {
                _rightThumb.Position = ThumbPosition.Right;
                _rightThumb.MouseDown += ImageCropperThumb_MouseDown;
            }

            if (_upperLeftThumb != null)
            {
                _upperLeftThumb.Position = ThumbPosition.UpperLeft;
                _upperLeftThumb.MouseDown += ImageCropperThumb_MouseDown;
            }

            if (_upperRightThumb != null)
            {
                _upperRightThumb.Position = ThumbPosition.UpperRight;
                _upperRightThumb.MouseDown += ImageCropperThumb_MouseDown;
            }

            if (_lowerLeftThumb != null)
            {
                _lowerLeftThumb.Position = ThumbPosition.LowerLeft;
                _lowerLeftThumb.MouseDown += ImageCropperThumb_MouseDown;
            }

            if (_lowerRigthThumb != null)
            {
                _lowerRigthThumb.Position = ThumbPosition.LowerRight;
                _lowerRigthThumb.MouseDown += ImageCropperThumb_MouseDown;
            }
        }

        private void UnhookEvents()
        {
            if (_layoutGrid != null)
            {
                _layoutGrid.MouseMove -= LayoutGrid_MouseMove;
                _layoutGrid.MouseUp -= LayoutGrid_MouseUp;
            }
            if (_imageCanvas != null)
            {
                _imageCanvas.SizeChanged -= ImageCanvas_SizeChanged;
                _imageCanvas.MouseWheel -= ImageCanvas_MouseWheel;
            }

            if (_maskAreaPath != null)
            {
                _maskAreaPath.Data = null;
            }

            if (_topThumb != null)
            {
                _topThumb.MouseDown -= ImageCropperThumb_MouseDown;
            }

            if (_bottomThumb != null)
            {
                _bottomThumb.MouseDown -= ImageCropperThumb_MouseDown;
            }

            if (_leftThumb != null)
            {
                _leftThumb.MouseDown -= ImageCropperThumb_MouseDown;
            }

            if (_rightThumb != null)
            {
                _rightThumb.MouseDown -= ImageCropperThumb_MouseDown;
            }

            if (_upperLeftThumb != null)
            {
                _upperLeftThumb.MouseDown -= ImageCropperThumb_MouseDown;
            }

            if (_upperRightThumb != null)
            {
                _upperRightThumb.MouseDown -= ImageCropperThumb_MouseDown;
            }

            if (_lowerLeftThumb != null)
            {
                _lowerLeftThumb.MouseDown -= ImageCropperThumb_MouseDown;
            }

            if (_lowerRigthThumb != null)
            {
                _lowerRigthThumb.MouseDown -= ImageCropperThumb_MouseDown;
            }
        }

        /// <inheritdoc/>
        protected override Size MeasureOverride(Size availableSize)
        {
            if (Source == null || Source.PixelWidth == 0 || Source.PixelHeight == 0)
            {
                return base.MeasureOverride(availableSize);
            }

            if (double.IsInfinity(availableSize.Width) || double.IsInfinity(availableSize.Height))
            {
                if (!double.IsInfinity(availableSize.Width))
                {
                    availableSize.Height = availableSize.Width / Source.PixelWidth * Source.PixelHeight;
                }
                else if (!double.IsInfinity(availableSize.Height))
                {
                    availableSize.Width = availableSize.Height / Source.PixelHeight * Source.PixelWidth;
                }
                else
                {
                    availableSize.Width = Source.PixelWidth;
                    availableSize.Height = Source.PixelHeight;
                }

                base.MeasureOverride(availableSize);
                return availableSize;
            }

            return base.MeasureOverride(availableSize);
        }

        /// <summary>
        /// Load an image from a file.
        /// </summary>
        /// <param name="imageFile">The image file.</param>
        /// <returns>Task</returns>
        public void LoadImageFromFile(string imageFile)
        {
            if (!string.IsNullOrEmpty(imageFile))
            {
                try
                {
                    System.Drawing.Bitmap bitmap = new System.Drawing.Bitmap(imageFile);
                    Source = BitmapToWriteableBitmap(bitmap);
                }
                catch (Exception)
                {

                }
            }
            else
            {
                Source = null;
            }
        }
        //将Bitmap 转换成WriteableBitmap 
        public static WriteableBitmap BitmapToWriteableBitmap(System.Drawing.Bitmap src)
        {
            var wb = CreateCompatibleWriteableBitmap(src);
            System.Drawing.Imaging.PixelFormat format = src.PixelFormat;
            if (wb == null)
            {
                wb = new WriteableBitmap(src.Width, src.Height, 0, 0, System.Windows.Media.PixelFormats.Bgra32, null);
                format = System.Drawing.Imaging.PixelFormat.Format32bppArgb;
            }
            BitmapCopyToWriteableBitmap(src, wb, new System.Drawing.Rectangle(0, 0, src.Width, src.Height), 0, 0, format);
            return wb;
        }
        //创建尺寸和格式与Bitmap兼容的WriteableBitmap
        public static WriteableBitmap CreateCompatibleWriteableBitmap(System.Drawing.Bitmap src)
        {
            System.Windows.Media.PixelFormat format;
            switch (src.PixelFormat)
            {
                case System.Drawing.Imaging.PixelFormat.Format16bppRgb555:
                    format = System.Windows.Media.PixelFormats.Bgr555;
                    break;
                case System.Drawing.Imaging.PixelFormat.Format16bppRgb565:
                    format = System.Windows.Media.PixelFormats.Bgr565;
                    break;
                case System.Drawing.Imaging.PixelFormat.Format24bppRgb:
                    format = System.Windows.Media.PixelFormats.Bgr24;
                    break;
                case System.Drawing.Imaging.PixelFormat.Format32bppRgb:
                    format = System.Windows.Media.PixelFormats.Bgr32;
                    break;
                case System.Drawing.Imaging.PixelFormat.Format32bppPArgb:
                    format = System.Windows.Media.PixelFormats.Pbgra32;
                    break;
                case System.Drawing.Imaging.PixelFormat.Format32bppArgb:
                    format = System.Windows.Media.PixelFormats.Bgra32;
                    break;
                default:
                    return null;
            }
            return new WriteableBitmap(src.Width, src.Height, 0, 0, format, null);
        }
        //将Bitmap数据写入WriteableBitmap中
        public static void BitmapCopyToWriteableBitmap(System.Drawing.Bitmap src, WriteableBitmap dst, System.Drawing.Rectangle srcRect, int destinationX, int destinationY, System.Drawing.Imaging.PixelFormat srcPixelFormat)
        {
            var data = src.LockBits(new System.Drawing.Rectangle(new System.Drawing.Point(0, 0), src.Size), System.Drawing.Imaging.ImageLockMode.ReadOnly, srcPixelFormat);
            dst.WritePixels(new Int32Rect(srcRect.X, srcRect.Y, srcRect.Width, srcRect.Height), data.Scan0, data.Height * data.Stride, data.Stride, destinationX, destinationY);
            src.UnlockBits(data);
        }

        /// <summary>
        /// Reset the cropped area.
        /// </summary>
        public void Reset()
        {
            InitImageLayout(true);
        }

        /// <summary>
        /// Tries to set a new value for the cropped region, returns true if it succeeded, false if the region is invalid
        /// </summary>
        /// <param name="rect">The new cropped region.</param>
        /// <returns>bool</returns>
        public bool TrySetCroppedRegion(Rect rect)
        {
            // Reject regions smaller than the minimum size
            if (rect.Width < MinCropSize.Width || rect.Height < MinCropSize.Height)
            {
                return false;
            }

            // Reject regions that are not contained in the original picture
            if (rect.Left < _restrictedCropRect.Left || rect.Top < _restrictedCropRect.Top || rect.Right > _restrictedCropRect.Right || rect.Bottom > _restrictedCropRect.Bottom)
            {
                return false;
            }

            // If an aspect ratio is set, reject regions that don't respect it
            // If cropping a circle, reject regions where the aspect ratio is not 1
            if (KeepAspectRatio && UsedAspectRatio != rect.Width / rect.Height)
            {
                return false;
            }

            _currentCroppedRect = rect;
            UpdateImageLayout(true);
            return true;
        }
    }
}
