## Veldrid

### What is it?

Veldrid is a cross-platform, graphics API-agnostic rendering library for .NET. It allows you to use a single set of rendering commands and run your application on a number of different graphics API's. With some small exceptions, applications written against Veldrid will run on any of its backends without modification.

Supported backends:

* Direct3D 11
* Vulkan
* OpenGL 3
* OpenGL ES 3

![Sponza](http://i.imgur.com/dQx3KTH.png)

### Build instructions

Veldrid  uses the standard .NET Core tooling. [Install the tools](https://www.microsoft.com/net/download/core) and build normally (`dotnet restore && dotnet build`).

Run the RenderDemo program to see a quick demonstration of the rendering capabilities of the library.

### Using the library

The recommended way to reference Veldrid is via source. Veldrid includes some debug-only validation code which is disabled in release builds.

A NuGet package is also available: https://www.nuget.org/packages/Veldrid
