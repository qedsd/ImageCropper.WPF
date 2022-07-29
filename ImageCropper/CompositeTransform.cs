using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace ImageCropper
{
    public sealed class CompositeTransform
    {
        public double TranslateY { get; set; }
        public double TranslateX { get; set; }
        public double SkewY { get; set; }
        public double SkewX { get; set; }
        public double ScaleY { get; set; }
        public double ScaleX { get; set; }
        public double Rotation { get; set; }
        public double CenterY { get; set; }
        public double CenterX { get; set; }

        public CompositeTransform()
        {

        }

        public Rect TransformBounds(Rect rect)
        {
            double x = rect.X * ScaleX + TranslateX;
            double y = rect.Y * ScaleY + TranslateY;
            double w = rect.Width * ScaleX;
            double h = rect.Height * ScaleY;
            var r = new Rect()
            {
                X = x,
                Y = y,
                Width = w,
                Height = h
            };
            return r;
        }
        public Point TransformPoint(Point point)
        {
            Point p = new Point();
            p.X = point.X * ScaleX + TranslateX;
            p.Y = point.Y * ScaleY + TranslateY;
            return p;
        }
    }
}
