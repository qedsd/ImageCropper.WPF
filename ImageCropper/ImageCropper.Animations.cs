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

        private static DoubleAnimation CreateDoubleAnimation(double to, TimeSpan duration, DependencyObject target, string propertyName, bool enableDependentAnimation)
        {
            var animation = new DoubleAnimation()
            {
                To = to,
                Duration = duration,
                //EnableDependentAnimation = enableDependentAnimation
            };

            Storyboard.SetTarget(animation, target);
            Storyboard.SetTargetProperty(animation,new PropertyPath(propertyName));

            return animation;
        }

        private static PointAnimation CreatePointAnimation(Point to, TimeSpan duration, DependencyObject target, string propertyName, bool enableDependentAnimation)
        {
            var animation = new PointAnimation()
            {
                To = to,
                Duration = duration,
               // EnableDependentAnimation = enableDependentAnimation
            };

            Storyboard.SetTarget(animation, target);
            Storyboard.SetTargetProperty(animation,new PropertyPath(propertyName));

            return animation;
        }

        private static ObjectAnimationUsingKeyFrames CreateRectangleAnimation(Rect to, TimeSpan duration, RectangleGeometry rectangle, bool enableDependentAnimation)
        {
            var animation = new ObjectAnimationUsingKeyFrames
            {
                Duration = duration,
                //EnableDependentAnimation = enableDependentAnimation
            };

            var frames = GetRectKeyframes(rectangle.Rect, to, duration);
            foreach (var item in frames)
            {
                animation.KeyFrames.Add(item);
            }

            Storyboard.SetTarget(animation, rectangle);
            Storyboard.SetTargetProperty(animation, new PropertyPath(nameof(RectangleGeometry.Rect)));

            return animation;
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
    }
}
