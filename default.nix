{
  sources ? import ./nix,
  system ? builtins.currentSystem,
  pkgs ? import sources.nixpkgs { inherit system; },
}:
let
  snowflaqe = pkgs.buildDotnetGlobalTool (finalAttrs: {
    pname = "snowflaqe";
    version = "1.48.0";

    nugetHash = "sha256-2VSOY3OecRWVWBjWX7Dlba04Iy7Ag84+mBS3vxuxNGk=";
  });

  version = "0.0.1";

  packages.spent = pkgs.buildDotnetModule {
    name = "Spent";
    pname = "spent";
    version = version;

    src = pkgs.nix-gitignore.gitignoreSource [ ] ./.;

    useDotnetFromEnv = false;

    projectFile = "src/Spent.fsproj";
    dotnet-sdk = pkgs.dotnetCorePackages.sdk_10_0;
    dotnet-runtime = pkgs.dotnetCorePackages.runtime_10_0;
    nugetDeps = ./nix/deps.json;
  };
in
{
  inherit packages;
  default = packages.spent;

  shell = pkgs.mkShell {
    packages = with pkgs; [
      npins
      packages.spent

      snowflaqe
      fantomas
      fsautocomplete
      dotnetCorePackages.sdk_10_0
    ];

    NPINS_DIRECTORY = "nix";
  };
}
