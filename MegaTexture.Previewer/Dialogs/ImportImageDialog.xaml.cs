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
using System.Windows.Shapes;
using CommunityToolkit.Mvvm.ComponentModel;
using EleCho.AetherTex;
using Microsoft.Win32;

namespace MegaTextures.Previewer.Dialogs
{
    /// <summary>
    /// ImportImageDialog.xaml 的交互逻辑
    /// </summary>
    [ObservableObject]
    public partial class ImportImageDialog : Window
    {
        private readonly MegaTexture _texture;

        private OpenFileDialog? _openFileDialog;

        [ObservableProperty]
        private string? _filePath;

        [ObservableProperty]
        private string _targetSource;

        [ObservableProperty]
        private int _row;

        [ObservableProperty]
        private int _column;

        public IReadOnlyList<string> AvailableSources => _texture.Sources;

        public ImportImageDialog(MegaTexture texture)
        {
            DataContext = this;
            _texture = texture;
            _targetSource = AvailableSources.First();

            InitializeComponent();
        }

        private void OpenFileButton_Click(object sender, RoutedEventArgs e)
        {
            _openFileDialog ??= new OpenFileDialog()
            {
                Title = "Open image",
                Filter = "Any Image|*.jpg;*.jpeg;*.png;*.bmp|JPEG Image|*.jpg;*.jpeg|PNG Image|*.png|BMP Image|*.bmp",
                CheckFileExists = true,
            };

            if (_openFileDialog.ShowDialog(this) != true)
            {
                return;
            }

            FilePath = _openFileDialog.FileName;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}
