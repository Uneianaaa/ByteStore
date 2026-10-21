using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using ByteStore.Models;

namespace ByteStore
{
    public partial class CatalogWindow : Window
    {
        private readonly HttpClient _http = new HttpClient
        {
            BaseAddress = new Uri("https://localhost:7121/")
        };

        private string _token;
        private string _role;
        private string _fullName;

        private List<Product> _allProducts = new List<Product>();
        private int _currentPage = 1;
        private const int _pageSize = 10;

        public CatalogWindow(string token, string role, string fullName)
        {
            InitializeComponent();

            _token = token;
            _role = role;
            _fullName = fullName;

            txtUserInfo.Text = fullName;

            ConfigureButtonsByRole();

            Loaded += async (s, e) => await LoadProductsAsync();
        }

        private void txtUserInfo_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            ProfileWindow profile = new ProfileWindow(_token);
            profile.ShowDialog();
        }

        private void btnChat_Click(object sender, RoutedEventArgs e)
        {
            int myId = GetMyUserIdFromToken(_token);
            ChatWindow chat = new ChatWindow(_token, myId);
            chat.ShowDialog();
        }

        private int GetMyUserIdFromToken(string token)
        {
            try
            {
                string[] parts = token.Split('.');
                string payload = parts[1];

                int mod4 = payload.Length % 4;
                if (mod4 > 0)
                {
                    payload += new string('=', 4 - mod4);
                }

                byte[] data = Convert.FromBase64String(payload.Replace('-', '+').Replace('_', '/'));
                string json = System.Text.Encoding.UTF8.GetString(data);

                using (JsonDocument doc = JsonDocument.Parse(json))
                {
                    JsonElement root = doc.RootElement;

                    if (root.TryGetProperty("nameidentifier", out JsonElement idElement))
                    {
                        return int.Parse(idElement.GetString());
                    }

                    foreach (JsonProperty prop in root.EnumerateObject())
                    {
                        if (prop.Name.EndsWith("nameidentifier"))
                        {
                            return int.Parse(prop.Value.GetString());
                        }
                    }
                }
            }
            catch { }

            return 0;
        }

        private void dgProducts_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            Product selected = dgProducts.SelectedItem as Product;

            if (selected == null)
            {
                return;
            }

            ProductProfileWindow profile = new ProductProfileWindow(_token, selected.Id);
            profile.ShowDialog();

            _ = LoadProductsAsync();
        }

        private void ConfigureButtonsByRole()
        {
            if (_role == "Администратор")
            {
                btnAdd.Visibility = Visibility.Visible;
                btnEdit.Visibility = Visibility.Visible;
                btnDelete.Visibility = Visibility.Visible;
            }
            else if (_role == "Кладовщик")
            {
                btnAdd.Visibility = Visibility.Visible;
                btnEdit.Visibility = Visibility.Visible;
            }
        }

        private async Task LoadProductsAsync()
        {
            try
            {
                _http.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _token);

                var response = await _http.GetAsync("api/products");

                if (response.IsSuccessStatusCode)
                {
                    _allProducts = await response.Content
                        .ReadFromJsonAsync<List<Product>>();

                    _currentPage = 1;
                    UpdatePage();

                    txtMessage.Text = $"Загружено товаров: {_allProducts.Count}";
                    txtMessage.Foreground = System.Windows.Media.Brushes.LightGreen;
                }
                else
                {
                    txtMessage.Text = $"Ошибка загрузки: {response.StatusCode}";
                    txtMessage.Foreground = System.Windows.Media.Brushes.OrangeRed;
                }
            }
            catch (Exception ex)
            {
                txtMessage.Text = "Не удалось связаться с сервером.";
                MessageBox.Show(ex.Message, "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdatePage()
        {
            int total = _allProducts.Count;
            int totalPages = (int)Math.Ceiling((double)total / _pageSize);

            if (totalPages == 0)
            {
                totalPages = 1;
            }

            if (_currentPage < 1)
            {
                _currentPage = 1;
            }

            if (_currentPage > totalPages)
            {
                _currentPage = totalPages;
            }

            var pageItems = _allProducts
                .Skip((_currentPage - 1) * _pageSize)
                .Take(_pageSize)
                .ToList();

            dgProducts.ItemsSource = pageItems;
            txtPageInfo.Text = $"Страница {_currentPage} из {totalPages}";

            int from = (total == 0) ? 0 : (_currentPage - 1) * _pageSize + 1;
            int to = Math.Min(_currentPage * _pageSize, total);

            txtRangeInfo.Text = $"Показано: {from}-{to} из {total}";

            btnPrevPage.IsEnabled = _currentPage > 1;
            btnNextPage.IsEnabled = _currentPage < totalPages;
        }

        private void btnPrevPage_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage > 1)
            {
                _currentPage--;
                UpdatePage();
            }
        }

        private void btnNextPage_Click(object sender, RoutedEventArgs e)
        {
            int totalPages = (int)Math.Ceiling((double)_allProducts.Count / _pageSize);

            if (_currentPage < totalPages)
            {
                _currentPage++;
                UpdatePage();
            }
        }

        private async void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            await LoadProductsAsync();
        }

        private async void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            ProductFormWindow form = new ProductFormWindow(_token, null);
            form.ShowDialog();

            await LoadProductsAsync();
        }

        private async void btnEdit_Click(object sender, RoutedEventArgs e)
        {
            Product selected = dgProducts.SelectedItem as Product;

            if (selected == null)
            {
                MessageBox.Show("Выберите товар для редактирования.",
                    "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            ProductFormWindow form = new ProductFormWindow(_token, selected.Id);
            form.ShowDialog();

            await LoadProductsAsync();
        }

        private async void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            Product selected = dgProducts.SelectedItem as Product;

            if (selected == null)
            {
                MessageBox.Show("Выберите товар для удаления.",
                    "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            MessageBoxResult confirm = MessageBox.Show(
                $"Удалить товар \"{selected.Name}\"?",
                "Подтверждение удаления",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes)
            {
                return;
            }

            try
            {
                var response = await _http.DeleteAsync($"api/products/{selected.Id}");

                if (response.IsSuccessStatusCode)
                {
                    MessageBox.Show("Товар удалён.",
                        "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                    await LoadProductsAsync();
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
                {
                    string error = await response.Content.ReadAsStringAsync();
                    MessageBox.Show(error,
                        "Удаление невозможно", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    MessageBox.Show("Товар не найден.",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                else
                {
                    string error = await response.Content.ReadAsStringAsync();
                    MessageBox.Show($"Ошибка {response.StatusCode}: {error}",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnLogout_Click(object sender, RoutedEventArgs e)
        {
            MainWindow mainWin = new MainWindow();
            mainWin.Show();
            this.Close();
        }
    }
}