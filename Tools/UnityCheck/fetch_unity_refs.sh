#!/usr/bin/env bash
# Downloads the managed DLLs and built-in package sources of Unity 6000.3.24f1 (Linux editor, streamed; only the
# needed folders are kept, ~2 GB) plus the Input System package, for the compile check in Tools/UnityCheck.
# Usage: bash Tools/UnityCheck/fetch_unity_refs.sh [/opt/unityref]
set -euo pipefail
DEST="${1:-/opt/unityref}"
EDITOR_URL="https://download.unity3d.com/download_unity/4e7b9b5b6244/LinuxEditorInstaller/Unity-6000.3.24f1.tar.xz"
INPUT_URL="https://download.packages.unity.com/com.unity.inputsystem/-/com.unity.inputsystem-1.20.0.tgz"

mkdir -p "$DEST/packages"
if [ ! -d "$DEST/Editor/Data/Managed" ]; then
  curl -sSL "$EDITOR_URL" | tar -xJ -C "$DEST" --wildcards \
    'Editor/Data/Managed/*' 'Editor/Data/NetStandard/*' \
    'Editor/Data/Resources/PackageManager/BuiltInPackages/*' 'Editor/Data/UnityReferenceAssemblies/*'
fi
if [ ! -d "$DEST/packages/com.unity.inputsystem" ]; then
  tmp="$(mktemp -d)"
  curl -sSL "$INPUT_URL" | tar -xz -C "$tmp"
  mv "$tmp/package" "$DEST/packages/com.unity.inputsystem"
  rm -rf "$tmp"
fi
echo "Unity refs ready in $DEST"
