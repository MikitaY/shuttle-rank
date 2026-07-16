<script setup>
defineProps({
  discipline: String,
  system: String,
  query: String,
  minMatches: Boolean,
})
const emit = defineEmits([
  'update:discipline', 'update:system', 'update:query', 'update:minMatches',
])

const disciplines = [
  { key: 'overall', label: 'Общий' },
  { key: 'singles', label: 'Одиночки' },
  { key: 'doubles', label: 'Пары' },
  { key: 'mixed', label: 'Микст' },
]
const systems = [
  { key: 'elo', label: 'Elo' },
  { key: 'points', label: 'Очки' },
]
</script>

<template>
  <div class="controls">
    <div class="tabs">
      <button
        v-for="d in disciplines" :key="d.key"
        :class="{ active: discipline === d.key }"
        @click="emit('update:discipline', d.key)"
      >{{ d.label }}</button>
    </div>

    <div class="tabs">
      <button
        v-for="s in systems" :key="s.key"
        :class="{ active: system === s.key }"
        @click="emit('update:system', s.key)"
      >{{ s.label }}</button>
    </div>

    <input
      type="search" placeholder="Поиск игрока…"
      :value="query"
      @input="emit('update:query', $event.target.value)"
    />

    <label class="minm">
      <input
        type="checkbox"
        :checked="minMatches"
        @change="emit('update:minMatches', $event.target.checked)"
      />
      от 5 матчей
    </label>
  </div>
</template>
