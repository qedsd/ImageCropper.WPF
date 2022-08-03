using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace ImageCropper
{
    public partial class ImageCropper
    {
        /// <summary>
        /// Initializes image source transform.
        /// </summary>
        /// <param name="animate">Whether animation is enabled.</param>
        private void InitImageLayout(bool animate = false)
        {
            if (Source != null)
            {
                _restrictedCropRect = new Rect(0, 0, Source.PixelWidth, Source.PixelHeight);
                if (IsValidRect(_restrictedCropRect))
                {
                    _currentCroppedRect = KeepAspectRatio ? GetUniformRect(_restrictedCropRect, UsedAspectRatio) : _restrictedCropRect;
                    UpdateImageLayout(animate);
                }
            }

            UpdateThumbsVisibility();
        }

        /// <summary>
        /// Update image source transform.
        /// </summary>
        /// <param name="animate">Whether animation is enabled.</param>
        private void UpdateImageLayout(bool animate = false)
        {
            if (Source != null && IsValidRect(CanvasRect))
            {
                var uniformSelectedRect = GetUniformRect(CanvasRect, _currentCroppedRect.Width / _currentCroppedRect.Height);
                UpdateImageLayoutWithViewport(uniformSelectedRect, _currentCroppedRect, animate);
            }
        }

        /// <summary>
        /// Update image source transform.
        /// </summary>
        /// <param name="viewport">Viewport</param>
        /// <param name="viewportImageRect"> The real image area of viewport.</param>
        /// <param name="animate">Whether animation is enabled.</param>
        private void UpdateImageLayoutWithViewport(Rect viewport, Rect viewportImageRect, bool animate = false)
        {
            if (!IsValidRect(viewport) || !IsValidRect(viewportImageRect))
            {
                return;
            }

            var imageScale = viewport.Width / viewportImageRect.Width;
            _imageTransform.ScaleX = _imageTransform.ScaleY = imageScale;
            _imageTransform.TranslateX = viewport.X - (viewportImageRect.X * imageScale);
            _imageTransform.TranslateY = viewport.Y - (viewportImageRect.Y * imageScale);
            _inverseImageTransform.ScaleX = _inverseImageTransform.ScaleY = 1 / imageScale;
            _inverseImageTransform.TranslateX = -_imageTransform.TranslateX / imageScale;
            _inverseImageTransform.TranslateY = -_imageTransform.TranslateY / imageScale;
            var selectedRect = _imageTransform.TransformBounds(_currentCroppedRect);
            _restrictedSelectRect = _imageTransform.TransformBounds(_restrictedCropRect);
            var startPoint = GetSafePoint(_restrictedSelectRect, new Point(selectedRect.X, selectedRect.Y));
            var endPoint = GetSafePoint(_restrictedSelectRect, new Point(
                selectedRect.X + selectedRect.Width,
                selectedRect.Y + selectedRect.Height));
            if (animate)
            {
                AnimateUIElementOffset(new Point(_imageTransform.TranslateX, _imageTransform.TranslateY), _animationDuration, _sourceImage);
                AnimateUIElementScale(imageScale, _animationDuration, _sourceImage);
            }
            else
            {
                TransformGroup transformGroup = new TransformGroup();
                transformGroup.Children.Add(new ScaleTransform(imageScale, imageScale));
                transformGroup.Children.Add(new TranslateTransform(_imageTransform.TranslateX, _imageTransform.TranslateY));

                _sourceImage.RenderTransform = transformGroup;
            }

            UpdateSelectedRect(startPoint, endPoint, animate);
        }

        /// <summary>
        /// Update cropped area.
        /// </summary>
        /// <param name="position">The control point</param>
        /// <param name="diffPos">Position offset</param>
        private void UpdateCroppedRect(ThumbPosition position, Point diffPos)
        {
            if (diffPos == default(Point) || !IsValidRect(CanvasRect))
            {
                return;
            }

            double radian = 0d, diffPointRadian = 0d;
            if (KeepAspectRatio)
            {
                radian = Math.Atan(UsedAspectRatio);
                diffPointRadian = Math.Atan(diffPos.X / diffPos.Y);
            }

            var startPoint = new Point(_startX, _startY);
            var endPoint = new Point(_endX, _endY);
            var currentSelectedRect = new Rect(startPoint, endPoint);
            switch (position)
            {
                case ThumbPosition.Top:
                    if (KeepAspectRatio)
                    {
                        var originSizeChange = new Point(-diffPos.Y * UsedAspectRatio, -diffPos.Y);
                        var safeChange = GetSafeSizeChangeWhenKeepAspectRatio(_restrictedSelectRect, position, currentSelectedRect, originSizeChange, UsedAspectRatio);
                        startPoint.X += -safeChange.X / 2;
                        endPoint.X += safeChange.X / 2;
                        startPoint.Y += -safeChange.Y;
                    }
                    else
                    {
                        startPoint.Y += diffPos.Y;
                    }

                    break;
                case ThumbPosition.Bottom:
                    if (KeepAspectRatio)
                    {
                        var originSizeChange = new Point(diffPos.Y * UsedAspectRatio, diffPos.Y);
                        var safeChange = GetSafeSizeChangeWhenKeepAspectRatio(_restrictedSelectRect, position, currentSelectedRect, originSizeChange, UsedAspectRatio);
                        startPoint.X += -safeChange.X / 2;
                        endPoint.X += safeChange.X / 2;
                        endPoint.Y += safeChange.Y;
                    }
                    else
                    {
                        endPoint.Y += diffPos.Y;
                    }

                    break;
                case ThumbPosition.Left:
                    if (KeepAspectRatio)
                    {
                        var originSizeChange = new Point(-diffPos.X, -diffPos.X / UsedAspectRatio);
                        var safeChange = GetSafeSizeChangeWhenKeepAspectRatio(_restrictedSelectRect, position, currentSelectedRect, originSizeChange, UsedAspectRatio);
                        startPoint.Y += -safeChange.Y / 2;
                        endPoint.Y += safeChange.Y / 2;
                        startPoint.X += -safeChange.X;
                    }
                    else
                    {
                        startPoint.X += diffPos.X;
                    }

                    break;
                case ThumbPosition.Right:
                    if (KeepAspectRatio)
                    {
                        var originSizeChange = new Point(diffPos.X, diffPos.X / UsedAspectRatio);
                        var safeChange = GetSafeSizeChangeWhenKeepAspectRatio(_restrictedSelectRect, position, currentSelectedRect, originSizeChange, UsedAspectRatio);
                        startPoint.Y += -safeChange.Y / 2;
                        endPoint.Y += safeChange.Y / 2;
                        endPoint.X += safeChange.X;
                    }
                    else
                    {
                        endPoint.X += diffPos.X;
                    }

                    break;
                case ThumbPosition.UpperLeft:
                    if (KeepAspectRatio)
                    {
                        var effectiveLength = diffPos.Y / Math.Cos(diffPointRadian) * Math.Cos(diffPointRadian - radian);
                        var originSizeChange = new Point(-effectiveLength * Math.Sin(radian), -effectiveLength * Math.Cos(radian));
                        var safeChange = GetSafeSizeChangeWhenKeepAspectRatio(_restrictedSelectRect, position, currentSelectedRect, originSizeChange, UsedAspectRatio);
                        diffPos.X = -safeChange.X;
                        diffPos.Y = -safeChange.Y;
                    }

                    startPoint.X += diffPos.X;
                    startPoint.Y += diffPos.Y;
                    break;
                case ThumbPosition.UpperRight:
                    if (KeepAspectRatio)
                    {
                        diffPointRadian = -diffPointRadian;
                        var effectiveLength = diffPos.Y / Math.Cos(diffPointRadian) * Math.Cos(diffPointRadian - radian);
                        var originSizeChange = new Point(-effectiveLength * Math.Sin(radian), -effectiveLength * Math.Cos(radian));
                        var safeChange = GetSafeSizeChangeWhenKeepAspectRatio(_restrictedSelectRect, position, currentSelectedRect, originSizeChange, UsedAspectRatio);
                        diffPos.X = safeChange.X;
                        diffPos.Y = -safeChange.Y;
                    }

                    endPoint.X += diffPos.X;
                    startPoint.Y += diffPos.Y;
                    break;
                case ThumbPosition.LowerLeft:
                    if (KeepAspectRatio)
                    {
                        diffPointRadian = -diffPointRadian;
                        var effectiveLength = diffPos.Y / Math.Cos(diffPointRadian) * Math.Cos(diffPointRadian - radian);
                        var originSizeChange = new Point(effectiveLength * Math.Sin(radian), effectiveLength * Math.Cos(radian));
                        var safeChange = GetSafeSizeChangeWhenKeepAspectRatio(_restrictedSelectRect, position, currentSelectedRect, originSizeChange, UsedAspectRatio);
                        diffPos.X = -safeChange.X;
                        diffPos.Y = safeChange.Y;
                    }

                    startPoint.X += diffPos.X;
                    endPoint.Y += diffPos.Y;
                    break;
                case ThumbPosition.LowerRight:
                    if (KeepAspectRatio)
                    {
                        var effectiveLength = diffPos.Y / Math.Cos(diffPointRadian) * Math.Cos(diffPointRadian - radian);
                        var originSizeChange = new Point(effectiveLength * Math.Sin(radian), effectiveLength * Math.Cos(radian));
                        var safeChange = GetSafeSizeChangeWhenKeepAspectRatio(_restrictedSelectRect, position, currentSelectedRect, originSizeChange, UsedAspectRatio);
                        diffPos.X = safeChange.X;
                        diffPos.Y = safeChange.Y;
                    }

                    endPoint.X += diffPos.X;
                    endPoint.Y += diffPos.Y;
                    break;
            }

            if (!IsSafeRect(startPoint, endPoint, MinSelectSize))
            {
                if (KeepAspectRatio)
                {
                    if ((endPoint.Y - startPoint.Y) < (_endY - _startY) ||
                        (endPoint.X - startPoint.X) < (_endX - _startX))
                    {
                        return;
                    }
                }
                else
                {
                    var safeRect = GetSafeRect(startPoint, endPoint, MinSelectSize, position);
                    safeRect.Intersect(_restrictedSelectRect);
                    startPoint = new Point(safeRect.X, safeRect.Y);
                    endPoint = new Point(safeRect.X + safeRect.Width, safeRect.Y + safeRect.Height);
                }
            }

            var isEffectiveRegion = IsSafePoint(_restrictedSelectRect, startPoint) &&
                                    IsSafePoint(_restrictedSelectRect, endPoint);
            var selectedRect = new Rect(startPoint, endPoint);
            if (!isEffectiveRegion)
            {
                if (!IsCornerThumb(position) && TryGetContainedRect(_restrictedSelectRect, ref selectedRect))
                {
                    startPoint = new Point(selectedRect.Left, selectedRect.Top);
                    endPoint = new Point(selectedRect.Right, selectedRect.Bottom);
                }
                else
                {
                    return;
                }
            }

            selectedRect.Union(CanvasRect);
            if (selectedRect != CanvasRect)
            {
                UpdateSelectedRect(startPoint, endPoint);
            }
            else
            {
                UpdateSelectedRect(startPoint, endPoint);
            }
        }

        /// <summary>
        /// Update selection area.
        /// </summary>
        /// <param name="startPoint">The point on the upper left corner.</param>
        /// <param name="endPoint">The point on the lower right corner.</param>
        /// <param name="animate">Whether animation is enabled.</param>
        private void UpdateSelectedRect(Point startPoint, Point endPoint, bool animate = false)
        {
            _startX = startPoint.X;
            _startY = startPoint.Y;
            _endX = endPoint.X;
            _endY = endPoint.Y;
            var centerX = ((_endX - _startX) / 2) + _startX;
            var centerY = ((_endY - _startY) / 2) + _startY;
            Storyboard storyboard = null;
            if (animate)
            {
                storyboard = new Storyboard();
            }

            if (_topThumb != null)
            {
                if (animate)
                {
                    storyboard.Children.Add(CreateDoubleAnimation(centerX, _animationDuration, _topThumb, nameof(ImageCropperThumb.X)));
                    storyboard.Children.Add(CreateDoubleAnimation(_startY, _animationDuration, _topThumb, nameof(ImageCropperThumb.Y)));
                }
                else
                {
                    _topThumb.UpdatePosition(centerX, _startY);
                }
            }

            if (_bottomThumb != null)
            {
                if (animate)
                {
                    storyboard.Children.Add(CreateDoubleAnimation(centerX, _animationDuration, _bottomThumb, nameof(ImageCropperThumb.X)));
                    storyboard.Children.Add(CreateDoubleAnimation(_endY, _animationDuration, _bottomThumb, nameof(ImageCropperThumb.Y)));
                }
                else
                {
                    _bottomThumb.UpdatePosition(centerX, _endY);
                }
            }

            if (_leftThumb != null)
            {
                if (animate)
                {
                    storyboard.Children.Add(CreateDoubleAnimation(_startX, _animationDuration, _leftThumb, nameof(ImageCropperThumb.X)));
                    storyboard.Children.Add(CreateDoubleAnimation(centerY, _animationDuration, _leftThumb, nameof(ImageCropperThumb.Y)));
                }
                else
                {
                    _leftThumb.UpdatePosition(_startX, centerY);
                }
            }

            if (_rightThumb != null)
            {
                if (animate)
                {
                    storyboard.Children.Add(CreateDoubleAnimation(_endX, _animationDuration, _rightThumb, nameof(ImageCropperThumb.X)));
                    storyboard.Children.Add(CreateDoubleAnimation(centerY, _animationDuration, _rightThumb, nameof(ImageCropperThumb.Y)));
                }
                else
                {
                    _rightThumb.UpdatePosition(_endX, centerY);
                }
            }

            if (_upperLeftThumb != null)
            {
                if (animate)
                {
                    storyboard.Children.Add(CreateDoubleAnimation(_startX, _animationDuration, _upperLeftThumb, nameof(ImageCropperThumb.X)));
                    storyboard.Children.Add(CreateDoubleAnimation(_startY, _animationDuration, _upperLeftThumb, nameof(ImageCropperThumb.Y)));
                }
                else
                {
                    _upperLeftThumb.UpdatePosition(_startX, _startY);
                }
            }

            if (_upperRightThumb != null)
            {
                if (animate)
                {
                    storyboard.Children.Add(CreateDoubleAnimation(_endX, _animationDuration, _upperRightThumb, nameof(ImageCropperThumb.X)));
                    storyboard.Children.Add(CreateDoubleAnimation(_startY, _animationDuration, _upperRightThumb, nameof(ImageCropperThumb.Y)));
                }
                else
                {
                    _upperRightThumb.UpdatePosition(_endX, _startY);
                }
            }

            if (_lowerLeftThumb != null)
            {
                if (animate)
                {
                    storyboard.Children.Add(CreateDoubleAnimation(_startX, _animationDuration, _lowerLeftThumb, nameof(ImageCropperThumb.X)));
                    storyboard.Children.Add(CreateDoubleAnimation(_endY, _animationDuration, _lowerLeftThumb, nameof(ImageCropperThumb.Y)));
                }
                else
                {
                    _lowerLeftThumb.UpdatePosition(_startX, _endY);
                }
            }

            if (_lowerRigthThumb != null)
            {
                if (animate)
                {
                    storyboard.Children.Add(CreateDoubleAnimation(_endX, _animationDuration, _lowerRigthThumb, nameof(ImageCropperThumb.X)));
                    storyboard.Children.Add(CreateDoubleAnimation(_endY, _animationDuration, _lowerRigthThumb, nameof(ImageCropperThumb.Y)));
                }
                else
                {
                    _lowerRigthThumb.UpdatePosition(_endX, _endY);
                }
            }

            if (animate)
            {
                storyboard.Completed += (s, e) =>
                {
                    //移除整个演示图板来禁止动画影响属性，并会自动恢复此次动画前的位置，然后代码修改每个thumb位置
                    storyboard.Remove();
                    UpdateSelectedRect();
                };
                storyboard.Begin();
            }

            UpdateMaskArea(animate);
        }
        private void UpdateSelectedRect()
        {
            var centerX = ((_endX - _startX) / 2) + _startX;
            var centerY = ((_endY - _startY) / 2) + _startY;
            _topThumb.UpdatePosition(centerX, _startY);
            _bottomThumb.UpdatePosition(centerX, _endY);
            _leftThumb.UpdatePosition(_startX, centerY);
            _rightThumb.UpdatePosition(_endX, centerY);
            _upperLeftThumb.UpdatePosition(_startX, _startY);
            _upperRightThumb.UpdatePosition(_endX, _startY);
            _lowerLeftThumb.UpdatePosition(_startX, _endY);
            _lowerRigthThumb.UpdatePosition(_endX, _endY);
        }

        /// <summary>
        /// Update crop shape.
        /// </summary>
        private void UpdateCropShape()
        {
            _maskAreaGeometryGroup.Children.Clear();
            _outerGeometry = new RectangleGeometry();
            switch (CropShape)
            {
                case CropShape.Rectangular:
                    _innerGeometry = new RectangleGeometry();
                    break;
                case CropShape.Circular:
                    _innerGeometry = new EllipseGeometry();
                    break;
            }

            _maskAreaGeometryGroup.Children.Add(_outerGeometry);
            _maskAreaGeometryGroup.Children.Add(_innerGeometry);
        }

        /// <summary>
        /// Update the mask layer.
        /// 更新遮挡层位置
        /// </summary>
        private void UpdateMaskArea(bool animate = false)
        {
            if (_layoutGrid == null || _maskAreaGeometryGroup.Children.Count < 2 || !CropperEnable)
            {
                return;
            }
            _outerGeometry.Rect = new Rect(0, 0, _layoutGrid.ActualWidth, _layoutGrid.ActualHeight);
            switch (CropShape)
            {
                case CropShape.Rectangular:
                    if (_innerGeometry is RectangleGeometry rectangleGeometry)
                    {
                        var to = new Rect(new Point(_startX, _startY), new Point(_endX, _endY));
                        if (animate)
                        {
                            StartRectangleAnimation(to, _animationDuration, rectangleGeometry);
                        }
                        else
                        {
                            rectangleGeometry.Rect = to;
                        }
                    }

                    break;
                case CropShape.Circular:
                    if (_innerGeometry is EllipseGeometry ellipseGeometry)
                    {
                        var center = new Point(((_endX - _startX) / 2) + _startX, ((_endY - _startY) / 2) + _startY);
                        var radiusX = (_endX - _startX) / 2;
                        var radiusY = (_endY - _startY) / 2;
                        if (animate)
                        {
                            StartCircularAnimation(center, radiusX, radiusY, _animationDuration, ellipseGeometry);
                        }
                        else
                        {
                            ellipseGeometry.Center = center;
                            ellipseGeometry.RadiusX = radiusX;
                            ellipseGeometry.RadiusY = radiusY;
                        }
                    }

                    break;
            }

            _layoutGrid.Clip = new RectangleGeometry
            {
                Rect = new Rect(0, 0, _layoutGrid.ActualWidth, _layoutGrid.ActualHeight)
            };
        }

        /// <summary>
        /// Update image aspect ratio.
        /// </summary>
        private void UpdateAspectRatio(bool animate = false)
        {
            if (KeepAspectRatio && Source != null && IsValidRect(_restrictedSelectRect))
            {
                var centerX = ((_endX - _startX) / 2) + _startX;
                var centerY = ((_endY - _startY) / 2) + _startY;
                var restrictedMinLength = MinCroppedPixelLength * _imageTransform.ScaleX;
                var maxSelectedLength = Math.Max(_endX - _startX, _endY - _startY);
                var viewRect = new Rect(centerX - (maxSelectedLength / 2), centerY - (maxSelectedLength / 2), maxSelectedLength, maxSelectedLength);
                var uniformSelectedRect = GetUniformRect(viewRect, UsedAspectRatio);
                if (uniformSelectedRect.Width > _restrictedSelectRect.Width || uniformSelectedRect.Height > _restrictedSelectRect.Height)
                {
                    uniformSelectedRect = GetUniformRect(_restrictedSelectRect, UsedAspectRatio);
                }

                if (uniformSelectedRect.Width < restrictedMinLength || uniformSelectedRect.Height < restrictedMinLength)
                {
                    var scale = restrictedMinLength / Math.Min(uniformSelectedRect.Width, uniformSelectedRect.Height);
                    uniformSelectedRect.Width *= scale;
                    uniformSelectedRect.Height *= scale;
                    if (uniformSelectedRect.Width > _restrictedSelectRect.Width || uniformSelectedRect.Height > _restrictedSelectRect.Height)
                    {
                        AspectRatio = -1;
                        return;
                    }
                }

                if (_restrictedSelectRect.X > uniformSelectedRect.X)
                {
                    uniformSelectedRect.X += _restrictedSelectRect.X - uniformSelectedRect.X;
                }

                if (_restrictedSelectRect.Y > uniformSelectedRect.Y)
                {
                    uniformSelectedRect.Y += _restrictedSelectRect.Y - uniformSelectedRect.Y;
                }

                if ((_restrictedSelectRect.X + _restrictedSelectRect.Width) < (uniformSelectedRect.X + uniformSelectedRect.Width))
                {
                    uniformSelectedRect.X += (_restrictedSelectRect.X + _restrictedSelectRect.Width) - (uniformSelectedRect.X + uniformSelectedRect.Width);
                }

                if ((_restrictedSelectRect.Y + _restrictedSelectRect.Height) < (uniformSelectedRect.Y + uniformSelectedRect.Height))
                {
                    uniformSelectedRect.Y += (_restrictedSelectRect.Y + _restrictedSelectRect.Height) - (uniformSelectedRect.Y + uniformSelectedRect.Height);
                }

                var croppedRect = _inverseImageTransform.TransformBounds(uniformSelectedRect);
                croppedRect.Intersect(_restrictedCropRect);
                _currentCroppedRect = croppedRect;
                UpdateImageLayout(animate);
            }
        }

        /// <summary>
        /// Update the visibility of the thumbs.
        /// </summary>
        private void UpdateThumbsVisibility()
        {
            if(!CropperEnable)
            {
                UpdateThumbsVisibility(Visibility.Collapsed);
                return;
            }
            var cornerThumbsVisibility = Visibility.Visible;
            var otherThumbsVisibility = Visibility.Visible;
            switch (ThumbPlacement)
            {
                case ThumbPlacement.All:
                    break;
                case ThumbPlacement.Corners:
                    otherThumbsVisibility = Visibility.Collapsed;
                    break;
            }

            switch (CropShape)
            {
                case CropShape.Rectangular:
                    break;
                case CropShape.Circular:
                    cornerThumbsVisibility = Visibility.Collapsed;
                    otherThumbsVisibility = Visibility.Visible;
                    break;
            }

            if (Source == null)
            {
                cornerThumbsVisibility = otherThumbsVisibility = Visibility.Collapsed;
            }

            if (_topThumb != null)
            {
                _topThumb.Visibility = otherThumbsVisibility;
            }

            if (_bottomThumb != null)
            {
                _bottomThumb.Visibility = otherThumbsVisibility;
            }

            if (_leftThumb != null)
            {
                _leftThumb.Visibility = otherThumbsVisibility;
            }

            if (_rightThumb != null)
            {
                _rightThumb.Visibility = otherThumbsVisibility;
            }

            if (_upperLeftThumb != null)
            {
                _upperLeftThumb.Visibility = cornerThumbsVisibility;
            }

            if (_upperRightThumb != null)
            {
                _upperRightThumb.Visibility = cornerThumbsVisibility;
            }

            if (_lowerLeftThumb != null)
            {
                _lowerLeftThumb.Visibility = cornerThumbsVisibility;
            }

            if (_lowerRigthThumb != null)
            {
                _lowerRigthThumb.Visibility = cornerThumbsVisibility;
            }
        }

        /// <summary>
        /// 缩放图片
        /// </summary>
        /// <param name="s">当前缩放的百分比，0-100</param>
        /// <param name="center">缩放中心</param>
        private void ScaleImage(int s, Point center)
        {
            var centerP = _imageTransform.TransformPoint(center);
            double leftXPer = (centerP.X - _startX) / (_endX - _startX);
            double leftYPer = 1 - leftXPer;
            double topYPer = (centerP.Y - _startY) / (_endY - _startY);
            double bottomYPer = 1 - topYPer;
            double per = s / 100f;
            double movedXUnit = (_endX - _startX) * per;
            double movedYUnit = (_endY - _startY) * per;
            double lw = movedXUnit * leftXPer;
            double rw = movedXUnit * leftYPer;
            double th = movedYUnit * topYPer;
            double bh = movedYUnit * bottomYPer;
            var selectedRect = new Rect(new Point(_startX + lw, _startY + th), new Point(_endX - rw, _endY - bh));
            var croppedRect = _inverseImageTransform.TransformBounds(selectedRect);
            if (croppedRect.Width > MinCropSize.Width && croppedRect.Height > MinCropSize.Height)
            {
                croppedRect.Intersect(_restrictedCropRect);
                _currentCroppedRect = croppedRect;
            }
            UpdateImageLayout(false);
        }

        /// <summary>
        /// 允许裁剪
        /// </summary>
        private void EnableCropper()
        {
            UpdateThumbsVisibility();
            EnableMaskArea();
            var startPoint = new Point(_startX, _startY);
            var endPoint = new Point(_endX, _endY);
            UpdateSelectedRect(startPoint, endPoint);
        }
        /// <summary>
        /// 不允许裁剪
        /// </summary>
        private void DisableCropper()
        {
            UpdateThumbsVisibility(Visibility.Collapsed);
            DisableMaskArea();
        }

        private void UpdateThumbsVisibility(Visibility visibility)
        {
            if (_topThumb != null)
            {
                _topThumb.Visibility = visibility;
            }

            if (_bottomThumb != null)
            {
                _bottomThumb.Visibility = visibility;
            }

            if (_leftThumb != null)
            {
                _leftThumb.Visibility = visibility;
            }

            if (_rightThumb != null)
            {
                _rightThumb.Visibility = visibility;
            }

            if (_upperLeftThumb != null)
            {
                _upperLeftThumb.Visibility = visibility;
            }

            if (_upperRightThumb != null)
            {
                _upperRightThumb.Visibility = visibility;
            }

            if (_lowerLeftThumb != null)
            {
                _lowerLeftThumb.Visibility = visibility;
            }

            if (_lowerRigthThumb != null)
            {
                _lowerRigthThumb.Visibility = visibility;
            }
        }

        private void EnableMaskArea()
        {
            _maskAreaGeometryGroup.Children.Add(_outerGeometry);
            _maskAreaGeometryGroup.Children.Add(_innerGeometry);
        }

        private void DisableMaskArea()
        {
            _maskAreaGeometryGroup.Children.Clear();
        }
    }
}
