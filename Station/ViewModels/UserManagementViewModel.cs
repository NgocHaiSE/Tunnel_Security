using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace Station.ViewModels
{
    public partial class UserManagementViewModel : ObservableObject
    {
        public ObservableCollection<UserItem> Users { get; } = new();
        public ObservableCollection<RoleItem> Roles { get; } = new();

        [ObservableProperty]
        private RoleItem? selectedRole;

        private UserItem? selectedUser;
        public UserItem? SelectedUser
        {
            get => selectedUser;
            set => SetProperty(ref selectedUser, value);
        }

        // ==== SỐ LIỆU TỔNG QUAN ====
        public int TotalUsers => Users.Count;
        public int ActiveUsers => Users.Count(u => u.IsActive);
        public int LockedUsers => Users.Count(u => !u.IsActive);

        public int TotalRoles => Roles.Count;
        public int CustomRoles => Roles.Count(r => !r.IsSystem);
        public int TotalUsersRoles => Roles.Sum(r => r.UsersCount);
        public double AvgPermissions => Roles.Count == 0 ? 0 : Roles.Average(r => r.PermissionsCount);

        public UserManagementViewModel()
        {
            Users.CollectionChanged += Users_CollectionChanged;
            Roles.CollectionChanged += Roles_CollectionChanged;


            // ===== USERS MOCK =====
            Users.Add(new UserItem
            {
                UserName = "admin",
                FullName = "Quản trị hệ thống",
                Role = "Administrator",
                Email = "admin@example.com",
                IsActive = true
            });

            Users.Add(new UserItem
            {
                UserName = "operator1",
                FullName = "Nhân viên trực ca 1",
                Role = "Operator",
                Email = "operator1@example.com",
                IsActive = true
            });

            Users.Add(new UserItem
            {
                UserName = "operator2",
                FullName = "Nhân viên trực ca 2",
                Role = "Operator",
                Email = "operator2@example.com",
                IsActive = true
            });

            Users.Add(new UserItem
            {
                UserName = "viewer1",
                FullName = "Người xem 1",
                Role = "Viewer",
                Email = "viewer1@example.com",
                IsActive = false
            });

            Users.Add(new UserItem
            {
                UserName = "viewer2",
                FullName = "Người xem 2",
                Role = "Viewer",
                Email = "viewer2@example.com",
                IsActive = false
            });

            Users.Add(new UserItem
            {
                UserName = "viewer3",
                FullName = "Người xem 3",
                Role = "Viewer",
                Email = "viewer3@example.com",
                IsActive = false
            });

            // ===== ROLES MOCK =====
            var admin = new RoleItem
            {
                Name = "Administrator",
                Description = "Full access to all system features and settings",
                IsSystem = true,
                PermissionsCount = 14,
                UsersCount = 2
            };
            admin.Permissions.Add("View all dashboards and data");
            admin.Permissions.Add("Create, edit and delete users");
            admin.Permissions.Add("Configure stations and devices");
            admin.Permissions.Add("Manage alert rules");

            var editor = new RoleItem
            {
                Name = "Operator",
                Description = "Có thể giám sát, xử lý cảnh báo và chỉnh sửa cấu hình cơ bản",
                IsSystem = true,
                PermissionsCount = 6,
                UsersCount = 3
            };
            editor.Permissions.Add("View live dashboards");
            editor.Permissions.Add("Acknowledge and comment alerts");
            editor.Permissions.Add("Edit basic device info");

            var viewer = new RoleItem
            {
                Name = "Viewer",
                Description = "Chỉ xem dữ liệu, không được phép chỉnh sửa",
                IsSystem = false,
                PermissionsCount = 3,
                UsersCount = 5
            };
            viewer.Permissions.Add("View dashboards and reports");
            viewer.Permissions.Add("View device status");
            viewer.Permissions.Add("View alert history");

            Roles.Add(admin);
            Roles.Add(editor);
            Roles.Add(viewer);

            // Mặc định chọn role đầu tiên để panel chi tiết luôn có dữ liệu
            if (Roles.Count > 0)
            {
                SelectedRole = Roles[0];
            }
        }

        private void Users_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            OnPropertyChanged(nameof(TotalUsers));
            OnPropertyChanged(nameof(ActiveUsers));
            OnPropertyChanged(nameof(LockedUsers));
        }
        private void Roles_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            OnPropertyChanged(nameof(TotalRoles));
            OnPropertyChanged(nameof(CustomRoles));
            OnPropertyChanged(nameof(AvgPermissions));
            OnPropertyChanged(nameof(TotalUsersRoles));
        }


        public void AddUser(UserItem user)
        {
            Users.Add(user);
            Users_CollectionChanged(this, null!);
        }

        public void UpdateUser(UserItem target, UserItem updated)
        {
            if (target == null) return;

            target.UserName = updated.UserName;
            target.FullName = updated.FullName;
            target.Role = updated.Role;
            target.Email = updated.Email;
            target.Note = updated.Note;
            target.IsActive = updated.IsActive;

            Users_CollectionChanged(this, null!);
        }

        public void DeleteUser(UserItem user)
        {
            if (user == null) return;
            Users.Remove(user);
            Users_CollectionChanged(this, null!);
        }
        public void AddRole(RoleItem role)
        {
            if (role == null) return;

            Roles.Add(role);
            Roles_CollectionChanged(this, null!);
        }

        public void UpdateRole(RoleItem target, RoleItem updated)
        {
            if (target == null || updated == null) return;

            target.Name = updated.Name;
            target.Description = updated.Description;
            target.IsSystem = updated.IsSystem;
            target.UsersCount = updated.UsersCount;

            // cập nhật danh sách quyền
            target.Permissions.Clear();
            foreach (var p in updated.Permissions)
            {
                target.Permissions.Add(p);
            }
            target.PermissionsCount = target.Permissions.Count;

            Roles_CollectionChanged(this, null!);
        }

        public void DeleteRole(RoleItem role)
        {
            if (role == null) return;

            Roles.Remove(role);
            Roles_CollectionChanged(this, null!);
        }

    }

    public partial class RoleItem : ObservableObject
    {
        private string name = string.Empty;
        public string Name
        {
            get => name;
            set => SetProperty(ref name, value);
        }

        private string description = string.Empty;
        public string Description
        {
            get => description;
            set => SetProperty(ref description, value);
        }

        private bool isSystem;
        public bool IsSystem
        {
            get => isSystem;
            set
            {
                if (SetProperty(ref isSystem, value))
                    OnPropertyChanged(nameof(KindText));
            }
        }

        private int permissionsCount;
        public int PermissionsCount
        {
            get => permissionsCount;
            set => SetProperty(ref permissionsCount, value);
        }

        private int usersCount;
        public int UsersCount
        {
            get => usersCount;
            set => SetProperty(ref usersCount, value);
        }

        // Danh sách mô tả quyền (để show ở panel chi tiết)
        public ObservableCollection<string> Permissions { get; } = new();

        public string KindText => IsSystem ? "Hệ thống" : "Tuỳ chỉnh";

        public SolidColorBrush BadgeBrush =>
            IsSystem
                ? new SolidColorBrush(Color.FromArgb(255, 59, 130, 246)) // xanh dương
                : new SolidColorBrush(Color.FromArgb(255, 234, 179, 8)); // vàng
    }

    public class UserItem : ObservableObject
    {
        private string userName = string.Empty;
        public string UserName
        {
            get => userName;
            set => SetProperty(ref userName, value);
        }

        private string fullName = string.Empty;
        public string FullName
        {
            get => fullName;
            set => SetProperty(ref fullName, value);
        }

        private string role = "Viewer";
        public string Role
        {
            get => role;
            set => SetProperty(ref role, value);
        }

        private string email = string.Empty;
        public string Email
        {
            get => email;
            set => SetProperty(ref email, value);
        }

        private string note = string.Empty;
        public string Note
        {
            get => note;
            set => SetProperty(ref note, value);
        }

        private bool isActive = true;
        public bool IsActive
        {
            get => isActive;
            set
            {
                if (SetProperty(ref isActive, value))
                {
                    OnPropertyChanged(nameof(StatusText));
                    OnPropertyChanged(nameof(StatusBrush));
                }
            }
        }

        public string StatusText => IsActive ? "Hoạt động" : "Khóa";

        public SolidColorBrush StatusBrush =>
            IsActive
                ? new SolidColorBrush(Color.FromArgb(255, 22, 163, 74))    // xanh lá
                : new SolidColorBrush(Color.FromArgb(255, 148, 163, 184)); // xám
    }
}
