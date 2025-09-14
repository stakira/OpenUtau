# Inter-Process Communication (IPC) between OpenUtau and DAW Plugin

This document outlines the inter-process communication (IPC) mechanism used for integration between the OpenUtau main application and its DAW plugin.

## Overview

The communication between OpenUtau and the DAW plugin is established using **TCP sockets** over the loopback interface (`127.0.0.1`, localhost). All messages exchanged are serialized and deserialized using **JSON**.

## Key Components

The IPC mechanism is primarily managed by classes within the `OpenUtau.Core.DawIntegration` namespace.

## IPC Workflow Summary

1.  **DAW Plugin Initialization**: A DAW plugin, upon starting, acts as a TCP server and typically creates a `.json` file in the designated temporary directory (`OpenUtau/PluginServers`) containing its listening port and name.
2.  **OpenUtau Server Discovery**: OpenUtau's `DawServerFinder` periodically scans this temporary directory to discover available DAW plugin servers.
3.  **Connection Establishment**: When a DAW plugin is selected or detected, OpenUtau (via `DawManager` and `DawClient`) initiates a TCP connection to the DAW plugin's exposed port on `127.0.0.1`.
4.  **Initial Handshake**: OpenUtau sends an `init` request, and loads the current USTX project data from the DAW plugin.
5.  **Continuous Synchronization**:
    - OpenUtau continuously monitors changes in its project (USTX, tracks, parts).
    - Using debounced notifications (`updateUstx`, `updateTracks`), OpenUtau pushes relevant updates to the DAW plugin.
    - For audio synchronization, OpenUtau sends `updatePartLayout` with hashes of audio parts. The DAW plugin responds with hashes of any missing audio.
    - OpenUtau then renders and sends the missing audio data via `UpdateAudioNotification` (compressed and encoded).
6.  **DAW to OpenUtau Communication**: The DAW plugin can send notifications (`ping`) or requests back to OpenUtau, which are handled by registered listeners in `DawClient`.

## OpenUtau to DAW Plugin Messages

There are 4 main types of messages sent from OpenUtau to the DAW plugin:

- **OpenUtau to DAW Requests**: Sent by OpenUtau to the DAW plugin, expecting a response.
- **OpenUtau to DAW Notifications**: Sent by OpenUtau to the DAW plugin, not expecting a response.
- **DAW Plugin to OpenUtau Requests**: Sent by the DAW plugin to OpenUtau, expecting a response. Currently not used.
- **DAW Plugin to OpenUtau Notifications**: Sent by the DAW plugin to OpenUtau, not expecting a response.

The following messages are sent from OpenUtau to the DAW plugin:

- `init` (Request):
  - Initializes the connection between OpenUtau and the DAW plugin. Also retrives the current USTX project data saved in plugin.
  - **Request**: None
  - **Response**: `{ "ustx": <USTX data> }`
- `updateUstx` (Notification):
  - Notifies the DAW plugin about changes to the entire USTX project in OpenUtau.
  - **Request**: `{ "ustx": <USTX data> }`
  - **Response**: None
- `updateTracks` (Notification):
  - Notifies the DAW plugin about changes to track properties (e.g., name, volume, pan) in OpenUtau.
  - **Request**: `{ "tracks": [ { "name": <track name>, "volume": <volume>, "pan": <pan> } ] }`
  - **Response**: None
- `updatePartLayout` (Request):
  - Synchronizes part layout information (e.g., track number, start/end times, audio hashes) from OpenUtau to the DAW.
  - **Request**: `{ "parts": [ { "trackNo": <track number>, "startMs": <start time>, "endMs": <end time>, "audioHash": <audio hash> } ] }`
  - **Response**: `{ "missingAudios": [ <hash1>, <hash2>, ... ] }`
- `updateAudio` (Notification):
  - Transmits missing audio data (prerenders) from OpenUtau to the DAW plugin.
  - **Request**: `{ "audios": { <audio hash>: <Base64 encoded and zstd compressed audio data> } }`
  - **Response**: None

The DAW plugin can send the following messages to OpenUtau:

- `ping` (Notification):
  - Checks if OpenUtau is responsive.
  - **Request**: None
  - **Response**: None
