// Shared formatting helpers used across components.

// '2026-05-10' -> '10.05.2026'
export function fmtDate(iso) {
  return iso ? iso.split('-').reverse().join('.') : ''
}

// Rating-category tabs shown in the controls bar: disciplines first, then skill levels
// (Masters first among levels — it's the featured bracket, not a footnote after E).
export const CATEGORIES = [
  { key: 'singles', label: 'Одиночки', group: 'discipline' },
  { key: 'doubles', label: 'Пары', group: 'discipline' },
  { key: 'mixed', label: 'Микст', group: 'discipline' },
  { key: 'M', label: 'Masters', group: 'level' },
  { key: 'A', label: 'A', group: 'level' },
  { key: 'B', label: 'B', group: 'level' },
  { key: 'C', label: 'C', group: 'level' },
  { key: 'D', label: 'D', group: 'level' },
  { key: 'E', label: 'E', group: 'level' },
]

// Combined discipline+level key, e.g. ("singles", "D") -> "singles_D" — matches the
// keys the backend uses for elo/by_combo/points_by_combo when both facets are active.
export function comboKey(discipline, level) {
  return `${discipline}_${level}`
}

// Genitive-case discipline names for the match-history summary (Russian UI text).
export const DISCIPLINE_GENITIVE = {
  singles: 'одиночки', doubles: 'пары', mixed: 'микст',
}

// "Никита Янковский" -> "Янковский Никита". Falls back to the raw name
// for anything that isn't exactly "given name + surname".
export function surnameFirst(name) {
  const parts = name.trim().split(/\s+/)
  return parts.length === 2 ? `${parts[1]} ${parts[0]}` : name
}
