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

namespace VisualiserWPF
{
    /// <summary>
    /// Interaction logic for RemoteCamChooser.xaml
    /// </summary>
    public partial class RemoteCamChooser : Window
    {
        public RemoteCamChooser()
        {
            InitializeComponent();
        }

        private void Accept(object sender, ExecutedRoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void Cancel(object sender, ExecutedRoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void UpdateStreamAddress(object sender, TextChangedEventArgs e)
        {
            try
            {
                txtStream.Text = "http://" + txtIP.Text + ":" + txtPort.Text + "/mjpegfeed?" + txtResolution.Text;
            } catch
            {

            }
            
        }
    }
}
