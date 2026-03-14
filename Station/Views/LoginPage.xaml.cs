using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System;
using Station.ViewModels;

namespace Station.Views
{
    public sealed partial class LoginPage : Page
    {
        public LoginPage()
        {
            this.InitializeComponent();
        }

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            ErrorText.Visibility = Visibility.Collapsed;

            var usernameOrEmail = UsernameBox.Text;
            var password = PasswordBox.Password;

            if (string.IsNullOrEmpty(usernameOrEmail) || string.IsNullOrEmpty(password))
            {
                ShowError("Vui lòng nhập đầy đủ thông tin.");
                return;
            }

            LoginButton.IsEnabled = false;

            try
            {
                var success = await Login(usernameOrEmail, password);

                if (success)
                {
                    var mainWindow = new MainWindow();
                    mainWindow.Activate();

                    var loginWindow = (Application.Current as App)?.m_window;
                    loginWindow?.Close();
                }
                else
                {
                    ShowError("Sai username hoặc mật khẩu.");
                }
            }
            catch (HttpRequestException ex)
            {
                ShowError("HTTP ERROR: " + ex.Message);
            }
            catch (Exception ex)
            {
                ShowError("OTHER ERROR: " + ex.Message);
            }

            LoginButton.IsEnabled = true;
        }

        private async Task<bool> Login(string usernameOrEmail, string password)
        {
            using var client = new HttpClient();

            var body = new
            {
                usernameOrEmail = usernameOrEmail,
                password = password
            };

            var json = JsonSerializer.Serialize(body);

            var content = new StringContent(
                json,
                Encoding.UTF8,
                "application/json");

            System.Diagnostics.Debug.WriteLine("CALLING API: http://localhost:5280/api/Auth/login");
            System.Diagnostics.Debug.WriteLine("REQUEST BODY: " + json);

            var response = await client.PostAsync(
                "http://localhost:5280/api/Auth/login",
                content);

            var result = await response.Content.ReadAsStringAsync();

            System.Diagnostics.Debug.WriteLine("STATUS: " + response.StatusCode);
            System.Diagnostics.Debug.WriteLine("RESPONSE: " + result);

            return response.IsSuccessStatusCode;
        }

        private void ShowError(string message)
        {
            ErrorText.Text = message;
            ErrorText.Visibility = Visibility.Visible;
        }
    }
}
