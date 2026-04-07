# ghgl
OpenGL shader support in Grasshopper

See the wiki for instructions to use
https://github.com/mcneel/ghgl/wiki

Bugs and wishes can be found at
https://mcneel.myjetbrains.com/youtrack/issues/GHGL

-----

Videos on using GhGL can be found on youtube at
https://www.youtube.com/playlist?list=PLs-XUlmGDNIxuQTOSBrknv5EvlrpGCxTs

![ghgl preview](https://img.youtube.com/vi/ZwBptnbeFHE/maxresdefault.jpg)


#### Build and Publish

- Choose `Release` build target
- Build in Visual Studio or run a build task in vscode
- `ghgl.gha` and associated files will be placed under `dist/`
- Open `dist/` in terminal
- Use `yak` command line to build and publish
  - `yak build --platform win`
  - `yak push ghgl-9.0.0-rh9_0-win.yak` (or whichever yak package was produced during build)