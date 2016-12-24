﻿using SmartHub.UWP.Core;
using SmartHub.UWP.Core.Communication.Stream;
using SmartHub.UWP.Plugins.Wemos.Models;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;

namespace SmartHub.UWP.Plugins.Wemos.UI
{
    public sealed partial class Settings : UserControl
    {
        #region Fields
        private StreamClient apiClient = new StreamClient();
        #endregion

        #region Properties
        public ObservableCollection<WemosNode> Nodes
        {
            get;
        } = new ObservableCollection<WemosNode>();
        public ObservableCollection<WemosLine> Lines
        {
            get;
        } = new ObservableCollection<WemosLine>();
        #endregion

        #region Constructor
        public Settings()
        {
            InitializeComponent();
            Utils.FindFirstVisualChild<Grid>(this).DataContext = this;
        }
        #endregion

        #region Event handlers
        private async void UserControl_Loaded(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            await apiClient.StartAsync(AppManager.RemoteUrl, AppManager.RemoteServiceName);

            await UpdateNodesList();
            await UpdateLinesList();

            //GridLocalizationManager.
        }
        #endregion

        #region Private methods
        private async Task UpdateNodesList()
        {
            var items = await apiClient.RequestAsync<IEnumerable<WemosNode>>("/api/wemos/nodes");

            Nodes.Clear();
            foreach (var item in items)
                Nodes.Add(item);
        }
        private async Task UpdateLinesList()
        {
            var items = await apiClient.RequestAsync<IEnumerable<WemosLine>>("/api/wemos/lines");

            Lines.Clear();
            foreach (var item in items)
                Lines.Add(item);
        }
        #endregion
    }
}
