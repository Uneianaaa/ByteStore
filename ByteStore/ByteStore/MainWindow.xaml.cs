using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;

namespace ByteStore
{
    public partial class MainWindow : Window
    {
        private readonly HttpClient _http = new HttpClient
        {
            BaseAddress = new Uri("https://localhost:7121/")
        };

        private string _token = "";

        public MainWindow()
        {
            InitializeComponent();
        }

        private async void btnLogin_Click(object sender, RoutedEventArgs e)
        {
            string email = txtEmail.Text.Trim();
            string password = txtPassword.Password;

            if (string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(password))
            {
                txtMessage.Text = "Введите email и пароль.";
                return;
            }

            try
            {
                var request = new
                {
                    email = email,
                    password = password
                };

                var response = await _http.PostAsJsonAsync("api/auth/login", request);

                if (response.IsSuccessStatusCode)
                {
                    JsonElement result = await response.Content.ReadFromJsonAsync<JsonElement>();

                    _token = result.GetProperty("token").GetString() ?? "";
                    string role = result.GetProperty("role").GetString() ?? "";
                    string fullName = result.GetProperty("fullName").GetString() ?? "";

                    if (role == "Сотрудник")
                    {
                        CatalogWindow catalog = new CatalogWindow(_token, role, fullName);
                        catalog.Show();
                        this.Close();
                    }
                    else if (role == "Администратор")
                    {
                        AdminWindow adminWin = new AdminWindow(_token, fullName);
                        adminWin.Show();
                        this.Close();
                    }
                    else if (role == "Продавец")
                    {
                        SellerWindow sellerWin = new SellerWindow(_token, fullName);
                        sellerWin.Show();
                        this.Close();
                    }
                    else if (role == "Кладовщик")
                    {
                        StorekeeperWindow storeWin = new StorekeeperWindow(_token, fullName);
                        storeWin.Show();
                        this.Close();
                    }
                    else
                    {
                        MessageBox.Show($"Неизвестная роль: {role}");
                    }
                }
                else
                {
                    string error = await response.Content.ReadAsStringAsync();
                    txtMessage.Text = $"Ошибка: {response.StatusCode}";
                    MessageBox.Show(error, "Ошибка входа",
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

        private void btnRegister_Click(object sender, RoutedEventArgs e)
        {
            RegisterWindow regWin = new RegisterWindow();

            regWin.Show();

            this.Close();
        }
    }
}