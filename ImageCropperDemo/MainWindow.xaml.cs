using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ImageCropperDemo
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            ImageCropperControl.LoadImageFromFile(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "sine_wave_omega.jpg"));
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {
                ImageCropperControl.LoadImageFromFile(openFileDialog.FileName);
            }
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            switch((e.AddedItems[0] as ComboBoxItem).Content)
            {
                case "Rectangular": ImageCropperControl.CropShape = ImageCropper.CropShape.Rectangular;break;
                case "Circular": ImageCropperControl.CropShape = ImageCropper.CropShape.Circular; break;
            }
        }

        private void ClearImg_Click(object sender, RoutedEventArgs e)
        {
            ImageCropperControl.LoadImageFromFile(null);
        }

        private void ResetDrawThumb_Click(object sender, RoutedEventArgs e)
        {
            ImageCropperControl.ResetDrawThumb();
        }
    }
}
