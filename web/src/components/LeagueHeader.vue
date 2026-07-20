<script setup>
import { ref, computed } from 'vue'
import { fmtDate } from '../utils/format.js'

const props = defineProps({
  tournaments: { type: Array, required: true },
  playersCount: { type: Number, required: true },
  updated: { type: String, default: null },
})

const totalMatches = computed(() =>
  props.tournaments.reduce((sum, t) => sum + t.matches, 0))

const lastDate = computed(() =>
  props.tournaments.length ? props.tournaments[props.tournaments.length - 1].date : null)

const updatedLabel = computed(() => fmtDate(props.updated || lastDate.value))

const stats = computed(() => [
  { value: props.tournaments.length, label: 'турниров' },
  { value: totalMatches.value, label: 'матчей' },
  { value: props.playersCount, label: 'игроков' },
  { value: fmtDate(lastDate.value), label: 'последний этап' },
])

// Newest first, for the tournament list popover.
const tournamentsByDate = computed(() =>
  [...props.tournaments].sort((a, b) => (b.date || '').localeCompare(a.date || '')))

const showList = ref(false)
</script>

<template>
  <header class="header-row">
    <div>
      <h1>🏸 ЛББ</h1>
      <p class="sub">
        Рейтинг игроков по открытым результатам
        <a href="https://www.tournamentsoftware.com" target="_blank" rel="noopener">tournamentsoftware.com</a>
        · обновлено {{ updatedLabel }}
      </p>
    </div>

    <button class="info-round-btn" type="button" @click="showList = !showList" aria-label="Инфо о рейтинге">
      ?
    </button>
  </header>

  <div v-if="showList" class="tournaments-panel">
    <div class="tournaments-panel-head">
      <b>Турниры, учитываемые в рейтинге</b>
      <button type="button" class="close-btn" @click="showList = false">✕</button>
    </div>

    <ul class="panel-stats">
      <li v-for="s in stats" :key="s.label"><b>{{ s.value }}</b> {{ s.label }}</li>
    </ul>

    <ul>
      <li v-for="t in tournamentsByDate" :key="t.id">
        <span class="t-date">{{ fmtDate(t.date) }}</span>
        <span class="t-name">{{ t.name }}</span>
        <span class="t-matches">{{ t.matches }} матчей</span>
      </li>
    </ul>
  </div>
</template>
