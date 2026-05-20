#!/usr/bin/env bash
# Сборка игры. Использование:
#   ./build.sh        — WebGL (по умолчанию), результат в web/
#   ./build.sh web    — то же
#   ./build.sh win    — Windows .exe, результат в build/Windows/Platformer.exe
# Перед сборкой вызывается SceneBuilder.Build (см. BuildScript.cs).
set -euo pipefail

TARGET="${1:-web}"
case "$TARGET" in
  web) METHOD="BuildScript.BuildWebGL" ;;
  win) METHOD="BuildScript.BuildWindows" ;;
  *)
    echo "Использование: $0 [web|win]" >&2
    exit 1
    ;;
esac

UNITY="${UNITY:-/Applications/Unity/Hub/Editor/6000.3.15f1/Unity.app/Contents/MacOS/Unity}"
PROJECT_DIR="$(cd "$(dirname "$0")" && pwd)"

if [[ ! -x "$UNITY" ]]; then
  echo "Unity не найден: $UNITY" >&2
  echo "Установи Unity 6000.3.15f1 через Unity Hub или передай путь: UNITY=... $0 $TARGET" >&2
  exit 1
fi

# -logFile - пишет логи в stdout (текущий терминал)
"$UNITY" -batchmode -nographics -projectPath "$PROJECT_DIR" \
         -executeMethod "$METHOD" \
         -quit -logFile -
