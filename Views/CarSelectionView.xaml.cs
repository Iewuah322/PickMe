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
    public partial class CarSelectionView : Window
    {
        private CarSelectionViewModel _viewModel;

        // Будем принимать User, чтобы передать его в VM
        public CarSelectionView(User driver)
        {
            InitializeComponent();
            _viewModel = new CarSelectionViewModel(driver);
            this.DataContext = _viewModel;

            // Это связывает VM с Окном, не нарушая MVVM
            _viewModel.RequestClose += (dialogResult) =>
            {
                this.DialogResult = dialogResult;
                this.Close();
            };
        }

        // Публичное свойство, чтобы Dashboard мог забрать выбранную машину
        public Car SelectedCar => _viewModel.SelectedCar;

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }

        private void CarCard_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.DataContext is Car car)
            {
                _viewModel.SelectCar(car);
            }
        }
    }
}
