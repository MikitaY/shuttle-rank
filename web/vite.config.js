import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'

// base: './' — относительные пути, чтобы сайт одинаково работал и из корня,
// и из подкаталога GitHub Pages (username.github.io/shuttle-rank/).
// https://vite.dev/config/
export default defineConfig({
  base: './',
  plugins: [vue()],
})
