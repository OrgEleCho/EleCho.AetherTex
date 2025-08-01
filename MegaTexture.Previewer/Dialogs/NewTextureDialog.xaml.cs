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

namespace AetherTex.Viewer.Dialogs
{
    /// <summary>
    /// NewTextureDialog.xaml 的交互逻辑
    /// </summary>
    [ObservableObject]
    public partial class NewTextureDialog : Window
    {
        public static TextureFormat[] Formats { get; } = Enum.GetValues<TextureFormat>();

        [ObservableProperty]
        private int _tileWidth = 1024;

        [ObservableProperty]
        private int _tileHeight = 1024;

        [ObservableProperty]
        private int _rows = 4;

        [ObservableProperty]
        private int _columns = 4;

        [ObservableProperty]
        private string _sources = "color";

        [ObservableProperty]
        private TextureFormat _format = TextureFormat.Rgba8888;

        public NewTextureDialog()
        {
            DataContext = this;
            InitializeComponent();
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
