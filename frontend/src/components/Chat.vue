<template>
  <div class="chat-page">
    <header class="chat-header">
      <h1>BotChat</h1>
    </header>

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

    <form @submit.prevent="send" class="input-area">
      <input
        v-model="input"
        type="text"
        placeholder="Type your message…"
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
  </div>
</template>

<script setup lang="ts">
import { nextTick, ref } from 'vue';
import axios from 'axios';

const apiBase = import.meta.env.VITE_API_BASE || 'http://localhost:8080';

interface Message {
  id: number;
  role: 'user' | 'assistant';
  text: string;
}

const messages = ref<Message[]>([]);
const input = ref('');
const loading = ref(false);
const error = ref('');
const messagesRef = ref<HTMLElement | null>(null);
let nextId = 0;

async function send() {
  const text = input.value.trim();
  if (!text || loading.value) return;

  error.value = '';
  input.value = '';

  messages.value.push({ id: nextId++, role: 'user', text });
  await scrollToBottom();

  loading.value = true;
  try {
    // Build conversation history (everything except the message we just added)
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

.chat-page {
  display: flex;
  flex-direction: column;
  height: 100vh;
  background: $color-bg-primary;
  font-family: system-ui, sans-serif;
}

.chat-header {
  padding: $spacing-md $spacing-lg;
  background: $color-bg-secondary;
  border-bottom: 1px solid $color-border-default;
  flex-shrink: 0;

  h1 {
    font-size: $font-size-xl;
    font-weight: $font-weight-semibold;
    color: $color-brand-primary;
  }
}

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

  &.user     { justify-content: flex-end; }
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
    background: $color-bg-secondary;
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

.error-bar {
  background: $color-error-bg;
  border-top: 1px solid $color-error-border;
  color: $color-error-text;
  padding: $spacing-xs $spacing-md;
  font-size: $font-size-sm;
  flex-shrink: 0;
}

.input-area {
  display: flex;
  gap: $spacing-xs;
  padding: $spacing-md;
  background: $color-bg-secondary;
  border-top: 1px solid $color-border-default;
  flex-shrink: 0;
}

.message-input {
  flex: 1;
  padding: $spacing-sm $spacing-md;
  border: 1px solid $color-border-default;
  border-radius: 8px;
  font-size: $font-size-base;
  background: $color-bg-tertiary;
  color: $color-text-primary;
  outline: none;

  &:focus {
    border-color: $color-border-focus;
    background: $color-bg-secondary;
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

  &:hover:not(:disabled) {
    background: $color-brand-hover;
  }

  &:disabled {
    opacity: 0.5;
    cursor: not-allowed;
  }
}
</style>
