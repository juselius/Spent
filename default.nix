{
  sources ? import ./nix,
  system ? builtins.currentSystem,
  pkgs ? import sources.nixpkgs { inherit system; },
  nix-utils ? import sources.nix-utils { },
}:
let
  snowflaqe = pkgs.buildDotnetGlobalTool (finalAttrs: {
    pname = "snowflaqe";
    version = "1.48.0";

    nugetHash = "sha256-2VSOY3OecRWVWBjWX7Dlba04Iy7Ag84+mBS3vxuxNGk=";
  });

  version =
    let
      clean = pkgs.lib.removeSuffix "\n";
      version = builtins.readFile ./VERSION;
    in
    clean version;

  dotnet-sdk = pkgs.dotnetCorePackages.sdk_10_0;
  dotnet-runtime = pkgs.dotnetCorePackages.runtime_10_0;
  deps = nix-utils.output.lib.nuget.deps;

  spent = pkgs.callPackage ./src {
    inherit
      deps
      dotnet-sdk
      dotnet-runtime
      version
      ;
  };

  container = pkgs.dockerTools.buildLayeredImage {
    name = "Spent";
    tag = version;
    created = "now";

    contents = [
      spent
      pkgs.busybox
      pkgs.dockerTools.binSh
      # pkgs.dockerTools.caCertificates
    ];

    extraCommands = ''
      mkdir -p app
      cp -r ${spent}/lib/Spent/* app
    '';

    config = {
      cmd = [ "Spent" ];
      workingDir = "/app";
    };
  };

  packages = {
    inherit spent;
  };

  containers = {
    inherit container;
  };
in
{
  default = spent;

  inherit
    packages
    containers
    ;

  shell = pkgs.mkShell {
    packages = with pkgs; [
      just
      npins
      snowflaqe
      fantomas
      fsautocomplete
      dotnet-sdk
    ];

    NPINS_DIRECTORY = "nix";

    DOTNET_ROOT = "${dotnet-sdk}/share/dotnet";

    passthru = pkgs.lib.mapAttrs (name: value: pkgs.mkShellNoCC (value // { inherit name; })) {
      spent-cli = {
        packages = [
          spent
        ];
      };
    };
  };
}
