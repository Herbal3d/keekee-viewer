# Architecture

KeeKee has three main parts:

- Session: A connection from a user to access a virtual world
- Comm: communication with some virtual world
- World: a collection of Regions that make up a virtual world
- Region: an Entity/Component representation things in a specific space in the virtual world
- View: a view into the World, and
- Renderer: the code that renders a view of the world on whatever output media
- AuthToken: a token denoting the authorizations for access

The central part is the **Region** which represents a portion of the
3D world. The goal is to be as flexible as possible and not have any
embedded "world logic" so that the World representation can be used
by many different virtual world systems.

It builds this flexibility with an **Entity** and **Component** model.

Implementation-wise, everything in the world is an **Entity**.
Even the **Region** itself. The **Entity** itself doesn't do much.
It's functionality is built with **Components** that are added to
the **Entity**. The **Components** add collection, state, and
representation to the **Entity**.

Thus, the **World** would have a collection of **Region** components that
collect and manage all the regions that make up the world.
The Region entities would contain a collection of **Entity** components
which represent the objects in the 3d area of the Region.

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

Entities are of a type. The type defines the components
that will be on that Entity. For instance, an Entity of
type `Primitive` will be a displayable object and thus have
capabilities for 3d location and display representation.
Other types cover avatars (boned, animated, displayable, ...),
cameras, etc.

The current version of KeeKee has :

- one Comm module which uses the [LibreMetaverse] library to talk Linden Lab Legacy Protocol ("LLLP") and
- one Renderer which is adapted to [Godot]

Future expansions will have communication for other virtual worlds as
well as viewers and renderers for other libraries and displays.

The view interface to KeeKee is through REST/JSON interfaces.
For instance, Comm.LLLP has an interface that allows logging in and out
as status for a connection to such virtual worlds.
Other parts of KeeKee make their status available through such interfaces (see the KeeKee.Rest routines).

## Top Level Components

### Session

When a user connects to KeeKee, they give a virtual world (grid)
selection and authentication (login) credentials. This creates
a **Session** that specifies the virtual world that **Comm**
should connect to and which **World** and **Regions** should
be read in an processed.

The **Session** is also the base for authentication. It contains
any tokens or keys needed to access the **World** and the Entities
therein. For instance, a user might have permission to view
Entities in the world but not permission to export them.

### Region

The world is made up of Regions.
A "region" defines a space in the world.
Regions can overlap so, for instance, a set of regions with identical
base coordinates can be used to classify entities in a spacial area.
The eventual view is of the union of all the 3d objects in all viewed regions.
The name "region" should not imply that it is a particular geographic or
spacial unit although it can be used that way (in the LL world, for instance).
The general definition of a region is a collection of Entities.
A region has a base world coordinate and the **Entities** in that region are
relative to the base of the region.

The entities in a region exist in a 4d space made up of 3d coordinates
and a resolution. Level-of-detail ("LOD") is handled by the entities
being overlapped in 3d space but being distinct in the 4th dimension.

### Entity

The Entity is the basic thing in the world.
An Entity might have a high level type (basic, avatar) but most of an
Entity's character comes from its components.
Each Entity has a Region Context (the space unit it is in) and an
Asset Context (the asset system to resolve this entity's needs).

An Entity is decorated with **components** --
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

The world is a container that manages a collection of Regions that contain Entities.
World will manage creation and destruction of Regions as it receives information
from Comm.

### Communication Module

The Comm module's job is talking with an external virtual world,
receiving information about entities to display in the client,
creating the proper Node object and placing it in the World.
Comm does not have any knowledge about how an Entity might be displayed.
It only performs the actions of creating and managing Entities it places in the world.

### Viewer Module

A **View** into the 3d world is represented by a Camera at
some location that is viewing the 3d Entities in the frustrum
of the camera. So, the view is the middle-man between the
Region and the Renderer.

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
[SecondLife(r)]: https://secondlife.com
[WebAssembly]: https://webassembly.org/
[Godot]: https://godotengine.org/
