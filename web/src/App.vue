<script setup>
import { ref, computed, onMounted } from 'vue'
import LeagueHeader from './components/LeagueHeader.vue'
import RatingControls from './components/RatingControls.vue'
import RatingTable from './components/RatingTable.vue'
import MethodologyFooter from './components/MethodologyFooter.vue'
import { LEVEL_NUM } from './utils/format.js'

// --- Данные ---
const data = ref(null)
const error = ref(null)

onMounted(async () => {
  try {
    const res = await fetch(`${import.meta.env.BASE_URL}data/ratings.json`)
    if (!res.ok) throw new Error(`HTTP ${res.status}`)
    data.value = await res.json()
  } catch (e) {
    error.value = e.message
  }
})

// --- Состояние интерфейса ---
const discipline = ref('overall')   // overall | singles | doubles | mixed
const system = ref('elo')           // elo | points
const query = ref('')
const minMatches = ref(true)
const sortKey = ref('rating')
const sortDir = ref(-1)             // 1 = по возрастанию, -1 = по убыванию

// Статистика игрока в разрезе выбранной дисциплины.
function disciplineStats(p) {
  if (discipline.value === 'overall') {
    return { matches: p.matches, wins: p.wins, losses: p.losses, winrate: p.winrate, form: p.form }
  }
  return p.by_discipline[discipline.value]
    || { matches: 0, wins: 0, losses: 0, winrate: 0, form: [] }
}

// Значение рейтинга для выбранной системы (Elo/очки) и дисциплины.
function ratingOf(p) {
  if (system.value === 'elo') {
    return discipline.value === 'overall' ? p.elo.overall : p.elo[discipline.value]
  }
  return discipline.value === 'overall'
    ? p.points
    : (p.points_by_discipline[discipline.value] || 0)
}

// Отфильтрованный и отсортированный список строк таблицы.
const rows = computed(() => {
  if (!data.value) return []
  let list = data.value.players.map(p => ({
    p, d: disciplineStats(p), rating: ratingOf(p),
  })).filter(r => r.d.matches > 0)

  if (minMatches.value) list = list.filter(r => r.d.matches >= 5)

  const q = query.value.trim().toLowerCase()
  if (q) list = list.filter(r => r.p.name.toLowerCase().includes(q))

  const getters = {
    rank: r => r.rating,
    rating: r => r.rating,
    name: r => r.p.name,
    level: r => LEVEL_NUM[r.p.level] || 0,
    tournaments: r => r.p.tournaments,
    matches: r => r.d.matches,
    wl: r => r.d.wins - r.d.losses,
    winrate: r => r.d.winrate,
    form: r => r.d.form.filter(x => x === 'W').length,
  }
  const get = getters[sortKey.value] || getters.rating
  const dir = sortDir.value
  return list.sort((x, y) => {
    const a = get(x), b = get(y)
    const cmp = a < b ? -1 : a > b ? 1 : 0
    return cmp * dir || x.p.name.localeCompare(y.p.name, 'ru')
  })
})

function onSort(key) {
  if (sortKey.value === key) {
    sortDir.value *= -1
  } else {
    sortKey.value = key
    sortDir.value = key === 'name' ? 1 : -1
  }
}

// Смена системы возвращает сортировку к рейтингу (по убыванию).
function onSystemChange(value) {
  system.value = value
  sortKey.value = 'rating'
  sortDir.value = -1
}
</script>

<template>
  <div class="wrap">
    <LeagueHeader
      v-if="data"
      :tournaments="data.tournaments"
      :players-count="data.players.length"
      :updated="data.updated"
    />

    <RatingControls
      :discipline="discipline"
      :system="system"
      :query="query"
      :min-matches="minMatches"
      @update:discipline="discipline = $event"
      @update:system="onSystemChange"
      @update:query="query = $event"
      @update:min-matches="minMatches = $event"
    />

    <RatingTable
      v-if="data"
      :rows="rows"
      :sort-key="sortKey"
      :sort-dir="sortDir"
      @sort="onSort"
    />
    <p v-else-if="error" class="empty">Не удалось загрузить данные: {{ error }}</p>
    <p v-else class="empty">Загрузка…</p>

    <MethodologyFooter />
  </div>
</template>
