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
    public partial class CarDetailsView : Window
    {
        // Конструктор принимает Car и User
        public CarDetailsView(User driver, Car car)
        {
            InitializeComponent();

            var viewModel = new CarDetailsViewModel(car, driver);
            this.DataContext = viewModel;

            // Настраиваем заголовок окна
            if (viewModel.IsAddMode)
            {
                TitleText.Text = "ДОБАВЛЕНИЕ АВТОМОБИЛЯ";
            }

            // Связываем событие "Закрыть"
            viewModel.RequestClose += () =>
            {
                // Для модальных окон используется DialogResult
                this.DialogResult = true;
                this.Close();
            };
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
