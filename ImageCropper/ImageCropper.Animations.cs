using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
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
        /// 释放拖动后的图片移动动画
        /// </summary>
        /// <param name="to"></param>
        /// <param name="duration"></param>
        /// <param name="target"></param>
        private static void AnimateUIElementOffset(Point to, TimeSpan duration, FrameworkElement target)
        {
            //与uwp原始代码不同，这里的RenderTransform是一个包含了TranslateTransform和ScaleTransform的TransformGroup
            //因为在ImageCropper.Logic.cs的UpdateImageLayoutWithViewport方法里面重新实现了targetVisual的修改
            var group = target.RenderTransform as TransformGroup;
            if(group != null)
            {
                var translateTransform = group.Children.FirstOrDefault(p => p.GetType() == typeof(TranslateTransform)) as TranslateTransform;
                if (translateTransform != null)
                {
                    EasingFunctionBase easeFunction = new PowerEase()
                    {
                        EasingMode = EasingMode.EaseInOut,
                        Power = 5
                    };

                    DoubleAnimation transAnimationX = new DoubleAnimation()
                    {
                        From = translateTransform.X,
                        To = to.X,
                        FillBehavior = FillBehavior.HoldEnd,
                        Duration = duration,
                        EasingFunction = easeFunction,
                    };
                    DoubleAnimation transAnimationY = new DoubleAnimation()
                    {
                        From = translateTransform.Y,
                        To = to.Y,
                        FillBehavior = FillBehavior.HoldEnd,
                        Duration = duration,
                        EasingFunction = easeFunction,
                    };
                    translateTransform.BeginAnimation(TranslateTransform.XProperty, transAnimationX);
                    translateTransform.BeginAnimation(TranslateTransform.YProperty, transAnimationY);
                }
            }
        }
        /// <summary>
        /// 释放拖动后的图片缩放动画
        /// </summary>
        /// <param name="to"></param>
        /// <param name="duration"></param>
        /// <param name="target"></param>
        private static void AnimateUIElementScale(double to, TimeSpan duration, FrameworkElement target)
        {
            var group = target.RenderTransform as TransformGroup;
            if (group != null)
            {
                var transform = group.Children.FirstOrDefault(p => p.GetType() == typeof(ScaleTransform)) as ScaleTransform;
                if (transform != null)
                {
                    EasingFunctionBase easeFunction = new PowerEase()
                    {
                        EasingMode = EasingMode.EaseInOut,
                        Power = 5
                    };

                    DoubleAnimation transAnimationX = new DoubleAnimation()
                    {
                        From = transform.ScaleX,
                        To = to,
                        FillBehavior = FillBehavior.HoldEnd,
                        Duration = duration,
                        EasingFunction = easeFunction,
                    };
                    DoubleAnimation transAnimationY = new DoubleAnimation()
                    {
                        From = transform.ScaleY,
                        To = to,
                        FillBehavior = FillBehavior.HoldEnd,
                        Duration = duration,
                        EasingFunction = easeFunction,
                    };
                    transform.BeginAnimation(ScaleTransform.ScaleXProperty, transAnimationX);
                    transform.BeginAnimation(ScaleTransform.ScaleYProperty, transAnimationY);
                }
            }
        }
        /// <summary>
        /// 释放拖动后的thumb点移动动画
        /// </summary>
        /// <param name="to"></param>
        /// <param name="duration"></param>
        /// <param name="target"></param>
        /// <param name="propertyName"></param>
        /// <param name="enableDependentAnimation"></param>
        /// <returns></returns>
        private static DoubleAnimation CreateDoubleAnimation(double to, TimeSpan duration, DependencyObject target, string propertyName)
        {
            var animation = new DoubleAnimation()
            {
                To = to,
                Duration = duration,
                FillBehavior = FillBehavior.HoldEnd,
            };

            Storyboard.SetTarget(animation, target);
            Storyboard.SetTargetProperty(animation,new PropertyPath(propertyName));

            return animation;
        }
        /// <summary>
        /// 圆形选择框时需要用到的中心点缩放动画
        /// </summary>
        /// <param name="to"></param>
        /// <param name="duration"></param>
        /// <param name="target"></param>
        /// <param name="propertyName"></param>
        /// <param name="enableDependentAnimation"></param>
        /// <returns></returns>
        private static PointAnimation CreatePointAnimation(Point to, TimeSpan duration, DependencyObject target, string propertyName)
        {
            var animation = new PointAnimation()
            {
                To = to,
                Duration = duration,
                FillBehavior = FillBehavior.HoldEnd,
            };

            Storyboard.SetTarget(animation, target);
            Storyboard.SetTargetProperty(animation,new PropertyPath(propertyName));

            return animation;
        }

        /// <summary>
        /// 在wpf下动画没生效，弃用
        /// </summary>
        /// <param name="to"></param>
        /// <param name="duration"></param>
        /// <param name="rectangle"></param>
        /// <returns></returns>
        private static ObjectAnimationUsingKeyFrames CreateRectangleAnimation_ab(Rect to, TimeSpan duration, RectangleGeometry rectangle)
        {
            var animation = new ObjectAnimationUsingKeyFrames()
            {
                Duration = duration
            };
            var frames = GetRectKeyframes(rectangle.Rect, to, duration);
            foreach (var item in frames)
            {
                animation.KeyFrames.Add(item);
            }

            Storyboard.SetTarget(animation, rectangle);
            Storyboard.SetTargetProperty(animation, new PropertyPath(RectangleGeometry.RectProperty));

            return animation;
        }
        /// <summary>
        /// 简单粗暴的矩形选择框动画
        /// </summary>
        /// <param name="to"></param>
        /// <param name="duration"></param>
        /// <param name="rectangle"></param>
        private static void StartRectangleAnimation(Rect to, TimeSpan duration, RectangleGeometry rectangle)
        {
            var frames = GetRectKeyframes(rectangle.Rect, to, duration);
            Task.Run( () =>
            {
                foreach (var f in frames)
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() => rectangle.Rect = (Rect)f.Value);
                    System.Threading.Thread.Sleep(8);
                }
            });
        }

        private static List<DiscreteObjectKeyFrame> GetRectKeyframes(Rect from, Rect to, TimeSpan duration)
        {
            var rectKeyframes = new List<DiscreteObjectKeyFrame>();
            var step = TimeSpan.FromMilliseconds(10);
            var startPointFrom = new Point(from.X, from.Y);
            var endPointFrom = new Point(from.X + from.Width, from.Y + from.Height);
            var startPointTo = new Point(to.X, to.Y);
            var endPointTo = new Point(to.X + to.Width, to.Y + to.Height);
            for (var time = default(TimeSpan); time < duration; time += step)
            {
                var progress = time.TotalMilliseconds / duration.TotalMilliseconds;
                var startPoint = new Point
                {
                    X = startPointFrom.X + (progress * (startPointTo.X - startPointFrom.X)),
                    Y = startPointFrom.Y + (progress * (startPointTo.Y - startPointFrom.Y)),
                };
                var endPoint = new Point
                {
                    X = endPointFrom.X + (progress * (endPointTo.X - endPointFrom.X)),
                    Y = endPointFrom.Y + (progress * (endPointTo.Y - endPointFrom.Y)),
                };
                rectKeyframes.Add(new DiscreteObjectKeyFrame
                {
                    KeyTime = KeyTime.FromTimeSpan(time),
                    Value = new Rect(startPoint, endPoint)
                });
            }

            rectKeyframes.Add(new DiscreteObjectKeyFrame
            {
                KeyTime = duration,
                Value = to
            });
            return rectKeyframes;
        }

        /// <summary>
        /// 简单粗暴的圆形选择框动画
        /// </summary>
        /// <param name="toP"></param>
        /// <param name="toRadiusX"></param>
        /// <param name="toRadiusY"></param>
        /// <param name="duration"></param>
        /// <param name="ellipseGeometry"></param>
        private static void StartCircularAnimation(Point toP,double toRadiusX, double toRadiusY, TimeSpan duration, EllipseGeometry ellipseGeometry)
        {
            Task.Run(() =>
            {
                int aniCount = (int)Math.Ceiling(duration.Milliseconds / 10f);
                double xp = 0;
                double yp = 0;
                double rxp = 0;
                double ryp = 0;
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    xp = (toP.X - ellipseGeometry.Center.X) / aniCount;
                    yp = (toP.Y - ellipseGeometry.Center.Y) / aniCount;
                    rxp = (toRadiusX - ellipseGeometry.RadiusX) / aniCount;
                    ryp = (toRadiusY - ellipseGeometry.RadiusY) / aniCount;
                });
                for (int i=0;i<aniCount; i++)
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        ellipseGeometry.Center = new Point(ellipseGeometry.Center.X + xp, ellipseGeometry.Center.Y + yp);
                        ellipseGeometry.RadiusX += rxp;
                        ellipseGeometry.RadiusY += ryp;
                    });
                    System.Threading.Thread.Sleep(8);
                }
            });
        }
    }
}
