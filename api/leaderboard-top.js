import { cmd, KEY_SCORES, KEY_NAMES } from "./_redis.js";

// GET /api/leaderboard-top — весь лидерборд по убыванию счёта.
// Бесконечного скролла на стороне сервера нет: для дружеского платформера всех
// игроков влезает в один JSON. Если когда-нибудь раздуется до десятков тысяч —
// перейдём на постраничную выдачу через ZRANGE с offset/limit.
export default async function handler(req, res) {
  if (req.method !== "GET") {
    res.status(405).json({ error: "method_not_allowed" });
    return;
  }
  try {
    // 1. Все записи по убыванию счёта: [id1, score1, id2, score2, ...]
    const raw = await cmd(["ZRANGE", KEY_SCORES, "0", "-1",
      "REV", "WITHSCORES"]);
    const ids = [];
    const scores = [];
    for (let i = 0; i < raw.length; i += 2) {
      ids.push(raw[i]);
      scores.push(Number(raw[i + 1]));
    }

    let names = [];
    if (ids.length > 0) {
      // 2. HMGET берёт все имена одним запросом
      names = await cmd(["HMGET", KEY_NAMES, ...ids]);
    }

    const entries = ids.map((_, i) => ({
      name: names[i] || "Игрок",
      score: scores[i],
    }));

    // 3. Кэш короткий — лидерборд обновляется на самом сабмите, но при
    //    наплыве пара секунд кэша спасёт от лишних обращений к KV.
    res.setHeader("Cache-Control",
      "public, s-maxage=2, stale-while-revalidate=10");
    res.status(200).json({ entries });
  } catch (e) {
    res.status(500).json({ error: "server_error", message: String(e.message) });
  }
}
