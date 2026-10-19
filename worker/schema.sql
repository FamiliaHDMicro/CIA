-- Tabela do D1 usada pelo worker.js (colunas que o INSERT preenche + as automáticas).
-- Só rode se precisar recriar o banco do zero.
CREATE TABLE IF NOT EXISTS uso_telemetria (
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  data_hora DATETIME DEFAULT CURRENT_TIMESTAMP,
  licenca_chave TEXT,
  tecnico_nome TEXT,
  cidade TEXT,
  evento TEXT,
  detalhes TEXT
);
