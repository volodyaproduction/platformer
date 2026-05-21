import { cmd, pipeline, readJsonBody,
         KEY_SCORES, KEY_RATE } from "./_redis.js";

// POST /api/leaderboard-save-score  { player_id, score }
//
// Защита от накрутки:
// (1) Sanity cap: счёт > MAX_SCORE отсекается на сервере. Это грубый барьер
//     от запросов вида «999999» из консоли. Реальный потолок задаётся в
//     клиенте через LevelLayout.MaxScore = CoinPositions.Length — серверу
//     знать точное число монет не обязательно, достаточно явно нереального.
// (2) Rate limit: один сабмит на player_id раз в 10 секунд. SET NX EX 10 —
//     ключ-стопор, существует короткое окно. Минимально-реалистичный спид-ран
//     уровня — около 10 секунд; не блокирует честные повторы.
//
// Отрицательный счёт разрешён: после штрафов от шипов счёт уходит в минус,
// и это нормальный валидный результат (попадает в лидерборд как есть).
//
// ZADD ... GT обновляет счёт только если новый больше предыдущего, поэтому
// сабмит «хуже моего рекорда» не двигает позицию в топе, но мы всё равно
// возвращаем игроку его personal best — для надписи «Твой рекорд: X».
const MAX_SCORE = 200;
const RATE_WINDOW = 10;

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
  const score = Number(body.score);

  // 1. Валидация формата
  if (!playerId || !/^[a-f0-9]{32}$/i.test(playerId)) {
    res.status(400).json({ error: "invalid_player_id" });
    return;
  }
  if (!Number.isFinite(score) || !Number.isInteger(score)) {
    res.status(400).json({ error: "invalid_score" });
    return;
  }

  // 2. Sanity cap — отсекает «999999». Минимум не проверяем: отрицательные
  //    счёта это валидный результат «упал в минус после шипов».
  if (score > MAX_SCORE) {
    res.status(400).json({ error: "score_too_high" });
    return;
  }

  try {
    // 3. Rate limit — атомарный SET NX EX
    const rateOk = await cmd(["SET", KEY_RATE(playerId), "1",
      "EX", String(RATE_WINDOW), "NX"]);
    if (rateOk === null) {
      res.status(429).json({ error: "rate_limited" });
      return;
    }

    // 4. Записываем счёт (только если новый рекорд) и сразу читаем PB
    const [, pbRaw] = await pipeline([
      ["ZADD", KEY_SCORES, "GT", String(score), playerId],
      ["ZSCORE", KEY_SCORES, playerId],
    ]);
    const personalBest = pbRaw === null ? score : Number(pbRaw);
    const isNewRecord = personalBest === score;

    res.status(200).json({
      personalBest,
      isNewRecord,
    });
  } catch (e) {
    res.status(500).json({ error: "server_error", message: String(e.message) });
  }
}
