// Shared formatting helpers used across components.

// '2026-05-10' -> '10.05.2026'
export function fmtDate(iso) {
  return iso ? iso.split('-').reverse().join('.') : ''
}

// Numeric value of a level — used to sort the level column.
export const LEVEL_NUM = { E: 1, D: 2, C: 3, B: 4, A: 5, M: 6 }

// CSS class for the colored level badge.
export const LEVEL_CLASS = {
  E: 'lvl-E', D: 'lvl-D', C: 'lvl-C', B: 'lvl-B', A: 'lvl-A', M: 'lvl-M',
}

// Rating-category tabs shown in the controls bar: disciplines first, then skill levels.
export const CATEGORIES = [
  { key: 'overall', label: 'Общий', group: 'discipline' },
  { key: 'singles', label: 'Одиночки', group: 'discipline' },
  { key: 'doubles', label: 'Пары', group: 'discipline' },
  { key: 'mixed', label: 'Микст', group: 'discipline' },
  { key: 'A', label: 'A', group: 'level' },
  { key: 'B', label: 'B', group: 'level' },
  { key: 'C', label: 'C', group: 'level' },
  { key: 'D', label: 'D', group: 'level' },
  { key: 'E', label: 'E', group: 'level' },
  { key: 'M', label: 'Masters', group: 'level' },
]

// Genitive-case discipline names for the match-history summary (Russian UI text).
export const DISCIPLINE_GENITIVE = {
  singles: 'одиночки', doubles: 'пары', mixed: 'микст',
}
