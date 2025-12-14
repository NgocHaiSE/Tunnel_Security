using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using Station.Views;

namespace Station.Services
{
    /// <summary>
    /// Manages secondary windows for navigation tabs
    /// </summary>
    public class WindowManager
    {
        private static WindowManager? _instance;
        private readonly Dictionary<string, Window> _openWindows = new();

        public static WindowManager Instance => _instance ??= new WindowManager();

        private WindowManager() { }

        /// <summary>
        /// Open or activate a window for the specified page type
        /// </summary>
        public void OpenOrActivateWindow(string windowKey, Type pageType, string title)
        {
            // If window already exists, activate it
            if (_openWindows.ContainsKey(windowKey) && _openWindows[windowKey] != null)
            {
                try
                {
                    _openWindows[windowKey].Activate();
                    return;
                }
                catch
                {
                    // Window was closed, remove from dictionary
                    _openWindows.Remove(windowKey);
                }
            }

            // Create new window
            var newWindow = new Window
            {
                Title = title,
                SystemBackdrop = new Microsoft.UI.Xaml.Media.MicaBackdrop()
            };

            // Set window size based on content
            var windowSize = GetWindowSizeForPage(pageType);
            newWindow.AppWindow.Resize(windowSize);

            // Create frame and navigate
            var frame = new Frame();
            frame.Navigate(pageType);
            newWindow.Content = frame;

            // Handle window closed event
            newWindow.Closed += (s, e) =>
            {
                if (_openWindows.ContainsKey(windowKey))
                {
                    _openWindows.Remove(windowKey);
                }
            };

            // Store and activate
            _openWindows[windowKey] = newWindow;
            newWindow.Activate();
        }

        /// <summary>
        /// Get recommended window size for specific page types
        /// </summary>
        private Windows.Graphics.SizeInt32 GetWindowSizeForPage(Type pageType)
        {
            if (pageType == typeof(DevicesPage))
                return new Windows.Graphics.SizeInt32(1400, 900);
            else if (pageType == typeof(LiveVideoPage))
                return new Windows.Graphics.SizeInt32(1600, 900);
            else if (pageType == typeof(MapPage))
                return new Windows.Graphics.SizeInt32(1400, 800);
            else if (pageType == typeof(AlertsPage))
                return new Windows.Graphics.SizeInt32(1200, 800);
            else if (pageType == typeof(DataPage))
                return new Windows.Graphics.SizeInt32(1400, 900);
            else if (pageType == typeof(ConfigurationPage))
                return new Windows.Graphics.SizeInt32(800, 700);
            else
                return new Windows.Graphics.SizeInt32(1200, 800);
        }

        /// <summary>
        /// Close all secondary windows
        /// </summary>
        public void CloseAllWindows()
        {
            foreach (var window in _openWindows.Values)
            {
                try
                {
                    window?.Close();
                }
                catch { }
            }
            _openWindows.Clear();
        }
    }
}