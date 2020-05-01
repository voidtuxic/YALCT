# Yet Another Live Coding Tool

A glsl live coding tool strongly inspired by [Shadertoy](https://www.shadertoy.com/), but as a cross platform cross graphics API desktop application. Code in modern GLSL and view your shaders using the (almost) full power of your computer's graphics APIs.

Why? Because I wanted to train on [Veldrid](https://veldrid.dev), I love live coding despite not contributing a lot to it, and Shadertoy is a really fun thing.

## Usage

### Building

**This repository uses git lfs.**

Requires [.Net Core 3.1](https://dotnet.microsoft.com/download/dotnet-core/3.1) SDK for building. Nothing particular about the building process, just go inside the project folder and build/run/publish it how you wish.

Quickstart :
```
cd YALCT
dotnet run -c Release
```

## API Support

| API | Windows | Linux | MacOS |
|---|---|---|---|
| OpenGL (default) |:white_check_mark:|:white_check_mark:| (deprecated) |
| Vulkan|:white_check_mark:|:white_check_mark:| |
| Direct3D11 |:white_check_mark: | | |
| Metal| | | :white_check_mark:|

## Credits

Fonts:
- [Open Sans](https://github.com/googlefonts/opensans) as main UI font
- [Fira Code](https://github.com/tonsky/FiraCode) as editor UI font