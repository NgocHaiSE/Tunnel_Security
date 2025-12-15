using System;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Station.ViewModels;
using Windows.UI;   // để dùng Colors

namespace Station.Views
{
    public sealed partial class UserManagementPage : Page
    {
        public UserManagementViewModel ViewModel => (UserManagementViewModel)DataContext;

        public UserManagementPage()
        {
            InitializeComponent();
            UpdateTabVisualState(showUsers: true);
        }

        private async void AddUser_Click(object sender, RoutedEventArgs e)
        {
            var dialog = CreateUserDialog("Thêm người dùng", null);
            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                var user = GetUserFromDialog(dialog);
                ViewModel.AddUser(user);
            }
        }

        private async void EditUser_Click(object sender, RoutedEventArgs e)
        {
            // Lấy User tương ứng với nút Sửa được bấm
            var user = (sender as FrameworkElement)?.DataContext as UserItem
                       ?? ViewModel.SelectedUser;
            if (user == null) return;

            var dialog = CreateUserDialog("Chỉnh sửa người dùng", user);
            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                var updated = GetUserFromDialog(dialog);
                ViewModel.UpdateUser(user, updated);
            }
        }

        private async void DeleteUser_Click(object sender, RoutedEventArgs e)
        {
            var user = (sender as FrameworkElement)?.DataContext as UserItem
                       ?? ViewModel.SelectedUser;
            if (user == null) return;

            var confirm = new ContentDialog
            {
                Title = "Xóa người dùng",
                Content = $"Bạn có chắc chắn muốn xóa tài khoản '{user.UserName}'?",
                PrimaryButtonText = "Xóa",
                CloseButtonText = "Hủy",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = this.XamlRoot
            };

            var result = await confirm.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                ViewModel.DeleteUser(user);
            }
        }

            
        // ===== Helper cho dialog thêm/sửa =====

        private ContentDialog CreateUserDialog(string title, UserItem? existing)
        {
            var userNameBox = new TextBox
            {
                Header = "Tài khoản",
                Text = existing?.UserName ?? "",
                Margin = new Thickness(0, 0, 0, 8)
            };

            var fullNameBox = new TextBox
            {
                Header = "Họ tên",
                Text = existing?.FullName ?? "",
                Margin = new Thickness(0, 0, 0, 8)
            };

            var roleBox = new ComboBox
            {
                Header = "Vai trò",
                ItemsSource = new[] { "Admin", "Operator", "Viewer" },
                SelectedItem = existing?.Role ?? "Viewer",
                Margin = new Thickness(0, 0, 0, 8)
            };

            var activeSwitch = new ToggleSwitch
            {
                Header = "Hoạt động",
                IsOn = existing?.IsActive ?? true
            };

            var panel = new StackPanel();
            panel.Children.Add(userNameBox);
            panel.Children.Add(fullNameBox);
            panel.Children.Add(roleBox);
            panel.Children.Add(activeSwitch);

            var dialog = new ContentDialog
            {
                Title = title,
                Content = panel,
                PrimaryButtonText = "Lưu",
                CloseButtonText = "Hủy",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.XamlRoot,
                Tag = new DialogControls(userNameBox, fullNameBox, roleBox, activeSwitch)
            };

            return dialog;
        }

        private UserItem GetUserFromDialog(ContentDialog dialog)
        {
            var controls = (DialogControls)dialog.Tag;

            return new UserItem
            {
                UserName = controls.UserNameBox.Text.Trim(),
                FullName = controls.FullNameBox.Text.Trim(),
                Role = controls.RoleBox.SelectedItem?.ToString() ?? "Viewer",
                IsActive = controls.ActiveSwitch.IsOn
            };
        }

        private record DialogControls(
            TextBox UserNameBox,
            TextBox FullNameBox,
            ComboBox RoleBox,
            ToggleSwitch ActiveSwitch);

        // ====== TAB HANDLERS ======

        private void UsersTabButton_Click(object sender, RoutedEventArgs e)
        {
            UpdateTabVisualState(showUsers: true);
        }

        private void RolesTabButton_Click(object sender, RoutedEventArgs e)
        {
            UpdateTabVisualState(showUsers: false);
        }

        private void UpdateTabVisualState(bool showUsers)
        {
            UsersSection.Visibility = showUsers ? Visibility.Visible : Visibility.Collapsed;
            RolesSection.Visibility = showUsers ? Visibility.Collapsed : Visibility.Visible;

            var primary = (Brush)Application.Current.Resources["PrimaryLightBrush"];
            var bgPrimary = (Brush)Application.Current.Resources["BackgroundPrimaryBrush"];
            var textSecondary = (Brush)Application.Current.Resources["TextSecondaryBrush"];

            if (showUsers)
            {
                UsersTabButton.Background = primary;
                UsersTabButton.Foreground = new SolidColorBrush(Colors.White);

                RolesTabButton.Background = bgPrimary;
                RolesTabButton.Foreground = textSecondary;
            }
            else
            {
                RolesTabButton.Background = primary;
                RolesTabButton.Foreground = new SolidColorBrush(Colors.White);

                UsersTabButton.Background = bgPrimary;
                UsersTabButton.Foreground = textSecondary;
            }
        }
        private record RoleDialogControls(
    TextBox NameBox,
    TextBox DescriptionBox,
    ToggleSwitch IsSystemSwitch,
    TextBox PermissionsBox,
    NumberBox UsersCountBox);
        private ContentDialog CreateRoleDialog(string title, RoleItem? existing)
        {
            var nameBox = new TextBox
            {
                Header = "Tên vai trò",
                Text = existing?.Name ?? "",
                Margin = new Thickness(0, 0, 0, 8)
            };

            var descriptionBox = new TextBox
            {
                Header = "Mô tả",
                Text = existing?.Description ?? "",
                Margin = new Thickness(0, 0, 0, 8)
            };

            var isSystemSwitch = new ToggleSwitch
            {
                Header = "Vai trò hệ thống",
                IsOn = existing?.IsSystem ?? false,
                Margin = new Thickness(0, 0, 0, 8)
            };

            var permissionsBox = new TextBox
            {
                Header = "Danh sách quyền (mỗi dòng 1 quyền)",
                AcceptsReturn = true,
                Text = existing != null
                    ? string.Join(Environment.NewLine, existing.Permissions)
                    : "",
                Height = 120,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 8)
            };

            var usersCountBox = new NumberBox
            {
                Header = "Số người dùng",
                Minimum = 0,
                Value = existing?.UsersCount ?? 0
            };

            var panel = new StackPanel();
            panel.Children.Add(nameBox);
            panel.Children.Add(descriptionBox);
            panel.Children.Add(isSystemSwitch);
            panel.Children.Add(permissionsBox);
            panel.Children.Add(usersCountBox);

            var dialog = new ContentDialog
            {
                Title = title,
                Content = panel,
                PrimaryButtonText = "Lưu",
                CloseButtonText = "Hủy",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.XamlRoot,
                Tag = new RoleDialogControls(
                    nameBox,
                    descriptionBox,
                    isSystemSwitch,
                    permissionsBox,
                    usersCountBox)
            };

            return dialog;
        }
        private RoleItem GetRoleFromDialog(ContentDialog dialog)
        {
            var controls = (RoleDialogControls)dialog.Tag;

            var role = new RoleItem
            {
                Name = controls.NameBox.Text.Trim(),
                Description = controls.DescriptionBox.Text.Trim(),
                IsSystem = controls.IsSystemSwitch.IsOn,
                UsersCount = (int)controls.UsersCountBox.Value
            };

            role.Permissions.Clear();
            var lines = controls.PermissionsBox.Text
                .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines)
            {
                role.Permissions.Add(line.Trim());
            }

            role.PermissionsCount = role.Permissions.Count;

            return role;
        }
        private async void AddRole_Click(object sender, RoutedEventArgs e)
        {
            var dialog = CreateRoleDialog("Tạo vai trò", null);
            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                var role = GetRoleFromDialog(dialog);
                ViewModel.AddRole(role);
            }
        }

        private async void EditRole_Click(object sender, RoutedEventArgs e)
        {
            var role = (sender as FrameworkElement)?.DataContext as RoleItem;
            if (role == null) return;

            var dialog = CreateRoleDialog("Chỉnh sửa vai trò", role);
            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                var updated = GetRoleFromDialog(dialog);
                ViewModel.UpdateRole(role, updated);
            }
        }

        private async void DeleteRole_Click(object sender, RoutedEventArgs e)
        {
            var role = (sender as FrameworkElement)?.DataContext as RoleItem;
            if (role == null) return;

            var confirm = new ContentDialog
            {
                Title = "Xóa vai trò",
                Content = $"Bạn có chắc chắn muốn xóa vai trò '{role.Name}'?",
                PrimaryButtonText = "Xóa",
                CloseButtonText = "Hủy",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = this.XamlRoot
            };

            var result = await confirm.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                ViewModel.DeleteRole(role);
            }
        }

    }


}
