<template>
  <div class="chat-page" :class="{ 'chat-open': chatOpen }">

    <!-- ── Header ──────────────────────────────────────────────────── -->
    <header class="chat-header">
      <div class="brand">
        <img src="@/assets/icons/mr-corn.svg" alt="Mr. Corn icon" class="brand-icon" />
        <h1>Movie Night</h1>
      </div>
      <button
        class="hamburger"
        @click="toggleChat"
        :aria-expanded="chatOpen"
        aria-label="Toggle chat"
      >
        <span></span>
        <span></span>
        <span></span>
      </button>
    </header>

    <!-- ── Chat panel (shown after hamburger click) ─────────────────── -->
    <div v-if="!chatOpen">
      <div class="banner">
        <img src="@/assets/icons/mr-corn-alt.svg" alt="Mr. Corn icon" class="brand-icon"/>
          <p class="banner-summary">Click on the burger in the top right to ask questions regarding our selection of movies!</p>
          <transition name="banner-details">
            <span v-if="bannerExpanded" class="banner-details">This AI-powered MCP tool connects directly to company databases, allowing employees to ask questions about sales and business performance in plain language. Instead of writing SQL or waiting for a custom report, users can quickly explore trends, compare results, and uncover useful insights with minimal friction. It helps make data analysis faster, more accessible, and easier for nontechnical teams.</span>
          </transition>
          <button type="button" class="banner-toggle" @click="toggleBannerDetails">
            {{ bannerExpanded ? 'Less' : 'More' }}
          </button>
      </div>

      <div>
        <div v-if="moviesError" class="catalog-error">{{ moviesError }}</div>
        <div v-else class="movie-grid">
          <article
            v-for="movie in movies"
            :key="movie.id"
            class="movie-card"
            :class="{ 'is-flipped': flippedId === movie.id }"
            @click="flippedId = flippedId === movie.id ? null : movie.id"
          >
            <div class="card-inner">
              <div class="card-front">
                <img :src="movie.posterUrl" :alt="`${movie.title} poster`" class="movie-poster" />
                <p class="movie-title">{{ movie.title }}</p>
              </div>
              <div class="card-back">
                <p class="back-title">{{ movie.title }}</p>
                <p class="back-meta">{{ movie.releaseYear }} &nbsp;·&nbsp; ⭐ {{ movie.imdbRating.toFixed(1) }}</p>
                <p class="back-genre">{{ movie.genre }}</p>
                <p class="back-description">{{ movie.description }}</p>
                <a :href="movie.imdbUrl" target="_blank" rel="noopener" class="back-link" @click.stop>IMDb ↗</a>
              </div>
            </div>
          </article>
        </div>
      </div>
    </div>

    <div v-if="chatOpen" class="chat-panel">

      <!-- Input pinned at the top of the panel -->
      <form @submit.prevent="send" class="input-area">
        <input
          v-model="input"
          ref="inputRef"
          type="text"
          placeholder="Ask about a movie…"
          :disabled="loading"
          class="message-input"
          autocomplete="off"
        />
        <button
          type="submit"
          :disabled="loading || !input.trim()"
          class="send-btn"
        >
          {{ loading ? '…' : 'Send' }}
        </button>
      </form>

      <!-- Messages flow below the input -->
      <div class="messages-container" ref="messagesRef">
        <div
          v-for="msg in messages"
          :key="msg.id"
          :class="['message-row', msg.role]"
        >
          <div class="bubble">
            <pre class="bubble-text">{{ msg.text }}</pre>
          </div>
        </div>

        <div v-if="loading" class="message-row assistant">
          <div class="bubble typing">Thinking…</div>
        </div>
      </div>

      <div v-if="error" class="error-bar">{{ error }}</div>
    </div>

  </div>
</template>

<script setup lang="ts">
import { nextTick, onMounted, ref } from 'vue';
import axios from 'axios';

const apiBase = import.meta.env.VITE_API_BASE || 'http://localhost:8080';

interface Message {
  id: number;
  role: 'user' | 'assistant';
  text: string;
}

interface Movie {
  id: number;
  title: string;
  posterUrl: string;
  description: string;
  genre: string;
  releaseYear: number;
  imdbRating: number;
  imdbUrl: string;
}

const chatOpen       = ref(false);
const bannerExpanded = ref(false);
const flippedId      = ref<number | null>(null);
const messages       = ref<Message[]>([]);
const movies     = ref<Movie[]>([]);
const input      = ref('');
const loading    = ref(false);
const error      = ref('');
const moviesError = ref('');
const messagesRef = ref<HTMLElement | null>(null);
const inputRef    = ref<HTMLInputElement | null>(null);
let nextId = 0;

onMounted(() => {
  void loadMovies();
});

async function toggleChat() {
  chatOpen.value = !chatOpen.value;
  if (chatOpen.value) {
    await nextTick();
    inputRef.value?.focus();
  }
}

function toggleBannerDetails() {
  bannerExpanded.value = !bannerExpanded.value;
}

async function loadMovies() {
  moviesError.value = '';

  try {
    const res = await axios.get<Movie[]>(`${apiBase}/api/movies`);
    movies.value = res.data;
  } catch {
    moviesError.value = 'Unable to load movie posters right now.';
  }
}

async function send() {
  const text = input.value.trim();
  if (!text || loading.value) return;

  error.value = '';
  input.value = '';

  messages.value.push({ id: nextId++, role: 'user', text });
  await scrollToBottom();

  loading.value = true;
  try {
    const history = messages.value
      .slice(0, -1)
      .map((m) => ({ role: m.role, text: m.text }));

    const res = await axios.post(`${apiBase}/api/chat`, { message: text, history });
    messages.value.push({ id: nextId++, role: 'assistant', text: res.data.answer });
    await scrollToBottom();
  } catch (err: unknown) {
    const axiosErr = err as { response?: { data?: { error?: string } } };
    error.value = axiosErr.response?.data?.error ?? 'Request failed. Please try again.';
  } finally {
    loading.value = false;
  }
}

async function scrollToBottom() {
  await nextTick();
  if (messagesRef.value) {
    messagesRef.value.scrollTop = messagesRef.value.scrollHeight;
  }
}
</script>

<style lang="scss" scoped>
@import '@/styles/variables.scss';

.banner {
    // display: flex;
    // flex-direction: column;
    // align-items: center;
    gap: $spacing-sm;
    position: relative;
  
    
    margin: 4rem auto;
    padding: $spacing-lg;
    border-radius: 10px;

    background-color: $color-primary;
    color: #fff;
    font-size: 1.5rem;
    font-weight: 600;

    width: min(50rem, calc(100% - 2rem));

    img {
      position: absolute;
      top: 10px;
      right: 10px;

      rotate: 30deg;
      height: 300px;
      width: 300px;

      opacity: .2;
    }
}

.banner-summary {
  margin: 0;
  margin-bottom: 10px;
//   text-align: center;
}

.banner-details {
  display: block;
  overflow: hidden;
//   text-align: center;
  line-height: 1.6;
}

.banner-toggle {
  padding: $spacing-xs $spacing-md;
  border: none;
  border-radius: 10px;
  background: $color-secondary;
  color: #000;
  cursor: pointer;
  font-size: $font-size-sm;
  font-weight: $font-weight-semibold;
  transition: transform 0.2s ease, opacity 0.2s ease;
//   align-self: start;

  &:hover {
    transform: translateY(-1px);
    opacity: 0.9;
  }
}

.banner-details-enter-active,
.banner-details-leave-active {
  transition: max-height 0.28s ease, opacity 0.28s ease, margin 0.28s ease;
}

.banner-details-enter-from,
.banner-details-leave-to {
  max-height: 0;
  opacity: 0;
  margin-top: 0;
}

.banner-details-enter-to,
.banner-details-leave-from {
  max-height: 16rem;
  opacity: 1;
  margin-top: $spacing-xs;
}

.movie-grid {
  display: grid;
  grid-template-columns: repeat(4, minmax(0, 1fr));
  gap: $spacing-lg;
  width: min(70rem, calc(100% - 2rem));
  margin: 0 auto 3rem;
}

// ── Card flip ─────────────────────────────────────────────────────────

.movie-card {
  position: relative;
  aspect-ratio: 2 / 3;
  cursor: pointer;
  perspective: 900px;

  &.is-flipped .card-inner {
    transform: rotateY(180deg);
  }
}

.card-inner {
  position: absolute;
  inset: 0;
  transform-style: preserve-3d;
  transition: transform 0.55s cubic-bezier(0.4, 0.2, 0.2, 1);
  border-radius: 10px;
}

.card-front,
.card-back {
  position: absolute;
  inset: 0;
  border-radius: 10px;
  backface-visibility: hidden;
  -webkit-backface-visibility: hidden;
  overflow: hidden;
}

.card-front {
  display: flex;
  flex-direction: column;
}

.card-back {
  transform: rotateY(180deg);
  background: $color-primary;
  color: #fff;
  display: flex;
  flex-direction: column;
  padding: $spacing-md;
  gap: $spacing-xs;
  box-shadow: 0 12px 28px rgba(0, 0, 0, 0.24);
}

.back-title {
  margin: 0;
  font-size: 1.5rem;
  font-weight: $font-weight-semibold;
  line-height: 1.25;
}

.back-meta {
  margin: 0;
  font-size: $font-size-sm;
  opacity: 0.8;
}

.back-genre {
  margin: 0;
  font-size: $font-size-sm;
  font-style: italic;
  opacity: 0.75;
}

.back-description {
  margin: 0;
  font-size: $font-size-sm;
  line-height: 1.5;
  flex: 1;
  overflow: hidden;
  display: -webkit-box;
  -webkit-line-clamp: 6;
  line-clamp: 6;
  -webkit-box-orient: vertical;
}

.back-link {
  display: inline-block;
  margin-top: auto;
  padding: $spacing-xs $spacing-sm;
  border-radius: 6px;
  background: rgba(255, 255, 255, 0.18);
  color: #fff;
  font-size: $font-size-sm;
  font-weight: $font-weight-semibold;
  text-decoration: none;
  align-self: flex-start;
  transition: background 0.15s ease;

  &:hover { background: rgba(255, 255, 255, 0.32); }
}

.movie-poster {
  width: 100%;
  flex: 1;
  min-height: 0;
  object-fit: cover;
  border-radius: 10px 10px 0 0;
  box-shadow: 0 12px 28px rgba(0, 0, 0, 0.16);
  background: $color-secondary;
}

.movie-title {
  margin: 0;
  padding: 0.75rem 0;
  border-radius: 0 0 10px 10px;
  font-size: 1rem;
  font-weight: 600;
  color: #fff;
  background-color: $color-primary;
  text-align: center;
  flex-shrink: 0;
}

.catalog-error {
  width: min(70rem, calc(100% - 2rem));
  margin: 0 auto 3rem;
  padding: $spacing-md;
  border-radius: 10px;
  background: $color-error-bg;
  color: $color-error-text;
  border: 1px solid $color-error-border;
}

@media (max-width: 960px) {
  .movie-grid {
    grid-template-columns: repeat(3, minmax(0, 1fr));
  }
}

@media (max-width: 720px) {
  .movie-grid {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }
}

@media (max-width: 480px) {
  .movie-grid {
    grid-template-columns: 1fr;
  }
}


// ── Page shell ────────────────────────────────────────────────────────

.chat-page {
  display: flex;
  flex-direction: column;
  min-height: 100vh;
  background: #fff;
  font-family: system-ui, sans-serif;
  overflow-y: auto;
}

.chat-page.chat-open {
  height: 100vh;
  overflow: hidden;
}

// ── Header ────────────────────────────────────────────────────────────

.chat-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: $spacing-md $spacing-lg;
  background: $color-primary;
  border-bottom: 1px solid $color-tertiary;
  flex-shrink: 0;
}

.brand {
  display: flex;
  align-items: center;
  gap: $spacing-sm;

  h1 {
    font-size: $font-size-xl;
    font-weight: $font-weight-semibold;
    color: #fff;
    margin: 0;
  }
}

.brand-icon {
  width: 36px;
  height: 36px;
  object-fit: contain;
  display: block;
}

// ── Hamburger ─────────────────────────────────────────────────────────

.hamburger {
  display: flex;
  flex-direction: column;
  justify-content: space-between;
  width: 22px;
  height: 16px;
  background: none;
  border: none;
  padding: 0;
  cursor: pointer;

  span {
    display: block;
    width: 100%;
    height: 2px;
    background: $color-tertiary;
    border-radius: 1px;
    transition: transform 0.2s, opacity 0.2s;
  }

  // Animate to an X when open
  &[aria-expanded='true'] {
    span:nth-child(1) { transform: translateY(7px) rotate(45deg); }
    span:nth-child(2) { opacity: 0; }
    span:nth-child(3) { transform: translateY(-7px) rotate(-45deg); }
  }

  &:hover span { background: $color-brand-hover; }
}

// ── Chat panel ────────────────────────────────────────────────────────

.chat-panel {
  display: flex;
  flex-direction: column;
  flex: 1;
  overflow: hidden;
}

// ── Input (pinned at top of panel) ────────────────────────────────────

.input-area {
  display: flex;
  gap: $spacing-xs;
  padding: $spacing-md;
  background: $color-secondary;
  border-bottom: 1px solid $color-border-default;
  flex-shrink: 0;
}

.message-input {
  flex: 1;
  padding: $spacing-sm $spacing-md;
  border: 1px solid $color-border-default;
  border-radius: 8px;
  font-size: $font-size-base;
  background: $color-tertiary;
  color: $color-text-primary;
  outline: none;

  &:focus {
    border-color: $color-border-focus;
    background: $color-primary;
  }

  &:disabled {
    opacity: 0.6;
    cursor: not-allowed;
  }
}

.send-btn {
  padding: $spacing-sm $spacing-lg;
  background: $color-brand-primary;
  color: #fff;
  border: none;
  border-radius: 8px;
  font-size: $font-size-base;
  font-weight: $font-weight-medium;
  cursor: pointer;
  transition: background 0.2s;

  &:hover:not(:disabled) { background: $color-brand-hover; }
  &:disabled { opacity: 0.5; cursor: not-allowed; }
}

// ── Messages (scroll area below input) ───────────────────────────────

.messages-container {
  flex: 1;
  overflow-y: auto;
  padding: $spacing-md;
  display: flex;
  flex-direction: column;
  gap: $spacing-sm;
}

.message-row {
  display: flex;

  &.user      { justify-content: flex-end; }
  &.assistant { justify-content: flex-start; }
}

.bubble {
  max-width: 70%;
  padding: $spacing-sm $spacing-md;
  border-radius: 12px;
  font-size: $font-size-base;
  line-height: 1.5;

  .user & {
    background: $color-brand-primary;
    color: #fff;
    border-bottom-right-radius: 4px;
  }

  .assistant & {
    background: $color-secondary;
    color: $color-text-primary;
    border: 1px solid $color-border-default;
    border-bottom-left-radius: 4px;
  }
}

.bubble-text {
  white-space: pre-wrap;
  word-break: break-word;
  margin: 0;
  font-family: inherit;
  font-size: inherit;
}

.typing {
  color: $color-text-muted;
  font-style: italic;
}

// ── Error bar ─────────────────────────────────────────────────────────

.error-bar {
  background: $color-error-bg;
  border-top: 1px solid $color-error-border;
  color: $color-error-text;
  padding: $spacing-xs $spacing-md;
  font-size: $font-size-sm;
  flex-shrink: 0;
}
</style>
