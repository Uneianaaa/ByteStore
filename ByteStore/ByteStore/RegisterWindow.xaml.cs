using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;

namespace ByteStore
{
    public partial class RegisterWindow : Window
    {
        private readonly HttpClient _http = new HttpClient
        {
            BaseAddress = new Uri("https://localhost:7121/")
        };

        public RegisterWindow()
        {
            InitializeComponent();
        }

        private async void btnGetCode_Click(object sender, RoutedEventArgs e)
        {
            string email = txtEmail.Text.Trim();
            string password = txtPassword.Password;
            string lastName = txtLastName.Text.Trim();
            string firstName = txtFirstName.Text.Trim();
            string middleName = txtMiddleName.Text.Trim();

            if (string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(password) ||
                string.IsNullOrWhiteSpace(lastName) ||
                string.IsNullOrWhiteSpace(firstName))
            {
                txtMessage.Text = "Заполните Email, Пароль, Фамилию, Имя.";
                return;
            }

            var request = new
            {
                email = email,
                password = password,
                lastName = lastName,
                firstName = firstName,
                middleName = string.IsNullOrWhiteSpace(middleName) ? null : middleName,
                phone = (object)null,
                address = (object)null,
                role = "Employee",
                isVerified = false,
                createdAt = DateTime.UtcNow
            };

            try
            {
                var response = await _http.PostAsJsonAsync("api/auth/register", request);

                if (response.IsSuccessStatusCode)
                {
                    txtMessage.Text = "Код отправлен на почту. Проверьте email.";
                    txtMessage.Foreground = System.Windows.Media.Brushes.LightGreen;

                    txtCode.IsEnabled = true;
                    txtCode.Focus();
                }
                else
                {
                    string error = await response.Content.ReadAsStringAsync();
                    txtMessage.Text = $"Ошибка: {response.StatusCode}";
                    MessageBox.Show(error, "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                txtMessage.Text = "Не удалось связаться с сервером.";
                MessageBox.Show(ex.Message, "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void btnRegister_Click(object sender, RoutedEventArgs e)
        {
            string email = txtEmail.Text.Trim();
            string code = txtCode.Text.Trim();

            if (string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(code))
            {
                txtMessage.Text = "Введите email и код из письма.";
                return;
            }

            var request = new
            {
                email = email,
                code = code
            };

            try
            {
                var response = await _http.PostAsJsonAsync("api/auth/verify", request);

                if (response.IsSuccessStatusCode)
                {
                    JsonElement result = await response.Content
                        .ReadFromJsonAsync<JsonElement>();

                    string token = result.GetProperty("token").GetString() ?? "";
                    string role = result.GetProperty("role").GetString() ?? "";

                    MessageBox.Show(
                        $"Регистрация завершена!\nРоль: {role}",
                        "Успех",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information
                    );
                    CatalogWindow catalog = new CatalogWindow(token, role, email);
                    catalog.Show();
                    this.Close();
                }
                else
                {
                    string error = await response.Content.ReadAsStringAsync();
                    txtMessage.Text = $"Ошибка: {response.StatusCode}";
                    MessageBox.Show(error, "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                txtMessage.Text = "Не удалось связаться с сервером.";
                MessageBox.Show(ex.Message, "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            MainWindow mainWin = new MainWindow();
            mainWin.Show();
            this.Close();
        }
    }
}