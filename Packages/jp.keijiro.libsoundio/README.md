jp.keijiro.libsoundio
=====================

![gif](https://i.imgur.com/lxWgeaA.gif)

This is a C# wrapper class library of [libsoundio] that is specialized for the
[Unity] runtime environment.

[libsoundio]: https://github.com/andrewrk/libsoundio
[Unity]: https://unity3d.com

At the moment, only the audio input features are implemented and tested because
the main aim of this project is providing low-latency audio input functionality
to Unity.

libsoundio binaries contained in this repository are slightly different from
the official ones. See the following fork for details:

https://github.com/keijiro/libsoundio

System Requirements
-------------------

- Unity 2019.4 or later
- Intel 64-bit desktop platforms (Windows, macOS, Linux)

On Linux, ALSA (libasound2) must be installed on the system.

How To Install
--------------

This package uses the [scoped registry] feature to resolve package
dependencies. Please add the following sections to the manifest file
(Packages/manifest.json).

[scoped registry]: https://docs.unity3d.com/Manual/upm-scoped.html

To the `scopedRegistries` section:

```
{
  "name": "Unity NuGet",
  "url": "https://unitynuget-registry.azurewebsites.net",
  "scopes": [ "org.nuget" ]
},
{
  "name": "Keijiro",
  "url": "https://registry.npmjs.com",
  "scopes": [ "jp.keijiro" ]
}
```

To the `dependencies` section:

```
"jp.keijiro.libsoundio": "1.0.2"
```

After changes, the manifest file should look like below:

```
{
  "scopedRegistries": [
    {
      "name": "Unity NuGet",
      "url": "https://unitynuget-registry.azurewebsites.net",
      "scopes": [ "org.nuget" ]
    },
    {
      "name": "Keijiro",
      "url": "https://registry.npmjs.com",
      "scopes": [ "jp.keijiro" ]
    }
  ],
  "dependencies": {
    "jp.keijiro.libsoundio": "1.0.2",
    ...
```
