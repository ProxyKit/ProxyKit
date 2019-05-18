workflow "Build" {
  on = "push"
  resolves = ["dotnet-cli"]
}

action "dotnet-cli" {
  uses = "Azure/github-actions/dotnetcore-cli@master"
  args = "dotnet run --project build"
}
