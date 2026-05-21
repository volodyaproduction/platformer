// Лёгкая обёртка над Upstash Redis REST API. Никаких npm-зависимостей —
// Vercel Node.js 18+ имеет глобальный fetch.
//
// Vercel игнорирует файлы в `api/`, имя которых начинается с `_`, поэтому
// этот модуль доступен соседним эндпоинтам, но сам наружу не выставлен.
//
// Префикс ключей `platformer:` отделяет данные платформера от других игр на
// той же базе Upstash (например, шутера с префиксом `shooter:`).

const URL_BASE = process.env.KV_REST_API_URL;
const TOKEN = process.env.KV_REST_API_TOKEN;

const PREFIX = "platformer:";
export const KEY_SCORES = PREFIX + "scores";          // ZSET: player_id -> best
export const KEY_NAMES = PREFIX + "names";            // Hash: player_id -> name
export const KEY_NAME_INDEX = PREFIX + "name-index";  // Hash: nameLower -> player_id
export const KEY_RATE = (id) => PREFIX + "rate:" + id;

// Одиночная команда: POST $URL/ с JSON-массивом [cmd, ...args].
export async function cmd(args) {
  if (!URL_BASE || !TOKEN) {
    throw new Error("KV_REST_API_URL/KV_REST_API_TOKEN not configured");
  }
  const r = await fetch(URL_BASE, {
    method: "POST",
    headers: {
      "Authorization": "Bearer " + TOKEN,
      "Content-Type": "application/json",
    },
    body: JSON.stringify(args),
  });
  if (!r.ok) {
    const text = await r.text().catch(() => "");
    throw new Error("Upstash " + r.status + ": " + text);
  }
  const data = await r.json();
  return data.result;
}

// Несколько команд одним HTTP-запросом. Возвращает массив result[] в порядке.
export async function pipeline(commands) {
  if (!URL_BASE || !TOKEN) {
    throw new Error("KV_REST_API_URL/KV_REST_API_TOKEN not configured");
  }
  const r = await fetch(URL_BASE + "/pipeline", {
    method: "POST",
    headers: {
      "Authorization": "Bearer " + TOKEN,
      "Content-Type": "application/json",
    },
    body: JSON.stringify(commands),
  });
  if (!r.ok) {
    const text = await r.text().catch(() => "");
    throw new Error("Upstash " + r.status + ": " + text);
  }
  const data = await r.json();
  return data.map((d) => d.result);
}

export function readJsonBody(req) {
  // Vercel парсит JSON-body автоматически только если Content-Type выставлен;
  // на всякий случай поддерживаем и сырое тело.
  if (req.body && typeof req.body === "object") return req.body;
  if (typeof req.body === "string") {
    try { return JSON.parse(req.body); } catch { return null; }
  }
  return null;
}
