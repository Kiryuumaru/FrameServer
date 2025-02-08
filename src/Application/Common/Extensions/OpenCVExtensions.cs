using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Common.Extensions;

public static class OpenCVExtensions
{
    public static VideoCaptureAPIs StringToVideoCaptureApi(string api)
    {
        api = api.ToUpperInvariant();
        return api.ToUpper() switch
        {
            "ANY" => VideoCaptureAPIs.ANY,
            "V4L" => VideoCaptureAPIs.V4L,
            "V4L2" => VideoCaptureAPIs.V4L2,
            "FIREWIRE" => VideoCaptureAPIs.FIREWIRE,
            "FIREWARE" => VideoCaptureAPIs.FIREWARE,
            "IEEE1394" => VideoCaptureAPIs.IEEE1394,
            "DC1394" => VideoCaptureAPIs.DC1394,
            "CMU1394" => VideoCaptureAPIs.CMU1394,
            "DSHOW" => VideoCaptureAPIs.DSHOW,
            "PVAPI" => VideoCaptureAPIs.PVAPI,
            "OPENNI" => VideoCaptureAPIs.OPENNI,
            "OPENNI_ASUS" => VideoCaptureAPIs.OPENNI_ASUS,
            "ANDROID" => VideoCaptureAPIs.ANDROID,
            "XIAPI" => VideoCaptureAPIs.XIAPI,
            "AVFOUNDATION" => VideoCaptureAPIs.AVFOUNDATION,
            "GIGANETIX" => VideoCaptureAPIs.GIGANETIX,
            "MSMF" => VideoCaptureAPIs.MSMF,
            "WINRT" => VideoCaptureAPIs.WINRT,
            "INTELPERC" => VideoCaptureAPIs.INTELPERC,
            "REALSENSE" => VideoCaptureAPIs.REALSENSE,
            "OPENNI2" => VideoCaptureAPIs.OPENNI2,
            "OPENNI2_ASUS" => VideoCaptureAPIs.OPENNI2_ASUS,
            "GPHOTO2" => VideoCaptureAPIs.GPHOTO2,
            "GSTREAMER" => VideoCaptureAPIs.GSTREAMER,
            "FFMPEG" => VideoCaptureAPIs.FFMPEG,
            "IMAGES" => VideoCaptureAPIs.IMAGES,
            "ARAVIS" => VideoCaptureAPIs.ARAVIS,
            "OPENCV_MJPEG" => VideoCaptureAPIs.OPENCV_MJPEG,
            "INTEL_MFX" => VideoCaptureAPIs.INTEL_MFX,
            "XINE" => VideoCaptureAPIs.XINE,
            "CAP_UEYE" => VideoCaptureAPIs.CAP_UEYE,
            _ => throw new NotSupportedException($"VideoCaptureAPI not supported '{api}'"),
        };
    }
}
