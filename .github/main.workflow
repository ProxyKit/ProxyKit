workflow "Build" {
  on = "push"
  resolves = ["build and test"]
}

action "build and test" {
  uses = "Azure/github-actions/dotnetcore-cli@master"
  args = "run --project build"
}
