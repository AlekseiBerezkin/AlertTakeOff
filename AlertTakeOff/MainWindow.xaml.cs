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

namespace AlertTakeOff
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private async void btnStart_Click(object sender, RoutedEventArgs e)
        {
            TimerController timerController = new TimerController();

             Properties.Settings.Default.StartHours = tbHStart.Text;
             Properties.Settings.Default.StopHours = tbHStop.Text;
             Properties.Settings.Default.Factor = decimal.Parse(tbFactor.Text);
             Properties.Settings.Default.NumberСandles = int.Parse(tbNumberCandle.Text);
             Properties.Settings.Default.ChatId = tbChatId.Text;
             Properties.Settings.Default.TimeStart = tbTimeStart.Text;
             Properties.Settings.Default.SilenceInterval = int.Parse(tbSilenceInterval.Text);

            Properties.Settings.Default.Save();

            await timerController.Start();

            btnStop.IsEnabled = true;
            btnStart.IsEnabled = false;

        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            tbHStart.Text = Properties.Settings.Default.StartHours;
            tbHStop.Text = Properties.Settings.Default.StopHours;
            tbFactor.Text = Properties.Settings.Default.Factor.ToString();
            tbNumberCandle.Text = Properties.Settings.Default.NumberСandles.ToString();
            tbChatId.Text=Properties.Settings.Default.ChatId;
            tbTimeStart.Text = Properties.Settings.Default.TimeStart;
            tbSilenceInterval.Text = Properties.Settings.Default.SilenceInterval.ToString();
            btnStop.IsEnabled = false;
        }

        private void NumericOnlyDC(Object sender, TextCompositionEventArgs e)
        {
            e.Handled = IsTextNumericDC(e.Text);
        }
        private static bool IsTextNumericDC(string str)
        {
            if (str == "," || str == "-")
                return false;
            else
            {
                System.Text.RegularExpressions.Regex reg = new System.Text.RegularExpressions.Regex("[^0-9]");
                return reg.IsMatch(str);
            }
        }

        private void NumericOnly(System.Object sender, TextCompositionEventArgs e)
        {
            e.Handled = IsTextNumeric(e.Text);
        }
        private static bool IsTextNumeric(string str)
        {
                System.Text.RegularExpressions.Regex reg = new System.Text.RegularExpressions.Regex("[^0-9]");
                return reg.IsMatch(str);
        }

        private async void btnStop_Click(object sender, RoutedEventArgs e)
        {
            await TimerController.Stop();

            btnStop.IsEnabled = false;
            btnStart.IsEnabled = true;
        }
    }
}
