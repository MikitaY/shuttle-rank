<script setup>
import { computed } from 'vue'
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
</script>

<template>
  <header>
    <h1>🏸 Минская бадминтонная лига</h1>
    <p class="sub">
      Рейтинг игроков по открытым результатам
      <a href="https://www.tournamentsoftware.com" target="_blank" rel="noopener">tournamentsoftware.com</a>
      · обновлено {{ updatedLabel }}
    </p>
  </header>

  <div class="stats-row">
    <div v-for="s in stats" :key="s.label" class="stat">
      <b>{{ s.value }}</b><span>{{ s.label }}</span>
    </div>
  </div>
</template>
