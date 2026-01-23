# Architecture

KeeKee has three main parts:

- Comm: communication with some virtual world
- World: an Entity/Component representation of the virtual world as communicated
- View: a view into the World, and
- Renderer: the code that renders a view of the world on whatever output media

The central part is the **World** which represents a portion of the
3D world. The goal is to be as flexible as possible and not have any
embedded "world logic" so that the World representation can be used
by many different virtual world systems.

It builds this flexibility with an **Entity** and **Component** model.

Implementation-wise, everything in the world is an **Entity**.
Even the **World** itself. The **Entity** itself doesn't do much.
It's functionality is built with **Components** that are added to
the **Entity**. The **Components** add collection, state, and
representation to the **Entity**.

Thus, the **World** would have a RegionCollection component that
collects and manages all the regions that make up the world.
The Region entities would contain an NodeCollection component
that contains the Entities that represent the objects in the
3d area of the Region.

One feature of both the Entities and Compoents is the concept 
of a **Contract**. Every entity and component is specified with
a contract of what is does and what other components it needs
top reference to get its job done.

Following the C# language and the Dependency Injection programming
style, this contract is started with the method signatures and
the dependencies that are injected into any entity or component.
There would be other operational specifications and possibly
specs on what is referenced and operated on.

It is planned, in the eventual implementation, that capabilites could be
added dynamically to increase the functionality in the world.
A dream is to allow [WebAssembly] components that are loaded
and used. This would require some sort of language to express
the contract as extensive use of C#'s reflection to build
interfaces and classes. But that's all for the future.

Entities, for instance, can be classes depending on the contracts
they use. For instance, top-level Nodes in the region could be
grouped into "Actors" or "Avatars" as entities that are guaranteed
to have centain capabilities.

The current version of KeeKee has :

- one Comm module which uses the [LibreMetaverse] library to talk Linden Lab Legacy Protocol ("LLLP") and
- one Renderer which uses the [OpenTK] library for display.

Future expansions will have communication for other virtual worlds as
well as viewers and renderers for other libraries and displays.

The view interface to KeeKee is through REST/JSON interfaces.
For instance, Comm.LLLP has an interface that allows logging in and out
as status for a connection to such virtual worlds.
Other parts of KeeKee make their status available through such interfaces (see the KeeKee.Rest routines).

## Top Level Components

### Region

The world is made up of Regions.
The name "region" should not imply that it is a particular geographic or
spacial unit although it can be used that way (in the LL world, for instance).
The general definition of a region is a collection of **Nodes**.
A region has a base world coordinate and the **Nodes** in that region are
relative to the base of the region.
Regions can overlap so, for instance, a set of regions with identical
base coordinates can be used to classify entities in a spacial area.

Regions have a level-of-detail ("LOD").
A RegionManager (note: as of 20260110, there is not yet a RegionManager implementation)
acts on the regions in the world to change to level-of-detail for the regions.
The effect is usually to keep regions close to the agent in high
level-of-detail while making regions far away from the agent low level-of-detail.
This LOD information passes through the viewer into the renderer to load
and unload the displayed objects in the regions.

### Node

The Node is the basic thing in the world.
An Node might have a high level type (basic, avatar) but most of an
Node's character comes from its components.
Each Entity has a Region Context (the space unit it is in) and an
Asset Context (the asset system to resolve this entity's needs).

Additionally, an Entity is decorated with additional **components** --
objects added to implement state and functionality.
These include position, avatar actions, and renderer specific data.

One of the difficult architectural questions is how to pass virtual
world specific information (3d object definition, for instance)
from Comm to the Renderer without muddling up the World with many
implementation specific variables.
The combination of the contexts and additional attributes allows Comm
to build an Entity with implementation specific information that can
be transparently passed through the world and used by the Renderer if it understands them.

### World Module

The world is a container that manages a collection of Region Entites that contain Nodes.
World will manage creation and destruction of Regions as it receives information
from Comm.

### Communication Module

The Comm module's job is talking with an external virtual world,
receiving information about entities to display in the client,
creating the proper Node object and placing it in the World.
Comm does not have any knowledge about how an Entity might be displayed.
It only performs the actions of creating and managing Entities it places in the world.

### Viewer Module

### Renderer Module

### Framework

All components exist within a framework of services.

### User Interface

KeeKee is controlled through several HTTP/REST/JSON interfaces
which return statistics and change the operation of the viewer.
The interfaces are all accessed via HTTP GET and POST operations
which store and return JSON formatted messages.

See other pages for the format of the REST messages for
Statistics, Chat, Login and Logout, Active Avatars.
Someday there will be [OpenAPI] specifications for them.

[LibreMetaverse]: https://github.com/cinderblocks/libremetaverse
[Mozilla License]: https://www.mozilla.org/en-US/MPL/
[OpenAPI]: https://openapis.org
[OpenSimulator]: http://opensimulator.org
[OpenTK]: https://opentk.net
[SecondLife(r)]: https://secondlife.com
[WebAssembly]: https://webassembly.org/

