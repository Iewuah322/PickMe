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

namespace TaxiWPF.Views
{
    public partial class NewSupportTicketView : Window
    {
        public string Subject { get; private set; }

        public NewSupportTicketView()
        {
            InitializeComponent();
        }

        private void CreateButton_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(SubjectTextBox.Text))
            {
                Subject = SubjectTextBox.Text;
                this.DialogResult = true;
            }
            else
            {
                MessageBox.Show("Пожалуйста, опишите вашу проблему.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }
    }
}
