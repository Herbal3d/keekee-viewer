# Introduction

## KeeKee Virtual World Viewer

The KeeKee Virtual World Viewer provides a modular framework for a client viewer of virtual world systems.
KeeKee is written in C# and licensed with the [Mozilla License].
The current version has a communication module that uses [LibreMetaverse] library
to talk Linden Lab Legacy Protocol ("LLLP") which is compatible
with [Second Life(r)] and [OpenSimulator].
A rendering module exists for OpenGL (OpenTK)
and [Godot] and [Unreal Engine] is planned in the future.
Other communication and rendering modules can be added.

The state of development is VERY alpha.
The sources came from my old [LookingGlass Viewer] project (back in 2010)
when I tried to build a viewer using the [Ogre Graphics Engine].
KeeKee is a massive modification of that code base to
use [Dotnet] features, updates to the C# language,
and use [dependency injection].

This project is no where near a point where a casual user can download and
run and view a virtual world of their choice.
The current development environment is [Ubuntu Linux] using
[Visual Studio Code].
The eventual goal is to expand the build environment to
Windows and ARM and have autobuild release packages available
on Github.
But that's in the future.

Sources are on GitHub as [Herbal3d/KeeKee-Viewer].
There is a start on information on building and running in the Development section.

## Main Features

Modular design to support many types of virtual worlds;
- Written in C# for operation under both the Windows(r) and Linux(r) operating systems;
- licensed under [Mozilla License] to promote reuse the contribution of improvements;
- User interface is not embedded into the viewer -- all operations are through REST/JSON interfaces so user interfacing can be supplied by JavaScript/AJAX web pages (initial sample page provided) or through a completely separate application;
- Initial communication module talking LLLP for connections to [SecondLife(r)] and [OpenSimulator] worlds;
- OpenGL (OpenTK) renderer

## Progress Reports
**January 16, 2026**

[dependency injection]: https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection
[Dotnet]:
[Herbal3d/KeeKee-Viewer]: https://github.com/Herbal3d/keekee-viewer
[Godot]: https://godotengine.org
[LibreMetaverse]: https://github.com/cinderblocks/libremetaverse
[LookingGlass Viewer]: https://github.com/Misterblue/LookingGlass-Viewer
[Mozilla License]: https://www.mozilla.org/en-US/MPL/
[OpenSimulator]: http://opensimulator.org
[SecondLife(r)]: https://secondlife.com
