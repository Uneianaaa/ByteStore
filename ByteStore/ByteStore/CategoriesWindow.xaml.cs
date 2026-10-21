using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ByteStore
{
    public partial class CategoriesWindow : Window
    {
        private readonly HttpClient _http = new HttpClient
        {
            BaseAddress = new Uri("https://localhost:7121/")
        };

        private string _token;
        private string _fullName;

        public CategoriesWindow(string token, string fullName)
        {
            System.Net.ServicePointManager.ServerCertificateValidationCallback =
                (sender, certificate, chain, sslPolicyErrors) => true;

            InitializeComponent();

            _token = token;
            _fullName = fullName;

            txtUserInfo.Text = fullName;

            _http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _token);

            Loaded += async (s, e) => await LoadCategoriesAsync();
        }

        private async Task LoadCategoriesAsync()
        {
            try
            {
                var response = await _http.GetAsync("api/categories");

                if (response.IsSuccessStatusCode)
                {
                    JsonElement arr = await response.Content.ReadFromJsonAsync<JsonElement>();

                    var list = new List<object>();

                    foreach (JsonElement c in arr.EnumerateArray())
                    {
                        string description = c.GetProperty("description").ValueKind == JsonValueKind.Null
                            ? "—"
                            : c.GetProperty("description").GetString();

                        list.Add(new
                        {
                            Id = c.GetProperty("id").GetInt32(),
                            Name = c.GetProperty("name").GetString(),
                            Description = description
                        });
                    }

                    dgCategories.ItemsSource = list;

                    txtMessage.Text = $"Загружено категорий: {list.Count}";
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
            await LoadCategoriesAsync();
        }

        private async void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            await ShowCategoryDialogAsync(null);
        }

        private async void btnEdit_Click(object sender, RoutedEventArgs e)
        {
            dynamic selected = dgCategories.SelectedItem;

            if (selected == null)
            {
                MessageBox.Show("Выберите категорию.",
                    "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int id = (int)selected.GetType().GetProperty("Id").GetValue(selected);
            string name = (string)selected.GetType().GetProperty("Name").GetValue(selected);
            string description = (string)selected.GetType().GetProperty("Description").GetValue(selected);

            await ShowCategoryDialogAsync(new { Id = id, Name = name, Description = description });
        }

        private async Task ShowCategoryDialogAsync(dynamic data)
        {
            bool isEdit = data != null;

            Window dlg = new Window
            {
                Title = isEdit ? "Редактировать категорию" : "Добавить категорию",
                Width = 400,
                Height = 300,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                Background = Brushes.DarkSlateGray,
                ResizeMode = ResizeMode.NoResize
            };

            StackPanel panel = new StackPanel { Margin = new Thickness(20) };

            TextBlock lbl1 = new TextBlock
            {
                Text = "Название *",
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 0, 5)
            };

            TextBox txtName = new TextBox
            {
                Height = 32,
                FontSize = 13,
                Padding = new Thickness(8, 0, 0, 0),
                Margin = new Thickness(0, 0, 0, 15)
            };

            if (isEdit)
            {
                txtName.Text = (string)data.GetType().GetProperty("Name").GetValue(data);
            }

            TextBlock lbl2 = new TextBlock
            {
                Text = "Описание",
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 0, 5)
            };

            TextBox txtDesc = new TextBox
            {
                Height = 60,
                FontSize = 13,
                Padding = new Thickness(8, 5, 0, 0),
                Margin = new Thickness(0, 0, 0, 15),
                TextWrapping = TextWrapping.Wrap,
                AcceptsReturn = true
            };

            if (isEdit)
            {
                string desc = (string)data.GetType().GetProperty("Description").GetValue(data);
                if (desc != "—")
                {
                    txtDesc.Text = desc;
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
                if (string.IsNullOrWhiteSpace(txtName.Text))
                {
                    MessageBox.Show("Введите название категории.");
                    return;
                }

                var body = new
                {
                    name = txtName.Text.Trim(),
                    description = string.IsNullOrWhiteSpace(txtDesc.Text)
                        ? null
                        : txtDesc.Text.Trim()
                };

                HttpResponseMessage resp;

                if (isEdit)
                {
                    int id = (int)data.GetType().GetProperty("Id").GetValue(data);
                    resp = await _http.PutAsJsonAsync($"api/categories/{id}", body);
                }
                else
                {
                    resp = await _http.PostAsJsonAsync("api/categories", body);
                }

                if (resp.IsSuccessStatusCode)
                {
                    MessageBox.Show("Категория сохранена.",
                        "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    dlg.Close();
                    await LoadCategoriesAsync();
                }
                else
                {
                    string err = await resp.Content.ReadAsStringAsync();
                    MessageBox.Show(err, "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            };

            panel.Children.Add(lbl1);
            panel.Children.Add(txtName);
            panel.Children.Add(lbl2);
            panel.Children.Add(txtDesc);
            panel.Children.Add(btnOk);
            dlg.Content = panel;

            dlg.ShowDialog();
        }

        private async void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            dynamic selected = dgCategories.SelectedItem;

            if (selected == null)
            {
                MessageBox.Show("Выберите категорию для удаления.",
                    "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int id = (int)selected.GetType().GetProperty("Id").GetValue(selected);
            string name = (string)selected.GetType().GetProperty("Name").GetValue(selected);

            var confirm = MessageBox.Show(
                $"Удалить категорию \"{name}\"?",
                "Подтверждение удаления",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes)
            {
                return;
            }

            var resp = await _http.DeleteAsync($"api/categories/{id}");

            if (resp.IsSuccessStatusCode)
            {
                MessageBox.Show("Категория удалена.",
                    "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                await LoadCategoriesAsync();
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