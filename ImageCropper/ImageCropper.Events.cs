using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace ImageCropper
{
    public partial class ImageCropper
    {
        private ImageCropperThumb currentImageCropperThumb;
        /// <summary>
        /// 整个控件布局鼠标移动事件
        /// 可能是按住Thumb拖动，也可能是拖动图片
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LayoutGrid_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                var endPos = e.GetPosition(_layoutGrid);
                if (currentImageCropperThumb != null)//拖动Thumb，调整选取区域大小
                {
                    var imageCropperThumb = currentImageCropperThumb;
                    var currentPointerPosition = new Point(
                    imageCropperThumb.X + (endPos.X - StartPoint.X),
                    imageCropperThumb.Y + (endPos.Y - StartPoint.Y));
                    var safePosition = GetSafePoint(_restrictedSelectRect, currentPointerPosition);
                    var safeDiffPoint = new Point(safePosition.X - imageCropperThumb.X, safePosition.Y - imageCropperThumb.Y);
                    UpdateCroppedRect(imageCropperThumb.Position, safeDiffPoint);
                }
                else if(ThumbMode == ThumbMode.Draw && IsDrawingThumb)//移动鼠标绘制选择区
                {
                    DrawingThumb(DrawingStartPoint, endPos);
                }
                else if(DragImgEnable)//拖动图片，移动选取区域
                {
                    MoveSourceImage(StartPoint.X - endPos.X, StartPoint.Y - endPos.Y);
                }
                StartPoint = e.GetPosition(_layoutGrid);
            }
            else if(e.MiddleButton == MouseButtonState.Pressed && DragImgEnable)
            {
                var endPos = e.GetPosition(_layoutGrid);
                MoveSourceImage(StartPoint.X - endPos.X, StartPoint.Y - endPos.Y);
                StartPoint = e.GetPosition(_layoutGrid);
            }
        }
        /// <summary>
        /// 整个控件布局鼠标抬起事件
        /// 可能是结束Thumb拖动，或者结束拖动图片
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LayoutGrid_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (currentImageCropperThumb != null || IsDrawingThumb)
            {
                var selectedRect = new Rect(new Point(_startX, _startY), new Point(_endX, _endY));
                var croppedRect = _inverseImageTransform.TransformBounds(selectedRect);
                if (croppedRect.Width > MinCropSize.Width && croppedRect.Height > MinCropSize.Height)
                {
                    croppedRect.Intersect(_restrictedCropRect);
                    _currentCroppedRect = croppedRect;
                }

                UpdateImageLayout(true);
            }
            currentImageCropperThumb = null;
            IsDrawingThumb = false;
        }
        /// <summary>
        /// 鼠标在控件布局上当次移动起始坐标
        /// </summary>
        private Point StartPoint;
        /// <summary>
        /// Thumb按下，开始拖动Thumb调整选取区域
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ImageCropperThumb_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            currentImageCropperThumb = (ImageCropperThumb)sender;
            StartPoint = e.GetPosition(_layoutGrid);
        }
        private bool IsDrawingThumb = false;
        private Point DrawingStartPoint;
        /// <summary>
        /// 图片按下鼠标事件
        /// 开始拖动图片移动选取区域
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SourceImage_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if(ThumbMode == ThumbMode.Draw && e.LeftButton == MouseButtonState.Pressed && DrawingStartPoint.X == 0 && DrawingStartPoint.Y == 0)
            {
                IsDrawingThumb = true;
                DrawingStartPoint = e.GetPosition((UIElement)sender);
            }
            StartPoint = e.GetPosition((UIElement)sender);
        }
        /// <summary>
        /// 移动图片
        /// </summary>
        /// <param name="offsetX"></param>
        /// <param name="offsetY"></param>
        private void MoveSourceImage(double offsetX, double offsetY)
        {
            if (offsetX > 0)
            {
                offsetX = Math.Min(offsetX, _restrictedSelectRect.X + _restrictedSelectRect.Width - _endX);
            }
            else
            {
                offsetX = Math.Max(offsetX, _restrictedSelectRect.X - _startX);
            }

            if (offsetY > 0)
            {
                offsetY = Math.Min(offsetY, _restrictedSelectRect.Y + _restrictedSelectRect.Height - _endY);
            }
            else
            {
                offsetY = Math.Max(offsetY, _restrictedSelectRect.Y - _startY);
            }

            var selectedRect = new Rect(new Point(_startX, _startY), new Point(_endX, _endY));
            selectedRect.X += offsetX;
            selectedRect.Y += offsetY;
            var croppedRect = _inverseImageTransform.TransformBounds(selectedRect);
            croppedRect.Intersect(_restrictedCropRect);
            _currentCroppedRect = croppedRect;
            UpdateImageLayout();
        }
        /// <summary>
        /// 调整窗体、控件大小时更新图片、Thumb位置
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ImageCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (Source == null)
            {
                return;
            }

            UpdateImageLayout();
            UpdateMaskArea();
        }
        /// <summary>
        /// 滚动鼠标滚轮
        /// 缩放图片
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ImageCanvas_MouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            ScaleImage(e.Delta > 0 ? 8 : -8, e.GetPosition(_sourceImage));
        }
    }
}
