using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ByteStore
{
    public partial class UsersWindow : Window
    {
        private readonly HttpClient _http = new HttpClient
        {
            BaseAddress = new Uri("https://localhost:7121/")
        };

        private string _token;
        private string _fullName;

        public UsersWindow(string token, string fullName)
        {
            System.Net.ServicePointManager.ServerCertificateValidationCallback =
                (sender, certificate, chain, sslPolicyErrors) => true;

            InitializeComponent();

            _token = token;
            _fullName = fullName;

            txtUserInfo.Text = fullName;

            _http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _token);

            Loaded += async (s, e) => await LoadUsersAsync();
        }

        private async Task LoadUsersAsync()
        {
            try
            {
                var response = await _http.GetAsync("api/users");

                if (response.IsSuccessStatusCode)
                {
                    JsonElement arr = await response.Content.ReadFromJsonAsync<JsonElement>();

                    var list = new List<object>();

                    foreach (JsonElement u in arr.EnumerateArray())
                    {
                        string middleName = u.GetProperty("middleName").ValueKind == JsonValueKind.Null
                            ? "—"
                            : u.GetProperty("middleName").GetString();

                        string phone = u.GetProperty("phone").ValueKind == JsonValueKind.Null
                            ? "—"
                            : u.GetProperty("phone").GetString();

                        list.Add(new
                        {
                            Id = u.GetProperty("id").GetInt32(),
                            LastName = u.GetProperty("lastName").GetString(),
                            FirstName = u.GetProperty("firstName").GetString(),
                            MiddleName = middleName,
                            Email = u.GetProperty("email").GetString(),
                            Phone = phone,
                            Role = u.GetProperty("role").GetString()
                        });
                    }

                    dgUsers.ItemsSource = list;

                    txtMessage.Text = $"Загружено сотрудников: {list.Count}";
                    txtMessage.Foreground = Brushes.LightGreen;
                }
                else
                {
                    txtMessage.Text = $"Ошибка загрузки: {response.StatusCode}";
                    txtMessage.Foreground = Brushes.OrangeRed;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            await LoadUsersAsync();
        }

        private async void btnChangeRole_Click(object sender, RoutedEventArgs e)
        {
            dynamic selected = dgUsers.SelectedItem;

            if (selected == null)
            {
                MessageBox.Show("Выберите сотрудника.",
                    "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int userId = (int)selected.GetType().GetProperty("Id").GetValue(selected);
            string currentRole = (string)selected.GetType().GetProperty("Role").GetValue(selected);

            Window dlg = new Window
            {
                Title = "Смена роли",
                Width = 350,
                Height = 250,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                Background = Brushes.DarkSlateGray,
                ResizeMode = ResizeMode.NoResize
            };

            StackPanel panel = new StackPanel { Margin = new Thickness(20) };

            TextBlock lbl = new TextBlock
            {
                Text = "Выберите новую роль:",
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 0, 10)
            };

            ComboBox cmb = new ComboBox
            {
                Height = 35,
                FontSize = 13,
                Margin = new Thickness(0, 0, 0, 15)
            };

            cmb.Items.Add("Сотрудник");
            cmb.Items.Add("Продавец");
            cmb.Items.Add("Кладовщик");
            cmb.Items.Add("Администратор");

            for (int i = 0; i < cmb.Items.Count; i++)
            {
                if ((string)cmb.Items[i] == currentRole)
                {
                    cmb.SelectedIndex = i;
                    break;
                }
            }

            Button btnOk = new Button
            {
                Content = "СОХРАНИТЬ",
                Height = 40,
                Background = Brushes.LightGreen,
                Foreground = Brushes.Black,
                FontWeight = FontWeights.Bold
            };

            btnOk.Click += async (s, args) =>
            {
                string newRole = cmb.SelectedItem as string;

                if (string.IsNullOrEmpty(newRole))
                {
                    return;
                }

                var body = new { role = newRole };
                var resp = await _http.PutAsJsonAsync($"api/users/{userId}/role", body);

                if (resp.IsSuccessStatusCode)
                {
                    MessageBox.Show("Роль изменена.",
                        "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    dlg.Close();
                    await LoadUsersAsync();
                }
                else
                {
                    string err = await resp.Content.ReadAsStringAsync();
                    MessageBox.Show(err, "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            };

            panel.Children.Add(lbl);
            panel.Children.Add(cmb);
            panel.Children.Add(btnOk);
            dlg.Content = panel;

            dlg.ShowDialog();
        }

        private async void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            dynamic selected = dgUsers.SelectedItem;

            if (selected == null)
            {
                MessageBox.Show("Выберите сотрудника для удаления.",
                    "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int userId = (int)selected.GetType().GetProperty("Id").GetValue(selected);
            string lastName = (string)selected.GetType().GetProperty("LastName").GetValue(selected);
            string firstName = (string)selected.GetType().GetProperty("FirstName").GetValue(selected);

            var confirm = MessageBox.Show(
                $"Удалить сотрудника \"{lastName} {firstName}\"?",
                "Подтверждение удаления",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes)
            {
                return;
            }

            var resp = await _http.DeleteAsync($"api/users/{userId}");

            if (resp.IsSuccessStatusCode)
            {
                MessageBox.Show("Сотрудник удалён.",
                    "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                await LoadUsersAsync();
            }
            else if (resp.StatusCode == System.Net.HttpStatusCode.Conflict)
            {
                string err = await resp.Content.ReadAsStringAsync();
                MessageBox.Show(err, "Удаление невозможно",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            else
            {
                string err = await resp.Content.ReadAsStringAsync();
                MessageBox.Show($"Ошибка {resp.StatusCode}: {err}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}