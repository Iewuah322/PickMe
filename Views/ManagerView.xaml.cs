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
using TaxiWPF.Models; // <-- Добавьте это
using TaxiWPF.ViewModels;


namespace TaxiWPF.Views
{
    public partial class ManagerView : Window
    {
        public ManagerView(User managerUser)
        {
            InitializeComponent();
            DataContext = new ManagerViewModel(managerUser);
        }

        // --- ДОБАВЬТЕ ЭТИ ДВА МЕТОДА ---
        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
