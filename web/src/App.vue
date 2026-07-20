<script setup>
import { ref, computed, onMounted } from 'vue'
import LeagueHeader from './components/LeagueHeader.vue'
import RatingControls from './components/RatingControls.vue'
import RatingTable from './components/RatingTable.vue'
import MethodologyFooter from './components/MethodologyFooter.vue'
import { LEVEL_NUM } from './utils/format.js'

// --- Data ---
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

// --- UI state ---
// 'overall' | 'singles' | 'doubles' | 'mixed' (discipline) or 'A' | 'B' | 'C' | 'D' | 'E' | 'M' (skill level).
const category = ref('overall')
const system = ref('elo')           // elo | points
const query = ref('')
const minMatches = ref(true)
const sortKey = ref('rating')
const sortDir = ref(-1)             // 1 = ascending, -1 = descending

// Player stats for the selected category (discipline or level).
// Optional chaining: older ratings.json snapshots may not have by_level yet.
function categoryStats(p) {
  if (category.value === 'overall') {
    return { matches: p.matches, wins: p.wins, losses: p.losses, winrate: p.winrate, form: p.form }
  }
  return p.by_discipline?.[category.value] || p.by_level?.[category.value]
    || { matches: 0, wins: 0, losses: 0, winrate: 0, form: [] }
}

// Rating value for the selected system (Elo/points) and category.
function ratingOf(p) {
  if (system.value === 'elo') {
    return p.elo[category.value] ?? 0
  }
  if (category.value === 'overall') return p.points
  return p.points_by_discipline?.[category.value] ?? p.points_by_level?.[category.value] ?? 0
}

// Filtered and sorted list of table rows.
const rows = computed(() => {
  if (!data.value) return []
  let list = data.value.players.map(p => ({
    p, d: categoryStats(p), rating: ratingOf(p),
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

// Switching the system resets sorting back to rating (descending).
function onSystemChange(value) {
  system.value = value
  sortKey.value = 'rating'
  sortDir.value = -1
}

// Switching the category (discipline/level) resets sorting back to rating (descending).
function onCategoryChange(value) {
  category.value = value
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
      :category="category"
      :system="system"
      :query="query"
      :min-matches="minMatches"
      @update:category="onCategoryChange"
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
