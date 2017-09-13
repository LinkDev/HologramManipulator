#if WINDOWS_UWP && NETFX_CORE
using Windows.System.Profile;
using Windows.UI.ViewManagement;


public static class DeviceTypeHelper
{
    static string test;
    public static DeviceFormFactorType GetDeviceFormFactorType()
    {
        test = AnalyticsInfo.VersionInfo.DeviceFamily;
        switch (AnalyticsInfo.VersionInfo.DeviceFamily)
        {
            case "Windows.Mobile":
                return DeviceFormFactorType.Phone;
            case "Windows.Desktop":
                return DeviceFormFactorType.Desktop;
                //return UIViewSettings.GetForCurrentView().UserInteractionMode == UserInteractionMode.Mouse
                //    ? DeviceFormFactorType.Desktop
                //    : DeviceFormFactorType.Tablet;
            case "Windows.Universal":
                return DeviceFormFactorType.IoT;
            case "Windows.Team":
                return DeviceFormFactorType.SurfaceHub;
            case "Windows.Holographic":
                return DeviceFormFactorType.HoloLens;
            default:
                return DeviceFormFactorType.Other;
        }
    }
}

public enum DeviceFormFactorType
{
    Phone,
    Desktop,
    Tablet,
    IoT,
    SurfaceHub,
    Other,
    HoloLens
}
#endif