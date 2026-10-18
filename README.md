# Worker da CIA (Cloudflare)

Arquivo único: `worker.js`. Cuida de licenças, avisos das CIAs e do painel do dono (`/admin`).

## No Cloudflare (Worker `control-ia`)
- Associações: `LICENCAS_KV` (KV) e `DB` (D1, tabela `uso_telemetria`, veja `schema.sql`).
- Segredo `ADMIN_TOKEN` = senha do painel. Mínimo de 8 caracteres (constante `SENHA_MINIMA`).
  Sem espaços e sem acentos. Quanto mais longa, mais segura.

## Como publicar uma mudança
1. Edite `worker.js` aqui no GitHub.
2. No Cloudflare: Worker `control-ia` > Editar código > cole o arquivo inteiro > Implantar.
   (O Worker NÃO está ligado ao GitHub: a publicação é manual.)

## Rotas
- `POST /api/validar-licenca` (app)
- `POST /api/telemetria` (app)
- `GET /admin` (painel) e `/api/admin/*` (exigem a senha)
