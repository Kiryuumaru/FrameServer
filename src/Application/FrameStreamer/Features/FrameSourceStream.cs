using Application.Common.Extensions;
using Application.Common.Features;
using DisposableHelpers.Attributes;
using Domain.Models;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Application.FrameStreamer.Features;

[Disposable]
public partial class FrameSourceStream
{
    private readonly Locker _locker = new();

    private Mat? _frameCallbackMat = null;

    private Func<CancellationToken, Task>? _frameCallback = null;

    public FrameSourceConfig? FrameSourceConfig { get; private set; } = null;

    public VideoCapture VideoCapture { get; set; } = new();

    public GateKeeper OpenGate { get; set; } = new();

    public GateKeeper StartGate { get; set; } = new();

    public void SetFrameCallback(Mat frame, Func<CancellationToken, Task> callback)
    {
        _frameCallbackMat = frame;
        _frameCallback = callback;
    }

    public Task Open(FrameSourceConfig frameSourceConfig, CancellationToken cancellationToken)
    {
        OpenGate.SetClosed();

        FrameSourceConfig = frameSourceConfig;

        CancellationToken ct = CancelWhenDisposing(cancellationToken);

        string source = frameSourceConfig.Source;
        VideoCaptureAPIs videoCaptureAPIs = VideoCaptureAPIs.ANY;
        if (!string.IsNullOrEmpty(frameSourceConfig.VideoApi))
        {
            videoCaptureAPIs = OpenCVExtensions.StringToVideoCaptureApi(frameSourceConfig.VideoApi);
        }

        return ThreadHelpers.WaitThread(async () =>
        {
            using var _ = await _locker.WaitAsync(ct);

            if (!VideoCapture.IsDisposed)
            {
                InvokerUtils.RunAndForget(VideoCapture.Release);
                InvokerUtils.RunAndForget(VideoCapture.Dispose);
                VideoCapture = new();
            }

            var openToken = ct.Register(() =>
            {
                InvokerUtils.RunAndForget(VideoCapture.Release);
                InvokerUtils.RunAndForget(VideoCapture.Dispose);
            });

            try
            {
                if (VideoCapture.IsDisposed || ct.IsCancellationRequested)
                {
                    OpenGate.SetClosed();
                    return;
                }

                VideoCapture.SetExceptionMode(true);

                bool isOpen = false;

                await ThreadHelpers.WaitThread(() =>
                {
                    try
                    {
                        if (int.TryParse(source, out int cameraSource))
                        {
                            isOpen = VideoCapture.Open(cameraSource, videoCaptureAPIs);
                        }
                        else
                        {
                            isOpen = VideoCapture.Open(source, videoCaptureAPIs);
                        }

                        if (FrameSourceConfig.Width != null)
                        {
                            VideoCapture.Set(VideoCaptureProperties.FrameWidth, FrameSourceConfig.Width.Value);
                        }
                        if (FrameSourceConfig.Height != null)
                        {
                            VideoCapture.Set(VideoCaptureProperties.FrameHeight, FrameSourceConfig.Height.Value);
                        }
                    }
                    catch { }
                }, ct);

                OpenGate.SetOpen(isOpen);

                if (!isOpen)
                {
                    throw new Exception($"Capture returned false");
                }
            }
            catch
            {
                InvokerUtils.RunAndForget(VideoCapture.Release);
                InvokerUtils.RunAndForget(VideoCapture.Dispose);
                VideoCapture = new();

                OpenGate.SetClosed();

                throw;
            }
            finally
            {
                openToken.Unregister();
            }

        }, ct);
    }

    public Task Start(CancellationToken cancellationToken)
    {
        if (FrameSourceConfig == null)
        {
            throw new Exception("Frame source is not open");
        }

        StartGate.SetOpen();

        CancellationToken ct = CancelWhenDisposing(cancellationToken);

        return ThreadHelpers.WaitThread(async () =>
        {
            Mat rawFrame = new();
            Mat resizeFrame = new();
            Mat finalFrame = new();

            bool isRunning() =>
                FrameSourceConfig != null &&
                !ct.IsCancellationRequested &&
                !VideoCapture.IsDisposed &&
                VideoCapture.IsOpened();

            void procFrame()
            {
                if (FrameSourceConfig.Width == null && FrameSourceConfig.Height == null)
                {
                    if (_frameCallbackMat.Size() != rawFrame.Size() || _frameCallbackMat.Type() != rawFrame.Type())
                    {
                        _frameCallbackMat.Create(rawFrame.Size(), rawFrame.Type());
                    }

                    rawFrame.CopyTo(_frameCallbackMat);

                    return;
                }

                int origWidth = rawFrame.Width;
                int origHeight = rawFrame.Height;
                double aspectRatioSrc = (double)origWidth / origHeight;

                int targetWidth;
                if (FrameSourceConfig.Width == null && FrameSourceConfig.Height != null)
                {
                    targetWidth = (int)(FrameSourceConfig.Height.Value * aspectRatioSrc);
                }
                else if (FrameSourceConfig.Width != null)
                {
                    targetWidth = FrameSourceConfig.Width.Value;
                }
                else
                {
                    throw new Exception("Something went wrong");
                }

                int targetHeight;
                if (FrameSourceConfig.Height == null && FrameSourceConfig.Width != null)
                {
                    targetHeight = (int)(FrameSourceConfig.Width.Value / aspectRatioSrc);
                }
                else if (FrameSourceConfig.Height != null)
                {
                    targetHeight = FrameSourceConfig.Height.Value;
                }
                else
                {
                    throw new Exception("Something went wrong");
                }

                double aspectRatioDst = (double)targetWidth / targetHeight;

                Rect cropRect;

                if (aspectRatioSrc > aspectRatioDst)
                {
                    int newHeight = targetHeight;
                    int newWidth = (int)(targetHeight * aspectRatioSrc);
                    Cv2.Resize(rawFrame, resizeFrame, new Size(newWidth, newHeight));

                    int xOffset = (newWidth - targetWidth) / 2;
                    cropRect = new Rect(xOffset, 0, targetWidth, targetHeight);
                }
                else
                {
                    int newWidth = targetWidth;
                    int newHeight = (int)(targetWidth / aspectRatioSrc);
                    Cv2.Resize(rawFrame, resizeFrame, new Size(newWidth, newHeight));

                    int yOffset = (newHeight - targetHeight) / 2;
                    cropRect = new Rect(0, yOffset, targetWidth, targetHeight);
                }

                finalFrame = new Mat(resizeFrame, cropRect);

                finalFrame.CopyTo(_frameCallbackMat);
            }

            try
            {
                while (isRunning())
                {
                    using var _ = await _locker.WaitAsync(ct);

                    if (!isRunning())
                    {
                        break;
                    }

                    bool hasRead = false;
                    try
                    {
                        hasRead = VideoCapture.Read(rawFrame);
                    }
                    catch
                    {
                        if (!isRunning())
                        {
                            break;
                        }
                    }
                    if (hasRead && !rawFrame.Empty() && _frameCallback != null && _frameCallbackMat != null)
                    {
                        procFrame();

                        await _frameCallback(ct);
                    }

                    Cv2.WaitKey(1);
                }

                Console.WriteLine($"Stopped1 {FrameSourceConfig.Source}");
            }
            finally
            {
                InvokerUtils.RunAndForget(rawFrame.Release);
                InvokerUtils.RunAndForget(resizeFrame.Release);
                InvokerUtils.RunAndForget(finalFrame.Release);
                InvokerUtils.RunAndForget(rawFrame.Dispose);
                InvokerUtils.RunAndForget(resizeFrame.Dispose);
                InvokerUtils.RunAndForget(finalFrame.Dispose);

                Console.WriteLine($"Stopped2 {FrameSourceConfig.Source}");

                StartGate.SetClosed();
            }

        }, ct);
    }

    protected void Dispose(bool disposing)
    {
        if (disposing)
        {
            Task.Run(async () =>
            {
                Console.WriteLine($"Disposee1 {FrameSourceConfig?.Source}");
                using var _ = await _locker.WaitAsync(default);
                Console.WriteLine($"Disposee2 {FrameSourceConfig?.Source}");
                InvokerUtils.RunAndForget(VideoCapture.Release);
                Console.WriteLine($"Disposee3 {FrameSourceConfig?.Source}");
                InvokerUtils.RunAndForget(VideoCapture.Dispose);
                Console.WriteLine($"Disposee4 {FrameSourceConfig?.Source}");
                OpenGate.SetClosed();
            });
        }
    }
}
