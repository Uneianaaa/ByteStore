using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace ByteStore
{
    public partial class ProfileWindow : Window
    {
        private readonly HttpClient _http = new HttpClient
        {
            BaseAddress = new Uri("https://localhost:7121/")
        };

        private string _token;

        public ProfileWindow(string token)
        {
            InitializeComponent();

            _token = token;

            _http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _token);

            Loaded += async (s, e) => await LoadProfileAsync();
        }

        private async Task LoadProfileAsync()
        {
            try
            {
                var response = await _http.GetAsync("api/profile/me");

                if (response.IsSuccessStatusCode)
                {
                    JsonElement u = await response.Content.ReadFromJsonAsync<JsonElement>();

                    txtFullName.Text = $"{u.GetProperty("lastName").GetString()} " +
                                       $"{u.GetProperty("firstName").GetString()} " +
                                       $"{u.GetProperty("middleName").GetString()}";

                    txtEmail.Text = u.GetProperty("email").GetString() ?? "";

                    txtLastName.Text = u.GetProperty("lastName").GetString() ?? "";
                    txtFirstName.Text = u.GetProperty("firstName").GetString() ?? "";
                    txtMiddleName.Text = u.GetProperty("middleName").GetString() ?? "";
                    txtPhone.Text = u.GetProperty("phone").GetString() ?? "";
                    txtAddress.Text = u.GetProperty("address").GetString() ?? "";

                    string photoPath = u.GetProperty("photoPath").GetString();

                    if (!string.IsNullOrEmpty(photoPath))
                    {
                        string fullUrl = "https://localhost:7121" + photoPath;

                        try
                        {
                            BitmapImage bmp = new BitmapImage();
                            bmp.BeginInit();
                            bmp.UriSource = new Uri(fullUrl, UriKind.Absolute);
                            bmp.CacheOption = BitmapCacheOption.OnLoad;
                            bmp.EndInit();

                            imgBrush.ImageSource = bmp;
                        }
                        catch
                        {
                        }
                    }

                    txtMessage.Text = "Профиль загружен.";
                    txtMessage.Foreground = System.Windows.Media.Brushes.LightGreen;
                }
                else
                {
                    txtMessage.Text = $"Ошибка: {response.StatusCode}";
                    txtMessage.Foreground = System.Windows.Media.Brushes.OrangeRed;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void btnUploadPhoto_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "Изображения|*.jpg;*.jpeg;*.png";
            dialog.Title = "Выберите фото";

            if (dialog.ShowDialog() != true)
            {
                return;
            }

            try
            {
                using (var form = new MultipartFormDataContent())
                {
                    byte[] fileBytes = File.ReadAllBytes(dialog.FileName);
                    var fileContent = new ByteArrayContent(fileBytes);
                    fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");

                    form.Add(fileContent, "photo", Path.GetFileName(dialog.FileName));

                    var response = await _http.PostAsync("api/profile/me/photo", form);

                    if (response.IsSuccessStatusCode)
                    {
                        txtMessage.Text = "Фото загружено.";
                        txtMessage.Foreground = System.Windows.Media.Brushes.LightGreen;

                        await LoadProfileAsync();
                    }
                    else
                    {
                        string error = await response.Content.ReadAsStringAsync();
                        txtMessage.Text = $"Ошибка загрузки фото: {response.StatusCode}";
                        MessageBox.Show(error, "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtLastName.Text) ||
                string.IsNullOrWhiteSpace(txtFirstName.Text))
            {
                txtMessage.Text = "Фамилия и Имя обязательны.";
                txtMessage.Foreground = System.Windows.Media.Brushes.OrangeRed;
                return;
            }

            try
            {
                var request = new
                {
                    lastName = txtLastName.Text.Trim(),
                    firstName = txtFirstName.Text.Trim(),
                    middleName = string.IsNullOrWhiteSpace(txtMiddleName.Text)
                        ? null : txtMiddleName.Text.Trim(),
                    phone = string.IsNullOrWhiteSpace(txtPhone.Text)
                        ? null : txtPhone.Text.Trim(),
                    address = string.IsNullOrWhiteSpace(txtAddress.Text)
                        ? null : txtAddress.Text.Trim()
                };

                var response = await _http.PutAsJsonAsync("api/profile/me", request);

                if (response.IsSuccessStatusCode)
                {
                    txtMessage.Text = "Данные сохранены.";
                    txtMessage.Foreground = System.Windows.Media.Brushes.LightGreen;

                    txtFullName.Text = $"{request.lastName} {request.firstName} {request.middleName}";
                }
                else
                {
                    txtMessage.Text = $"Ошибка сохранения: {response.StatusCode}";
                    txtMessage.Foreground = System.Windows.Media.Brushes.OrangeRed;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnChangePassword_Click(object sender, RoutedEventArgs e)
        {
            Window dlg = new Window
            {
                Title = "Смена пароля",
                Width = 350,
                Height = 250,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                Background = System.Windows.Media.Brushes.DarkSlateGray,
                ResizeMode = ResizeMode.NoResize
            };

            StackPanel panel = new StackPanel { Margin = new Thickness(20) };

            TextBlock lbl1 = new TextBlock
            {
                Text = "Старый пароль",
                Foreground = System.Windows.Media.Brushes.White
            };
            PasswordBox pbOld = new PasswordBox
            {
                Height = 30,
                Margin = new Thickness(0, 5, 0, 15),
                Padding = new Thickness(8, 0, 0, 0)
            };

            TextBlock lbl2 = new TextBlock
            {
                Text = "Новый пароль",
                Foreground = System.Windows.Media.Brushes.White
            };
            PasswordBox pbNew = new PasswordBox
            {
                Height = 30,
                Margin = new Thickness(0, 5, 0, 15),
                Padding = new Thickness(8, 0, 0, 0)
            };

            Button btnOk = new Button
            {
                Content = "СМЕНИТЬ",
                Height = 35,
                Background = System.Windows.Media.Brushes.LightGreen,
                Foreground = System.Windows.Media.Brushes.Black,
                FontWeight = FontWeights.Bold
            };

            btnOk.Click += async (s, args) =>
            {
                if (string.IsNullOrWhiteSpace(pbOld.Password) ||
                    string.IsNullOrWhiteSpace(pbNew.Password))
                {
                    MessageBox.Show("Заполните оба поля.");
                    return;
                }

                var req = new
                {
                    oldPassword = pbOld.Password,
                    newPassword = pbNew.Password
                };

                var resp = await _http.PostAsJsonAsync("api/profile/me/change-password", req);

                if (resp.IsSuccessStatusCode)
                {
                    MessageBox.Show("Пароль изменён.");
                    dlg.Close();
                }
                else
                {
                    string err = await resp.Content.ReadAsStringAsync();
                    MessageBox.Show(err, "Ошибка");
                }
            };

            panel.Children.Add(lbl1);
            panel.Children.Add(pbOld);
            panel.Children.Add(lbl2);
            panel.Children.Add(pbNew);
            panel.Children.Add(btnOk);

            dlg.Content = panel;
            dlg.ShowDialog();
        }

        private void btnChangeEmail_Click(object sender, RoutedEventArgs e)
        {
            Window dlg = new Window
            {
                Title = "Смена email",
                Width = 400,
                Height = 300,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                Background = System.Windows.Media.Brushes.DarkSlateGray,
                ResizeMode = ResizeMode.NoResize
            };

            StackPanel panel = new StackPanel { Margin = new Thickness(20) };

            TextBlock lbl1 = new TextBlock
            {
                Text = "Новый email",
                Foreground = System.Windows.Media.Brushes.White
            };
            TextBox tbEmail = new TextBox
            {
                Height = 30,
                Margin = new Thickness(0, 5, 0, 10),
                Padding = new Thickness(8, 0, 0, 0)
            };

            Button btnSendCode = new Button
            {
                Content = "ПОЛУЧИТЬ КОД",
                Height = 35,
                Margin = new Thickness(0, 0, 0, 15),
                Background = System.Windows.Media.Brushes.Transparent,
                Foreground = System.Windows.Media.Brushes.LightGreen,
                BorderBrush = System.Windows.Media.Brushes.LightGreen,
                BorderThickness = new Thickness(2),
                FontWeight = FontWeights.Bold
            };

            TextBlock lbl2 = new TextBlock
            {
                Text = "Код из письма",
                Foreground = System.Windows.Media.Brushes.White
            };
            TextBox tbCode = new TextBox
            {
                Height = 30,
                Margin = new Thickness(0, 5, 0, 15),
                Padding = new Thickness(8, 0, 0, 0)
            };

            Button btnOk = new Button
            {
                Content = "ПОДТВЕРДИТЬ",
                Height = 35,
                Background = System.Windows.Media.Brushes.LightGreen,
                Foreground = System.Windows.Media.Brushes.Black,
                FontWeight = FontWeights.Bold
            };

            btnSendCode.Click += async (s, args) =>
            {
                if (string.IsNullOrWhiteSpace(tbEmail.Text))
                {
                    MessageBox.Show("Введите новый email.");
                    return;
                }

                var req = new { newEmail = tbEmail.Text.Trim() };
                var resp = await _http.PostAsJsonAsync("api/profile/me/request-email-change", req);

                if (resp.IsSuccessStatusCode)
                {
                    MessageBox.Show("Код отправлен на новый email.");
                }
                else
                {
                    string err = await resp.Content.ReadAsStringAsync();
                    MessageBox.Show(err, "Ошибка");
                }
            };

            btnOk.Click += async (s, args) =>
            {
                if (string.IsNullOrWhiteSpace(tbEmail.Text) ||
                    string.IsNullOrWhiteSpace(tbCode.Text))
                {
                    MessageBox.Show("Введите email и код.");
                    return;
                }

                var req = new
                {
                    newEmail = tbEmail.Text.Trim(),
                    code = tbCode.Text.Trim()
                };

                var resp = await _http.PostAsJsonAsync("api/profile/me/confirm-email-change", req);

                if (resp.IsSuccessStatusCode)
                {
                    MessageBox.Show("Email изменён.");
                    dlg.Close();
                    await LoadProfileAsync();
                }
                else
                {
                    string err = await resp.Content.ReadAsStringAsync();
                    MessageBox.Show(err, "Ошибка");
                }
            };

            panel.Children.Add(lbl1);
            panel.Children.Add(tbEmail);
            panel.Children.Add(btnSendCode);
            panel.Children.Add(lbl2);
            panel.Children.Add(tbCode);
            panel.Children.Add(btnOk);

            dlg.Content = panel;
            dlg.ShowDialog();
        }
    }
}