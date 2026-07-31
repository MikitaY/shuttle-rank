<script setup>
import { ref, computed } from 'vue'
import { surnameFirst, pairRating, categoryOf, winProbability, plural } from '../utils/format.js'

const props = defineProps({
  players: { type: Array, required: true },   // only players with a unified rating
  ladder: { type: Array, required: true },
})

const open = ref(false)

// Datalist entries are display names ("Янковский Никита"), so keep a way back to the player.
const byDisplay = computed(() => {
  const map = new Map()
  for (const p of props.players) if (!map.has(surnameFirst(p.name))) map.set(surnameFirst(p.name), p)
  return map
})
const byName = computed(() => new Map(props.players.map(p => [p.name, p])))
const options = computed(() => [...byDisplay.value.keys()].sort((a, b) => a.localeCompare(b, 'ru')))

const picked = ref(['', '', '', ''])         // [pair1 a, pair1 b, pair2 a, pair2 b]
const playerAt = i => byDisplay.value.get(picked.value[i].trim()) || null

function pairOf(i, j) {
  const a = playerAt(i), b = playerAt(j)
  if (!a || !b || a.name === b.name) return null
  const rating = pairRating(a, b)
  return {
    a, b, rating,
    category: categoryOf(rating, props.ladder),
    provisional: a.unified.provisional || b.unified.provisional,
  }
}

const pair1 = computed(() => pairOf(0, 1))
const pair2 = computed(() => pairOf(2, 3))

// How the pair has actually done together, and how it was expected to do: every joint match
// is weighed against the opponents' pair rating, so the surplus is real synergy and not
// just an easy draw.
const together = computed(() => {
  const p = pair1.value
  if (!p) return null

  let matches = 0, wins = 0, expected = 0, rated = 0
  for (const m of p.a.match_log) {
    if (!m.teammates.includes(p.b.name)) continue
    matches++
    if (m.won) wins++

    const opponents = m.opponents.map(n => byName.value.get(n)).filter(o => o?.unified)
    if (opponents.length === m.opponents.length && opponents.length) {
      const oppRating = opponents.reduce((s, o) => s + o.unified.rating, 0) / opponents.length
      expected += winProbability(p.rating, oppRating)
      rated++
    }
  }
  if (!matches) return { matches: 0 }
  return {
    matches, wins, losses: matches - wins,
    winrate: wins / matches,
    expected: rated ? expected : null,
    rated,
  }
})

const chance = computed(() =>
  pair1.value && pair2.value ? winProbability(pair1.value.rating, pair2.value.rating) : null)

const pct = x => Math.round(x * 100)
const labels = ['Игрок 1', 'Игрок 2', 'Игрок 1', 'Игрок 2']

function clear() {
  picked.value = ['', '', '', '']
}
</script>

<template>
  <section class="calc">
    <button type="button" class="calc-head" @click="open = !open">
      <span>🧮 Калькулятор рейтинга пары</span>
      <span class="calc-toggle">{{ open ? '−' : '+' }}</span>
    </button>

    <div v-if="open" class="calc-body">
      <datalist id="calc-players">
        <option v-for="name in options" :key="name" :value="name" />
      </datalist>

      <div class="calc-pairs">
        <div v-for="pi in [0, 1]" :key="pi" class="calc-panel">
          <h4>{{ pi === 0 ? 'Пара 1' : 'Пара 2 — для прогноза матча' }}</h4>
          <input
            v-for="k in [0, 1]" :key="k"
            v-model="picked[pi * 2 + k]"
            list="calc-players" type="search" :placeholder="labels[pi * 2 + k]"
          />
          <p v-if="pi === 0 && pair1" class="calc-out">
            <b>{{ Math.round(pair1.rating) }}</b> · категория {{ pair1.category }}<template
              v-if="pair1.provisional">&thinsp;?</template>
            <span class="calc-sub">
              {{ Math.round(pair1.a.unified.rating) }} + {{ Math.round(pair1.b.unified.rating) }}
            </span>
          </p>
          <p v-else-if="pi === 1 && pair2" class="calc-out">
            <b>{{ Math.round(pair2.rating) }}</b> · категория {{ pair2.category }}<template
              v-if="pair2.provisional">&thinsp;?</template>
            <span class="calc-sub">
              {{ Math.round(pair2.a.unified.rating) }} + {{ Math.round(pair2.b.unified.rating) }}
            </span>
          </p>
        </div>
      </div>

      <ul v-if="pair1" class="calc-stats">
        <li v-if="together.matches">
          <b>Вместе:</b> {{ together.matches }}
          {{ plural(together.matches, ['матч', 'матча', 'матчей']) }},
          {{ together.wins }}–{{ together.losses }}, {{ pct(together.winrate) }}%
        </li>
        <li v-else><b>Вместе:</b> ещё не играли</li>
        <li v-if="together.expected != null">
          <b>Синергия:</b> {{ together.wins }} побед против {{ together.expected.toFixed(1) }} ожидаемых
          <span :class="together.wins - together.expected >= 0 ? 'res-W' : 'res-L'">
            ({{ together.wins - together.expected >= 0 ? '+' : '−'
            }}{{ Math.abs(together.wins - together.expected).toFixed(1) }})
          </span>
        </li>
        <li v-if="chance != null">
          <b>Прогноз:</b> пара 1 побеждает с вероятностью {{ pct(chance) }}%
          (пара 2 — {{ pct(1 - chance) }}%)
        </li>
      </ul>

      <p class="calc-note">
        Рейтинг пары — среднее рейтингов игроков: именно от него движок считает ожидаемый
        результат парного матча. Своей категории у пары нет — категории присваиваются только
        игрокам. «?» — в паре есть игрок с предварительным рейтингом.
        <button type="button" class="calc-clear" @click="clear">Очистить</button>
      </p>
    </div>
  </section>
</template>
