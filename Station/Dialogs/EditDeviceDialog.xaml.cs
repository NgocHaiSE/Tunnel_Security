using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Station.ViewModels;

namespace Station.Dialogs;

/// <summary>
/// Content Dialog for editing device information
/// </summary>
public sealed partial class EditDeviceDialog : ContentDialog
{
    private readonly DeviceItemViewModel _device;

    public EditDeviceDialog(DeviceItemViewModel device)
    {
        this.InitializeComponent();
        _device = device;
        LoadDeviceData();

        // Handle save button click
        this.PrimaryButtonClick += OnPrimaryButtonClick;
    }

    private void LoadDeviceData()
    {
        // Load current device data into form fields
        DeviceNameTextBox.Text = _device.Name;
        DeviceIdTextBox.Text = _device.DeviceId;
        TypeDisplayTextBox.Text = _device.TypeDisplay;
        LocationTextBox.Text = _device.Location;
        IpAddressTextBox.Text = _device.IpAddress;
        ManufacturerTextBox.Text = _device.Manufacturer;
        FirmwareVersionTextBox.Text = _device.FirmwareVersion;
    }

    private void OnPrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        // Validate required fields
        if (string.IsNullOrWhiteSpace(DeviceNameTextBox.Text))
        {
            args.Cancel = true;
            _ = ShowValidationErrorAsync("Vui lòng nh?p tên thi?t b?");
            return;
        }

        if (string.IsNullOrWhiteSpace(IpAddressTextBox.Text))
        {
            args.Cancel = true;
            _ = ShowValidationErrorAsync("Vui lòng nh?p ??a ch? IP");
            return;
        }

        // Update device properties
        _device.Name = DeviceNameTextBox.Text.Trim();
        _device.TypeDisplay = TypeDisplayTextBox.Text.Trim();
        _device.Location = LocationTextBox.Text.Trim();
        _device.IpAddress = IpAddressTextBox.Text.Trim();
        _device.Manufacturer = ManufacturerTextBox.Text.Trim();
        _device.FirmwareVersion = FirmwareVersionTextBox.Text.Trim();

        System.Diagnostics.Debug.WriteLine($"Device updated: {_device.Name}");
    }

    private async System.Threading.Tasks.Task ShowValidationErrorAsync(string message)
    {
        var errorDialog = new ContentDialog
        {
            Title = "L?i xác th?c",
            Content = message,
            CloseButtonText = "?óng",
            XamlRoot = this.XamlRoot,
            RequestedTheme = ElementTheme.Light
        };

        await errorDialog.ShowAsync();
    }
}
