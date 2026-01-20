Whenever you work on code, please ensure that the app version number in /.Directory.Build.props is updated correctly. I use the format yyyy.mm.dd.# - for instance, 2026.01.03.0 is for January 3, 2026. The last number is the zero-based build number, so 0 means it's the first build for that day. 1 would be the second build, 2 would be the third, etc.

Please also do your best to respect the existing code format and style. You can reference /.editorconfig as well.

Please note, in order to build Pkmds.Web, you will need to first install the wasm-tools workload like so:

```
dotnet workload install wasm-tools
```

When creating a new PR for an issue or otherwise, please branch from the dev branch, and plan to merge into dev. I'll take it from there to merge to main.
