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
using TaxiWPF.Models;
using TaxiWPF.ViewModels;

namespace TaxiWPF.Views
{
    public partial class DriverDashboardView : Window
    {
        public DriverDashboardView()
        {
            InitializeComponent();
            
            this.IsVisibleChanged += DriverDashboardView_IsVisibleChanged;
        }
        private void DriverDashboardView_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            // Если окно стало видимым и у него есть ViewModel
            if ((bool)e.NewValue == true && this.DataContext is DriverDashboardViewModel vm)
            {
                // Вызываем метод для обновления данных
                vm.LoadDashboardData();
            }
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            // (В идеале здесь должно быть Application.Current.Shutdown(), 
            // но Close() тоже подойдет, если LoginView закрылся)
            Close();
        }


        private void CarCard_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.DataContext is Car car && DataContext is DriverDashboardViewModel vm)
            {
                vm.SelectCarInList(car);
            }
        }
    }
}