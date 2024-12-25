# WebRTC Showcase with SignalR Backend

This project is a **WebRTC Showcase** that demonstrates real-time communication using a **SignalR backend**. The SignalR backend serves as a signaling server to manage clients and facilitate WebRTC connections. The client is implemented as a .NET 8 iOS mobile app.

<div style="display: flex; gap: 10px;">
  <img src="https://github.com/ValonK/WebRTC.Demo/blob/main/assets/IMG_0369.PNG?raw=true" width="230">
  <img src="https://github.com/ValonK/WebRTC.Demo/blob/main/assets/IMG_0370.PNG?raw=true" width="230">
  <img src="https://github.com/ValonK/WebRTC.Demo/blob/main/assets/IMG_0371.PNG?raw=true" width="230">
  <img src="https://github.com/ValonK/WebRTC.Demo/blob/main/assets/IMG_0372.PNG?raw=true" width="230">
</div>

## Features

-   **WebRTC Integration**: Real-time audio/video streaming between peers.
-   **SignalR Backend**: Manages signaling, connection establishment, and client communication.
-   **iOS Client**: A .NET 8 iOS mobile application.
-   **WebRTC iOS Bindings**: The client integrates WebRTC using custom bindings generated from the [WebRTC SDK](https://github.com/webrtc-sdk).

## Architecture Overview

1.  **Backend**: Built with ASP.NET Core 9 and SignalR, it manages signaling, client registration, and communication.
2.  **Client**: A .NET 8 iOS mobile application handles WebRTC peer connections and streams audio/video.

## Prerequisites

### Backend

-   [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
### Client

-   [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
