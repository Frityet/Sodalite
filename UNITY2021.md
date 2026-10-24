# NativeH3VR / Unity 2021 target

`src/Sodalite.Unity2021/Sodalite.Unity2021.csproj` builds a standalone native-game variant of Sodalite against the converted NativeH3VR assemblies. The original BepInEx target remains available.

```sh
dotnet build src/Sodalite.Unity2021/Sodalite.Unity2021.csproj -c Release \
  -p:NativeH3VRManaged=/absolute/path/NewEngineH3VR.app/Contents/Resources/Data/Managed \
  -p:UnityEngineManaged=/Applications/Unity/Hub/Editor/2021.3.45f2/Unity.app/Contents/Managed/UnityEngine
```

Call `Sodalite.Sodalite.Initialize()` once before game scene initialization. The native host uses Unity logging, marks GM modded after its Awake, and installs Harmony 2.4.3 hooks for wrist-menu extensions, leaderboard locks, and resource injection. It does not require the old BepInEx preloader to publicize game assemblies. Current public accessors or narrow reflection replace the changed private-field access.

The native build includes game/player/firearm/vault/Sosig APIs, wrist-menu sections, UI widgets, and utilities. `GameAPI.PreloadAllAssets` adapts `AnvilAsset.m_anvilPrefab` through the current `AssetID` struct and portable paths. Resource injection hooks Unity 2021's managed `ResourcesAPI.LoadAll` seam; injected resources are appended only to an all-resources query with a matching type. As with the original API, calling `Resources.LoadAll("")` can load unrelated original game resources. Do not use a global enumeration as a map-readiness probe: NativeH3VR may contain unused legacy resources outside its converted gameplay paths.

`GameAPI.BuildId` is `-10` and `BetaName` is `native-unity2021`; use capability checks instead of Steam build numbers for native compatibility. The BepInEx-only `ModsAPI` and ModPanel are excluded. This is not a general BepInEx replacement or a promise that every unrelated Unity 5.6 mod will run unchanged.

H3VRBackrooms exercises initialization, leaderboard lock/release across map transitions, and creation of a real custom wrist-menu button. Full firearm/vault/Sosig API coverage and all game resources require their own gameplay tests. Keep all game DLLs and recovered content outside the repository.
