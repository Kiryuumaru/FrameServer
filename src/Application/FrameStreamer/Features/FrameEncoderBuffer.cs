//using Application.Common.Features;
//using DisposableHelpers.Attributes;
//using OpenCvSharp;
//using OpenCvSharp.Flann;
//using System;
//using System.Collections;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace Application.FrameStreamer.Features;

//[Disposable]
//public partial class FrameEncoderBufferCruncher
//{
//    public Locker Locker { get; private set; } = new();

//    public Mat Frame { get; private set; } = new();

//    public GateKeeper WorkloadAvailableGate { get; private set; } = new(true);

//    public byte[] Crunched { get; private set; } = [];

//    public FrameEncoderBufferCruncher Last { get; private set; }

//    public FrameEncoderBufferCruncher Next { get; private set; }

//    private readonly CancellationToken ct;

//    private readonly FrameEncoderBuffer _frameEncoderBuffer;

//    private readonly Thread _worker;

//    public FrameEncoderBufferCruncher(
//        FrameEncoderBuffer frameEncoderBuffer,
//        FrameEncoderBufferCruncher last,
//        FrameEncoderBufferCruncher next)
//    {
//        ct = CancelWhenDisposing();
//        _frameEncoderBuffer = frameEncoderBuffer;
//        Last = last;
//        Next = next;
//        _worker = new(async () =>
//        {
//            while (!IsDisposedOrDisposing)
//            {
//                await WorkloadAvailableGate.WaitForOpen(ct);
//                WorkloadAvailableGate.SetClosed();
//                if (IsDisposedOrDisposing)
//                {
//                    break;
//                }
//                using var workerLocker = await Locker.WaitAsync(ct);
//                gateKeeper.SetClosed();
//                var bytes = Frame.ToBytes(ext: ".png");
//                await last.AvailableGate.WaitForOpen(ct);
//                using var generalLocker = await _locker.WaitAsync(ct);
//                _frameEncoderBuffer.Queue.Enqueue(bytes);
//                _frameEncoderBuffer.NextCruncher = Next;
//            }
//        });
//    }

//    public
//}

//[Disposable]
//public partial class FrameEncoderBuffer
//{
//    private readonly Locker _locker = new();
//    private readonly GateKeeper _readAvailable = new(false);
//    private readonly GateKeeper _writeAvailable = new(true);
//    private readonly Mat[] _frames;
//    private readonly GateKeeper[] _availableGates;
//    private readonly GateKeeper[] _gates;
//    private readonly Locker[] _lockers;
//    private readonly Thread[] _workers;
//    private readonly int _frameCount;
//    private readonly CancellationToken ct;

//    internal readonly Queue<byte[]> Queue = new();

//    internal FrameEncoderBufferCruncher NextCruncher;

//    public FrameEncoderBuffer(int frameCount = 100)
//    {
//        ct = CancelWhenDisposing();

//        _frameCount = frameCount;
//        _frames = new Mat[_frameCount];
//        _gates = new GateKeeper[_frameCount];
//        _lockers = new Locker[_frameCount];
//        _workers = new Thread[_frameCount];
//        for (int i = 0; i < _frameCount; i++)
//        {
//            GateKeeper availableGate;
//            GateKeeper lastGateKeeper;

//            if (i == 0)
//            {
//                gateKeeper = new(true);
//                lastGateKeeper = new(true);
//                _gates[i] = gateKeeper;
//                _gates[_frameCount - 1] = lastGateKeeper;
//            }
//            else
//            {
//                gateKeeper = new(true);
//                lastGateKeeper = _gates[i - 1];
//                _gates[i] = gateKeeper;
//            }

//            Mat frame = new();
//            Locker locker = new();
//            int index = i;
//            Thread worker = new(async () =>
//            {
//                while (!IsDisposedOrDisposing)
//                {
//                    await gateKeeper.WaitForOpen(ct);
//                    if (IsDisposedOrDisposing)
//                    {
//                        break;
//                    }
//                    using var workerLocker = await locker.WaitAsync(ct);
//                    gateKeeper.SetClosed();
//                    var bytes = frame.ToBytes(ext: ".png");
//                    await lastGateKeeper.WaitForOpen(ct);
//                    using var generalLocker = await _locker.WaitAsync(ct);
//                    _queue.Enqueue(bytes);
//                    _writeIndex = index;
//                }
//            });
//            _frames[i] = frame;
//            _lockers[i] = locker;
//            _workers[i] = worker;

//        }
//    }

//    public async Task Forward(Mat mat)
//    {
//        using var generalLocker = await _locker.WaitAsync(ct);
//        int toWriteIndex;
//        if (_writeIndex + 1 == _frameCount)
//        {
//            toWriteIndex = 0;
//        }
//        else
//        {
//            toWriteIndex = _writeIndex + 1;
//        }
//        using var workerLocker = await _lockers[toWriteIndex].WaitAsync(ct);
//    }

//    //public byte[] Next()
//    //{
        
//    //}
//}
