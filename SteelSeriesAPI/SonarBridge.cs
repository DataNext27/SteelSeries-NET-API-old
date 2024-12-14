﻿using System.Security.Principal;
using SteelSeriesAPI.Interfaces;
using SteelSeriesAPI.Sonar;
using SteelSeriesAPI.Sonar.Enums;
using SteelSeriesAPI.Sonar.Http;
using SteelSeriesAPI.Sonar.Models;

namespace SteelSeriesAPI;

/// <summary>
/// The Sonar object, allow you to get or set volumes, muted states, ...
/// </summary>
public class SonarBridge : ISonarBridge
{
    public bool IsRunning => _sonarRetriever is { IsEnabled: true, IsReady: true, IsRunning: true };
    
    private readonly IAppRetriever _sonarRetriever;
    private readonly ISonarCommandHandler _sonarCommand;
    private readonly ISonarDataProvider _sonarProvider;
    private readonly ISonarSocket _sonarSocket;
    public readonly SonarEventManager SonarEventManager;

    private string _sonarWebServerAddress;

    public SonarBridge()
    {
        _sonarRetriever = SonarRetriever.Instance;
        WaitUntilSonarStarted();
        _sonarWebServerAddress = _sonarRetriever.WebServerAddress();
        SonarEventManager = new SonarEventManager();
        _sonarSocket = new SonarSocket(_sonarWebServerAddress, SonarEventManager);
        _sonarCommand = new SonarHttpCommand(this);
        _sonarProvider = new SonarHttpProvider(this);
    }

    #region Listener
    
    public bool StartListener()
    {
        if (!IsRunAsAdmin())
        {
            throw new ApplicationException("Listener requires Administrator rights to be used");
        }
        
        if (_sonarSocket.IsConnected)
        {
            throw new Exception("Listener already started");
        }
        
        var connected = _sonarSocket.Connect();
        if (!connected)
        {
            return false;
        }

        var listening = _sonarSocket.Listen();
        if (!listening)
        {
            return false;
        }

        return true;
    }

    public void StopListener()
    {
        _sonarSocket.CloseSocket();
    }
    
    private bool IsRunAsAdmin()
    {
        WindowsIdentity id = WindowsIdentity.GetCurrent();
        WindowsPrincipal principal = new WindowsPrincipal(id);

        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }
    
    #endregion

    /// <summary>
    /// Wait until Sonar is started and running before running your code below
    /// </summary>
    public void WaitUntilSonarStarted()
    {
        _sonarRetriever.WaitUntilAppStarted();
    }

    #region Providers

    public Mode GetMode()
    {
        return _sonarProvider.GetMode();
    }

    public VolumeSettings GetVolumeSetting(Device device, Mode mode = Mode.Classic,
        Channel channel = Channel.Monitoring)
    {
        return _sonarProvider.GetVolumeSetting(device, mode, channel);
    }
    
    // volume = 0,00000000 <-- 8 decimal max
    /// <summary>
    /// Get the volume of a device or of a channel
    /// </summary>
    /// <param name="device">The <see cref="Device"/> you want the volume</param>
    /// <param name="mode">The <see cref="Mode"/> you want the volume</param>
    /// <param name="channel">The <see cref="Channel"/> you want the volume</param>
    /// <returns>The volume level between 0 and 1</returns>
    /// <remarks>To use <paramref name="channel"/>, you should put <paramref name="mode"/> to <see cref="Mode.Streamer"/></remarks>
    public double GetVolume(Device device, Mode mode = Mode.Classic, Channel channel = Channel.Monitoring)
    {
        return _sonarProvider.GetVolumeSetting(device, mode, channel).Volume;
    }
    
    /// <summary>
    /// Get the muted state of a device or of a channel
    /// </summary>
    /// <param name="device">The <see cref="Device"/> you want the muted state</param>
    /// <param name="mode">The <see cref="Mode"/> you want the muted state</param>
    /// <param name="channel">The <see cref="Channel"/> you want the muted state</param>
    /// <returns>The muted state, un/muted</returns>
    /// <remarks>To use <paramref name="channel"/>, you should put <paramref name="mode"/> to <see cref="Mode.Streamer"/></remarks>
    public bool GetMute(Device device, Mode mode = Mode.Classic, Channel channel = Channel.Monitoring)
    {
        return _sonarProvider.GetVolumeSetting(device, mode, channel).Mute;
    }

    public IEnumerable<SonarAudioConfiguration> GetAllAudioConfigurations()
    {
        return _sonarProvider.GetAllAudioConfigurations();
    }

    public IEnumerable<SonarAudioConfiguration> GetAudioConfigurations(Device device)
    {
        return _sonarProvider.GetAudioConfigurations(device);
    }

    public SonarAudioConfiguration GetSelectedAudioConfiguration(Device device)
    {
        return _sonarProvider.GetSelectedAudioConfiguration(device);
    }

    public Device GetDeviceFromAudioConfigurationId(string configId)
    {
        return _sonarProvider.GetDeviceFromAudioConfigurationId(configId);
    }
    
    public double GetChatMixBalance()
    {
        return _sonarProvider.GetChatMixBalance();
    }

    public bool GetChatMixState()
    {
        return _sonarProvider.GetChatMixState();
    }

    public IEnumerable<RedirectionDevice> GetRedirectionDevices(Direction direction)
    {
        return _sonarProvider.GetRedirectionDevices(direction);
    }

    public RedirectionDevice GetClassicRedirectionDevice(Device device)
    {
        return _sonarProvider.GetClassicRedirectionDevice(device);
    }

    public RedirectionDevice GetStreamRedirectionDevice(Channel channel)
    {
        return _sonarProvider.GetStreamRedirectionDevice(channel);
    }

    public RedirectionDevice GetStreamRedirectionDevice(Device device = Device.Mic)
    {
        return _sonarProvider.GetStreamRedirectionDevice(device);
    }

    public RedirectionDevice GetRedirectionDeviceFromId(string deviceId)
    {
        return _sonarProvider.GetRedirectionDeviceFromId(deviceId);
    }

    public bool GetRedirectionState(Device device, Channel channel)
    {
        return _sonarProvider.GetRedirectionState(device, channel);
    }

    public bool GetAudienceMonitoringState()
    {
        return _sonarProvider.GetAudienceMonitoringState();
    }

    public IEnumerable<RoutedProcess> GetRoutedProcess(Device device)
    {
        return _sonarProvider.GetRoutedProcess(device);
    }

    #endregion

    #region Commands

    public void SetMode(Mode mode)
    {
        _sonarCommand.SetMode(mode);
    }

    public void SetVolume(double vol, Device device)
    {
        _sonarCommand.SetVolume(vol, device);
    }

    public void SetVolume(double vol, Device device, Channel channel)
    {
        _sonarCommand.SetVolume(vol, device, channel);
    }

    public void SetMute(bool mute, Device device, Mode mode = Mode.Classic, Channel channel = Channel.Monitoring)
    {
        _sonarCommand.SetMute(mute, device, mode, channel);
    }

    public void SetConfig(string configId)
    {
        _sonarCommand.SetConfig(configId);
    }
    
    public void SetConfig(Device device, string name)
    {
        _sonarCommand.SetConfig(device, name);
    }

    public void SetChatMixBalance(double balance)
    {
        _sonarCommand.SetChatMixBalance(balance);
    }

    public void SetClassicRedirectionDevice(string deviceId, Device device)
    {
        _sonarCommand.SetClassicRedirectionDevice(deviceId, device);
    }

    public void SetStreamRedirectionDevice(string deviceId, Channel channel)
    {
        _sonarCommand.SetStreamRedirectionDevice(deviceId, channel);
    }

    public void SetStreamRedirectionDevice(string deviceId, Device device = Device.Mic)
    {
        _sonarCommand.SetStreamRedirectionDevice(deviceId, device);
    }
    
    public void SetRedirectionState(bool newState, Device device, Channel channel)
    {
        _sonarCommand.SetRedirectionState(newState, device, channel);
    }

    public void SetAudienceMonitoringState(bool newState)
    {
        _sonarCommand.SetAudienceMonitoringState(newState);
    }

    public void SetProcessToDeviceRouting(int pId, Device device)
    {
        _sonarCommand.SetProcessToDeviceRouting(pId, device);
    }

    #endregion
}