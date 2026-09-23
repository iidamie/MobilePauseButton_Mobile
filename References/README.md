# Compile-time references

构建时优先使用 `../../StArray.ModManager-csharp/artifacts` 中的：

- `StArray.ModManager.dll`
- `StArray.ModManager.Analyzer.dll`
- `ImGui.NET.dll`

这些程序集只用于编译和生成 Hook 代码，不会被打进最终 Mod 包；手机端管理器已经提供。
