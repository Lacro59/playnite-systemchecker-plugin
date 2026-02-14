using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace SystemChecker.Controls
{
    public class StatusIcon : Canvas
    {
        public static readonly DependencyProperty StatusProperty =
            DependencyProperty.Register("Status", typeof(string), typeof(StatusIcon),
                new PropertyMetadata(string.Empty, OnStatusChanged));

        public string Status
        {
            get { return (string)GetValue(StatusProperty); }
            set { SetValue(StatusProperty, value); }
        }

        private static void OnStatusChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            StatusIcon icon = d as StatusIcon;
            if (icon != null)
            {
                icon.UpdateIcon();
            }
        }

        private void UpdateIcon()
        {
            Children.Clear();

            if (string.IsNullOrEmpty(Status))
            {
                return;
            }

            if (Status.Contains("✓") || Status.Contains("✔"))
            {
                DrawCheckmark();
            }
            else if (Status.Contains("✗") || Status.Contains("✘"))
            {
                DrawCross();
            }
            else if (Status.Contains("⚠") || Status.Contains("~"))
            {
                DrawWarning();
            }
        }

        private void DrawCheckmark()
        {
            Ellipse circle = new Ellipse
            {
                Width = 24,
                Height = 24,
                Fill = new SolidColorBrush(Color.FromRgb(34, 197, 94))
            };
            Children.Add(circle);

            Path checkPath = new Path
            {
                Data = Geometry.Parse("M 6,12 L 10,16 L 18,8"),
                Stroke = Brushes.White,
                StrokeThickness = 2.5,
                StrokeLineJoin = PenLineJoin.Round,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round
            };
            Children.Add(checkPath);
        }

        private void DrawCross()
        {
            Ellipse circle = new Ellipse
            {
                Width = 24,
                Height = 24,
                Fill = new SolidColorBrush(Color.FromRgb(239, 68, 68))
            };
            Children.Add(circle);

            Path crossPath1 = new Path
            {
                Data = Geometry.Parse("M 7,7 L 17,17"),
                Stroke = Brushes.White,
                StrokeThickness = 2.5,
                StrokeLineJoin = PenLineJoin.Round,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round
            };
            Children.Add(crossPath1);

            Path crossPath2 = new Path
            {
                Data = Geometry.Parse("M 17,7 L 7,17"),
                Stroke = Brushes.White,
                StrokeThickness = 2.5,
                StrokeLineJoin = PenLineJoin.Round,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round
            };
            Children.Add(crossPath2);
        }

        private void DrawWarning()
        {
            Path trianglePath = new Path
            {
                Data = Geometry.Parse("M 12,2 L 22,20 L 2,20 Z"),
                Fill = new SolidColorBrush(Color.FromRgb(251, 146, 60))
            };
            Children.Add(trianglePath);

            Path exclamationLine = new Path
            {
                Data = Geometry.Parse("M 12,8 L 12,14"),
                Stroke = Brushes.White,
                StrokeThickness = 2,
                StrokeLineJoin = PenLineJoin.Round,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round
            };
            Children.Add(exclamationLine);

            Ellipse exclamationDot = new Ellipse
            {
                Width = 2,
                Height = 2,
                Fill = Brushes.White
            };
            Canvas.SetLeft(exclamationDot, 11);
            Canvas.SetTop(exclamationDot, 16);
            Children.Add(exclamationDot);
        }
    }
}