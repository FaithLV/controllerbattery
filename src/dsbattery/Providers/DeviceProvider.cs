using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using dsbattery.Enums;
using dsbattery.Interfaces;
using dsbattery.Models;

namespace dsbattery.Providers
{
    public class DeviceProvider : IDeviceProvider
    {
        private const string DeviceBasePath = "/sys/class/power_supply";
        private DualshockDevice[] _deviceCache;

        public async Task<DualshockDevice[]> QueryConnected(string pathQuery)
        {
            var devices = Directory.EnumerateFileSystemEntries(DeviceBasePath, pathQuery + "*").ToArray();
            var serialized = new DualshockDevice[devices.Length];

            for(var i = 0; i < serialized.Length; i++)
            {
                serialized[i] = await SerializeDevice(devices[i]).ConfigureAwait(false);
            }

            _deviceCache = serialized;
            return _deviceCache;
        }

        public DualshockDevice[] CachedQuery()
        {
            if (_deviceCache == null)
            {
                throw new InvalidOperationException("No devices cached");
            }

            return _deviceCache;
        }

        private static async Task<DualshockDevice> SerializeDevice(string path)
        {
            var battery = await ReadBattery(path).ConfigureAwait(false);
            var status = await ReadStatus(path).ConfigureAwait(false);
            var macAddress = path.Split('_').LastOrDefault();

            return new DualshockDevice(path)
            {
                BatteryPercentage = battery,
                Status = status,
                Mac = macAddress
            };
        }

        private static async Task<int> ReadBattery(string devicePath)
        {
            const string property = "capacity";

            var batteryResult = await ReadDeviceProperty(devicePath, property).ConfigureAwait(false);
            return int.Parse(batteryResult);
        }

        private static async Task<Ds4Status> ReadStatus(string devicePath)
        {
            const string property = "status";

            var statusResult = await ReadDeviceProperty(devicePath, property).ConfigureAwait(false);
            return Enum.Parse<Ds4Status>(statusResult, true);
        }

        private static async Task<string> ReadDeviceProperty(string devicePath, string propertyName)
        {
            var statusPath = new StringBuilder(devicePath);
            statusPath.Append(Path.DirectorySeparatorChar);
            statusPath.Append(propertyName);

            return await File.ReadAllTextAsync(statusPath.ToString()).ConfigureAwait(false);
        }
    }
}