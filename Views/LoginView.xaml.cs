using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using TaxiWPF.ViewModels;
using System.Windows.Media.Animation;


namespace TaxiWPF.Views
{
    /// <summary>
    /// Логика взаимодействия для LoginView.xaml
    /// </summary>
    public partial class LoginView : Window
    {
        public LoginView()
        {
            InitializeComponent();
            // Подписываемся на событие загрузки окна
            Loaded += LoginView_Loaded;
        }

        private void LoginView_Loaded(object sender, RoutedEventArgs e)
        {
            // Подписываемся на изменение Content через DependencyPropertyDescriptor
            var dpd = DependencyPropertyDescriptor.FromProperty(ContentControl.ContentProperty, typeof(ContentControl));
            if (dpd != null)
            {
                dpd.AddValueChanged(AuthContent, (s, args) => 
                {
                    // Небольшая задержка для того, чтобы DataTemplate успел загрузиться
                    Dispatcher.BeginInvoke(new Action(() => UpdatePasswordBoxSubscriptions()), System.Windows.Threading.DispatcherPriority.Loaded);
                });
            }
            // Подписываемся на событие загрузки ContentControl
            AuthContent.Loaded += AuthContent_Loaded;
            // Первоначальная подписка
            Dispatcher.BeginInvoke(new Action(() => UpdatePasswordBoxSubscriptions()), System.Windows.Threading.DispatcherPriority.Loaded);
        }

        private void UpdatePasswordBoxSubscriptions()
        {
            // Подписываемся на изменения Content для обработки PasswordBox
            if (AuthContent.Content is LoginViewModel)
            {
                var passwordBox = FindVisualChild<PasswordBox>(AuthContent, "PasswordBox");
                if (passwordBox != null)
                {
                    passwordBox.PasswordChanged -= OnPasswordChanged;
                    passwordBox.PasswordChanged += OnPasswordChanged;
                }
            }
            else if (AuthContent.Content is RegistrationViewModel)
            {
                var passwordBox = FindVisualChild<PasswordBox>(AuthContent, "RegPasswordBox");
                if (passwordBox != null)
                {
                    passwordBox.PasswordChanged -= OnRegistrationPasswordChanged;
                    passwordBox.PasswordChanged += OnRegistrationPasswordChanged;
                }
                var confirmPasswordBox = FindVisualChild<PasswordBox>(AuthContent, "RegConfirmPasswordBox");
                if (confirmPasswordBox != null)
                {
                    confirmPasswordBox.PasswordChanged -= OnRegistrationConfirmPasswordChanged;
                    confirmPasswordBox.PasswordChanged += OnRegistrationConfirmPasswordChanged;
                }
            }
            else if (AuthContent.Content is PasswordRecoveryViewModel)
            {
                var passwordBox = FindVisualChild<PasswordBox>(AuthContent, "RecoveryPasswordBox");
                if (passwordBox != null)
                {
                    passwordBox.PasswordChanged -= OnRecoveryPasswordChanged;
                    passwordBox.PasswordChanged += OnRecoveryPasswordChanged;
                }
            }
        }

        private void AuthContent_Loaded(object sender, RoutedEventArgs e)
        {
            UpdatePasswordBoxSubscriptions();
        }

        private void OnRegistrationPasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is AuthViewModel authViewModel && authViewModel.CurrentView is RegistrationViewModel viewModel)
            {
                viewModel.Password = (sender as PasswordBox).Password;
            }
        }

        private void OnRegistrationConfirmPasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is AuthViewModel authViewModel && authViewModel.CurrentView is RegistrationViewModel viewModel)
            {
                viewModel.ConfirmPassword = (sender as PasswordBox).Password;
            }
        }

        private void OnRecoveryPasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is AuthViewModel authViewModel && authViewModel.CurrentView is PasswordRecoveryViewModel viewModel)
            {
                viewModel.NewPassword = (sender as PasswordBox).Password;
            }
        }

        private T FindVisualChild<T>(DependencyObject parent, string name = null) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T t)
                {
                    if (string.IsNullOrEmpty(name) || (child is FrameworkElement fe && fe.Name == name))
                        return t;
                }
                var childOfChild = FindVisualChild<T>(child, name);
                if (childOfChild != null)
                    return childOfChild;
            }
            return null;
        }

        private void Window_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }


        private void OnPasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is AuthViewModel authViewModel && authViewModel.CurrentView is LoginViewModel viewModel)
            {
                viewModel.Password = (sender as PasswordBox).Password;
            }
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            // Находим CrazyModeToggle внутри DataTemplate
            var crazyToggle = FindVisualChild<ToggleButton>(AuthContent);
            
            // Проверяем, включен ли наш ToggleButton
            if (crazyToggle != null && crazyToggle.IsChecked == true)
            {
                // Если да - запускаем анимацию,
                // а она УЖЕ ПОСЛЕ СЕБЯ запустит логин
                AnimateAndLogin();
            }
            else
            {
                // Если нет - просто логинимся как обычно
                ExecuteLoginLogic();
            }
        }


        private void AnimateAndLogin()
        {
            // Настраиваем анимацию
            var animation = new DoubleAnimation
            {
                From = 0,
                To = 720, // Два полных оборота
                Duration = TimeSpan.FromSeconds(4.0), // 1.5 секунды
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            // ==== ВАЖНО ====
            // Мы "подписываемся" на событие ЗАВЕРШЕНИЯ анимации.
            animation.Completed += (s, e) =>
            {
                // Когда анимация закончилась - выполняем вход
                ExecuteLoginLogic();
            };

            // Находим наш RotateTransform
            var transform = RootBorder.RenderTransform as RotateTransform;

            // ==== ВОТ ИСПРАВЛЕНИЕ ====
            // Добавляем проверку, что transform не null
            if (transform != null)
            {
                // Запускаем анимацию на свойстве "Angle" (Угол)
                transform.BeginAnimation(RotateTransform.AngleProperty, animation);
            }
            else
            {
                // Если transform почему-то null, просто логинимся без анимации
                ExecuteLoginLogic();
            }
        }

        private void ExecuteLoginLogic()
        {
            // Получаем наш ViewModel из DataContext
            if (DataContext is AuthViewModel authViewModel && authViewModel.CurrentView is LoginViewModel viewModel)
            {
                // ...и просто выполняем команду, 
                // которую мы убрали из XAML
                if (viewModel.LoginCommand.CanExecute(null))
                {
                    viewModel.LoginCommand.Execute(null);
                }
            }
        }

    }
}
