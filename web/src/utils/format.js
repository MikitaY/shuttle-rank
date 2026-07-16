// Вспомогательные функции форматирования, общие для компонентов.

// '2026-05-10' -> '10.05.2026'
export function fmtDate(iso) {
  return iso ? iso.split('-').reverse().join('.') : ''
}

// Числовое значение уровня — для сортировки колонки «Уровень».
export const LEVEL_NUM = { E: 1, D: 2, C: 3, B: 4, M: 5 }

// CSS-класс цветного бейджа уровня.
export const LEVEL_CLASS = {
  E: 'lvl-E', D: 'lvl-D', C: 'lvl-C', B: 'lvl-B', M: 'lvl-M',
}

// Родительный падеж дисциплины для сводки в истории матчей.
export const DISCIPLINE_GENITIVE = {
  singles: 'одиночки', doubles: 'пары', mixed: 'микст',
}
