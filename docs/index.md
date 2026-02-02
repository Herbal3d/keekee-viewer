---
_layout: landing
---
# KeeKee Virtual World Viewer

The KeeKee Virtual World Viewer provides a modular framework for a client viewer of virtual world systems.
KeeKee is written in C# and licensed with the
[Mozilla License](https://www.mozilla.org/en-US/MPL/)
.
The current version has a communication module that uses
[LibreMetaverse](https://github.com/cinderblocks/libremetaverse)
library to talk Linden Lab Legacy Protocol ("LLLP") which is compatible
with
[Second Life(r)](https://secondlife.com)
and
[OpenSimulator](http://opensimulator.org)
.
A rendering module exists for OpenGL (OpenTK)
and
[Godot](https://godotengine.org)
and
[Unreal Engine](https://www.unrealengine.com/en-US)
is planned in the future.
Other communication and rendering modules can be added.

The state of development is VERY alpha.
The sources came from my old
[LookingGlass Viewer](https://github.com/Misterblue/LookingGlass-Viewer)
project (back in 2010)
when I tried to build a viewer using the [Ogre Graphics Engine].
KeeKee is a massive modification of that code base to use
Dotnet
features, updates to the C# language,
and use
[dependency injection](https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection)
.

This project is no where near a point where a casual user can download and
run and view a virtual world of their choice.
The current development environment is Ubuntu Linux using
Visual Studio Code.
The eventual goal is to expand the build environment to
Windows, Mobile and ARM and have autobuild release packages available
on Github.
But that's in the future.

Sources are on GitHub as
[Herbal3d/KeeKee-Viewer](https://github.com/Herbal3d/keekee-viewer)
.
There is a start on information on building and running in the Development section.

## Main Features

Modular design to support many types of virtual worlds;
- Written in C# for operation under both the Windows(r) and Linux(r) operating systems;
- licensed under Mozilla License to promote reuse the contribution of improvements;
- User interface is not embedded into the viewer -- all operations are through REST/JSON interfaces so user interfacing can be supplied by JavaScript/AJAX web pages (initial sample page provided) or through a completely separate application;
- Initial communication module talking LLLP for connections to SecondLife(r) and OpenSimulator worlds;
- OpenGL (OpenTK) renderer

## Progress Reports

**February 2, 2026** The sources are to the point that they build and start talking
to an OpenSimulator server. But now I've started looking at modern game engines
(Godot, Unreal) and found that engines, these days, are not just libraries that
an application includes. They are applications themselves and anything that is
to be added to them must be a library.

So, with a focus on Godot, I am changing things away from a REST UI to an API
and looking to make, at least part of KeeKee, NuGet packages to add to a
graphics/game engine.

