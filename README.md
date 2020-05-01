# Yet Another Live Coding Tool

A glsl live coding tool strongly inspired by [Shadertoy](https://www.shadertoy.com/), but as a cross platform cross graphics API desktop application. Code in modern GLSL and view your shaders using the (almost) full power of your computer's graphics APIs.

Why? Because I wanted to train on [Veldrid](https://veldrid.dev), I love live coding despite not contributing a lot to it, and Shadertoy is a really fun thing. There's also a part of me who wants to show that dotnet is fun, not only for Windows and for boring aspnet and big IT service companies. Veldrid helps a lot in this sense.

![FAST DEMO](./assets/demo.gif)

Credits in this hectic gif:
- ["Protean Clouds" by nimitz](https://www.shadertoy.com/view/3l23Rh)
- ["[TWITCH] Isometric Cages" by Flopine](https://www.shadertoy.com/view/WdXfW7)

## Features

- full GLSL v450 fragment shader support
- load/save your shaders
- import (or at least try to) shadertoy shaders (doesn't support shaders with texture and sound inputs right now)
- supports OpenGL, Vulkan, Direct3D11 and Metal
- supports Windows 10, Linux and MacOS

## Usage

### Building

**This repository uses git lfs.**

Requires [.Net Core 3.1](https://dotnet.microsoft.com/download/dotnet-core/3.1) SDK for building. Nothing particular about the building process, just go inside the project folder and build/run/publish it how you wish.

Quickstart :
```
cd YALCT
dotnet run -c Release
```
### Making shaders

Wether you create or load a shader, the entry point is the `main()` function. Regarding the rest, you are free to declare as many functions as you need, the entire Vulkan flavor GLSL specs should be supported.

Default shader on create :

```glsl
void main()
{
    float x = gl_FragCoord.x / resolution.x;
    float y = gl_FragCoord.y / resolution.y;
    out_Color = vec4(0,x,y,1);
}
```

### Shader parameters

| Name | Type | Description |
| --- | --- | --- |
| `mouse` | `vec4` | `xy` for mouse, `z` for mouse 1 click, `w` for mouse 2 click |
| `resolution` | `vec2` | screen resolution in pixels |
| `time` | `float` | global time in seconds |
| `deltaTime` | `float` | time since last frame in seconds |
| `frame` | `int` | frame number |

### API Support

This has mostly been tested on Windows 10, but it does run on MacOS High Sierra and Ubuntu 19.10 in Virtualbox. Some more testing is required.

| API | Windows | Linux | MacOS |
|---|---|---|---|
| OpenGL (default) |:white_check_mark:|:white_check_mark:| (deprecated) |
| Vulkan|:white_check_mark:|:white_check_mark:| |
| Direct3D11 |:white_check_mark: | | |
| Metal| | | :white_check_mark:|

### Running different graphics APIs

By default, YALCT launches with OpenGL (and Metal on MacOS). You can switch to Vulkan or Direct3D11 through cli :

```
YALCT opengl
YALCT vulkan
YALCT direct3d11
```

As you can guess, MacOS doesn't get a choice.

## Known issues

- OpenGL switches y coordinates compared to the other APIs so things will be vertically switched
- editor layout gets funky when there are a lot of compile errors

## Roadmap and contribution

Contributions are welcome, feel free to fork and open PRs. Here are some features that I wish to implement in the future:

- add fullscreen mode, just forgot about this one
- a way better code editor
- input textures
- input audio
- more exotic inputs : game controllers, midi, OSC, why not some interaction with [OSSIA Score](https://ossia.io/)
- server/p2p with viewer mode for multiuser sessions (for comps for instance)
    - should start simple with local rendering by transfering fragment code to the viewer
    - then I'm thinking about webrtc to allow a bit more efficiency with a lot of users
- shadertoy API instead of just opening local files
- on the fly backend changing
- more options like font choosing for UI and editor. We're using [dearimgui](https://github.com/ocornut/imgui), might as well exploit its full potential

There's a lot of experimentation going around : getting better at this, trying out new stuff, colaborating with established tools. I have no pretentions of making something to replace your favorite livecoding tool.

Trying to use some semblance of git flow, but not the tool. Main branch is `develop`, releases are on `master`, and I do enjoy the `feature/` paradigm.

## License

[Licensed under the MIT License](./LICENSE).

## Credits

Fonts:
- [Open Sans](https://github.com/googlefonts/opensans) as main UI font
- [Fira Code](https://github.com/tonsky/FiraCode) as editor UI font