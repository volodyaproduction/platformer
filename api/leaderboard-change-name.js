import { cmd, pipeline, readJsonBody,
         KEY_NAMES, KEY_NAME_INDEX } from "./_redis.js";

// POST /api/leaderboard-change-name  { player_id, name }
//
// Имена в лидерборде уникальны (case-insensitive). При совпадении возвращаем
// 409, и клиент показывает «имя уже занято». Старое имя автоматически
// освобождается — обратный индекс KEY_NAME_INDEX обновляется в той же
// транзакции через pipeline.
//
// Имя может начинаться с `@` — тогда клиент покажет его как Telegram-ссылку.
// Никакой особой логики для `@` на сервере нет; уникальность проверяется
// по обоим вариантам без отличий.
const MIN_LEN = 2;
const MAX_LEN = 24;

export default async function handler(req, res) {
  if (req.method !== "POST") {
    res.status(405).json({ error: "method_not_allowed" });
    return;
  }
  const body = readJsonBody(req);
  if (!body) {
    res.status(400).json({ error: "bad_request" });
    return;
  }
  const playerId = String(body.player_id || "").trim();
  const name = String(body.name || "").trim();

  // 1. Валидация
  if (!playerId || !/^[a-f0-9]{32}$/i.test(playerId)) {
    res.status(400).json({ error: "invalid_player_id" });
    return;
  }
  if (name.length < MIN_LEN || name.length > MAX_LEN) {
    res.status(400).json({ error: "invalid_name" });
    return;
  }
  // 2. Запрещаем переносы строк и управляющие символы
  if (/\s/.test(name)) {
    res.status(400).json({ error: "invalid_name" });
    return;
  }

  const nameKey = name.toLowerCase();

  try {
    // 3. Атомарно «занять» имя через HSETNX. Без этого два одновременных
    //    запроса с одним именем оба прошли бы проверку HGET и оба сделали
    //    бы HSET — последний победил, у двух игроков оказалось бы общее имя.
    const claimed = await cmd(
        ["HSETNX", KEY_NAME_INDEX, nameKey, playerId]);
    if (claimed === 0) {
      // Слот уже занят. Если владелец — мы сами (повторная отправка, смена
      // регистра), это не конфликт — продолжаем.
      const owner = await cmd(["HGET", KEY_NAME_INDEX, nameKey]);
      if (owner !== playerId) {
        res.status(409).json({ error: "name_taken" });
        return;
      }
    }

    // 4. Освобождаем прежний слот и записываем новое отображаемое имя
    const prevName = await cmd(["HGET", KEY_NAMES, playerId]);
    const ops = [];
    if (prevName && prevName.toLowerCase() !== nameKey) {
      ops.push(["HDEL", KEY_NAME_INDEX, prevName.toLowerCase()]);
    }
    ops.push(["HSET", KEY_NAMES, playerId, name]);

    await pipeline(ops);
    res.status(200).json({ ok: true });
  } catch (e) {
    res.status(500).json({ error: "server_error", message: String(e.message) });
  }
}
