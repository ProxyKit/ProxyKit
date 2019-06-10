workflow "Build and Test on push" {
  on = "push"
  resolves = ["Build and Test"]
}

action "Build and Test" {
  uses = "Azure/github-actions/dotnetcore-cli@master"
  args = "run --project build"
}

