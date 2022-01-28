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
dependencies. Please add the following lines to the manifest file
(`Packages/manifest.json`).

[scoped registry]: https://docs.unity3d.com/Manual/upm-scoped.html

<details>
<summary>.NET Standard 2.0 (Unity 2021.1 or earlier)</summary>

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
"org.nuget.system.memory": "4.5.3",
"jp.keijiro.libsoundio": "1.0.4"
```

After the changes, the manifest file should look like:

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
    "org.nuget.system.memory": "4.5.3",
    "jp.keijiro.libsoundio": "1.0.4",
    ...
```
</details>

<details>
<summary>.NET Standard 2.1 (Unity 2021.2 or later)</summary>

To the `scopedRegistries` section:

```
{
  "name": "Keijiro",
  "url": "https://registry.npmjs.com",
  "scopes": [ "jp.keijiro" ]
}
```

To the `dependencies` section:

```
"jp.keijiro.libsoundio": "1.0.4"
```

After the changes, the manifest file should look like:

```
{
  "scopedRegistries": [
    {
      "name": "Keijiro",
      "url": "https://registry.npmjs.com",
      "scopes": [ "jp.keijiro" ]
    }
  ],
  "dependencies": {
    "jp.keijiro.libsoundio": "1.0.4",
    ...
```
</details>
