using AVFoundation;
using CoreFoundation;
using CoreMedia;
using WebRTC.Bindings.iOS;
using static RTCDemo.iOS.AppDelegate;

namespace RTCDemo.iOS.Services.RTC;

public class RtcService : NSObject,
    IRtcService,
    IRTCPeerConnectionDelegate,
    IRTCVideoViewDelegate,
    IRTCDataChannelDelegate
{
    private readonly RTCPeerConnectionFactory _peerConnectionFactory;
    private RTCCameraVideoCapturer _videoCapturer;
    private RTCVideoTrack _localVideoTrack;
    private RTCAudioTrack _localAudioTrack;
    private RTCMediaStream _remoteStream;
    private readonly RTCEAGLVideoView _localRenderView;
    private readonly RTCEAGLVideoView _remoteRenderView;
    private readonly object _connectionLock = new();
    private RTCPeerConnection _peerConnection;
    private RTCDataChannel _dataChannel;
    private bool _isConnected;
    private AVCaptureDevice _currentCaptureDevice;

    public bool IsConnected => _isConnected;
    public event EventHandler<NSObject> DataReceived;
    public event EventHandler<string> MessageReceived;
    public event EventHandler<bool> RtcConnectionChanged;
    public event EventHandler<RTCIceConnectionState> IceConnectionChanged;
    public event EventHandler<RTCIceCandidate> IceCandidateGenerated;
    public event EventHandler DataChannelOpened;

    public UIView LocalVideoView => _localRenderView;
    public UIView RemoteVideoView => _remoteRenderView;

    public RtcService()
    {
        var videoEncoderFactory = new RTCDefaultVideoEncoderFactory();
        var videoDecoderFactory = new RTCDefaultVideoDecoderFactory();
        _peerConnectionFactory = new RTCPeerConnectionFactory(videoEncoderFactory, videoDecoderFactory);

        _localRenderView = new RTCEAGLVideoView();
        _localRenderView.Delegate = this;
        _localRenderView.Transform = CGAffineTransform.MakeScale(-1, 1);

        _remoteRenderView = new RTCEAGLVideoView();
        _remoteRenderView.Delegate = this;
        _remoteRenderView.Transform = CGAffineTransform.MakeScale(-1, 1);

        _remoteRenderView.ContentMode = UIViewContentMode.ScaleAspectFit;
        _localRenderView.ContentMode = UIViewContentMode.ScaleAspectFit;
    }

    private (RTCPeerConnection peer, RTCDataChannel data) Connection
    {
        get
        {
            if (_peerConnection != null) return (_peerConnection, _dataChannel);
            lock (_connectionLock)
            {
                _peerConnection ??= SetupPeerConnection();
            }

            return (_peerConnection, _dataChannel);
        }
    }

    private RTCPeerConnection SetupPeerConnection()
    {
        var rtcConfig = new RTCConfiguration
        {
            IceServers = [new RTCIceServer(["stun:stun.l.google.com:19302"])]
        };
        var mediaConstraints = new RTCMediaConstraints(null, null);
        var pc = _peerConnectionFactory.PeerConnectionWithConfiguration(rtcConfig, mediaConstraints, this);

        pc.AddTrack(_localVideoTrack, ["stream0"]);
        pc.AddTrack(_localAudioTrack, ["stream0"]);

        return pc;
    }

    public void SetupMediaTracks()
    {
        _localVideoTrack = CreateVideoTrack();

        StartCaptureLocalVideo(AVCaptureDevicePosition.Front, 640, Convert.ToInt32(640 * 16 / 9f), 30);
        _localVideoTrack.AddRenderer(_localRenderView);

        _localAudioTrack = CreateAudioTrack();
    }

    private RTCVideoTrack CreateVideoTrack()
    {
        var videoSource = _peerConnectionFactory.VideoSource;
        _videoCapturer = new RTCCameraVideoCapturer();
        _videoCapturer.Delegate = videoSource;
        var videoTrack = _peerConnectionFactory.VideoTrackWithSource(videoSource, "video0");
        return videoTrack;
    }

    private RTCAudioTrack CreateAudioTrack()
    {
        var audioConstraints = new RTCMediaConstraints(null, null);
        var audioSource = _peerConnectionFactory.AudioSourceWithConstraints(audioConstraints);
        var audioTrack = _peerConnectionFactory.AudioTrackWithSource(audioSource, "audio0");

        return audioTrack;
    }

    public void Connect(Action<RTCSessionDescription, NSError> completionHandler)
    {
        _dataChannel = SetupDataChannel();
        _dataChannel.Delegate = this;

        MakeOffer(completionHandler);
    }

    private RTCDataChannel SetupDataChannel()
    {
        var dataChannelConfig = new RTCDataChannelConfiguration { ChannelId = 1 };

        var dc = Connection.peer.DataChannelForLabel("dataChannel", dataChannelConfig);
        if (dc == null)
        {
            Logger.Log("RTCDataChannel is null");
            return null;
        }

        dc.Delegate = this;
        return dc;
    }

    private void StartCaptureLocalVideo(AVCaptureDevicePosition position, int width, int? height, int fps)
    {
        var devices = RTCCameraVideoCapturer.CaptureDevices;
        var targetDevice = devices.FirstOrDefault(d => d.Position == position);

        if (targetDevice == null) return;

        var formats = RTCCameraVideoCapturer.SupportedFormatsForDevice(targetDevice);

        var targetFormat = formats.FirstOrDefault(f =>
        {
            var description = f.FormatDescription;
            if (description is not CMVideoFormatDescription videoDescription) return false;
            var dimensions = videoDescription.Dimensions;
            return (dimensions.Width == width && dimensions.Height == height) || dimensions.Width == width;
        });

        if (targetFormat != null)
        {
            _currentCaptureDevice = targetDevice; 
            _videoCapturer.StartCaptureWithDevice(targetDevice, targetFormat, fps);
        }
    }

    private void MakeOffer(Action<RTCSessionDescription, NSError> completionHandler)
    {
        var mediaConstraints = new RTCMediaConstraints(null, null);
        Connection.peer.OfferForConstraints(mediaConstraints, (sdp, err0) =>
        {
            if (err0 == null)
            {
                Connection.peer.SetLocalDescription(sdp, (err1) => { completionHandler(sdp, err1); });
            }
            else
            {
                completionHandler(null, err0);
            }
        });
    }

    private void MakeAnswer(Action<RTCSessionDescription, NSError> completionHandler)
    {
        var mediaConstraints = new RTCMediaConstraints(null, null);
        Connection.peer.AnswerForConstraints(mediaConstraints, (sdp, err0) =>
        {
            if (err0 == null)
            {
                Connection.peer.SetLocalDescription(sdp, (err1) => { completionHandler(sdp, err1); });
            }
            else
            {
                completionHandler(null, err0);
            }
        });
    }

    public void OfferReceived(RTCSessionDescription offerSdp, Action<RTCSessionDescription, NSError> completionHandler)
    {
        Connection.peer.SetRemoteDescription(offerSdp, (err) =>
        {
            if (err == null)
            {
                MakeAnswer(completionHandler);
            }
            else
            {
                completionHandler(offerSdp, err);
            }
        });
    }

    public void CandiateReceived(RTCIceCandidate candidate)
    {
        Connection.peer.AddIceCandidate(candidate);
    }

    public void AnswerReceived(RTCSessionDescription answerSdp,
        Action<RTCSessionDescription, NSError> completionHandler)
    {
        Connection.peer.SetRemoteDescription(answerSdp, err => { completionHandler(answerSdp, err); });
    }

    public void DataChannel(RTCDataChannel dataChannel, RTCDataBuffer buffer)
    {
        Logger.Log($"{nameof(DataChannel)}");

        DispatchQueue.MainQueue.DispatchAsync(() =>
        {
            if (buffer.IsBinary)
            {
                DataReceived?.Invoke(this, buffer.Data);
            }
            else
            {
                MessageReceived?.Invoke(this, new NSString(buffer.Data, NSStringEncoding.UTF8));
            }
        });
    }

    public void DataChannelDidChangeState(RTCDataChannel dataChannel)
    {
        Logger.Log($"{nameof(DataChannelDidChangeState)}");
    }

    private AVCaptureDeviceFormat _initialFrontCameraFormat;
    private int _initialFrontCameraFps = 30;

    public void SwitchCamera()
    {
        if (_videoCapturer == null || _currentCaptureDevice == null) return;

        var devices = RTCCameraVideoCapturer.CaptureDevices;

        var newDevice = devices.FirstOrDefault(d => d.Position != _currentCaptureDevice.Position);
        if (newDevice == null) return;

        var formats = RTCCameraVideoCapturer.SupportedFormatsForDevice(newDevice);
        var targetFormat = formats.FirstOrDefault(); 

        if (targetFormat != null)
        {
            _currentCaptureDevice = newDevice;
            _videoCapturer.StartCaptureWithDevice(newDevice, targetFormat, 30);
        }
    }


    public void DidChangeVideoSize(IRTCVideoRenderer videoView, CGSize size)
    {
        if (videoView is not RTCEAGLVideoView { Superview: { } parentView } rendererView)
        {
            Logger.Log("VideoView is not RTCVideoView or null");
            return;
        }

        // Remove existing constraints for clean slate
        var constraintsToRemove = parentView.Constraints
            .Where(c => c.FirstItem == rendererView &&
                        (c.FirstAttribute == NSLayoutAttribute.Width || c.FirstAttribute == NSLayoutAttribute.Height))
            .ToArray();
        parentView.RemoveConstraints(constraintsToRemove);

        // Calculate the aspect ratio
        var videoAspectRatio = size.Width / size.Height;
        var parentWidth = parentView.Bounds.Width;
        var parentHeight = parentView.Bounds.Height;

        NSLayoutConstraint widthConstraint;
        NSLayoutConstraint heightConstraint;

        if (videoAspectRatio > parentWidth / parentHeight)
        {
            widthConstraint = NSLayoutConstraint.Create(
                rendererView,
                NSLayoutAttribute.Width,
                NSLayoutRelation.Equal,
                parentView,
                NSLayoutAttribute.Width,
                1.0f,
                0);

            heightConstraint = NSLayoutConstraint.Create(
                rendererView,
                NSLayoutAttribute.Height,
                NSLayoutRelation.Equal,
                rendererView,
                NSLayoutAttribute.Width,
                1 / videoAspectRatio,
                0);
        }
        else
        {
            heightConstraint = NSLayoutConstraint.Create(
                rendererView,
                NSLayoutAttribute.Height,
                NSLayoutRelation.Equal,
                parentView,
                NSLayoutAttribute.Height,
                1.0f,
                0);

            widthConstraint = NSLayoutConstraint.Create(
                rendererView,
                NSLayoutAttribute.Width,
                NSLayoutRelation.Equal,
                rendererView,
                NSLayoutAttribute.Height,
                videoAspectRatio,
                0);
        }

        parentView.AddConstraints([widthConstraint, heightConstraint]);
    }

    public void PeerConnectionDidChangeSignalingState(RTCPeerConnection peerConnection,
        RTCSignalingState stateChanged)
    {
        Logger.Log($"{nameof(RTCSignalingState)} changed {stateChanged}");
    }

    public void PeerConnectionDidChangeIceConnectionState(RTCPeerConnection peerConnection,
        RTCIceConnectionState newState)
    {
        System.Diagnostics.Debug.WriteLine($"{nameof(RTCIceConnectionState)} changed {newState}");

        switch (newState)
        {
            case RTCIceConnectionState.Connected:
            case RTCIceConnectionState.Completed:
                if (!_isConnected)
                {
                    _isConnected = true;
                    DispatchQueue.MainQueue.DispatchAsync(() =>
                    {
                        _remoteRenderView.Hidden = false;
                        RtcConnectionChanged?.Invoke(this, true);
                    });
                }

                break;
            default:
                if (_isConnected)
                {
                    _isConnected = false;
                    DispatchQueue.MainQueue.DispatchAsync(() =>
                    {
                        _remoteRenderView.Hidden = true;
                        Disconnect();
                        RtcConnectionChanged?.Invoke(this, false);
                    });
                }

                break;
        }

        DispatchQueue.MainQueue.DispatchAsync(() => { IceConnectionChanged?.Invoke(this, newState); });
    }

    public void PeerConnectionDidChangeIceGatheringState(RTCPeerConnection peerConnection,
        RTCIceGatheringState newState)
    {
        Logger.Log($"{nameof(RTCIceGatheringState)} changed {newState}");
    }

    public void PeerConnectionDidGenerateIceCandidate(RTCPeerConnection peerConnection,
        RTCIceCandidate candidate)
    {
        Logger.Log(nameof(PeerConnectionDidGenerateIceCandidate));
        IceCandidateGenerated?.Invoke(this, candidate);
    }

    public void PeerConnectionDidRemoveIceCandidates(RTCPeerConnection peerConnection,
        RTCIceCandidate[] candidates)
    {
        Logger.Log(nameof(PeerConnectionDidRemoveIceCandidates));
    }

    public void PeerConnectionDidOpenDataChannel(RTCPeerConnection peerConnection, RTCDataChannel dataChannel)
    {
        Logger.Log(nameof(PeerConnectionDidOpenDataChannel));
        DataChannelOpened?.Invoke(this, EventArgs.Empty);

        _dataChannel?.Close();
        _dataChannel?.Dispose();
        _dataChannel = null;

        _dataChannel = dataChannel;
        dataChannel.Delegate = this;
    }

    public void PeerConnectionDidAddStream(RTCPeerConnection peerConnection, RTCMediaStream stream)
    {
        Logger.Log($"{nameof(PeerConnectionDidAddStream)}");

        _remoteStream = stream;

        if (_remoteStream.VideoTracks.FirstOrDefault() is { } rtcVideoTrack)
            rtcVideoTrack.AddRenderer(_remoteRenderView);
        if (_remoteStream.AudioTracks.FirstOrDefault() is { } audioTrack) audioTrack.Source.Volume = 10;
    }

    public void PeerConnectionDidRemoveStream(RTCPeerConnection peerConnection, RTCMediaStream stream)
    {
        Logger.Log($"{nameof(PeerConnectionDidRemoveStream)}");
    }

    public void PeerConnectionShouldNegotiate(RTCPeerConnection peerConnection)
    {
        Logger.Log($"{nameof(PeerConnectionShouldNegotiate)}");
    }

    public void Disconnect()
    {
        if (_peerConnection == null) return;

        lock (_connectionLock)
        {
            if (_peerConnection == null) return;

            _dataChannel?.Close();
            _peerConnection?.Close();

            _dataChannel?.Dispose();
            _peerConnection?.Dispose();

            _dataChannel = null;
            _dataChannel = null;
            _peerConnection = null;
        }
    }
}