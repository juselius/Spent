# Spent

A minimal time-tracking tool for logging time in GitLab at the Epic level.

## Build

```console
just bundle
cd dist
```

## NixOS

```console
nix-build -A default
cd result/bin
./spent --help
```

or

```console
nix-shell -A spent-cli
spent
```

## Run

1. Log in to GitLab using `glab auth login`
2. Run `spent ls` to list available epics
2. Log time on epic: `spent time -i 1 -s "stuff" 3h15m`
