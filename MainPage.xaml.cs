using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Devices;
using Windows.Devices.Enumeration;
using System.Collections.ObjectModel;
using Windows.UI.Core;
using Windows.Devices.Bluetooth;
using Windows.Security.Cryptography;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.GenericAttributeProfile;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace BLE
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        DeviceWatcher deviceWatcher;
        BluetoothLEDevice currentDevice;

        public ObservableCollection<DiscoveredDevice> DiscoveredDevices { get; set; } = new ObservableCollection<DiscoveredDevice>();
        public MainPage()
        {
            this.InitializeComponent();
            ProgressRing.IsActive = false;
            DataContext = this;
        }

        private void BtnSearch_Click(object sender, RoutedEventArgs e)
        {
            DiscoveredDevices.Clear();
            StartWatching();
        }

        void StartWatching()
        {
            if (deviceWatcher == null)
            {
                string[] requestedProperties = { "System.Devices.Aep.DeviceAddress", "System.Devices.Aep.IsConnected", "System.Devices.Aep.Bluetooth.Le.IsConnectable" };

                // BT_Code: Example showing paired and non-paired in a single query.
                string aqsAllBluetoothLEDevices = "(System.Devices.Aep.ProtocolId:=\"{bb7bb05e-5972-42b5-94fc-76eaa7084d49}\")";

                deviceWatcher =
                        DeviceInformation.CreateWatcher(
                            aqsAllBluetoothLEDevices,
                            requestedProperties,
                            DeviceInformationKind.AssociationEndpoint);

                deviceWatcher.Added += DeviceWatcher_Added;
                deviceWatcher.Updated += DeviceWatcher_Updated;
                deviceWatcher.Removed += DeviceWatcher_Removed;
                deviceWatcher.EnumerationCompleted += DeviceWatcher_EnumerationCompleted;
                deviceWatcher.Stopped += DeviceWatcher_Stopped;
            }

            deviceWatcher.Start();
            ProgressRing.IsActive = true;
        }

        private void DeviceWatcher_Stopped(DeviceWatcher sender, object args)
        {
            Dispatch(() =>
            {
                ProgressRing.IsActive = false;
            });
        }

        private void DeviceWatcher_EnumerationCompleted(DeviceWatcher sender, object args)
        {
            Dispatch(() =>
            {
                ProgressRing.IsActive = false;
            });
        }

        private void DeviceWatcher_Added(DeviceWatcher sender, DeviceInformation args)
        {
            Dispatch(() =>
            {
                DiscoveredDevices.Add(new DiscoveredDevice(args));
            });
        }

        private void DeviceWatcher_Updated(DeviceWatcher sender, DeviceInformationUpdate args)
        {
            Dispatch(() =>
            {
                var device = DiscoveredDevices.FirstOrDefault(x => x.Id == args.Id);
                if (device != null) device.Update(args);
            });
        }

        private void DeviceWatcher_Removed(DeviceWatcher sender, DeviceInformationUpdate args)
        {
            Dispatch(() =>
            {
                var device = DiscoveredDevices.FirstOrDefault(x => x.Id == args.Id);
                if (device != null) DiscoveredDevices.Remove(device);
            });
        }

        async void Dispatch(Action a)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                () =>
                {
                    lock (this)
                    {
                        a();
                    }
                });
        }

        private async void BtnConnect_Click(object sender, RoutedEventArgs e)
        {
            deviceWatcher.Stop();

            TxtStatus.Text = "Connecting...";
            if (DevicesList.SelectedItem is DiscoveredDevice d)
            {
                currentDevice = await BluetoothLEDevice.FromIdAsync(d.Id);
                if (currentDevice == null)
                {
                    TxtStatus.Text = "Could not connect: device is null.";
                    return;
                }

                TxtStatus.Text = "Services:\n";
                var services = await currentDevice.GetGattServicesAsync();
                foreach (var service in services.Services)
                {
                    TxtStatus.Text += service.Uuid + "\n";
                }

                var selectedService = services.Services[0];

                TxtStatus.Text += $"Characteristics of {selectedService.Uuid}:\n";
                var characteristics = await selectedService.GetCharacteristicsAsync();
                CharacteristicsList.ItemsSource = characteristics.Characteristics;

                var selectedCharacteristic = characteristics.Characteristics.FirstOrDefault(
                    x => x.Uuid.ToString().Contains("FFE2")); //write command to this, elegoo responds
                if (selectedCharacteristic != null)
                    CharacteristicsList.SelectedItem = selectedCharacteristic;
            }
        }

        async Task WriteAsync(GattCharacteristic characteristic, string command)
        {
            var writeBuffer = CryptographicBuffer.ConvertStringToBinary(command,
                BinaryStringEncoding.Utf8);

            try
            {
                // BT_Code: Writes the value from the buffer to the characteristic.
                var result = await characteristic.WriteValueWithResultAsync(writeBuffer);

                if (result.Status == GattCommunicationStatus.Success)
                {
                    TxtStatus.Text = $"Write to characteristic successful for: {command}\n" + TxtStatus.Text;
                    return;
                }
                else
                {
                    TxtStatus.Text = $"Write to characteristic fail for: {command} -- {result.Status}\n" + TxtStatus.Text;
                    return;
                }
            }
            catch (Exception ex)
            {
                TxtStatus.Text = $"Error writing characteristic: {ex}\n{TxtStatus.Text}";
            }
        }

        private void BtnSend_Click(object sender, RoutedEventArgs e)
        {
            Send(TxtCommand.Text);
        }

        async void Send(string command)
        {
            if (CharacteristicsList.SelectedItem is GattCharacteristic characteristic)
            {
                await WriteAsync(characteristic, command);
            }
        }

        private void BtnF_Click(object sender, RoutedEventArgs e)
        {
            Send("f");
        }

        private void BtnB_Click(object sender, RoutedEventArgs e)
        {
            Send("b");
        }

        private void BtnL_Click(object sender, RoutedEventArgs e)
        {
            Send("l");
        }

        private void BtnR_Click(object sender, RoutedEventArgs e)
        {
            Send("r");
        }

        private void Btn1_Click(object sender, RoutedEventArgs e)
        {
            Send("1");
        }

        private void Btn2_Click(object sender, RoutedEventArgs e)
        {
            Send("2");
        }

        private void BtnS_Click(object sender, RoutedEventArgs e)
        {
            Send("s");
        }
    }
}
