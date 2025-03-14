# libsoundio Package for Unity

![gif](https://i.imgur.com/lxWgeaA.gif)

This repository contains a Unity package that provides a C# wrapper for [libsoundio].

[libsoundio]: https://github.com/andrewrk/libsoundio

Currently, only the audio input features are implemented, as the primary focus
of this project is to provide low-latency audio input for Unity.

## System Requirements

- Unity 2019.4 or later
- 64-bit desktop platforms (Windows, macOS, Linux)

## Installation

You can install the libsoundio package (`jp.keijiro.libsoundio`) via the "Keijiro"
scoped registry using the Unity Package Manager. To add the registry to your project,
please follow [these instructions].

[these instructions]:
  https://gist.github.com/keijiro/f8c7e8ff29bfe63d86b888901b82644c

## Related Repository

I have made some modifications to libsoundio to address issues encountered during
its integration with Unity. For more details, please refer to the following fork:

https://github.com/keijiro/libsoundio
