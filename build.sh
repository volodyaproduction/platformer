#!/usr/bin/env bash
# Сборка игры. Использование:
#   ./build.sh        — WebGL (по умолчанию), результат в web/
#   ./build.sh web    — то же
#   ./build.sh win    — Windows .exe, результат в build/Windows/
# Перед сборкой запускается регенерация сцен и ассетов (см. BuildScript.cs).
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

# Пост-патч для WebGL: меняем фиксированный размер canvas (960×600) на
# адаптивный — игра сохраняет пропорции и помещается в любое окно браузера.
if [[ "$TARGET" == "web" ]]; then
  python3 - "$PROJECT_DIR/web/index.html" <<'PY'
import sys, pathlib
p = pathlib.Path(sys.argv[1])
src = p.read_text()
old = '        canvas.style.width = "960px";\n        canvas.style.height = "600px";'
new = '''        function fitCanvas() {
          var aspect = 960 / 600;
          var maxW = window.innerWidth * 0.95;
          var maxH = (window.innerHeight - 38) * 0.95;
          var w = Math.min(maxW, maxH * aspect);
          canvas.style.width = w + "px";
          canvas.style.height = (w / aspect) + "px";
        }
        fitCanvas();
        window.addEventListener("resize", fitCanvas);'''
if old not in src:
    sys.stderr.write("WARNING: canvas-size block not found in index.html — patch skipped\n")
    sys.exit(0)
p.write_text(src.replace(old, new))
print("Patched web/index.html: canvas теперь адаптивный")
PY
fi
