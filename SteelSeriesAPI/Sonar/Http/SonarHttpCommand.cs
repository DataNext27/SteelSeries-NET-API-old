using System.Globalization;
using SteelSeriesAPI.Interfaces;
using SteelSeriesAPI.Sonar.Enums;
using SteelSeriesAPI.Sonar.Models;

namespace SteelSeriesAPI.Sonar.Http;

public class SonarHttpCommand : ISonarCommandHandler
{
    private readonly ISonarBridge _sonarBridge;
    
    public SonarHttpCommand(SonarBridge sonarBridge)
    {
        _sonarBridge = sonarBridge;
    }
    
    public void SetMode(Mode mode)
    {
        if (mode == _sonarBridge.GetMode())
        {
            throw new Exception("Already using this mode");
        }

        if (mode == Mode.Classic) new HttpPut("mode/classic");
        if (mode == Mode.Streamer) new HttpPut("mode/stream");
        Thread.Sleep(100);
    }
    
    public void SetVolume(double vol, Device device, Mode mode, Channel channel)
    {
        string _vol = vol.ToString("0.00", CultureInfo.InvariantCulture);
        string target = mode.ToDictKey() + "/";
        
        if (mode == Mode.Streamer)
        {
            target += channel.ToDictKey() + "/" + device.ToDictKey(DeviceMapChoice.HttpDict) + "/volume/" + _vol;
        }
        else
        {
            target += device.ToDictKey(DeviceMapChoice.HttpDict) + "/Volume/" + _vol;
        }
        Console.WriteLine("volumeSettings/" + target);
        new HttpPut("volumeSettings/" + target);
    }
    
    public void SetMute(bool mute, Device device, Mode mode, Channel channel)
    {
        string target = mode.ToDictKey() + "/";
        if (mode == Mode.Streamer)
        {
            target += channel.ToDictKey() + "/" + device.ToDictKey(DeviceMapChoice.HttpDict) + "/isMuted/" + mute;
        }
        else
        {
            target += device.ToDictKey(DeviceMapChoice.HttpDict) + "/Mute/" + mute;
        }

        new HttpPut("volumeSettings/" + target);
    }

    public void SetConfig(string configId)
    {
        if (string.IsNullOrEmpty(configId)) throw new Exception("Couldn't retrieve config id");

        new HttpPut("configs/" + configId + "/select");
    }

    public void SetConfig(Device device, string name)
    {
        var configs = _sonarBridge.GetAudioConfigurations(device).ToList();
        foreach (var config in configs)
        {
            if (config.Name  == name)
            {
                SetConfig(config.Id);
            }
        }
    }
}