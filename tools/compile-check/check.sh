#!/usr/bin/env bash
# Compile-checks the game's C# scripts without Unity (for cloud/Linux sessions).
# Usage: tools/compile-check/check.sh        (from anywhere in the repo)
# First run installs mono and downloads Unity reference DLLs + Roslyn into $CACHE.
# Unity refs are 2021-era, so Unity-6-only APIs fail here; uGUI, EventSystems,
# InputSystem and URP come from hand-written stubs in Stubs.cs (extend as needed).
set -euo pipefail

HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT="$(cd "$HERE/../.." && pwd)"
CACHE="${UNITYCHECK_CACHE:-$HOME/.cache/unitycheck}"
OUT="$CACHE/out"
mkdir -p "$CACHE" "$OUT"

if ! command -v mono >/dev/null || [ ! -d /usr/lib/mono/4.8-api ]; then
  echo "Installing mono-devel..."
  SUDO=""; [ "$(id -u)" != 0 ] && SUDO="sudo"
  $SUDO apt-get install -y mono-devel >/dev/null 2>&1 || { $SUDO apt-get update >/dev/null; $SUDO apt-get install -y mono-devel >/dev/null; }
fi

fetch() { # id version
  local dir="$CACHE/$1.$2"
  [ -d "$dir" ] && return
  echo "Downloading $1 $2..."
  curl -fsSL "https://api.nuget.org/v3-flatcontainer/$1/$2/$1.$2.nupkg" -o "$CACHE/pkg.zip"
  mkdir -p "$dir" && (cd "$dir" && unzip -q "$CACHE/pkg.zip") && rm "$CACHE/pkg.zip"
}
fetch unity3d.sdk 2021.1.14.1
fetch microsoft.net.compilers.toolset 4.8.0

UNITY="$CACHE/unity3d.sdk.2021.1.14.1/lib"
CSC="$CACHE/microsoft.net.compilers.toolset.4.8.0/tasks/net472/csc.exe"
API=/usr/lib/mono/4.8-api
REFS=(-nostdlib+ -r:$API/mscorlib.dll -r:$API/System.dll -r:$API/System.Core.dll
      -r:$API/Facades/netstandard.dll -r:"$UNITY/UnityEngine.dll")
FLAGS=(-nologo -noconfig -target:library -langversion:9.0 -nowarn:0618,0414,0649,0169,0219,0067)

csc() { mono "$CSC" "${FLAGS[@]}" "${REFS[@]}" "$@"; }

echo "[1/3] Stubs"
csc -out:"$OUT/Stubs.dll" "$HERE/Stubs.cs"

echo "[2/3] Game scripts"
mapfile -t GAME < <(find "$ROOT/Assets/_Game/Scripts" -name '*.cs' | sort)
csc -out:"$OUT/Game.dll" -r:"$OUT/Stubs.dll" "${GAME[@]}"

echo "[3/3] Editor scripts"
mapfile -t EDITOR < <(find "$ROOT/Assets/_Game/Editor" -name '*.cs' | sort)
csc -out:"$OUT/Editor.dll" -define:UNITY_EDITOR -r:"$OUT/Stubs.dll" -r:"$OUT/Game.dll" \
    -r:"$UNITY/UnityEditor.dll" "${EDITOR[@]}"

echo "Compile check passed."
