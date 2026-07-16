// Shared formatting helpers used across components.

// '2026-05-10' -> '10.05.2026'
export function fmtDate(iso) {
  return iso ? iso.split('-').reverse().join('.') : ''
}

// Numeric value of a level — used to sort the level column.
export const LEVEL_NUM = { E: 1, D: 2, C: 3, B: 4, M: 5 }

// CSS class for the colored level badge.
export const LEVEL_CLASS = {
  E: 'lvl-E', D: 'lvl-D', C: 'lvl-C', B: 'lvl-B', M: 'lvl-M',
}

// Genitive-case discipline names for the match-history summary (Russian UI text).
export const DISCIPLINE_GENITIVE = {
  singles: 'одиночки', doubles: 'пары', mixed: 'микст',
}
