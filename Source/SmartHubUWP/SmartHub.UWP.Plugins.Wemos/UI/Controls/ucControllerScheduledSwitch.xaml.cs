﻿using Newtonsoft.Json;
using SmartHub.UWP.Core;
using SmartHub.UWP.Core.Xaml;
using SmartHub.UWP.Plugins.Things.Models;
using SmartHub.UWP.Plugins.Wemos.Core.Models;
using SmartHub.UWP.Plugins.Wemos.Infrastructure.Controllers;
using SmartHub.UWP.Plugins.Wemos.Infrastructure.Controllers.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace SmartHub.UWP.Plugins.Wemos.UI.Controls
{
    public sealed partial class ucControllerScheduledSwitch : UserControl
    {
        #region Fields
        private WemosControllerWorkerScheduledSwitch.ControllerConfiguration configuration = null;
        #endregion

        #region Properties
        public static readonly DependencyProperty ControllerProperty = DependencyProperty.Register("Controller", typeof(WemosControllerObservable), typeof(ucControllerScheduledSwitch), new PropertyMetadata(null, new PropertyChangedCallback(OnControllerChanged)));
        public WemosControllerObservable Controller
        {
            get { return (WemosControllerObservable) GetValue(ControllerProperty); }
            set { SetValue(ControllerProperty, value); }
        }
        private static void OnControllerChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
        }

        public ObservableCollection<WemosLine> Lines
        {
            get;
        } = new ObservableCollection<WemosLine>();
        #endregion

        #region Constructor
        public ucControllerScheduledSwitch(WemosControllerObservable ctrl)
        {
            InitializeComponent();
            XamlUtils.FindFirstVisualChild<Grid>(this).DataContext = this;

            Controller = ctrl;
            if (Controller != null)
            {
                if (string.IsNullOrEmpty(Controller.Configuration))
                {
                    configuration = new WemosControllerWorkerScheduledSwitch.ControllerConfiguration();
                    SaveConfiguration();
                }
                else
                    configuration = JsonConvert.DeserializeObject<WemosControllerWorkerScheduledSwitch.ControllerConfiguration>(Controller.Configuration);
            }
        }
        #endregion

        #region Private methods
        private async Task UpdateLinesList()
        {
            var models = (await CoreUtils.RequestAsync<List<WemosLine>>("/api/wemos/lines")).Where(m => m.Type == LineType.Switch);
            foreach (var model in models)
                Lines.Add(model);

            cbLines.SelectedItem = Lines.FirstOrDefault(l => l.ID == configuration.LineSwitchID);
        }
        private void UpdatePeriodsList()
        {
            lvPeriods.ItemsSource = configuration.ActivePeriods;
            if (configuration.ActivePeriods != null)
            {
                configuration.ActivePeriods.All(period => {
                    period.PropertyChanged += (s, e) => { SaveConfiguration(); };
                    return true;
                });

                configuration.ActivePeriods.CollectionChanged += (s, e) =>
                {
                    if (e.NewItems != null)
                        foreach (Period period in e.NewItems)
                            period.PropertyChanged += (s1, e1) => { SaveConfiguration(); };

                    SaveConfiguration();
                };
            }
        }
        private void SaveConfiguration()
        {
            if (Controller != null && configuration != null)
                Controller.Configuration = JsonConvert.SerializeObject(configuration);
        }
        #endregion

        #region Event handlers
        private async void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            await UpdateLinesList();
            UpdatePeriodsList();
        }
        private void cbLines_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var line = cbLines.SelectedItem as WemosLine;
            if (line != null && line.ID != configuration.LineSwitchID)
            {
                configuration.LineSwitchID = line.ID;
                SaveConfiguration();
            }
        }
        private void btnAddPeriod_Click(object sender, RoutedEventArgs e)
        {
            configuration.ActivePeriods.Add(new Period
            {
                From = new TimeSpan(0, 0, 0),
                To = new TimeSpan(1, 0, 0),
                IsEnabled = false,
                Value = 0
            });
        }
        private async void btnDeletePeriod_Click(object sender, RoutedEventArgs e)
        {
            var period = (Period) ((sender as Button).Tag);

            await CoreUtils.MessageBoxYesNo(XamlUtils.GetLocalizedString(AppManager.AppData.Language, "confirmDeleteItem"), (onYes) =>
            {
                configuration.ActivePeriods.Remove(period);
            });
        }
        #endregion
    }
}
