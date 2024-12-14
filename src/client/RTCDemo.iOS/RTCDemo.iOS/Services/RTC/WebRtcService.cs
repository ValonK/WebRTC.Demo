using AVFoundation;
using CoreFoundation;
using CoreMedia;
using WebRTC.Bindings.iOS;

namespace RTCDemo.iOS.Services.RTC
{
    public class WebRtcService : NSObject, IRTCPeerConnectionDelegate, IRTCVideoViewDelegate, IRTCDataChannelDelegate
    {
        private RTCPeerConnectionFactory _peerConnectionFactory;
        private RTCCameraVideoCapturer _videoCapturer;
        private RTCVideoTrack _localVideoTrack;
        private RTCAudioTrack _localAudioTrack;
        private RTCMediaStream _remoteStream;
        private RTCEAGLVideoView _localRenderView;
        private RTCEAGLVideoView _remoteRenderView;
        private readonly object _connectionLock = new object();
        private RTCPeerConnection _peerConnection;
        private RTCDataChannel _dataChannel;
        private bool _isConnected;

        public bool IsConnected => _isConnected;
        public IWebRtcService Delegate { get; private set; }

        // Expose the video render views as public properties
        public UIView LocalVideoView => _localRenderView;
        public UIView RemoteVideoView => _remoteRenderView;

        private (RTCPeerConnection peer, RTCDataChannel data) _connection
        {
            get
            {
                if (_peerConnection != null) return (_peerConnection, _dataChannel);
                lock (_connectionLock)
                {
                    if (_peerConnection == null) _peerConnection = SetupPeerConnection();
                }

                return (_peerConnection, _dataChannel);
            }
        }

        public WebRtcService(IWebRtcService @delegate)
        {
            Delegate = @delegate;

            var videoEncoderFactory = new RTCDefaultVideoEncoderFactory();
            var videoDecoderFactory = new RTCDefaultVideoDecoderFactory();
            _peerConnectionFactory = new RTCPeerConnectionFactory(videoEncoderFactory, videoDecoderFactory);

            _localRenderView = new RTCEAGLVideoView();
            _localRenderView.Delegate = this;

            _remoteRenderView = new RTCEAGLVideoView();
            _remoteRenderView.Delegate = this;
        }

        public void SetupMediaTracks()
        {
            _localVideoTrack = CreateVideoTrack();
            StartCaptureLocalVideo(AVCaptureDevicePosition.Front, 640, Convert.ToInt32(640 * 16 / 9f), 30);
            _localVideoTrack.AddRenderer(_localRenderView);

            _localAudioTrack = CreateAudioTrack();
        }

        public void StartCall(Action<RTCSessionDescription, NSError> completionHandler)
        {
            // Setup data channel
            _dataChannel = SetupDataChannel();
            _dataChannel.Delegate = this;

            // Create Offer
            MakeOffer(completionHandler);
        }

        public void ReceiveOffer(RTCSessionDescription offerSdp, Action<RTCSessionDescription, NSError> completionHandler)
        {
            _connection.peer.SetRemoteDescription(offerSdp, err =>
            {
                if (err == null)
                {
                    MakeAnswer(completionHandler);
                }
                else
                {
                    completionHandler(null, err);
                }
            });
        }

        public void ReceiveAnswer(RTCSessionDescription answerSdp, Action<RTCSessionDescription, NSError> completionHandler)
        {
            _connection.peer.SetRemoteDescription(answerSdp, (err) => { completionHandler(answerSdp, err); });
        }

        public void ReceiveCandidate(RTCIceCandidate candidate)
        {
            _connection.peer.AddIceCandidate(candidate);
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
                _peerConnection = null;
            }
        }

        public bool SendMessage(string message)
        {
            if (_connection.data != null && _connection.data.ReadyState == RTCDataChannelState.Open)
            {
                var buffer = new RTCDataBuffer(NSData.FromString(message, NSStringEncoding.UTF8), false);
                return _connection.data.SendData(buffer);
            }

            return false;
        }

        #region Private Helpers

        private RTCPeerConnection SetupPeerConnection()
        {
            var rtcConfig = new RTCConfiguration
            {
                IceServers = new[]
                {
                    new RTCIceServer(new[] { "stun:stun.l.google.com:19302" })
                }
            };
            var mediaConstraints = new RTCMediaConstraints(null, null);
            var pc = _peerConnectionFactory.PeerConnectionWithConfiguration(rtcConfig, mediaConstraints, this);

            pc.AddTrack(_localVideoTrack, new[] { "stream0" });
            pc.AddTrack(_localAudioTrack, new[] { "stream0" });

            return pc;
        }

        private RTCDataChannel SetupDataChannel()
        {
            var dataChannelConfig = new RTCDataChannelConfiguration
            {
                ChannelId = 1
            };

            var dc = _connection.peer.DataChannelForLabel("dataChannel", dataChannelConfig);
            dc.Delegate = this;
            return dc;
        }

        private RTCAudioTrack CreateAudioTrack()
        {
            var audioConstraints = new RTCMediaConstraints(null, null);
            var audioSource = _peerConnectionFactory.AudioSourceWithConstraints(audioConstraints);
            var audioTrack = _peerConnectionFactory.AudioTrackWithSource(audioSource, "audio0");

            return audioTrack;
        }

        private RTCVideoTrack CreateVideoTrack()
        {
            var videoSource = _peerConnectionFactory.VideoSource;
            _videoCapturer = new RTCCameraVideoCapturer();
            _videoCapturer.Delegate = videoSource;
            var videoTrack = _peerConnectionFactory.VideoTrackWithSource(videoSource, "video0");
            return videoTrack;
        }

        private void MakeOffer(Action<RTCSessionDescription, NSError> completionHandler)
        {
            var mediaConstraints = new RTCMediaConstraints(null, null);
            _connection.peer.OfferForConstraints(mediaConstraints, (sdp, err0) =>
            {
                if (err0 == null)
                {
                    _connection.peer.SetLocalDescription(sdp, (err1) =>
                    {
                        completionHandler(sdp, err1);
                    });
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
            _connection.peer.AnswerForConstraints(mediaConstraints, (sdp, err0) =>
            {
                if (err0 == null)
                {
                    _connection.peer.SetLocalDescription(sdp, (err1) =>
                    {
                        completionHandler(sdp, err1);
                    });
                }
                else
                {
                    completionHandler(null, err0);
                }
            });
        }

        private void StartCaptureLocalVideo(AVCaptureDevicePosition position, int width, int? height, int fps)
        {
            if (_videoCapturer is RTCCameraVideoCapturer cameraCapturer)
            {
                var devices = RTCCameraVideoCapturer.CaptureDevices;
                var targetDevice = devices.FirstOrDefault(d => d.Position == position);

                if (targetDevice != null)
                {
                    var formats = RTCCameraVideoCapturer.SupportedFormatsForDevice(targetDevice);

                    var targetFormat = formats.FirstOrDefault(f =>
                    {
                        var description = f.FormatDescription;
                        if (description is CMVideoFormatDescription videoDescription)
                        {
                            var dimensions = videoDescription.Dimensions;
                            if ((dimensions.Width == width && dimensions.Height == height) ||
                                (dimensions.Width == width))
                            {
                                return true;
                            }
                        }

                        return false;
                    });

                    if (targetFormat != null)
                    {
                        cameraCapturer.StartCaptureWithDevice(targetDevice, targetFormat, fps);
                    }
                }
            }
        }

        #endregion

        #region IRTCDataChannelDelegate

        public void DataChannel(RTCDataChannel dataChannel, RTCDataBuffer buffer)
        {
            DispatchQueue.MainQueue.DispatchAsync(() =>
            {
                if (buffer.IsBinary)
                {
                    Delegate?.DidReceiveData(buffer.Data);
                }
                else
                {
                    Delegate?.DidReceiveMessage(new NSString(buffer.Data, NSStringEncoding.UTF8));
                }
            });
        }

        public void DataChannelDidChangeState(RTCDataChannel dataChannel)
        {
            // Data channel state changed
        }

        public void DidReceiveMessageWithBuffer(RTCDataChannel dataChannel, RTCDataBuffer buffer)
        {
            // Not used in this implementation
        }

        #endregion

        #region IRTCVideoViewDelegate

        public void DidChangeVideoSize(IRTCVideoRenderer videoView, CGSize size)
        {
            // Handle video size changes if needed
        }

        #endregion

        #region IRTCPeerConnectionDelegate

        public void PeerConnectionDidChangeSignalingState(RTCPeerConnection peerConnection, RTCSignalingState stateChanged)
        {
            // Signaling state changed
        }

        public void PeerConnectionDidChangeIceConnectionState(RTCPeerConnection peerConnection, RTCIceConnectionState newState)
        {
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
                            Delegate?.DidConnectWebRtc();
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
                            Delegate?.DidDisconnectWebRtc();
                        });
                    }
                    break;
            }

            DispatchQueue.MainQueue.DispatchAsync(() => { Delegate?.DidIceConnectionStateChanged(newState); });
        }

        public void PeerConnectionDidChangeIceGatheringState(RTCPeerConnection peerConnection, RTCIceGatheringState newState)
        {
            // Gathering state changed
        }

        public void PeerConnectionDidGenerateIceCandidate(RTCPeerConnection peerConnection, RTCIceCandidate candidate)
        {
            // Notify delegate so it can send the candidate via SignalR
            Delegate?.DidGenerateCandiate(candidate);
        }

        public void PeerConnectionDidRemoveIceCandidates(RTCPeerConnection peerConnection, RTCIceCandidate[] candidates)
        {
            // ICE candidates removed
        }

        public void PeerConnectionDidOpenDataChannel(RTCPeerConnection peerConnection, RTCDataChannel dataChannel)
        {
            Delegate?.DidOpenDataChannel();

            _dataChannel?.Close();
            _dataChannel?.Dispose();
            _dataChannel = null;

            _dataChannel = dataChannel;
            dataChannel.Delegate = this;
        }

        public void PeerConnectionDidAddStream(RTCPeerConnection peerConnection, RTCMediaStream stream)
        {
            _remoteStream = stream;
            if (_remoteStream.VideoTracks.FirstOrDefault() is RTCVideoTrack vTrack)
            {
                vTrack.AddRenderer(_remoteRenderView);
            }

            if (_remoteStream.AudioTracks.FirstOrDefault() is RTCAudioTrack aTrack)
            {
                aTrack.Source.Volume = 10;
            }
        }

        public void PeerConnectionDidRemoveStream(RTCPeerConnection peerConnection, RTCMediaStream stream)
        {
            // Stream removed
        }

        public void PeerConnectionShouldNegotiate(RTCPeerConnection peerConnection)
        {
            // Renegotiation needed
        }

        #endregion

        public void DidChangeSignalingState(RTCPeerConnection peerConnection, RTCSignalingState stateChanged)
        {
            
        }

        public void DidAddStream(RTCPeerConnection peerConnection, RTCMediaStream stream)
        {
        }

        public void DidRemoveStream(RTCPeerConnection peerConnection, RTCMediaStream stream)
        {
        }

        public void ShouldNegotiate(RTCPeerConnection peerConnection)
        {
        }

        public void DidChangeIceConnectionState(RTCPeerConnection peerConnection, RTCIceConnectionState newState)
        {
        }

        public void DidChangeIceGatheringState(RTCPeerConnection peerConnection, RTCIceGatheringState newState)
        {
        }

        public void DidGenerateIceCandidate(RTCPeerConnection peerConnection, RTCIceCandidate candidate)
        {
        }

        public void DidRemoveIceCandidates(RTCPeerConnection peerConnection, RTCIceCandidate[] candidates)
        {
        }

        public void DidOpenDataChannel(RTCPeerConnection peerConnection, RTCDataChannel dataChannel)
        {
        }

        public void DidChangeConnectionState(RTCPeerConnection peerConnection, RTCPeerConnectionState newState)
        {
        }
    }
}
