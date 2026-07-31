<script setup>
import { ref, computed, onMounted, watch } from 'vue'
import LeagueHeader from './components/LeagueHeader.vue'
import RatingControls from './components/RatingControls.vue'
import RatingTable from './components/RatingTable.vue'
import UnifiedTable from './components/UnifiedTable.vue'
import PairCalculator from './components/PairCalculator.vue'
import { comboKey, categoryLadder } from './utils/format.js'

// --- Data ---
const data = ref(null)
const error = ref(null)

onMounted(async () => {
  try {
    // ?v= — see the __DATA_VERSION__ note in vite.config.js.
    const res = await fetch(`${import.meta.env.BASE_URL}data/ratings.json?v=${__DATA_VERSION__}`)
    if (!res.ok) throw new Error(`HTTP ${res.status}`)
    data.value = await res.json()
  } catch (e) {
    error.value = e.message
  }
})

// --- UI state ---
// Rating style: 'category' — the per-discipline/level tables; 'unified' — one rating for
// everyone with an assigned category. Remembered between visits.
const mode = ref(localStorage.getItem('sr.mode') === 'unified' ? 'unified' : 'category')
watch(mode, m => localStorage.setItem('sr.mode', m))

// Pair calculator panel, opened from the header button.
const showCalc = ref(false)

// Phones drop the secondary columns; this checkbox puts them back.
const detailed = ref(false)

// Masters (M) is only ever played as doubles in this league, so "Пары" + Masters
// is the default view that actually has players in it.
const discipline = ref('doubles')   // 'singles' | 'doubles' | 'mixed' — always one selected.
const level = ref('M')              // 'A' | 'B' | 'C' | 'D' | 'E' | 'M' | null — optional refinement.
const system = ref('elo')           // elo | points
const query = ref('')
const minMatches = ref(true)
const sortKey = ref('rating')
const sortDir = ref(-1)             // 1 = ascending, -1 = descending

// The compound key ("singles_D") once a level narrows the discipline, discipline alone otherwise.
const categoryKey = computed(() => level.value ? comboKey(discipline.value, level.value) : discipline.value)

// Player stats for the selected discipline (+ level, if set).
// Optional chaining: older ratings.json snapshots may not have by_combo yet.
function categoryStats(p) {
  const stats = level.value ? p.by_combo?.[categoryKey.value] : p.by_discipline?.[discipline.value]
  return stats || { matches: 0, wins: 0, losses: 0, winrate: 0, form: [] }
}

// Rating value for the selected system (Elo/points), discipline (+ level, if set).
function ratingOf(p) {
  if (system.value === 'elo') {
    return p.elo[categoryKey.value] ?? 0
  }
  return level.value
    ? p.points_by_combo?.[categoryKey.value] ?? 0
    : p.points_by_discipline?.[discipline.value] ?? 0
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

// --- Cross-category rating ---
const rated = computed(() => data.value ? data.value.players.filter(p => p.unified) : [])
const ladder = computed(() => categoryLadder(data.value))
// Rated matches needed before a category is confirmed.
const confirmAt = computed(() => data.value?.unified_params?.provisional_matches ?? 10)
const ladderIndex = key => ladder.value.findIndex(b => b.key === key)

const unifiedRows = computed(() => {
  let list = rated.value.map(p => ({ p, u: p.unified }))

  // The threshold counts rated matches — the same ones the rating is built from.
  if (minMatches.value) list = list.filter(r => r.u.matches >= 5)

  const q = query.value.trim().toLowerCase()
  if (q) list = list.filter(r => r.p.name.toLowerCase().includes(q))

  const getters = {
    rank: r => r.u.rating,
    rating: r => r.u.rating,
    // Categories sort as a ladder, players inside one keeping the rating order.
    category: r => ladderIndex(r.u.category) * 1e6 + r.u.rating,
    progress: r => r.u.progress,
    name: r => r.p.name,
    matches: r => r.p.matches,
    wl: r => r.p.wins - r.p.losses,
    winrate: r => r.p.winrate,
    form: r => r.p.form.filter(x => x === 'W').length,
  }
  const get = getters[sortKey.value] || getters.rating
  const dir = sortDir.value
  return list.slice().sort((x, y) => {
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

// Switching the discipline or level resets sorting back to rating (descending).
function onDisciplineChange(value) {
  discipline.value = value
  sortKey.value = 'rating'
  sortDir.value = -1
}
function onLevelChange(value) {
  level.value = value
  sortKey.value = 'rating'
  sortDir.value = -1
}

// The two modes have different columns, so a sort key from one may not exist in the other.
function onModeChange(value) {
  mode.value = value
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
      :mode="mode"
      @toggle-calc="showCalc = !showCalc"
    />

    <PairCalculator
      v-if="data && showCalc"
      :players="rated"
      :ladder="ladder"
      @close="showCalc = false"
    />

    <RatingControls
      :mode="mode"
      :discipline="discipline"
      :level="level"
      :system="system"
      :query="query"
      :min-matches="minMatches"
      :detailed="detailed"
      @update:detailed="detailed = $event"
      @update:mode="onModeChange"
      @update:discipline="onDisciplineChange"
      @update:level="onLevelChange"
      @update:system="onSystemChange"
      @update:query="query = $event"
      @update:min-matches="minMatches = $event"
    />

    <template v-if="data">
      <RatingTable
        v-if="mode === 'category'"
        :rows="rows"
        :sort-key="sortKey"
        :sort-dir="sortDir"
        :discipline="discipline"
        :level="level"
        :detailed="detailed"
        @sort="onSort"
      />
      <UnifiedTable
        v-else
        :rows="unifiedRows"
        :sort-key="sortKey"
        :sort-dir="sortDir"
        :confirm-at="confirmAt"
        :detailed="detailed"
        @sort="onSort"
      />
    </template>
    <p v-else-if="error" class="empty">Не удалось загрузить данные: {{ error }}</p>
    <p v-else class="empty">Загрузка…</p>
  </div>
</template>
