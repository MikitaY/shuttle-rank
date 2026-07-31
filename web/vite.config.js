import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'

// base: './' — relative asset paths, so the site works both from the root and from a
// GitHub Pages subdirectory (username.github.io/shuttle-rank/).
// https://vite.dev/config/
export default defineConfig({
  base: './',
  plugins: [vue()],
})
